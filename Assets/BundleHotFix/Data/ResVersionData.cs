using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class ResVersionData : IJsonData<ResVersionData> {


	public ResVersionDataParam versionData;

	public ResVersionData()
	{
		string settingDataStr = InitData("ResVersionData", true, false);
		this.versionData = JsonConvert.DeserializeObject<ResVersionDataParam>(settingDataStr);
	}

	public void SaveData()
	{
		base.SaveData( JsonConvert.SerializeObject(this.versionData) );
	}


	public class ResVersionDataParam{
		public int resVersionCode;
		public string resVersionName;
	}
}
