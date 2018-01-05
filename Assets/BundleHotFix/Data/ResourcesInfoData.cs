using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BundleHotFix
{
	public class ResourcesInfoData : CsvDataBase {


		Dictionary<string,int> dicVersionCode= new Dictionary<string, int>();

		public void InitFromResource()
		{
			//从Resources 中加载数据
			TextAsset  binAsset = Resources.Load ("Data/ResourceInfoData", typeof(TextAsset)) as TextAsset;
			assetString=binAsset.text;
			LoadData();
			InitDicVerCode();
		}

		public  void InitFromString(string str)
		{
			base.InitFromString(str);
			InitDicVerCode();
		}

		public void InitFromPath(string path)
		{
			InitDataFromPath(path);
			InitDicVerCode();
		}


		void InitDicVerCode()
		{
			dicVersionCode.Clear();
			int row =GetDataRow();
			for(int i=1;i<=row;++i)
			{
				string key = GetProperty("bundleName",i);
				int v= int.Parse(GetProperty("versionCode",i));
				dicVersionCode.Add(key,v);
			}
		}


		public string GetBundleName(int id)
		{
			return GetProperty("bundleName",id).ToString();
		}
		public int GetVersionCode(int id)
		{
			return  int.Parse(GetProperty("versionCode",id));
		}
		public string GetCRC(int id)
		{
			return GetProperty("crc",id);
		}
		public string GetHashCode(int id)
		{
			return GetProperty("hashCode",id);
		}

		public int GetIDByBundleName(string bundleName)
		{
			for(int i=1;i<=GetDataRow();++i)
			{
				if(levelArray[i][1].CompareTo(bundleName)==0)
				{
					return int.Parse(levelArray[i][0]);
				}
			}
			return -1;
		}

		public int GetVersionCodeByBundleName(string bundleName)
		{
			if(dicVersionCode.ContainsKey(bundleName) ==false)
			{
				return -1;
			}
			return dicVersionCode[bundleName];
		}

		public void SetVersionCode(int id, int code)
		{
			SetProperty(id,"versionCode",code.ToString());
		}
		public void SetCRC(int id,string crc)
		{
			SetProperty(id,"crc",crc);
		}
		public void SetHashCode(int id ,string hashCode)
		{
			SetProperty(id,"hashCode",hashCode);
		}

	}
}
