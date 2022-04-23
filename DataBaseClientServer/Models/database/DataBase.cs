using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseClientServer.Models.database
{
	public class DataBase
	{
		public string Path { get; set; } = "Database.mdb";
		public string Provider = "Provider=Microsoft.Jet.OLEDB.4.0";
		private OleDbConnection myConnection;

		public void Connect()
		{
			myConnection = new OleDbConnection($"{Provider};Data Source={Path};");
			myConnection.Open();
			Log.WriteLine($"Connect database {Provider};Data Source={Path};");
		}
		public void SendQuery(string query)
		{
			OleDbDataAdapter myDataAdapter = new System.Data.OleDb.OleDbDataAdapter(query, myConnection);
			DataSet myDataSet = new DataSet("Table");
			myDataAdapter.Fill(myDataSet, "Table");
			foreach (DataTable i in myDataSet.Tables)
			{
				Console.WriteLine(i);
				foreach (DataRow j in i.Rows)
				{
					Console.WriteLine($"{string.Join(";" ,j.ItemArray)}");
				}
			}
		}
	}
}
