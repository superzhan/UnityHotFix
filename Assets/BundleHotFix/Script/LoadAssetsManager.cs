using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace BundleHotFix
{
	/// <summary>
	///  热更新的资源管理类
	/// </summary>
	public class LoadAssetsManager : SingletonBase<LoadAssetsManager>  {

		//是否从AssetBundle 中加载资源
		private static  bool isLoadFromBundle=true;
		private static Dictionary<string ,AssetBundle> bundleDic =new Dictionary<string, AssetBundle>();

		private  Dictionary<string, bool> bundleTag = new Dictionary<string, bool>();

		#region  AssetBundle 加载

		//加载依赖信息的Bundle
		private  AssetBundleManifest _BundleManifest=null;
		private AssetBundle totalAssetBundle=null;
		public AssetBundleManifest BundleManifest{
			get{
				if(_BundleManifest!=null)
					return _BundleManifest;
				try{
					string path = AssetBundleFilePath.ServerExtensionDataPath+GetPlatformFolderForAssetBundles();
					if(totalAssetBundle==null)
					{
					    totalAssetBundle = AssetBundle.LoadFromFile(path);
					}
					UnityEngine.Object obj = totalAssetBundle.LoadAsset("AssetBundleManifest");
					_BundleManifest=obj as AssetBundleManifest;
					return _BundleManifest;
				}catch(Exception e)
				{
					Debug.Log(e.Message+'\n'+ e.StackTrace);
					return null;
				}
			}
		}

		public AssetBundle GetAssetBundleByName(string bundleName)
		{
			if(isLoadFromBundle==false)
				return null;
			
			bundleTag.Clear();
			return	Instance.GetAssetBundle(bundleName);
		}

		/// <summary>
		/// Gets the asset bundle.
		/// 根据名称从文件中创建AssetBundle
		/// </summary>
		/// <returns>The asset bundle.</returns>
		/// <param name="bundleName">Bundle name.</param>
		private AssetBundle GetAssetBundle(string bundleName)
		{
			if(bundleTag.ContainsKey(bundleName))
				return null;
			bundleTag.Add(bundleName,true);

			if( bundleDic.ContainsKey(bundleName))
			{
				return bundleDic[bundleName];
			}

			string path = AssetBundleFilePath.ServerExtensionDataPath+bundleName;
			//Debug.Log(path);
			if(File.Exists(path))
			{
				try{
					//load dependen
					string[] dependens=Instance.BundleManifest.GetAllDependencies(bundleName);
					for(int i=0; i<dependens.Length;++i)
					{
						string dependenName = dependens[i];
						Instance.GetAssetBundle(dependenName); //递归加载
					}

					AssetBundle bundle = AssetBundle.LoadFromFile(path);
					bundleDic.Add(bundleName,bundle);
	
					return bundle;
				}catch(Exception e)
				{
					Debug.Log(e.Message + '\n'+ e.StackTrace);
					return null;
				}
			}else{
				return null;
			}
		}

		public string[] GetAllPrefabNameInBundle(string bundleName)
		{
			AssetBundle asset = Instance.GetAssetBundleByName(bundleName);
			if(asset==null)
			{
				return null;
			}

			string[] names= asset.GetAllAssetNames();
			if(names==null || names.Length<1)
				return names;

			for(int i=0;i<names.Length;++i)
			{
				int index = names[i].LastIndexOf('/');
				names[i]=names[i].Substring(index+1);
				names[i]=names[i].Replace(".prefab","");
			}
			return names;
		}

		public UnityEngine.Object LoadAsset(string bundleName,string assetName)
		{
			//Debug.Log(bundleName +" "+assetName);
			AssetBundle assetBundle = Instance.GetAssetBundleByName(bundleName);
			UnityEngine.Object obj = assetBundle.LoadAsset(assetName);
			return obj;
		}


		public void UnLoadAssetBundle(string bundleName)
		{
			if(bundleDic.ContainsKey(bundleName) && bundleDic[bundleName]!=null)
			{
				bundleDic[bundleName].Unload(false);
				bundleDic.Remove(bundleName);
			}
		}

		public void UnLoadWithDependen(string bundleName)
		{
			string[] dependens= Instance.BundleManifest.GetAllDependencies(bundleName);
			for(int i=0; i<dependens.Length;++i)
			{
				string dependenName = dependens[i];
				if(bundleDic.ContainsKey(dependenName) && bundleDic[dependenName]!=null)
				{
					bundleDic[dependenName].Unload(false);
				}
			}

			if(bundleDic.ContainsKey(bundleName) && bundleDic[bundleName]!=null)
			{
				bundleDic[bundleName].Unload(false);
			}
		}

		public void UnLoadAllBundle()
		{
			foreach(AssetBundle bundle in bundleDic.Values)
			{
				if(bundle!=null)
				{
					bundle.Unload(true);
				}
			}
			bundleDic.Clear();

			if( totalAssetBundle !=null)
				totalAssetBundle.Unload(true);

			totalAssetBundle = null;
			_BundleManifest=null;

		}
		#endregion


		#region 加载CSV 数据
		private const string csvBundleName = "resources.data.csv.n";
		public TextAsset GetCSVData(string fileName)
		{
			TextAsset binAsset= LoadAssetsManager.Instance.LoadAsset(csvBundleName, fileName +".csv") as TextAsset;

			if (binAsset == null) {
				Debug.LogError ("not found "+ fileName+" in "+ csvBundleName);
			}
			return binAsset;
		}

		#endregion

		/// <summary>
		/// 获取总AssetBundle 的路径
		/// </summary>
		/// <returns>The platform folder for asset bundles.</returns>
		string GetPlatformFolderForAssetBundles()
		{
			
			switch(Application.platform)
			{
			case RuntimePlatform.Android:
				return "Android";
			case RuntimePlatform.IPhonePlayer:
				return "iOS";
			case RuntimePlatform.WindowsPlayer:
				return "Windows";
			case RuntimePlatform.OSXPlayer:
				return "OSX";
			case RuntimePlatform.WindowsEditor:
			case RuntimePlatform.OSXEditor:
				#if UNITY_ANDROID
				return "Android";
				#else
				return "IOS";
				#endif
			default:
				return null;
			}
		}
	}
}
