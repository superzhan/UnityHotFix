using UnityEngine;
using System.Collections;
using System.IO;
using System;


public class FileTool {

	public static string RootPath
	{
		get{
			if(Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android) 
			{
				string tempPath = Application.persistentDataPath, dataPath;
				if (!string.IsNullOrEmpty (tempPath)) {
					
					dataPath = PlayerPrefs.GetString ("DataPath", "");
					if (string.IsNullOrEmpty (dataPath)) {
						PlayerPrefs.SetString ("DataPath", tempPath);
					}
					
					return tempPath + "/";
				} else {
					Debug.Log ("Application.persistentDataPath Is Null.");
					
					dataPath = PlayerPrefs.GetString ("DataPath", "");
					
					return dataPath + "/";
				}
			}
			else if(Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
			{
				///*如果是电脑的编辑模式，先放在项目外面*/
				return Application.dataPath.Replace ("Assets", "");
			}
			else
			{
				return Application.dataPath + "/";
			}
		}
	}

	/// <summary>
	/// 写文件操作
	/// 指定路径文件不存在会被创建
	/// </summary>
	/// <param name="path">文件路径（包含Application.persistentDataPath）.</param>
	/// <param name="name">文件名.</param>
	/// <param name="info">写入内容.</param>
	public static void createORwriteFile (string fileName, string info)
	{
		FileStream fs = new FileStream (RootPath + fileName, FileMode.Create, FileAccess.Write);
		StreamWriter sw = new StreamWriter (fs);
		fs.SetLength (0);	///*清空文件*/
		sw.WriteLine (info);
		sw.Close ();
		sw.Dispose ();
	}

	/// <summary>
	/// 读取文件内容  仅读取第一行
	/// </summary>
	/// <returns>The file.</returns>
	/// <param name="path">Path.</param>
	/// <param name="name">Name.</param>
	public static string ReadFile (string fileName)
	{
		string fileContent; 
		StreamReader sr = null;
		try{
			sr = File.OpenText(RootPath + fileName);
		}
		catch(Exception e){
			Debug.Log(e.Message);
			return null;
		}
		
		while ((fileContent = sr.ReadLine()) != null) {
			break; 
		}
		sr.Close ();
		sr.Dispose ();
		return fileContent;
	}

	public static bool IsFileExists(string fileName)
	{
		return File.Exists (RootPath + fileName);
	}

}
