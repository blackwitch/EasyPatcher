using UnityEngine;
using System.Collections;

public class LoadAssets : BaseLoader {

	public string assetBundleName = "cube.unity3d";
	public string assetName = "cube";

	// Use this for initialization
	IEnumerator Start () {

		yield return StartCoroutine(Initialize() );

		EasyPatcher patcher = GameObject.Find ("EasyPatcherPrefab").GetComponent<EasyPatcher> ();
		EasyPatcher.SetRepogitory("http://localhost/test/");
		yield return patcher.StartCoroutine (patcher.startPatch ());

		if (EasyPatcher.PatchErrorCount == 0) {
			// Load asset.
			yield return StartCoroutine (Load (assetBundleName, assetName));

			// Unload assetBundles.
			AssetBundleManager.UnloadAssetBundle (assetBundleName);
		} else 
			yield break;
	}
}
