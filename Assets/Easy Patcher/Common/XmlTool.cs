using UnityEngine;
using System;
using System.IO;
using System.Xml;

[Serializable]
public static class XmlTool
{
	public static XmlDocument loadXml(byte [] _data)
	{
		if (_data.Length == 0)
			return null;
		MemoryStream _stream = new MemoryStream (_data);
		XmlReader reader = XmlReader.Create (_stream);
		XmlDocument xmlDoc = new XmlDocument ();
		
		try
		{
			xmlDoc.Load( reader );
		}
		catch( Exception ex)
		{
			Debug.LogError( "Error Loading " + ex);
			return null;
		}

		return xmlDoc;
	}

	public static XmlDocument loadXml(string _fullpath)
	{
		if (File.Exists (_fullpath) == false)
			return null;

		byte [] _data = File.ReadAllBytes (_fullpath);
		
		MemoryStream _stream = new MemoryStream (_data);
		XmlReader reader = XmlReader.Create (_stream);
		XmlDocument xmlDoc = new XmlDocument ();

		try
		{
			xmlDoc.Load( reader );
		}
		catch( Exception ex)
		{
			Debug.LogError( "Error Loading " + _fullpath + ":\n" + ex);
		}
		finally
		{
			Debug.Log( _fullpath + "loaded!");
		}

		return xmlDoc;
	}

	//	move another class for patching
	public static void writeXml( string _fullpath, XmlDocument xmlDoc)
	{
		//	Actually, no matter the file is exist or not. This code is no append in the old file.
		if (File.Exists (_fullpath)) {
			File.Delete(_fullpath);
			using (TextWriter sw = new StreamWriter(_fullpath, false, System.Text.Encoding.UTF8))
			{
				xmlDoc.Save(sw);
			}
		}else {
			TextWriter sw = new StreamWriter(_fullpath, false, System.Text.Encoding.UTF8);
			if(sw != null){
				xmlDoc.Save(sw);
			}
		}
	}

}

