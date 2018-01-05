using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace BundleHotFix
{
public class EditorResourcesInfoData :CsvDataBase {

	bool isExistFile=false;
	Dictionary<string,int> idDict= new Dictionary<string, int>();

	private static EditorResourcesInfoData instance;
	public static EditorResourcesInfoData Instance
	{
		get
		{
			if(instance == null)
			{
				instance = new EditorResourcesInfoData();
			}
			return instance;
		}
	}
	private EditorResourcesInfoData()
	{

	}

	public  void Init()
	{
		string path = Application.dataPath.Replace("Assets","")  + "AssetBundles" + "/ResourceInfoData.csv";
		
		isExistFile = File.Exists(path);
		if(isExistFile)
		{
			InitDataFromPath(path);
			InitDataID();
		}
	}

	void InitDataID()
	{
		int row = GetDataRow();
		idDict.Clear();
		for(int i=1;i<=row;++i)
		{
			idDict.Add(GetProperty("bundleName",i),i);
		}
	}

	public string GetHashCode(string bundleName)
	{
		if(isExistFile==false)
		{
			return "";
		}

		if(idDict.ContainsKey(bundleName))
		{
			int id = idDict[bundleName];
			return GetProperty("hashCode",id);
		}
		return "";
	}
	public int GetVersionCode(string bundleName)
	{
		if(isExistFile==false)
		{
			return 0;
		}
		if(idDict.ContainsKey(bundleName))
		{
			int id = idDict[bundleName];
			return int.Parse( GetProperty("versionCode",id));
		}
		return 0;
	}


	public int  GetNextVersionCode(string bundleName)
	{
		if(isExistFile==false)
		{
				Debug.Log("No AssetInfo file ");
			return 1;
		}
		if(idDict.ContainsKey(bundleName))
		{
			int id = idDict[bundleName];
			return int.Parse( GetProperty("versionCode",id))+1;
		}
		Debug.Log("New Bundle");
		return 1;
	}
}
}