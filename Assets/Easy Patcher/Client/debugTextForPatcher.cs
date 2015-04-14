using UnityEngine;
using System.Collections;

public class debugTextForPatcher : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnGUI() {
		GUI.Button (new Rect (10,10,200,20), "Download : " + EasyPatcher.PatchProgress+"%");
		GUI.Button (new Rect (10,40,600,20),  EasyPatcher.PatchMessage);
		GUI.Button (new Rect (10,70,600,20),  EasyPatcher.PatchLastLog);
	}
}
