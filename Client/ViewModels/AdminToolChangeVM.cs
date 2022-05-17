using DataBaseClientServer.Base.Command;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DataBaseClientServer.ViewModels
{
	public class AdminToolChangeVM: Base.ViewModel.BaseViewModel
	{
		public readonly ClientViewModel ClientViewModel;
		public Client.Views.Windows.AdminToolChangeWindow Window { get; set; }
		public AdminToolChangeVM(ClientViewModel clientViewModel)
		{
			ClientViewModel = clientViewModel;
			#region Commands
			AddRowCommand = new LambdaCommand(OnAddRowCommand, CanAddRowCommand);
			#endregion
		}

		public void Table_RowChanged(object sender, System.Data.DataRowChangeEventArgs e)
		{
			int index = 0;
			string sql = $"UPDATE [{Table.Table.TableName}] SET "; //UPDATE Книги SET Название = 'test12312312' WHERE ID_книга = 5;
			foreach (DataColumn i in Table.Table.Columns)
			{
				if (index != 0) sql += ",";
				
				sql += $"[{i.ColumnName}] = '{e.Row.ItemArray[index]}' ";
				index++;
			}
			sql += $"WHERE {e.Row.ItemArray[0]} == {e.Row.ItemArray[0]};";
			if (SQLQueryes.ContainsKey(e.Row.ItemArray[0].ToString())) SQLQueryes[e.Row.ItemArray[0].ToString()] = sql;
			else SQLQueryes.Add(e.Row.ItemArray[0].ToString(), sql);
			Console.WriteLine(string.Join("\n", SQLQueryes.Values));
			Console.WriteLine($"{string.Join(";", e.Row.ItemArray)}");
		}

		public AdminToolChangeVM()
		{

		}
		private string _Title = "Инструмент администратора";
		public string Title { get => _Title; set => Set(ref _Title, value); }

		private API.TableDataBase _Table;
		public API.TableDataBase Table { get => _Table; set => Set(ref _Table, value); }


		public Dictionary<string, string> SQLQueryes = new Dictionary<string, string>();

		#region Commnads

		#region AddRowCommand
		public ICommand AddRowCommand { get; set; }
		public bool CanAddRowCommand(object e) => true;
		public void OnAddRowCommand(object e)
		{
			Console.WriteLine(Table.Table.TableName);
			
		}
		#endregion
		#endregion
	}
}
