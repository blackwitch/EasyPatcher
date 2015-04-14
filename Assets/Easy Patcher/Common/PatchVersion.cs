using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
#if UNITY_EDITOR	
using UnityEditor;
#endif

public class PatchVersion : MonoBehaviour
{
	public static int major
	{
		get { return majorVersion; }
		set { majorVersion = value; }
	}
	public static int minor
	{
		get { return minorVersion; }
		set { minorVersion = value; }
	}

	static int majorVersion = 0;
	static int minorVersion = 0;

	public static bool setVersion(string _value){	//	_value format is VER_XX_XXX
		string [] verString = _value.Split ('_');
		if (verString.Length < 3)
			return false;

		majorVersion = Convert.ToInt32( verString [1] );
		minorVersion = Convert.ToInt32( verString [2] );

		return true;
	}

	public static bool isEqualToLocalVersion(string _value){
		string [] verString = _value.Split ('_');
		if (verString.Length < 3)
			return true;	//	dont update.

		if (majorVersion < Convert.ToInt32 (verString [1]))
			return false;

		if (majorVersion == Convert.ToInt32 (verString [1]) && 
		    minorVersion < Convert.ToInt32( verString [2] ) 
		){
			return false;
		}

		return true;
	}

	//	move another class for patching
	public static XmlNode getMajorNode(XmlDocument _xmlDoc, int _major){
		XmlNode node = _xmlDoc.SelectSingleNode("/VERSIONS/MAJOR");
		while (node != null) {
			if( Convert.ToInt32(node.Attributes["value"].Value ) == _major)
				return node;
			node = node.NextSibling;
		}
		return node;
	}

	public static XmlNode getVersionNode(XmlDocument _xmlDoc, int _major, int _minor){
		XmlNode node = _xmlDoc.SelectSingleNode("/VERSIONS/MAJOR");
		while (node != null) {
			if( Convert.ToInt32(node.Attributes["value"].Value ) == _major){
				XmlNode nodeChild = node.FirstChild;
				while(nodeChild != null){
					if( Convert.ToInt32(nodeChild.Attributes["value"].Value ) == _minor)
						return nodeChild;
					nodeChild = nodeChild.NextSibling;
				}
			}
			node = node.NextSibling;
		}
		return null;
	}

	public static bool removeVersionNode(XmlDocument _xmlDoc, int _major, int _minor){
		XmlNode node = _xmlDoc.SelectSingleNode("/VERSIONS/MAJOR");
		while (node != null) {
			if( Convert.ToInt32(node.Attributes["value"].Value ) == _major){
				XmlNode nodeChild = node.FirstChild;
				while(nodeChild != null){
					if( Convert.ToInt32(nodeChild.Attributes["value"].Value ) == _minor)
					{
						node.RemoveChild( nodeChild);
						return true;
					}
					nodeChild = nodeChild.NextSibling;
				}
			}
			node = node.NextSibling;
		}
		return false;
	}

	public static string getPreviousVersion( string _path){
		string prevVersion = "";
		string lastVersion = "";

		XmlDocument xmlDoc = XmlTool.loadXml (_path);

		XmlNode nodeLast = xmlDoc.SelectSingleNode("/VERSIONS/PATCH");
		if (nodeLast != null) {
			lastVersion = nodeLast.Attributes["LastVersion"].Value;
			string [] info = lastVersion.Split('_');
			int major = Convert.ToInt32(info[1]);
			int minor = Convert.ToInt32(info[2]);
			int prevMajor = 0;
			int prevMinor = 0;

			XmlNode majorNode = xmlDoc.SelectSingleNode("/VERSIONS").FirstChild;
			while (majorNode != null) {
				if(majorNode.Name == "MAJOR"){
					int chkMajorVer = Convert.ToInt32(majorNode.Attributes["value"].Value);
					if(major == chkMajorVer){
						XmlNode minorNode = majorNode.FirstChild;
						while(minorNode != null){
							int chkMinorVer = Convert.ToInt32(minorNode.Attributes["value"].Value);
							if(minor == chkMinorVer){
								prevVersion = "VER_"+prevMajor.ToString("D2")+"_"+prevMinor.ToString("D3");
								return prevVersion;
							}else if(minor > chkMinorVer){
								prevMajor = chkMajorVer;
								prevMinor = chkMinorVer;
							}
							minorNode = minorNode.NextSibling;
						}
						break;
					}else if(major > chkMajorVer){
						prevMajor = chkMajorVer;
						prevMinor = Convert.ToInt32(majorNode.LastChild.Attributes["value"].Value);
					}
				}
				majorNode = majorNode.NextSibling;
			}
		}
		return prevVersion;
	}

	public static Dictionary<string , int> getAssetBundleVerList(){
		Dictionary<string , int> list = new Dictionary<string , int>();
		string path = Application.persistentDataPath + "/" + CommonPatcherData.abverListFN;
		XmlDocument xmlDoc = XmlTool.loadXml (path);

		if (xmlDoc != null) {
			XmlNode node = xmlDoc.SelectSingleNode ("/AssetBundleVerison").FirstChild;

			while (node != null) {
				string key = node.Attributes ["name"].Value;
				int version = Convert.ToInt32 (node.Attributes ["version"].Value);
				list.Add (key, version);
			}
		}

		return list;
	}

	public static void setAssetBundleVerList(Dictionary<string, int> list){
		string path = Application.persistentDataPath + "/" + CommonPatcherData.abverListFN;

		XmlDocument verDoc = new XmlDocument();
		XmlNode root = verDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
		verDoc.AppendChild( root );
		
		XmlNode node = verDoc.CreateElement("AssetBundleVerison");

		foreach( KeyValuePair<string, int> pair in list){
			XmlNode nodeChild = verDoc.CreateElement("FILE");
			XmlAttribute attr = verDoc.CreateAttribute("name");
			attr.Value = pair.Key;
			XmlAttribute attrP = verDoc.CreateAttribute("version");
			attrP.Value = pair.Value.ToString ();
			nodeChild.Attributes.Append( attr);
			nodeChild.Attributes.Append( attrP);
			node.AppendChild( nodeChild);
		}

		verDoc.AppendChild( node );
		
		XmlTool.writeXml(path, verDoc);
	}

	public static List<string> getPatchList(XmlDocument _xmlDoc, int _major, int _minor){
		List<string> list = new List<string> ();
		bool checkStart = false;
		int curMajor = 0, curMinor = 0;
		XmlNode node = _xmlDoc.SelectSingleNode ("/VERSIONS/MAJOR");
		while (node != null) {
			curMajor = Convert.ToInt32(node.Attributes["value"].Value );
			if( curMajor == _major){
				XmlNode nodeChild = node.FirstChild;
				while(nodeChild != null){
					curMinor = Convert.ToInt32(nodeChild.Attributes["value"].Value );
					if(checkStart == true){
						list.Add ( "VER_"+ curMajor.ToString ("D2") + "_"+  curMinor.ToString ("D3") );
					}
					if( curMinor == _minor)
						checkStart = true;
					nodeChild = nodeChild.NextSibling;
				}
			}
			node = node.NextSibling;
		}
		return list;
	}
}

