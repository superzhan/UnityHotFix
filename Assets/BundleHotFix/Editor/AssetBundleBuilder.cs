using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using SevenZip.Compression.LZMA;
using System;

/// <summary>
/// Asset bundle 
/// 资源打包
/// </summary>
using System.Text;
using BundleHotFix;

namespace BundleHotFix
{

	public class AssetBundleBuilder 
	{

		#region 打包

		[MenuItem("AssetBundle/Build AssetBundle (资源打包)")]
		static public void BuildAssetBundle()
		{
			Debug.Log("开始资源打包");
			Caching.CleanCache();
			string[] paths= Directory.GetFiles(GetOutPutPath());
			for(int i=0;i<paths.Length;++i)
			{
				File.Delete(paths[i]);
			}

			SetAssetBundleName ();

			BuildPipeline.BuildAssetBundles (GetOutPutPath(),BuildAssetBundleOptions.UncompressedAssetBundle,EditorUserBuildSettings.activeBuildTarget);
			AssetDatabase.Refresh();
			GenerateFileInfoTable();

			Debug.Log("完成 资源打包");
		}

		static public string GetOutPutPath()
		{
			string path = Application.dataPath.Replace("Assets","")  + "AssetBundles" + "/" + GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
			if (!Directory.Exists (path)) 
			{
				Directory.CreateDirectory(path);
			}
			return path;
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

		static string GetPlatformFolderForAssetBundles(RuntimePlatform platform)
		{
			switch(platform)
			{
			case RuntimePlatform.Android:
				return "Android";
			case RuntimePlatform.IPhonePlayer:
				return "iOS";
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

		#endregion

		#region  文件压缩

		[MenuItem ("AssetBundle/CompressFile(打包成服务器资源)")]
		static void CompressFile () 
		{
			Debug.Log("开始压缩文件");
			//清理文件
			string outPutPath = Application.dataPath.Replace("Assets","")  + "AssetBundles" + "/ServerCompressAssets";
			if (!Directory.Exists (outPutPath)) 
			{
				Directory.CreateDirectory(outPutPath);
			}

			string[] paths= Directory.GetFiles(outPutPath);
			for(int i=0;i<paths.Length;++i)
			{
				File.Delete(paths[i]);
			}


			//copy csv info file
			File.Copy(GetOutPutPath()+"/"+csvFileName,outPutPath+"/"+csvFileName);
			File.Copy(GetOutPutPath()+"/"+"resVer.txt",outPutPath+"/"+"resVer.txt");

			//压缩文件
			String[] names = AssetDatabase.GetAllAssetBundleNames();
			List<string> fileList = new List<string>();
			fileList.AddRange(names);
			string bundleName =GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
			fileList.Add(bundleName);

			string sourcesPath = GetOutPutPath();
			Loom.RunAsync(()=>{
				for(int i=0;i<fileList.Count;++i)
				{
					CompressFileLZMA(sourcesPath+"/"+fileList[i], outPutPath+"/"+fileList[i]+".7z");
					Debug.Log("Compress "+fileList[i]);
				}
				Debug.Log("完成 打包成服务器资源");	
			});
				
		}
	
	//	[MenuItem ("AssetBundle/DecompressFile")]
	//	static void DecompressFile () 
	//	{
	//		//解压文件
	//		DecompressFileLZMA(Application.dataPath+"/StreamingAssets/AssetBundles/Android/configdata.normal.zip",Application.dataPath+"/StreamingAssets/AssetBundles/Android/configdata.normal");
	//		AssetDatabase.Refresh();
	//	}
		
		
		private static void CompressFileLZMA(string inFile, string outFile)
		{
			SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();
			FileStream input = new FileStream(inFile, FileMode.Open);
			FileStream output = new FileStream(outFile, FileMode.Create);
			
			// Write the encoder properties
			coder.WriteCoderProperties(output);
			
			// Write the decompressed file size.
			output.Write(BitConverter.GetBytes(input.Length), 0, 8);
			
			// Encode the file.
			coder.Code(input, output, input.Length, -1, null);
			output.Flush();
			output.Close();
			input.Close();
		}
		
		private static void DecompressFileLZMA(string inFile, string outFile)
		{

			SevenZip.Compression.LZMA.Decoder coder = new SevenZip.Compression.LZMA.Decoder();
			FileStream input = new FileStream(inFile, FileMode.Open);
			FileStream output = new FileStream(outFile, FileMode.Create);
			
			// Read the decoder properties
			byte[] properties = new byte[5];
			input.Read(properties, 0, 5);
			
			// Read in the decompress file size.
			byte [] fileLengthBytes = new byte[8];
			input.Read(fileLengthBytes, 0, 8);
			long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);
			
			// Decompress the file.
			coder.SetDecoderProperties(properties);
			coder.Code(input, output, input.Length, fileLength, null);
			output.Flush();
			output.Close();
			input.Close();
		}

		#endregion

		#region 产生文件信息表格

		static string csvFileName="ResourceInfoData.csv";

		/// <summary>
		/// Generates the file info table.
		/// </summary>
		private static void GenerateFileInfoTable()
		{
			EditorResourcesInfoData.Instance.Init();

		   
			List<AssetInfoItem> assetInfoItemList = new List<AssetInfoItem>();
			AssetDatabase.RemoveUnusedAssetBundleNames();

			//普通bundle 的信息
			bool isChange=false;
			String[] names = AssetDatabase.GetAllAssetBundleNames();
			for(int i=0;i<names.Length;++i)
			{
				AssetInfoItem assetInfoItem = new AssetInfoItem();
				string bundlename = names[i];
				assetInfoItem.bunndleName=bundlename;


				ManifestInfo info = GetManifestInfo(bundlename);
				assetInfoItem.crc=info.crc;
				assetInfoItem.hashCode=info.hashCode;


				string oldHashCode = EditorResourcesInfoData.Instance.GetHashCode(bundlename);
				if(oldHashCode != info.hashCode) //upgrade versioncode
				{
					assetInfoItem.versionCode=EditorResourcesInfoData.Instance.GetNextVersionCode(bundlename);
					isChange=true;
				}else//not change ,keep versioncode
				{
					assetInfoItem.versionCode=EditorResourcesInfoData.Instance.GetVersionCode(bundlename);
				}

				assetInfoItemList.Add(assetInfoItem);
			}


			//总包的信息
			AssetInfoItem totalInfoItem = new AssetInfoItem();
			totalInfoItem.bunndleName =  GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
			ManifestInfo totalInfo = GetManifestInfo(totalInfoItem.bunndleName);
			totalInfoItem.crc=totalInfo.crc;
			totalInfoItem.hashCode = "";
			if(isChange)
				totalInfoItem.versionCode = EditorResourcesInfoData.Instance.GetNextVersionCode(totalInfoItem.bunndleName);
			else
				totalInfoItem.versionCode = EditorResourcesInfoData.Instance.GetVersionCode(totalInfoItem.bunndleName);

			assetInfoItemList.Insert(0,totalInfoItem);

			GenerateCSVFile( assetInfoItemList);
			GenerateVerInfo(isChange);
		}

		/// <summary>
		/// Generates the ver info.
		///版本号码
		/// </summary>
		/// <param name="isChange">If set to <c>true</c> is change.</param>
		private static void GenerateVerInfo(bool isChange)
		{
			int resVerCode =0;
			string resVerName="1.0.0";

			string resVerPath = Application.dataPath.Replace("Assets","") +"AssetBundles/"+"resVer.txt";
			if(File.Exists(resVerPath))
			{
				FileStream fs = new FileStream (resVerPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				StreamReader sr = new StreamReader (fs, Encoding.UTF8);
				resVerCode =  int.Parse( sr.ReadLine () );
				resVerName = sr.ReadLine();
				sr.Close ();
				fs.Close ();
			}else
			{
				resVerCode = ResVersionData.Instance.versionData.resVersionCode;
				resVerName =ResVersionData.Instance.versionData.resVersionName;
			}

			if(isChange)
			{
				++resVerCode;

				string[] verNames= resVerName.Split('.');
				int last = int.Parse(verNames[2]) +1;
				resVerName = verNames[0]+"."+verNames[1]+"."+last;
			}



			FileStream assetFile=  File.Open(resVerPath,FileMode.Create);
			Encoding utf8WithoutBom = new UTF8Encoding(false);
			StreamWriter sw = new StreamWriter(assetFile, utf8WithoutBom);
			sw.WriteLine(resVerCode.ToString());
			sw.WriteLine(resVerName);
			sw.Close();
			assetFile.Close();



			string desPath =GetOutPutPath()+"/resVer.txt";
			if(File.Exists(desPath))
			{
				File.Delete(GetOutPutPath()+"/resVer.txt");
			}
			File.Copy(resVerPath,desPath);


		}

		private static void GenerateCSVFile(List<AssetInfoItem> assetInfoItemList)
		{
			StringBuilder   csvFileStr   =   new   StringBuilder();
			csvFileStr.Append("id,bundleName,versionCode,crc,hashCode\r");
			for(int i=0;i<assetInfoItemList.Count;++i)
			{
				AssetInfoItem info= assetInfoItemList[i];
				csvFileStr.Append( (i+1).ToString());
				csvFileStr.Append(",");
				csvFileStr.Append(info.bunndleName);
				csvFileStr.Append(",");
				csvFileStr.Append(info.versionCode.ToString());
				csvFileStr.Append(",");
				csvFileStr.Append(info.crc);
				csvFileStr.Append(",");
				csvFileStr.Append(info.hashCode);

				if(i<assetInfoItemList.Count-1)
				     csvFileStr.Append('\r');
			}

			FileStream assetFile=  File.Open(GetOutPutPath()+"/"+csvFileName,FileMode.Create);
			Encoding utf8WithoutBom = new UTF8Encoding(false);
			StreamWriter sw = new StreamWriter(assetFile, utf8WithoutBom);
			sw.Write(csvFileStr);
			sw.Close();
			assetFile.Close();

			if(File.Exists(Application.dataPath.Replace("Assets","") +"AssetBundles/"+csvFileName))
			{
				File.Delete(Application.dataPath.Replace("Assets","") +"AssetBundles/"+csvFileName);
			}
			File.Copy(GetOutPutPath()+"/"+csvFileName,Application.dataPath.Replace("Assets","") +"AssetBundles/"+csvFileName);
			
		}


		#endregion

		#region 读取manifest 文件信息
		struct ManifestInfo{
			public string bundleName;
			public string crc;
			public string hashCode;
		}

		private static ManifestInfo GetManifestInfo(string bundleName)
		{
			ManifestInfo info = new ManifestInfo();

			string path=  GetOutPutPath()+"/"+bundleName+".manifest";
			StreamReader manifestFile  = File.OpenText(path);
			manifestFile.ReadLine();
			string crc = manifestFile.ReadLine();
			crc= crc.Substring( crc.IndexOf(": ")+2);
			manifestFile.ReadLine();
			manifestFile.ReadLine();
			manifestFile.ReadLine();
			string hashCode = manifestFile.ReadLine();
			hashCode = hashCode.Substring(hashCode.IndexOf(":")+2);
			manifestFile.Close();

			info.bundleName=bundleName;
			info.crc=crc;
			info.hashCode=hashCode;
			return info;
		}
		#endregion

		/// 一个AssetBundle 包的信息
		struct AssetInfoItem{
			public string bunndleName;
			public int versionCode;
			public string crc;
			public string hashCode;
		}


		#region 设置assetbundle Name

		static List<string> allFilePath = new List<string>();
		static string curBundleName = "";


	    /// <summary>
	    /// 配置要打包的资源路径
		/// 资源包的名称= 路径名+.n
	    /// </summary>
		static string[] pathArray = {
			"BundleHotFix/TestBundle",
	
		};

		[MenuItem ("AssetBundle/设置AssetBundle Name")]
		static void SetAssetBundleName()
		{
			
			for(int i=0;i<pathArray.Length;++i)
			{
				allFilePath.Clear();
				GetAllFilePathInDir(Application.dataPath+"/"+pathArray[i]);
				curBundleName = pathArray[i];
				SetBundleName();
			}
		}



		static void SetBundleName()
		{
			for(int i=0;i<allFilePath.Count;++i)
			{
				string path = allFilePath[i];
				path= path.Substring(path.IndexOf("Assets"));

				AssetImporter impor = AssetImporter.GetAtPath(path);

				if(impor == null)
					continue;

				string bundleName = curBundleName.ToLower();
				bundleName = bundleName.Replace("/",".");
				impor.assetBundleName = bundleName;
				impor.assetBundleVariant = "n";

			}
		}

		private static void GetAllFilePathInDir(string dirPath)
		{
			DirectoryInfo direct = new DirectoryInfo(dirPath);
			FileInfo[] fileInfos=direct.GetFiles();
			DirectoryInfo[] subDir = direct.GetDirectories();

			for(int i=0;i<fileInfos.Length;++i)
			{
				if(fileInfos[i].Extension.Contains("meta"))
					continue;

				allFilePath.Add(fileInfos[i].FullName);
				//Debug.Log(fileInfos[i].FullName);
			}

			for(int j=0;j<subDir.Length;++j)
			{
				GetAllFilePathInDir(subDir[j].FullName);
			}
		}
		#endregion


		[MenuItem ("AssetBundle/清理缓存")]
		public static void DeleteCache()
		{
			//清理文件
			string outPutPath = Application.dataPath.Replace("Assets","")  + "AssetBundles" + "/ServerCompressAssets";
			string[] paths= Directory.GetFiles(outPutPath);
			for(int i=0;i<paths.Length;++i)
			{
				File.Delete(paths[i]);
			}


			string serverFilePath = Application.dataPath.Replace("Assets","")  + "ServerExtensionData" ;
			string[] serverPaths= Directory.GetFiles(serverFilePath);
			for(int k=0;k<serverPaths.Length;++k)
			{
				File.Delete(serverPaths[k]);
			}

			Caching.CleanCache();
			string[] bundlepaths= Directory.GetFiles(GetOutPutPath());
			for(int j=0;j<bundlepaths.Length;++j)
			{
				File.Delete(bundlepaths[j]);
			}

			File.Delete ("ResVersionData");

			Debug.Log ("文件清理完成");
		}
	}
}
