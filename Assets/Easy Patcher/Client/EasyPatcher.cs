/// <summary>
//	1. Add "EasyPatcher" Object in your scene what you want.  
//	2. Add this code as below.
//		>> EasyPatcher patcher = GameObject.Find ("EasyPatcherPrefab").GetComponent<EasyPatcher> ();
//		>> EasyPatcher.SetRepogitory("your repository address"); //  ex) http://localhost/
//		>> yield return patcher.StartCoroutine (patcher.startPatch ());
//	3. Check patch processing and result with the member variant " PatchProgress" and "PatchMessage"
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
#if UNITY_EDITOR	
using UnityEditor;
#endif

public class EasyPatcher : MonoBehaviour
{
	static public EasyPatcher instance;
	static string url = "";
	static int process = 0;				//	You can check the processing percentage value after staring downloading.
	static string message = "Done.";	//	You can check current situation in patch progressing.
	static string log = "";
	static int patchErrorCount = 0;


	public static int PatchErrorCount
	{
		get { return patchErrorCount; }
		set { patchErrorCount = value; }
	}

	public static string PatchURL
	{
		get { return url; }
		set { url = value; }
	}

	public static string PatchLastLog
	{
		get { return log; }
		set { log = value; }
	}

	public static string PatchMessage
	{
		get { return message; }
		set { message = value; }
	}
	
	public static int PatchProgress
	{
		get { return process; }
		set { process = value; }
	}

	void Awake(){
		instance = this;
	}

	public static void SetRepogitory(string _url){
		PatchURL = _url;
		#if UNITY_EDITOR
			PatchURL = PatchURL + BaseLoader.GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget) + "/";
		#else
			PatchURL = PatchURL + BaseLoader.GetPlatformFolderForAssetBundles(Application.platform) + "/";
		#endif
		AssetBundleManager.BaseDownloadingURL = PatchURL + CommonPatcherData.lastVersionRepo+ "/";
	}
	public static bool isDone(){
		if (PatchProgress >= 100)
			return true;

		return false;
	}

	public IEnumerator startPatch(){
		//	loading local patch info
		PatchMessage = "Checking new version...";
		process = 0;
		string localpath = Application.persistentDataPath + "/"+ CommonPatcherData.patchVersionFN;
		XmlDocument localVerDoc = XmlTool.loadXml (localpath);
		if (localVerDoc != null) {
			PatchVersion.setVersion ( localVerDoc.SelectSingleNode ("/VERSIONS/PATCH").Attributes ["LastVersion"].Value );
		}

		//	loading latest patch info
		string infofile = PatchURL + "/" + CommonPatcherData.patchVersionFN;
		WWW patchInfo = new WWW (infofile);

		yield return patchInfo;

		if (patchInfo.error == null) {
			XmlDocument xmlDoc = XmlTool.loadXml( patchInfo.bytes );
			string newVersion_Folder = xmlDoc.SelectSingleNode ("/VERSIONS/PATCH").Attributes ["LastVersion"].Value;
			if( !PatchVersion.isEqualToLocalVersion(newVersion_Folder) ){
				//	proc update
				PatchMessage = "Start updating...";
				//	get list to update

				yield return instance.StartCoroutine( instance.getPatchInfo( xmlDoc ) );

				//	startcoroutine for all list
			}
			if(patchErrorCount > 0 ){
				PatchProgress = 100;
			}else{
				PatchMessage = "Finish !";
				PatchProgress = 100;
				File.WriteAllBytes( localpath, patchInfo.bytes);
			}
		}else{
			Debug.LogError ("Patch Error : " + patchInfo.error);
			PatchMessage = "Error..." + infofile;
			PatchProgress = 100;
			PatchErrorCount = 1;
		}
	}

	IEnumerator getPatchInfo( XmlDocument _verDoc){
		//	get all list for patching
		List<string> versions = PatchVersion.getPatchList (_verDoc, PatchVersion.major, PatchVersion.minor);

		//	get all assetbundle's version when you got them last time.
		Dictionary<string, int> verList = PatchVersion.getAssetBundleVerList ();

		List<string> patchList = new List<string> ();
		Dictionary<string, string> patchListPath = new Dictionary<string, string> ();	//	file name, fullpath
		//	sort patch files list
		foreach( string verStr in versions){
			string [] ver = verStr.Split ('_');
			string listPath = url + ver[0] + "_" + ver[1] + "/" + ver[2] + "/" + CommonPatcherData.assetbundleFN;

			WWW patchListWWW = new WWW (listPath);
			yield return patchListWWW;

			XmlDocument xmlDoc = XmlTool.loadXml( patchListWWW.bytes );

			if (xmlDoc != null) {

				{	//	create files
					XmlNode _nodeCreate = xmlDoc.SelectSingleNode("/AssetBundles/CREATE");
					XmlNode _nodeC_Files = _nodeCreate.FirstChild;
					while(_nodeC_Files != null){
						string name = _nodeC_Files.Attributes["name"].Value;

						if( patchList.FindIndex(delegate( string r){ return r == name;}) == -1){
							patchList.Add( name );
						}
						patchListPath[name] = url + ver[0] + "_" + ver[1] + "/" + ver[2] + "/" + name;

						_nodeC_Files = _nodeC_Files.NextSibling;
					}
				}

				{	//	modify files
					XmlNode _nodeModify = xmlDoc.SelectSingleNode("/AssetBundles/MODIFY");
					XmlNode _nodeM_Files = _nodeModify.FirstChild;
					while(_nodeM_Files != null){
						string name = _nodeM_Files.Attributes["name"].Value;
						if( patchList.FindIndex(delegate( string r){ return r == name;}) == -1){
							patchList.Add( name );
						}
						patchListPath[name] = url + ver[0] + "_" + ver[1] + "/" + ver[2] + "/" + name;

						_nodeM_Files = _nodeM_Files.NextSibling;
					}
				}
			}
		}
		//	start downloading dictionary
		int count = 0;
		foreach(string name in patchList){
			PatchMessage = "Downloading.. " + name + "("+ count + "/" + patchList.Count + ")";
			int newversion = 0;
			if(verList.ContainsKey(name))
				newversion = verList[name] + 1;
			else 
				verList.Add( name, 0);
			
			yield return StartCoroutine( Downloading( patchListPath[name],  newversion));
			
			verList[name] += 1;
			count++;
			PatchProgress = (count*100)/patchList.Count;
		}

		PatchVersion.setAssetBundleVerList( verList );
	}

	IEnumerator Downloading(string _path, int _version){
		using (WWW www = WWW.LoadFromCacheOrDownload (_path, _version)) {
			yield return www;

			if (www.error != null) {
				patchErrorCount++;
				Debug.LogError ("WWW Error : " + www.error);
				PatchMessage = www.error;
				yield break;
			}
		}
	}
}