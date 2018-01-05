using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 数据读取的基类
/// </summary>
using System.Text;
using BundleHotFix;

namespace BundleHotFix
{
	public class CsvDataBase
	{
		//存数数据
		protected Hashtable DataTable = new Hashtable();
		protected List<List<string>> levelArray = new List<List<string>> ();
	
		int DataRow=0;
		protected	string assetString = "";
		static AssetBundle dataBundle = null;

		#region 初始化

		//设置读取数据表的名称
		protected void InitData (string fileName)
		{
//			assetString = LoadAssetsManager.Instance.GetConfigData (fileName);
//			LoadData ();
		}

		protected void InitDataFromPath (string path)
		{
			FileStream fs = new FileStream (path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
			StreamReader sr = new StreamReader (fs, Encoding.UTF8);
			assetString = sr.ReadToEnd ();
			sr.Close ();
			fs.Close ();
			LoadData ();
		}

	
		protected void InitFromString (string str)
		{
			assetString = str;
			LoadData ();
		}

		protected void LoadData ()
		{
			levelArray.Clear ();
			DataTable = new Hashtable ();
			//读取每一行的内容
			string[] lineArray = assetString.Split ('\r');
			//创建二维数组
			for (int n = 0; n < lineArray.Length; ++n)
				levelArray.Add (new List<string> ());
			//把csv中的数据储存在二位数组中
			for (int i = 0; i < lineArray.Length; i++) {
				levelArray [i].AddRange (lineArray [i].Split (','));
			}
			//将数据存储到哈希表中，存储方法：Key为name+id，Value为值
			int nRow = levelArray.Count;
			int nCol = levelArray [0].Count;
		
			DataRow = nRow - 1;
		
			for (int i = 1; i < nRow; ++i) {
				if (levelArray [i] [0] == "\n" || levelArray [i] [0] == "") {
					nRow--;
					DataRow = nRow - 1;
					continue;
				}
			
				string id = levelArray [i] [0].Trim ();
			
				for (int j = 1; j < nCol; ++j) {  
					DataTable.Add (levelArray [0] [j] + "_" + id, levelArray [i] [j]);
				}
			}
		
		}

		#endregion

		#region 读取数据

		/// <summary>
		/// Gets the data row.
		/// 返回表格的行数
		/// </summary>
		/// <returns>
		/// The data row.
		/// </returns>
		public int GetDataRow ()
		{
			return DataRow;
		}
	
		//根据name和id获取相关属性，返回string类型
		protected virtual string GetProperty (string name, int id)
		{
			return GetProperty (name, id.ToString ());
		}

		protected virtual string GetProperty (string name, string id)
		{
			string key = name + "_" + id;
			if (DataTable.ContainsKey (key))
				return DataTable [key].ToString ();
			else
				return "";
		}

		public List<string> GetRowData (int index)
		{
			return levelArray [index];
		}

		#endregion

		#region  写入数据

		public void SetProperty (int row, int col, string strValue)
		{
			if (row < 0 || row >= levelArray.Count || col < 0 || col >= levelArray [0].Count) {
				Debug.Log ("out of range");
				return;
			}
		
			levelArray [row] [col] = strValue;
			string key = levelArray [0] [col] + "_" + levelArray [row] [0];
			DataTable [key] = strValue;
		}

		public void SetProperty (int id, string property, string strValue)
		{
			int col = -1;
			for (int i = 0; i < levelArray [0].Count; ++i) {
				if (property.CompareTo (levelArray [0] [i]) == 0) {
					col = i;
					break;
				}
			}
		
			int row = -1;
			for (int j = 1; j < levelArray.Count; ++j) {
				if (id.ToString ().CompareTo (levelArray [j] [0]) == 0) {
					row = j;
					break;
				}
			}
		
			if (row < 0 || col < 0) {
				Debug.Log ("not id or property");
				return;
			}
			SetProperty (row, col, strValue);
		}

		/// <summary>
		/// 添加一行数据 不带ID
		/// </summary>
		/// <param name="rowData">Row data.</param>
		public void AddRowWithoutId (params  string[] rowData)
		{
			int id =DataRow + 1;
			List<string> dataList = new List<string> ();
			dataList.Add (id.ToString ());
			for (int i = 0; i < rowData.Length; ++i) {
				dataList.Add (rowData [i]);
				string key = levelArray [0] [i + 1] + "_" + id;
				DataTable.Add (key, rowData [i]);
			}
			levelArray.Add (dataList);
			DataRow++;
		}

		public void AddRowData (List<string> dataList, int index = -1)
		{
			if (index == -1) {
				levelArray.Add (dataList);
			} else {
				levelArray.Insert (index, dataList);
			}
		}

		/// <summary>
		/// 把csv 表格序列化成字符串
		/// </summary>
		/// <returns>The total string.</returns>
		public StringBuilder GetTotalString ()
		{
			StringBuilder strBuilder = new StringBuilder ();
			for (int i = 0; i < levelArray.Count; ++i) {
				
				if (levelArray [i].Count != levelArray [0].Count)
					continue;

				for (int j = 0; j < levelArray [0].Count; ++j) {
					//Debug.Log (i + " " + j);
					strBuilder.Append (levelArray [i] [j]);
					if (j < levelArray [0].Count - 1)
						strBuilder.Append (',');
				}
			
				if (i < levelArray.Count - 1)
					strBuilder.Append ('\r');
			}
			return strBuilder;
		}

		#endregion

		public void JustInit ()
		{
		}
	}
}
