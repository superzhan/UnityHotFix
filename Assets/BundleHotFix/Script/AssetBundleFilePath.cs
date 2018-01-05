using UnityEngine;
using System.Collections;
using System.IO;

namespace BundleHotFix
{

public class AssetBundleFilePath  {


		public static void Init()
		{
			applicationDataPath = Application.dataPath;
		}

		private static string applicationDataPath="";


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
					if(applicationDataPath!="")
						return applicationDataPath.Replace ("Assets", "");

				///*如果是电脑的编辑模式，先放在项目外面*/
				return Application.dataPath.Replace ("Assets", "");
			}
			else
			{
					if(applicationDataPath!="")
						return applicationDataPath+"/";
				return Application.dataPath + "/";
			}
		}
	}
	
	//从服务器下载的数据的保存目录 
	public static string ServerExtensionDataPath
	{
		get {
			if(Directory.Exists(RootPath + "ServerExtensionData") ==false)
			{
				Directory.CreateDirectory(RootPath + "ServerExtensionData");
			}
			return RootPath + "ServerExtensionData/";
		}
	}

		public static string StreamingPath
		{
			get{
				string filePath =   
					#if UNITY_EDITOR  
					   Application.dataPath + "/StreamingAssets" + "/";  
				
					#elif UNITY_ANDROID 
					Application.dataPath + "!/assets/";  
					#elif UNITY_IPHONE 
					Application.dataPath + "/Raw/";  
					#else
					string.Empty;  
					#endif

				return filePath;
			}
		}
}
}
