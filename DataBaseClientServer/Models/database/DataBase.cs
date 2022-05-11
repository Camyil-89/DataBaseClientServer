using System;
using System.Collections.Generic;
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
		public API.LibraryDataBase GetTablesDT()
		{
			API.LibraryDataBase LDB = new API.LibraryDataBase();
			DataTable dT = myConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
			foreach (DataRow row in dT.Rows)
			{
				string tbName = row[2].ToString();
				if (!tbName.Contains("MSys"))
				{
					var dt = SendQuery($"SELECT * FROM [{tbName}]");
					dt.TableName = tbName;
					if (tbName == "Хранилище книг") LDB.BookStorage = dt;
					else if (tbName == "Библиотеки") LDB.Libraries = dt;
					else if (tbName == "Авторы") LDB.Authors = dt;
					else if (tbName == "Абонементы") LDB.Subscriptions = dt;
					else if (tbName == "Выданные книги") LDB.IssuedBooks = dt;
					else if (tbName == "Должности") LDB.Positions = dt;
					else if (tbName == "Жанр авторов") LDB.GenreAuthors = dt;
					else if (tbName == "жанр книг") LDB.BookGenre = dt;
					else if (tbName == "Зарегистрированные читатели") LDB.RegisteredReaders = dt;
					else if (tbName == "Категория читателей") LDB.CategoryReaders = dt;
					else if (tbName == "Книги") LDB.Books = dt;
					else if (tbName == "обслуженные читатели") LDB.ServedReaders = dt;
					else if (tbName == "Обязанности") LDB.Responsibilities = dt;
					else if (tbName == "Полка") LDB.Shelf = dt;
					else if (tbName == "Работники") LDB.Employees = dt;
					else if (tbName == "Стелаж") LDB.Shelving = dt;
					else if (tbName == "Тип книги") LDB.BookType = dt;
					else if (tbName == "Читатели") LDB.Readers = dt;
					else if (tbName == "Читательский зал") LDB.ReadingRoom = dt;
				}
			}
			return LDB;
		}
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
		public DataTable SendQuery(string query)
		{
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
