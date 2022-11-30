using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API;
using API.Logging;
using MySql.Data.MySqlClient;

namespace DataBaseClientServer.Models.database
{
	/// <summary>
	/// интерфейс для взаимодействия с базой данных (так как их 2 (sql и access))
	/// </summary>
	public interface IDataBase
	{
		bool Connect();
		ObservableCollection<API.TableDataBase> GetTablesDT();
		List<string> GetTables();
		DataTable SendQuery(string query);
		void SetPath(string path);
	}
	/// <summary>
	/// класс для взаимодействия с базой sql
	/// </summary>
	public class SQLDataBase : IDataBase
	{
		private static MySqlConnection _connection;
		public const string ConnectionString = "server=localhost;port=3306;username=webapi;password=!1N7XmccClyGXMOb;database=библиотечный фонд";
		/// <summary>
		/// Подключение к phpadmin
		/// </summary>
		/// <returns></returns>
		public bool Connect()
		{
			try
			{
				Log.WriteLine($"connecting...");
				_connection = new MySqlConnection(ConnectionString);
				_connection.Open();
				Log.WriteLine($"connect");
				return true;
			}
			catch { return false; }
		}
		/// <summary>
		/// Получение таблиц
		/// </summary>
		/// <returns></returns>
		public List<string> GetTables()
		{
			List<string> tables = new List<string>();
			DataTable dt = _connection.GetSchema("Tables");
			foreach (DataRow row in dt.Rows)
			{
				string tablename = (string)row[2];
				tables.Add(tablename);
			}
			return tables;
		}
		/// <summary>
		/// Получение таблиц в определенном формате
		/// </summary>
		/// <returns></returns>
		public ObservableCollection<TableDataBase> GetTablesDT()
		{
			ObservableCollection<API.TableDataBase> tablesDataBase = new ObservableCollection<API.TableDataBase>();
			var list = GetTables();
			foreach (var i in list)
			{
				var dt = SendQuery($"SELECT * FROM `{i}`");
				dt.TableName = i.ToLower();
				tablesDataBase.Add(new API.TableDataBase() { Table = dt });
			}
			return tablesDataBase;
		}
		/// <summary>
		/// отправка запросов
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public DataTable SendQuery(string query)
		{
			var _query = query.Replace("[", "`").Replace("]", "`").Replace("Тип книги", "тип книги");
			Console.WriteLine(_query);
			MySqlCommand command = new MySqlCommand(_query, _connection);
			MySqlDataAdapter dtb = new MySqlDataAdapter();
			dtb.SelectCommand = command;
			DataTable dtable = new DataTable();
			dtb.Fill(dtable);
			return dtable;
		}

		public void SetPath(string path)
		{
			
		}
	}
	public class DataBase: IDataBase
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
		}

		public void SetPath(string path)
		{
			Path = path;
		}
	}
}
