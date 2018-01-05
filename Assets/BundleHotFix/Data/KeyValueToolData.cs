using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;


/// <summary>
/// 简单键值对数据的存放 
/// 替换掉 PlayerPerfab 
/// </summary>
public class KeyValueToolData :SingletonBase<KeyValueToolData> {

	private const string fileName="keyValueToolData.json";
	protected static Dictionary<string, object> miniJsonData = new Dictionary<string, object>();			//当前的json数据.

	public string GetString(string key,string defautValue="")
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			return miniJsonData[key].ToString();
		}else
		{
			return defautValue;
		}
	}

	public void SetString(string key ,string value)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			 miniJsonData[key]=value;
		}else
		{
			 miniJsonData.Add(key,value);
		}

		Write();
	}
	public int GetInt(string key,int defautValue=0)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			return  int.Parse( miniJsonData[key].ToString() );
		}else
		{
			return defautValue;
		}
	}

	public void SetInt(string key ,int value)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			miniJsonData[key]=value;
		}else
		{
			miniJsonData.Add(key,value);
		}

		Write();
	}



	public long GetLong(string key,long defautValue=0)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			return  long.Parse( miniJsonData[key].ToString() );
		}else
		{
			return defautValue;
		}
	}

	public void SetLong(string key ,long value)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			miniJsonData[key]=value;
		}else
		{
			miniJsonData.Add(key,value);
		}

		Write();
	}


	public float GetFloat(string key,float defautValue=0f)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			return  float.Parse( miniJsonData[key].ToString());
		}else
		{
			return defautValue;
		}
	}

	public void SetFloat(string key ,float value)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			miniJsonData[key]=value;
		}else
		{
			miniJsonData.Add(key,value);
		}

		Write();
	}

	public bool GetBool(string key,bool defautValue=false)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			return  bool.Parse( miniJsonData[key].ToString() );
		}else
		{
			return defautValue;
		}
	}

	public void SetBool(string key ,bool value)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			miniJsonData[key]=value;
		}else
		{
			miniJsonData.Add(key,value);
		}

		Write();
	}



	public bool IsExistKey(string key)
	{
		Read();
		if (miniJsonData.ContainsKey (key)) {
			return true;
		} else {
			return false;
		}
	}

	public void RemoveKey(string key)
	{
		Read();
		if(miniJsonData.ContainsKey(key))
		{
			miniJsonData.Remove (key);
			Write ();
		}

	}


	private void Read()
	{
		if(!FileTool.IsFileExists(fileName))
		{
			FileTool.createORwriteFile(fileName,"");
		}
		string content= FileTool.ReadFile(fileName);

		if(!string.IsNullOrEmpty(content))
            miniJsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
	
		if(miniJsonData == null)
			miniJsonData = new Dictionary<string, object>();
	}

	private void Write()
	{
        string jsonStr = JsonConvert.SerializeObject(miniJsonData);
		FileTool.createORwriteFile(fileName, jsonStr);
	}
}
