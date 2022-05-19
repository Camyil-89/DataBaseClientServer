using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Logging;

namespace DataBaseClientServer.Models.database
{
	public class DataBase
	{
		public string Path { get; set; } = "Database.mdb";
		public string Provider = "Provider=Microsoft.Jet.OLEDB.4.0";
		private OleDbConnection myConnection;
		/// <summary>
		/// Подключение БД
		/// </summary>
		/// <returns></returns>
		public bool Connect()
		{
			try
			{
				myConnection = new OleDbConnection($"{Provider};Data Source={Path};");
				myConnection.Open();
				Log.WriteLine($"Connect database {Provider};Data Source={Path};");
				return true;
			} catch (Exception e) { Log.WriteLine($"Error connect database: {e}"); return false; }
			
		}
		/// <summary>
		/// Получение таблиц из БД
		/// </summary>
		/// <returns></returns>
		public ObservableCollection<API.TableDataBase> GetTablesDT()
		{
			ObservableCollection<API.TableDataBase> tablesDataBase = new ObservableCollection<API.TableDataBase>();
			DataTable dT = myConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
			foreach (DataRow row in dT.Rows)
			{
				string tbName = row[2].ToString();
				if (!tbName.Contains("MSys"))
				{
					var dt = SendQuery($"SELECT * FROM [{tbName}]");
					dt.TableName = tbName;
					tablesDataBase.Add(new API.TableDataBase() { Table = dt});
				}
			}
			return tablesDataBase;
		}
		/// <summary>
		/// получение названий таблиц
		/// </summary>
		/// <returns></returns>
		public List<string> GetTables()
		{
			List<string> DtataBaseNames = new List<string>();
			DataTable dT = myConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
			foreach (DataRow row in dT.Rows)
			{
				string tbName = row[2].ToString();
				if (!tbName.Contains("MSys")) DtataBaseNames.Add(tbName);
			}
			return DtataBaseNames;
		}
		/// <summary>
		/// отправка в БД sql запроса
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public DataTable SendQuery(string query)
		{
			Log.WriteLine(query);
			OleDbDataAdapter myDataAdapter = new System.Data.OleDb.OleDbDataAdapter(query, myConnection);
			DataTable dataTable = new DataTable();
			myDataAdapter.Fill(dataTable);
			return dataTable;
			//foreach (DataTable i in myDataSet.Tables)
			//{
			//	Console.WriteLine(i);
			//	foreach (DataRow j in i.Rows)
			//	{
			//		Console.WriteLine($"{string.Join(";" ,j.ItemArray)}");
			//	}
			//}
		}
	}
}
