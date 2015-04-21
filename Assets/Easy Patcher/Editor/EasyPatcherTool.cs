using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;


public class EasyPatcherTool : EditorWindow {

	Vector2 unregistedAssetListScrollPos=new Vector2(0,0);
	Vector2 registedAssetListScrollPos=new Vector2(0,0);

	List<UnityEngine.Object> listUnregInAB = new List<UnityEngine.Object>();
	List<UnityEngine.Object> listRegInAB = new List<UnityEngine.Object>();
	List<string> listABName = new List<string>();
	List<string> listPlatform = new List<string>();

	bool ctrlPressed=false;
	string selectedABName = "all";
	int selectedABIndex = 0;
	int selectedPlatform = 0;
	int lastMajorVersion = 0;
	string chkLastMajorVersion = "0";
	int lastMinorVersion = 0;
	string chkLastMinorVersion = "1";
	XmlDocument cnfDoc = null;
	XmlDocument verDoc = null;

	enum ABMWType 
	{
		Build, Unregisted, PatchInfo, Upload
	};
	ABMWType  ActiveABMWType=ABMWType.Build;

	[MenuItem("Easy Patcher/Manager")]
	public static void ShowWindow(){
		EasyPatcherTool window = (EasyPatcherTool)EditorWindow.GetWindow(typeof(EasyPatcherTool),false, "Easy Patcher", false);

		window.listPlatform.Add (BuildTarget.Android.ToString());
		window.listPlatform.Add (BuildTarget.iOS.ToString ());
		window.listPlatform.Add (BuildTarget.StandaloneWindows.ToString());

		window.LoadConfigXML (CommonPatcherData.cnfFN);
		window.LoadVersionXML ();
	}

