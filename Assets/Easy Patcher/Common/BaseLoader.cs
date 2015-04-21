﻿using UnityEngine;
using System.Collections;
#if UNITY_EDITOR	
using UnityEditor;
#endif

public class BaseLoader : MonoBehaviour {

	const string kAssetBundlesPath = "/AssetBundles/";

	// Use this for initialization.
	IEnumerator Start ()
	{
		yield return StartCoroutine(Initialize() );
	}

	// Initialize the downloading url and AssetBundleManifest object.
	protected IEnumerator Initialize()
	{
		// Don't destroy the game object as we base on it to run the loading script.
		DontDestroyOnLoad(gameObject);
		
#if UNITY_EDITOR
		Debug.Log ("We are " + (AssetBundleManager.SimulateAssetBundleInEditor ? "in Editor simulation mode" : "in normal mode") );
#endif

		string platformFolderForAssetBundles = 
#if UNITY_EDITOR
			GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
#else
			GetPlatformFolderForAssetBundles(Application.platform);
#endif

		// Set base downloading url.
		// string relativePath = GetRelativePath();
		// AssetBundleManager.BaseDownloadingURL = relativePath + kAssetBundlesPath + platformFolderForAssetBundles + "/";

		// Initialize AssetBundleManifest which loads the AssetBundleManifest object.
		var request = AssetBundleManager.Initialize(platformFolderForAssetBundles);
		if (request != null)
			yield return StartCoroutine(request);
	}

	public string GetRelativePath()
	{
		if (Application.isEditor)
			return "file://" +  System.Environment.CurrentDirectory.Replace("\\", "/"); // Use the build output folder directly.
		else if (Application.isWebPlayer)
			return System.IO.Path.GetDirectoryName(Application.absoluteURL).Replace("\\", "/")+ "/StreamingAssets";
		else if (Application.isMobilePlatform || Application.isConsolePlatform)
			return Application.streamingAssetsPath;
		else // For standalone player.
			return "file://" +  Application.streamingAssetsPath;
	}

#if UNITY_EDITOR
	public static string GetPlatformFolderForAssetBundles(BuildTarget target)
	{
		switch(target)
		{
		case BuildTarget.Android:
			return "Android";
		case BuildTarget.iOS:
			return "iOS";
		case BuildTarget.WebPlayer:
			return "WebPlayer";
		case BuildTarget.StandaloneWindows:
		case BuildTarget.StandaloneWindows64:
			return "Windows";
		case BuildTarget.StandaloneOSXIntel:
		case BuildTarget.StandaloneOSXIntel64:
		case BuildTarget.StandaloneOSXUniversal:
			return "OSX";
			// Add more build targets for your own.
			// If you add more targets, don't forget to add the same platforms to GetPlatformFolderForAssetBundles(RuntimePlatform) function.
		default:
			return null;
		}
	}
#endif

	public static string GetPlatformFolderForAssetBundles(RuntimePlatform platform)
	{
		switch(platform)
		{
		case RuntimePlatform.Android:
			return "Android";
		case RuntimePlatform.IPhonePlayer:
			return "iOS";
		case RuntimePlatform.WindowsWebPlayer:
		case RuntimePlatform.OSXWebPlayer:
			return "WebPlayer";
		case RuntimePlatform.WindowsPlayer:
			return "Windows";
		case RuntimePlatform.OSXPlayer:
			return "OSX";
			// Add more build platform for your own.
			// If you add more platforms, don't forget to add the same targets to GetPlatformFolderForAssetBundles(BuildTarget) function.
		default:
			return null;
		}
	}

	protected IEnumerator Load (string assetBundleName, string assetName)
	{
		Debug.Log("Start to load " + assetName + " at frame " + Time.frameCount);

		// Load asset from assetBundle.
		AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(GameObject) );

		if (request == null) {
			yield break;
		}

		yield return StartCoroutine(request);

		// Get the asset.
		GameObject prefab = request.GetAsset<GameObject> ();

		if (prefab != null)
			GameObject.Instantiate(prefab);
	}

	protected IEnumerator LoadLevel (string assetBundleName, string levelName, bool isAdditive)
	{
		Debug.Log("Start to load scene " + levelName + " at frame " + Time.frameCount);

		// Load level from assetBundle.
		AssetBundleLoadOperation request = AssetBundleManager.LoadLevelAsync(assetBundleName, levelName, isAdditive);
		if (request == null)
			yield break;
		yield return StartCoroutine(request);

		// This log will only be output when loading level additively.
		Debug.Log("Finish loading scene " + levelName + " at frame " + Time.frameCount);
	}

	// Update is called once per frame
	protected void Update () {
	}
}
