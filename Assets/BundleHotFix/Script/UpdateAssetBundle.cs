using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;

namespace BundleHotFix
{
	/// <summary>
	/// 资源更新类
	/// </summary>
	public class UpdateAssetBundle : MonoBehaviour
	{
		public static UpdateAssetBundle Instance=null;
		public static bool isFirstUpdate =true;

		private bool isClose=false;

		[Header("是否开启资源热更新")]
		public bool isOpenUpdateAssets = true;

		[Header("资源服务器地址")]
		public string serverAddr = "";

		ResourcesInfoData serverResourceInfoData = null;
		List<string> downLoadFileList = new List<string> ();
		private int downingFileCount = 0;

		private int downloadFileIndex = 1,totalFileCount=0;

		private int downloadRetryCount=0;

		int serverVerCode = 0;
		string serverVerName= "";

		//标记是否正在下载文件
		bool isDownloadingFile {
			get {
				bool b = PlayerPrefs.GetInt ("isDownloadingFile", 0) == 1;
				return b;
			}
			set {
				int i = value ? 1 : 0;
				PlayerPrefs.SetInt ("isDownloadingFile", i);
			}
		}

		void Awake ()
		{
			Instance= this;
			AssetBundleFilePath.Init();

		}

		void OnEnable()
		{
			isClose = false;
		}
		void OnDisable()
		{
			isClose = true;
		}

		// Use this for initialization
		void Start ()
		{
			if (isOpenUpdateAssets == false) {
			
				//GameStart.Instance.StartLoading ();
				return;
			}


			if (isFirstUpdate) {
				Debug.Log ("start update assets");

				//显示资源更新UI
//				GameObject resourcesUpdatePanel = ResourcesManager.Instance.LoadUIPanel ("ResourcesUpdatePanel");
//				resourcesUpdatePanel.transform.parent = this.transform;
//				resourcesUpdatePanel.SetActive (true);

				StartCoroutine (IECheckResVersionCode ());
			}

			isFirstUpdate = false;
		}




		#region 资源版本号



		private IEnumerator IECheckResVersionCode()
		{
			Debug.Log ("IECheckResVersionCode");
			WWW w = new WWW(serverAddr+"resVer.txt");
			//Debug.Log (w.url);
			yield return w;
			if(w.error !=null)
			{
				Debug.Log(w.error);
				EndHotFixUpdate ();
				yield break;
			}
			//serverResVerInfoDic.Clear();
			string serverResult = w.text;
			//Debug.Log(serverResult);

			//serverResult=serverResult.Replace("\n","");
			string[] serverVers = serverResult.Split('\n');

			string strCode = serverVers[0];
			//Debug.Log(strCode);
			serverVerCode = int.Parse(strCode);
			serverVerName= serverVers[1];
			string serverFirstVerStr = serverVerName.Split('.')[0];
			int serverFirstVer = int.Parse(serverFirstVerStr);


			int localVerCode = ResVersionData.Instance.versionData.resVersionCode;
			string localVerName =ResVersionData.Instance.versionData.resVersionName;
			string localFirstVerStr = localVerName.Split('.')[0];
			int localFirstVer = int.Parse(localFirstVerStr);

			Debug.Log(string.Format("serVer: {0}    localVer {1}    serName:{2}   localName:{3}",serverVerCode,localVerCode,serverVerName,localVerName));


			if(serverVerCode>localVerCode && serverFirstVer==localFirstVer)
			{
				StartCoroutine(IECompareInfoFile());
			}else
			{
				Debug.Log("allreadly update");
				EndHotFixUpdate ();
			}

		}