	void OnGUI(){
		GUIStyle styleCmdArea = new GUIStyle();
		styleCmdArea.normal.background = MakeTex(600, 80, Color.white);

		//	info area 
		GUILayout.BeginArea (new Rect (10, 10, 600, 80), styleCmdArea);

		GUILayout.BeginHorizontal();
		GUILayout.Label ("Platform:", GUILayout.Width(200));
		selectedPlatform = EditorGUILayout.Popup( selectedPlatform, listPlatform.ToArray() );
		switch (selectedPlatform) {
		case 0:
			if(EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android){
				EditorUserBuildSettings.SwitchActiveBuildTarget( BuildTarget.Android );
				LoadConfigXML (CommonPatcherData.cnfFN);
				LoadVersionXML ();
			}else
				GUILayout.EndHorizontal();
			break;
		case 1: 
			if(EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows){
				EditorUserBuildSettings.SwitchActiveBuildTarget( BuildTarget.StandaloneWindows );
				LoadConfigXML (CommonPatcherData.cnfFN);
				LoadVersionXML ();
			}else
				GUILayout.EndHorizontal();
			break;
		}

		GUILayout.BeginHorizontal();
		GUILayout.Label ("Last Version : "+ lastMajorVersion + "."+lastMinorVersion);
		GUILayout.Label (">>>");
		GUILayout.Label ("New Version :");
		chkLastMajorVersion = GUILayout.TextField(""+chkLastMajorVersion);
		chkLastMinorVersion = GUILayout.TextField(""+chkLastMinorVersion);
		if (GUILayout.Button ("Apply", GUILayout.Width (70))) {
			//	apply last version info and make folders and modify xml files.
			if(EditorUtility.DisplayDialog("You know that ?!","This work just makes a folder for new version and change the text of last version. Later, you can make new resources for next patch when you press the button [Upload to repository].","I see!!") == true){
				SaveVersionXML();
			}
		}
		if (GUILayout.Button ("Rollback", GUILayout.Width (70))) {

			string prevVersion = PatchVersion.getPreviousVersion( CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/" + CommonPatcherData.patchVersionFN );
			int prevMajor = Convert.ToInt32(prevVersion.Split('_')[1]);
			int prevMinor = Convert.ToInt32(prevVersion.Split('_')[2]);

			string curVersion = verDoc.SelectSingleNode("/VERSIONS/PATCH").Attributes["LastVersion"].Value;
			int curMajor = Convert.ToInt32(curVersion.Split('_')[1]);
			int curMinor = Convert.ToInt32(curVersion.Split('_')[2]);

			if(EditorUtility.DisplayDialog("Caution!!","Your last version(VER "+ curMajor.ToString ("D2") + "." + curMinor.ToString ("D3") +") data will remove complete. Are you sure?","YES","NO") == true)
			{
				//	check last version
				Debug.Log ( "Rollback to previous Version >> "+  prevVersion);

				//	modify patch.xml file 
				verDoc.SelectSingleNode("/VERSIONS/PATCH").Attributes["LastVersion"].Value = prevVersion;
				PatchVersion.removeVersionNode( verDoc, curMajor, curMinor);
				XmlTool.writeXml(CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/"+CommonPatcherData.patchVersionFN , verDoc);

				//	remove assets.xml and files, and backup folder
				string _dn = CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/VER_" + curMajor.ToString ("D2") + "/" + curMinor.ToString ("D3");
				Directory.Delete(_dn,true);

				//	latest folder change
				Directory.Delete(CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/"+CommonPatcherData.lastVersionRepo, true);
				Directory.Move ( CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/" + CommonPatcherData.lastVersionRepo + "_VER_" + curMajor.ToString ("D2") + "_" + curMinor.ToString ("D3"),
				                CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/"+CommonPatcherData.lastVersionRepo);

				lastMajorVersion = prevMajor;
				chkLastMajorVersion = curMajor.ToString ("D2");
				lastMinorVersion = prevMinor;
				chkLastMinorVersion = curMinor.ToString ("D3");
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label ("Path :");
		CommonPatcherData.repoPath = GUILayout.TextField(CommonPatcherData.repoPath);
		//	read config file
		if (GUILayout.Button ("Read", GUILayout.Width (100))) {
			LoadConfigXML ( CommonPatcherData.cnfFN );
		}

		if (GUILayout.Button ("Save", GUILayout.Width (100))) {
			cnfDoc.SelectSingleNode ("/ToolConfig/Repository").Attributes ["path"].Value = CommonPatcherData.repoPath;
			SaveConfigXML(CommonPatcherData.cnfFN , cnfDoc);
		}

		GUILayout.EndHorizontal();
		GUILayout.EndArea();

		//	command area
		GUILayout.BeginArea (new Rect (10, 100, 600, 140));
		GUILayout.BeginHorizontal();

		if (GUILayout.Button ("Build AssetBundles", GUILayout.Width (150))) {
			ActiveABMWType=ABMWType.Build;
			BuildScript.BuildAssetBundles();
		}

		if (GUILayout.Button ("unregisted assets", GUILayout.Width (150))) {
			ActiveABMWType=ABMWType.Unregisted;
			checkUnregistedAssets();
		}

		if (GUILayout.Button ("All AssetBundles List", GUILayout.Width (150))) {
			ActiveABMWType=ABMWType.PatchInfo;
			checkRegistedAssets();
		}

		if (GUILayout.Button ("Upload to repository", GUILayout.Width (150))) {

			if(EditorUtility.DisplayDialog("Upload !!","Did you make a folder for new version?! If not, press the button [apply]. This will make a folder and change the version number for new version.","I DID!!", "Ooops!") == true){

				ActiveABMWType=ABMWType.Upload;
				BuildScript.BuildAssetBundles();

				//	compare all AssetBundles with "repoPath + lastVersionRepo"'s all files
				List<FileInfo> listNew = new List<FileInfo>();
				List<FileInfo> listModify = new List<FileInfo>();
				List<FileInfo> listRemoved = new List<FileInfo>();

				{
					DirectoryInfo latestDir = new DirectoryInfo(CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget);
					FileInfo [] latestABFiles = latestDir.GetFiles("*.*", SearchOption.AllDirectories);

					DirectoryInfo buildDir = new DirectoryInfo( BuildScript.GetAssetBundleBuildPath() + "/"+EditorUserBuildSettings.activeBuildTarget);
					FileInfo [] newABFiles = buildDir.GetFiles("*.*", SearchOption.AllDirectories);

					int newIndex = 0;
					foreach(FileInfo fi in newABFiles)
					{
						int latestIndex = 0;
						foreach(FileInfo latefi in latestABFiles)
						{
							int ret = compareFile(fi, latefi);
							if( ret == 0){ //	completely different
							}else if( ret == 1){//	same exactly
								break;
							}else if( ret == 2){ //	modified
								listModify.Add(fi);
								break;
							}
							latestIndex++;
						}

						if(latestIndex == latestABFiles.Length)
						{
							listNew.Add(fi);
						}
						newIndex++;
					}

					foreach(FileInfo latefiR in latestABFiles){
						int chkIndex = 0;
						foreach(FileInfo fiR in newABFiles){
							if(fiR.Name == latefiR.Name)
							{
								break;
							}
							chkIndex++;
						}
						if(chkIndex == latestABFiles.Length)
							listRemoved.Add(latefiR);
					}
				}

				//	upload updated AssetBundles to the new repository.
				SaveAssetsXML(listNew, listModify, listRemoved);
			}
		}

		GUILayout.EndHorizontal ();
		GUILayout.EndArea ();

		//	console area
		GUILayout.BeginArea (new Rect (10, 150, 600, 600));
		switch (ActiveABMWType)
		{
		case ABMWType.Build:

			break;
		case ABMWType.Unregisted:
			ListUnregistedAssets ();
			break;
		case ABMWType.PatchInfo:
			ListRegistedAssets ();
			break;	
		case ABMWType.Upload:
			
			break;	
		}
		GUILayout.EndArea ();
	}

	void Update(){
	}

	private Texture2D MakeTex(int width, int height, Color col)
	{
		Color[] pix = new Color[width*height];
		
		for (int i = 0; i < pix.Length; i++) {
			if(col == Color.white){
				pix [i].a = 1.0f;
				pix [i].r = i* 0.00002f;
				pix [i].g = i* 0.00004f;
				pix [i].b = i* 0.00006f;
			}else
				pix [i] = col;
		}
		
		Texture2D result = new Texture2D(width, height);
		result.SetPixels(pix);
		result.Apply();
		
		return result;
	}

	void checkRegistedAssets(){
		listRegInAB.Clear();
		listABName.Clear ();
		listABName.Add ("all");

		UnityEngine.Object [] _list = Selection.GetFiltered (typeof(UnityEngine.Object), SelectionMode.DeepAssets);
		foreach (UnityEngine.Object obj in _list) {
			if(obj.GetType().ToString() != "UnityEditor.MonoScript")
			{
				AssetImporter ai = AssetImporter.GetAtPath( AssetDatabase.GetAssetOrScenePath(obj) );
				
				if(ai.assetBundleName != "")
				{
					listRegInAB.Add( obj );
					if( !listABName.Contains( ai.assetBundleName ) )
						listABName.Add( ai.assetBundleName );
				}
			}
		}
	}

	void checkUnregistedAssets(){
		listUnregInAB.Clear();

		UnityEngine.Object [] _list = Selection.GetFiltered (typeof(UnityEngine.Object), SelectionMode.DeepAssets );
		foreach (UnityEngine.Object obj in _list) {
			if(obj.GetType().ToString() != "UnityEditor.MonoScript")
			{
				AssetImporter ai = AssetImporter.GetAtPath( AssetDatabase.GetAssetOrScenePath(obj) );

				if(ai.assetBundleName == "" && obj.GetType ().ToString() != "UnityEditor.DefaultAsset")
				{
					listUnregInAB.Add( obj );
				}
			}
		}
	}

	void SelectObject(UnityEngine.Object selectedObject,bool append)
	{
		if (append)
		{
			List<UnityEngine.Object> currentSelection=new List<UnityEngine.Object>(Selection.objects);
			// Allow toggle selection
			if (currentSelection.Contains(selectedObject)) currentSelection.Remove(selectedObject);
			else currentSelection.Add(selectedObject);
			
			Selection.objects=currentSelection.ToArray();
		}
		else Selection.activeObject=selectedObject;
	}

	void ListUnregistedAssets(){

		ctrlPressed=Event.current.control || Event.current.command;
		GUILayout.Label ("All Count : " + listUnregInAB.Count(), GUILayout.Width(200));

		unregistedAssetListScrollPos = EditorGUILayout.BeginScrollView(unregistedAssetListScrollPos);

		foreach(UnityEngine.Object o in listUnregInAB){

			{
				GUILayout.BeginHorizontal ();
				string sizeLabel= "NAME : " + o.name;
				sizeLabel += "   ,  TYPE : " + o.GetType().ToString();
				GUILayout.Label (sizeLabel,GUILayout.Width(500));
				GUILayout.EndHorizontal();
			}

			{
				GUILayout.BeginHorizontal ();
				string sizeLabel= "PATH : " + AssetDatabase.GetAssetOrScenePath(o);
				GUILayout.Label (sizeLabel,GUILayout.Width(500));

				if(GUILayout.Button("GO",GUILayout.Width(50) ) ){
					SelectObject(o, ctrlPressed);
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.BeginHorizontal ();
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
	}

	void ListRegistedAssets(){
		
		ctrlPressed=Event.current.control || Event.current.command;
		GUILayout.Label ("All Count : " + listRegInAB.Count(), GUILayout.Width(200));

		if (!listABName.Contains (selectedABName)) {
			selectedABName = "all";
			selectedABIndex = 0;
		}

		selectedABIndex = EditorGUILayout.Popup( selectedABIndex, listABName.ToArray() );

		registedAssetListScrollPos = EditorGUILayout.BeginScrollView(registedAssetListScrollPos);
		foreach(UnityEngine.Object o in listRegInAB){

			AssetImporter ai = AssetImporter.GetAtPath( AssetDatabase.GetAssetOrScenePath(o) );
			if( listABName[selectedABIndex] != "all" && ai.assetBundleName != listABName[selectedABIndex])
				continue;

			{
				GUILayout.BeginHorizontal ();
				string sizeLabel= "NAME : " + o.name;
				sizeLabel += "   ,  TYPE : " + o.GetType().ToString();
				GUILayout.Label (sizeLabel,GUILayout.Width(500));
				GUILayout.EndHorizontal();
			}
			
			{
				GUILayout.BeginHorizontal ();
				string sizeLabel= "PATH : " + AssetDatabase.GetAssetOrScenePath(o);
				GUILayout.Label (sizeLabel,GUILayout.Width(500));
				
				if(GUILayout.Button("GO",GUILayout.Width(50) ) ){
					SelectObject(o, ctrlPressed);
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.BeginHorizontal ();
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
	}

	///////////////////////////////////////////////////////
	//	config xml file
	void LoadConfigXML(string _fn){
		try{
			cnfDoc = XmlTool.loadXml (_fn);
			if(cnfDoc == null)
				throw new Exception("not found the file");
		}catch(Exception e){
			Debug.LogError ( e.Message );
			cnfDoc = new XmlDocument();
			XmlNode root = cnfDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
			cnfDoc.AppendChild( root );
			
			XmlNode node = cnfDoc.CreateElement("ToolConfig");
			
			XmlNode nodeChild = cnfDoc.CreateElement("Repository");
			XmlAttribute attr = cnfDoc.CreateAttribute("address");
			attr.Value = Application.absoluteURL;
			XmlAttribute attrP = cnfDoc.CreateAttribute("path");
			attrP.Value = "";
			nodeChild.Attributes.Append( attr);
			nodeChild.Attributes.Append( attrP);

			node.AppendChild( nodeChild);
			cnfDoc.AppendChild( node );
			
			SaveConfigXML (_fn, cnfDoc);
		}finally{
			CommonPatcherData.repoPath = cnfDoc.SelectSingleNode ("/ToolConfig/Repository").Attributes ["path"].Value;
		}
	}

	void SaveConfigXML(string _fn, XmlDocument _doc){
		XmlTool.writeXml (_fn, _doc);
	}
	//	config xml file
	///////////////////////////////////////////////////////

	///////////////////////////////////////////////////////
	//	version xml file
	void LoadVersionXML(){
		string url = CommonPatcherData.repoPath + "/"+ EditorUserBuildSettings.activeBuildTarget + "/"+ CommonPatcherData.patchVersionFN;

		verDoc = XmlTool.loadXml (url);

		if(verDoc == null){
			Debug.LogError ( "Failed to read [" + CommonPatcherData.patchVersionFN + "] in repository. So read it in local repository.");
			verDoc = XmlTool.loadXml (CommonPatcherData.repoPath + "/"+ EditorUserBuildSettings.activeBuildTarget+ "/" + CommonPatcherData.patchVersionFN);
		}

		if(verDoc == null)
		{
			//	make local repo
			{
				MakeLocalRepo();	//	
			}

			verDoc = new XmlDocument();
			XmlNode root = verDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
			verDoc.AppendChild( root );
			
			XmlNode node = verDoc.CreateElement("VERSIONS");
			XmlNode nodeChild = verDoc.CreateElement("PATCH");
			//	Add attribute info for Last Version
			{
				XmlAttribute attr = verDoc.CreateAttribute("LastVersion");
				attr.Value = "VER_00_000";
				nodeChild.Attributes.Append( attr);
				node.AppendChild( nodeChild);
			}

			//	add major and minor's version info
			{
				XmlNode nodeMajor = verDoc.CreateElement("MAJOR");
				//	Add attribute info for Major version
				{
					XmlAttribute attr = verDoc.CreateAttribute("value");
					attr.Value = "00";
					nodeMajor.Attributes.Append( attr);
				}

				XmlNode nodeMinor = verDoc.CreateElement("MINOR");
				//	Add attribute info for Major version
				{
					XmlAttribute attr = verDoc.CreateAttribute("value");
					attr.Value = "000";
					nodeMinor.Attributes.Append( attr);
				}

				nodeMajor.AppendChild( nodeMinor );
				node.AppendChild( nodeMajor );
			}

			verDoc.AppendChild( node );
			
			SaveConfigXML (CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/" + CommonPatcherData.patchVersionFN, verDoc);
		}else{
			string ver = verDoc.SelectSingleNode ("/VERSIONS/PATCH").Attributes ["LastVersion"].Value;
			string [] data = ver.Split('_');
			if(data.Length < 2)
			{
				Debug.LogError ( "data is incorrect!!" );
			}else{
				lastMajorVersion = Convert.ToInt32(data[1]);
				lastMinorVersion = Convert.ToInt32(data[2]);
				chkLastMajorVersion = (Convert.ToInt32(data[1])).ToString ("D2");
				chkLastMinorVersion = (Convert.ToInt32(data[2]) +1).ToString("D3");
			}
		}
		//	make sub folder
		MakeLocalRepo();
	}
	
	void SaveVersionXML(){
		string url = CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/" + CommonPatcherData.patchVersionFN;

		if (verDoc == null) {
			LoadVersionXML();
			if(verDoc == null) {
				Debug.LogError (" Restart AssetBundleMngWindow. Version data is incorrect!");
				return;
			}
		}

		if (lastMajorVersion >= Convert.ToInt32 (chkLastMajorVersion) && lastMinorVersion >= Convert.ToInt32 (chkLastMinorVersion)) {
			Debug.LogError (" Version is incorrect! New version is higher than old version.");
			return;
		}
		if (getVersionNode (Convert.ToInt32 (chkLastMajorVersion), Convert.ToInt32 (chkLastMinorVersion)) != null) {
			Debug.LogError (" This version is exist! Please re-check the version.");
			return;
		}

		lastMajorVersion = Convert.ToInt32 (chkLastMajorVersion);
		lastMinorVersion = Convert.ToInt32 (chkLastMinorVersion);

		XmlNode _parent = getVersionNode (lastMajorVersion);
		bool _bNewParent = false;
		if (_parent == null) {
			_parent = verDoc.CreateElement("MAJOR");
			XmlAttribute attr = verDoc.CreateAttribute("value");
			attr.Value = lastMajorVersion.ToString("D2");
			_parent.Attributes.Append( attr);
			_bNewParent = true;
		}

		XmlNode _child = verDoc.CreateElement("MINOR");
		XmlAttribute attrC = verDoc.CreateAttribute("value");
		attrC.Value = lastMinorVersion.ToString("D3");
		_child.Attributes.Append( attrC);
		_parent.AppendChild (_child);
		if (_bNewParent)
			verDoc.AppendChild (_parent);
		XmlNode _last = verDoc.SelectSingleNode ("/VERSIONS/PATCH");
		_last.Attributes["LastVersion"].Value = "VER_"+ lastMajorVersion.ToString("D2") + "_" +lastMinorVersion.ToString("D3");

		XmlTool.writeXml (url, verDoc);

		chkLastMajorVersion = lastMajorVersion.ToString ("D2");
		chkLastMinorVersion = (lastMinorVersion+1).ToString("D3");

		//	make sub folder
		MakeLocalRepo();
	}

	XmlNode getVersionNode(int _major){
		XmlNode _root = verDoc.SelectSingleNode("/VERSIONS");
		XmlNode _node = _root.FirstChild;
		while(_node != null){
			if(_node.Name == "MAJOR"){
				if( _major == Convert.ToInt32( _node.Attributes["value"].Value))
					return _node;
			}
			_node = _node.NextSibling;
		}

		return null;
	}

	XmlNode getVersionNode(int _major, int _minor){
		XmlNode _parent = getVersionNode( _major );
		if(_parent == null)
			return null;

		XmlNode _child = _parent.FirstChild;
		while(_child != null){
			if(_child.Name == "MINOR"){
				if( _minor == Convert.ToInt32( _child.Attributes["value"].Value))
					return _child;
			}
			_child = _child.NextSibling;
		}

		return null;
	}

	int compareFile(FileInfo _a, FileInfo _b){
		if (_a.Name != _b.Name)
			return 0;
		if (_a.LastWriteTime != _b.LastWriteTime || _a.Length != _b.Length)
			return 2;

		return 1;
	}

	//	version xml file
	///////////////////////////////////////////////////////

	///////////////////////////////////////////////////////
	//	assets xml file
	void SaveAssetsXML(List<FileInfo> listNew, List<FileInfo> listModify, List<FileInfo> listRemoved){
		XmlDocument xmlDoc = new XmlDocument ();
		XmlNode root = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
		xmlDoc.AppendChild( root );
		
		XmlNode nodeMain = xmlDoc.CreateElement("AssetBundles");

		XmlNode nodeVer = xmlDoc.CreateElement ("VERSION");
		XmlAttribute attrVer = xmlDoc.CreateAttribute ("value");
		attrVer.Value = "VER_"+ lastMajorVersion.ToString("D2") + "_" +lastMinorVersion.ToString("D3");
		nodeVer.Attributes.Append( attrVer);
		nodeMain.AppendChild (nodeVer);

		{
			XmlNode nodeCreate = xmlDoc.CreateElement ("CREATE");
			foreach(FileInfo info in listNew){
				XmlNode nodeFILE = xmlDoc.CreateElement("FILE");
				XmlAttribute attrName = xmlDoc.CreateAttribute ("name");
				attrName.Value = info.Name;
				nodeFILE.Attributes.Append( attrName);
				nodeCreate.AppendChild(nodeFILE);
			}
			nodeMain.AppendChild( nodeCreate);
		}

		{
			XmlNode nodeModify = xmlDoc.CreateElement ("MODIFY");
			foreach(FileInfo info in listModify){
				XmlNode nodeFILE = xmlDoc.CreateElement("FILE");
				XmlAttribute attrName = xmlDoc.CreateAttribute ("name");
				attrName.Value = info.Name;
				nodeFILE.Attributes.Append( attrName);
				nodeModify.AppendChild(nodeFILE);
			}
			nodeMain.AppendChild( nodeModify);
		}

		{
			XmlNode nodeRemove = xmlDoc.CreateElement ("REMOVE");
			foreach(FileInfo info in listRemoved){
				XmlNode nodeFILE = xmlDoc.CreateElement("FILE");
				XmlAttribute attrName = xmlDoc.CreateAttribute ("name");
				attrName.Value = info.Name;
				nodeFILE.Attributes.Append( attrName);
				nodeRemove.AppendChild(nodeFILE);
			}
			nodeMain.AppendChild( nodeRemove);
		}

		xmlDoc.AppendChild (nodeMain );

		string xmlFullpath = CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/" + "VER_" + lastMajorVersion.ToString ("D2") + "/" + lastMinorVersion.ToString ("D3") + "/" + CommonPatcherData.assetbundleFN;
		XmlTool.writeXml( xmlFullpath , xmlDoc);

		//	backup all AssetBundles from "repopath/ostype/latest/"  to "repopath/ostype/latest_ver_XX_XXX"
		string latest = CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/" + CommonPatcherData.lastVersionRepo;
		string backuppath = CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/"  + CommonPatcherData.lastVersionRepo + "_VER_" + lastMajorVersion.ToString ("D2") + "_" + lastMinorVersion.ToString ("D3");

		if(lastMajorVersion!=0 || lastMinorVersion != 0)
		{
			//Directory.CreateDirectory(backuppath);
			Directory.Move(latest, backuppath);
			Directory.CreateDirectory(latest);
		}

		// copy all AssetBundles to "repopath/ostype/latest/"
		BuildScript.CopyAssetBundlesTo( CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget+ "/" + CommonPatcherData.lastVersionRepo);

		// copy some assetbundles for patching to "repopath/ostype/ver_xx/xxx/
		string vertargetPath = CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/" + "VER_" + lastMajorVersion.ToString ("D2") + "/" + lastMinorVersion.ToString ("D3");
		foreach (FileInfo info in listNew) {
			File.Copy( info.FullName, vertargetPath + "/" + info.Name);
		}
		foreach (FileInfo info in listModify) {
			File.Copy( info.FullName, vertargetPath + "/" + info.Name);
		}
		foreach (FileInfo info in listRemoved) {
			File.Copy( info.FullName, vertargetPath + "/" + info.Name);
		}
	}
	//	assets xml file
	///////////////////////////////////////////////////////

	///////////////////////////////////////////////////////
	//	make local repository
	void MakeLocalRepo(){
		Directory.CreateDirectory (CommonPatcherData.repoPath);
		Directory.CreateDirectory (CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget);
		Directory.CreateDirectory (CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/" + CommonPatcherData.lastVersionRepo);

		//	make folders for vertions 
		if (verDoc != null) {
			XmlNode _node = verDoc.SelectSingleNode ("/VERSIONS");
			if(_node != null){
				XmlNode _child = _node.FirstChild;
				while(_child != null)
				{
					if(_child.Name == "PATCH")
					{
						_child = _child.NextSibling;
						continue;
					}
					string _dn = CommonPatcherData.repoPath + "/" + EditorUserBuildSettings.activeBuildTarget + "/VER_" + _child.Attributes["value"].Value;
					Directory.CreateDirectory (_dn);
					makeSubRepo( _dn, _child);
					_child = _child.NextSibling;
				}
			}
		}
	}

	void makeSubRepo(string _parent, XmlNode _parentNode){
		XmlNode _child = _parentNode.FirstChild;
		while(_child != null)
		{
			string _dn = _parent+"/" + _child.Attributes["value"].Value;
			Directory.CreateDirectory (_dn);
			_child = _child.NextSibling;
		}
	}
	//	local repository
	///////////////////////////////////////////////////////

	///////////////////////////////////////////////////////
	//	for compare files, but it doesn't use now. I just have a plan to use this function in "compareFile" function.
	public  string Md5Sum(byte[] bytes)
	{
		// encrypt bytes
		System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		byte[] hashBytes = md5.ComputeHash(bytes);
		
		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";
		
		for (int i = 0; i < hashBytes.Length; i++)
		{
			hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
		}
		
		return hashString.PadLeft(32, '0');
	}

}