		/// <summary>
		///  对比本地和服务器上的ResourceInfoData
		/// 确定要下载的文件
		/// </summary>
		/// <returns>The compare info file.</returns>
		IEnumerator IECompareInfoFile ()
		{
			WWW getAssetInfoW = new WWW (serverAddr + "ResourceInfoData.csv");
			yield return getAssetInfoW;
			if (getAssetInfoW.error != null) {
				Debug.LogError ("can not connect to server");
				yield  break;
			}


			string infoText = getAssetInfoW.text;
			serverResourceInfoData = new ResourcesInfoData ();
			serverResourceInfoData.InitFromString (infoText);


			ResourcesInfoData localResourceInfoData = new ResourcesInfoData ();
			if (File.Exists (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv")) {
				localResourceInfoData.InitFromPath (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv");
			} else {
				localResourceInfoData.InitFromResource ();
			}


			downLoadFileList.Clear ();
			downingFileCount = 0;
			for (int i = 1; i <= serverResourceInfoData.GetDataRow (); ++i) {
				string bundleName = serverResourceInfoData.GetBundleName (i);
				int serverVersionCode = serverResourceInfoData.GetVersionCode (i);

				int localVerCode = localResourceInfoData.GetVersionCodeByBundleName (bundleName);
				if (localVerCode < 0) {
					downLoadFileList.Add (bundleName);
				} else if (localVerCode < serverVersionCode) {
					downLoadFileList.Add (bundleName);
				}
				Debug.Log (bundleName + " " + serverVersionCode + " " + localVerCode);
			}

			if (downLoadFileList.Count > 0) {
				isDownloadingFile = true;
				downloadFileIndex = 1;
				totalFileCount = downLoadFileList.Count;
				downloadRetryCount = 0;
				DownLoadNextFile ();
			} else {
				Debug.Log ("ALL file had updated");
				EndHotFixUpdate ();
			}
		}

		#endregion


		#region 资源更新过程

		/// <summary>
		/// 下载要更新的文件 
		/// 执行顺序 DownloadNextFile -> FinishDownload ->UpdateResoureceInfoData ->DownloadNextFile
		/// 直到结束  EndDownload
		/// </summary>
		/// <param name="fileList">File list.</param>
		void DownLoadNextFile ()
		{
			if(downLoadFileList.Count<1)
				return;

			//ResourcesUpdatePanel.Instance.SetProgress (downloadFileIndex, totalFileCount, true);


		    string fileName = downLoadFileList [0];
			string serverFilePath = serverAddr + fileName + ".7z";
			string localFilePath = AssetBundleFilePath.ServerExtensionDataPath + fileName + ".7z";

		
			Debug.Log ("downloading" + serverFilePath);
			downingFileCount++;



			float progress=0f;
			bool isDone=false;

			//开启子线程下载,使用匿名方法
			Loom.RunAsync(()=> {
				
				//使用流操作文件
				FileStream fs = new FileStream(localFilePath, FileMode.OpenOrCreate, FileAccess.Write);
				//获取文件现在的长度
				long fileLength = fs.Length;
				//获取下载文件的总长度
				long totalLength = GetLength(serverFilePath);

				//如果没下载完
				if(fileLength < totalLength)
				{
					//断点续传核心，设置本地文件流的起始位置
					fs.Seek(fileLength, SeekOrigin.Begin);

					HttpWebRequest request = HttpWebRequest.Create(serverFilePath) as HttpWebRequest;

					//断点续传核心，设置远程访问文件流的起始位置
					request.AddRange((int)fileLength);
					Stream  stream = request.GetResponse().GetResponseStream();

					byte[] buffer = new byte[1024];
					//使用流读取内容到buffer中
					//注意方法返回值代表读取的实际长度,并不是buffer有多大，stream就会读进去多少
					int length = stream.Read(buffer, 0, buffer.Length);
					while(length > 0)
					{
						if(isClose)
						{
							break;
						}
						
						//将内容再写入本地文件中
						fs.Write(buffer, 0, length);
						//计算进度
						fileLength += length;
						progress = (float)fileLength / (float)totalLength;
						//UnityEngine.Debug.Log(progress);
						//类似尾递归
						length = stream.Read(buffer, 0, buffer.Length);
					}
					stream.Close();
					stream.Dispose();

				}
				else
				{
					progress = 1;
				}

				fs.Close();
				fs.Dispose();

				//如果下载完毕，执行回调
				if(progress == 1)
				{
					isDone = true;
					Debug.Log (fileName + "Download finished!");
					downingFileCount--;
					FinishDownload (fileName);
				}else
				{
					if(isClose)
					{
						return;
					}

					downingFileCount--;
		
					//多次重试 失败后 直接开始游戏
					if(downloadRetryCount >4)
					{
						Loom.QueueOnMainThread(()=>{
							EndHotFixUpdate();
						});
						return;
					}

					//下载出错了  再次下载
					Loom.QueueOnMainThread(()=>{
						++downloadRetryCount;
						DownLoadNextFile();
					});

					return;
				}

			});
		}

		/// <summary>
		/// 获取下载文件的大小
		/// </summary>
		/// <returns>The length.</returns>
		/// <param name="url">URL.</param>
		long GetLength(string url)
		{
			HttpWebRequest requet = HttpWebRequest.Create(url) as HttpWebRequest;
			requet.Method = "HEAD";
			HttpWebResponse response = requet.GetResponse() as HttpWebResponse;
			return response.ContentLength;
		}

		/// <summary>
		/// Finishs the download.
		/// </summary>
		/// <param name="fileName">File name.</param>
		private void FinishDownload (string fileName)
		{

//			Loom.QueueOnMainThread (()=>{
//				ResourcesUpdatePanel.Instance.SetProgress (downloadFileIndex, totalFileCount, false);
//			});

			string localFilePath = AssetBundleFilePath.ServerExtensionDataPath + fileName + ".7z";
			string decompressFilePath = AssetBundleFilePath.ServerExtensionDataPath + fileName + ".temp7z"; //中间文件

			Loom.RunAsync(()=>{DecompressFileLZMA (fileName,localFilePath, decompressFilePath);});
				
		}

		private void FinishDecompressFile(string fileName)
		{
			string localFilePath = AssetBundleFilePath.ServerExtensionDataPath + fileName + ".7z";
			string decompressFilePath = AssetBundleFilePath.ServerExtensionDataPath + fileName + ".temp7z"; //中间文件

			try {
				File.Delete (localFilePath); //删除下载的文件
				if(File.Exists(AssetBundleFilePath.ServerExtensionDataPath + fileName + ".temp"))
					File.Delete(AssetBundleFilePath.ServerExtensionDataPath + fileName + ".temp");

				File.Move (decompressFilePath, AssetBundleFilePath.ServerExtensionDataPath + fileName + ".temp"); //最后下载的文件



				UpdateResoureceInfoData (fileName);
				downLoadFileList.Remove (fileName);

				++downloadFileIndex;
				DownLoadNextFile(); //下载下一个文件
				if (downLoadFileList.Count < 1) {
					EndDownload ();
				}
			} catch (Exception e) {
				Debug.Log (e.StackTrace);
			}
		}

		/// <summary>
		/// 结束整个下载过程 
		/// </summary>
		private void EndDownload ()
		{
			isDownloadingFile = false;

			//同步服务器的文件信息
			StringBuilder strBuilder = serverResourceInfoData.GetTotalString ();
			FileStream assetFile = File.Open (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv", FileMode.Create);
			StreamWriter sw = new StreamWriter (assetFile, Encoding.UTF8);
			sw.Write (strBuilder);
			sw.Close ();
			assetFile.Close ();

			UpdateResVerInfo();

			EndHotFixUpdate ();
		}
			
		/// <summary>
		/// 更新资源信息文件 
		/// </summary>
		/// <param name="fileName">File name.</param>
		private void UpdateResoureceInfoData (string fileName)
		{
			ResourcesInfoData localResourceInfoData = new ResourcesInfoData ();

			if (File.Exists (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv")) {
				localResourceInfoData.InitFromPath (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv");
			} else {
				
				localResourceInfoData.InitFromResource ();
			}
			int localId =	localResourceInfoData.GetIDByBundleName (fileName);



			int serverId = serverResourceInfoData.GetIDByBundleName (fileName);
			int verCode = serverResourceInfoData.GetVersionCode (serverId);
			string crc = serverResourceInfoData.GetCRC (serverId);
			string hash = serverResourceInfoData.GetHashCode (serverId);


			if (localId < 0) {
				localResourceInfoData.AddRowWithoutId (fileName, verCode.ToString (), crc, hash);
			} else {
				localResourceInfoData.SetVersionCode (localId, verCode);
				localResourceInfoData.SetCRC (localId, crc);
				localResourceInfoData.SetHashCode (localId, hash);
			}


			StringBuilder strBuilder = localResourceInfoData.GetTotalString ();
			FileStream assetFile = File.Open (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv", FileMode.Create);
			StreamWriter sw = new StreamWriter (assetFile, Encoding.UTF8);
			sw.Write (strBuilder);
			sw.Close ();
			assetFile.Close ();

		}

		/// <summary>
		/// Decompresses the file LZM.
		/// 解压文件
		/// </summary>
		/// <param name="inFile">In file.</param>
		/// <param name="outFile">Out file.</param>
		private  void DecompressFileLZMA (string fileName, string inFile, string outFile)
		{
			Debug.Log ("DecompressFileLZMA " + inFile);
			SevenZip.Compression.LZMA.Decoder coder = new SevenZip.Compression.LZMA.Decoder ();
			FileStream input = new FileStream (inFile, FileMode.Open);
			FileStream output = new FileStream (outFile, FileMode.Create);
			
			// Read the decoder properties
			byte[] properties = new byte[5];
			input.Read (properties, 0, 5);
			
			// Read in the decompress file size.
			byte[] fileLengthBytes = new byte[8];
			input.Read (fileLengthBytes, 0, 8);
			long fileLength = BitConverter.ToInt64 (fileLengthBytes, 0);
			
			// Decompress the file.
			coder.SetDecoderProperties (properties);
			coder.Code (input, output, input.Length, fileLength, null);
			output.Flush ();
			output.Close ();
			input.Close ();

			/*回到主线程*/
			Loom.QueueOnMainThread(()=>{
				FinishDecompressFile(fileName);
			});
		}

		/// <summary>
		/// 把网络下载过来的文件 改成可以正式使用的文件 
		/// </summary>
		private void ChangeFileToUse ()
		{
			if (isDownloadingFile)
				return;

			ResourcesInfoData localResourceInfoData = new ResourcesInfoData ();
			if (File.Exists (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv")) {
				localResourceInfoData.InitFromPath (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv");
			} else {
				localResourceInfoData.InitFromResource ();
			}

			int dataRow = localResourceInfoData.GetDataRow ();
			for (int i = 1; i <= dataRow; ++i) {
				string bundleName = localResourceInfoData.GetBundleName (i);
				string path = AssetBundleFilePath.ServerExtensionDataPath + bundleName + ".temp";
				if (File.Exists (path)) {
					string outFile = AssetBundleFilePath.ServerExtensionDataPath + bundleName;
					if (File.Exists (outFile)) {
						File.Delete (outFile);
					}
					File.Move (path, outFile);
				}
			}
		}


		/// <summary>
		///更新结束后，同步服务器上的版本号码信息 
		/// </summary>
		void UpdateResVerInfo()
		{
			ResVersionData.Instance.versionData.resVersionCode=serverVerCode;
			ResVersionData.Instance.versionData.resVersionName=serverVerName;
			ResVersionData.Instance.SaveData();
		}
			

		#endregion

		#region  判断资源完整性

		public bool IsAllAssetsIsReady()
		{
			ResourcesInfoData localResourceInfoData = new ResourcesInfoData ();
			if (File.Exists (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv")) {
				localResourceInfoData.InitFromPath (AssetBundleFilePath.ServerExtensionDataPath + "ResourceInfoData.csv");
			} else
			{
				return  false;
			}

			int dataRow = localResourceInfoData.GetDataRow();
			for(int i=1;i<=dataRow;++i)
			{
				string bundleName = localResourceInfoData.GetBundleName(i);
				if(! File.Exists(AssetBundleFilePath.ServerExtensionDataPath+bundleName))
				{
					return false;
				}
			}
			return true;
		}
		#endregion


		#region 结束热更新过程


		/// <summary>
		/// 结束游戏资源更新 开始加载游戏
		/// </summary>
		private void EndHotFixUpdate()
		{
			ChangeFileToUse ();
			if (IsAllAssetsIsReady ()) {
			  
				//ResourcesManager.Instance.curResType = ResourcesManager.ResType.Assets;
			} else {
				//ResourcesManager.Instance.curResType = ResourcesManager.ResType.Resources;
			}

//			ResourcesUpdatePanel.Instance.gameObject.SetActive (false);
//			GameStart.Instance.StartLoading ();


			Debug.Log ("End HotFix");
		}

		#endregion


	}
}