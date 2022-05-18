using API.Logging;
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
			RemoveRowCommand = new LambdaCommand(OnRemoveRowCommand, CanRemoveRowCommand);
			#endregion
		}

		public void Table_RowDeleted(object sender, DataRowChangeEventArgs e)
		{
			string sql = $"DELETE FROM [{Table.Table.TableName}] WHERE {Table.Table.Columns[0].ColumnName} = {e.Row.ItemArray[0]}";
			if (SQLQueryes.ContainsKey(e.Row.ItemArray[0].ToString())) SQLQueryes[e.Row.ItemArray[0].ToString()] = sql;
			else SQLQueryes.Add(e.Row.ItemArray[0].ToString(), sql);

			Log.WriteLine(string.Join("\n", SQLQueryes.Values));
		}

		public void Table_RowChanged(object sender, System.Data.DataRowChangeEventArgs e)
		{
			if (e.Action == DataRowAction.Commit) return;
			int index = 0;
			string sql = $"UPDATE [{Table.Table.TableName}] SET "; //UPDATE Книги SET Название = 'test12312312' WHERE ID_книга = 5;
			foreach (DataColumn i in Table.Table.Columns)
			{
				if (i.ColumnName == Table.Table.Columns[0].ColumnName) { index++; continue; }
				if (index != 1) sql += ",";
				sql += $"[{i.ColumnName}] = '{e.Row.ItemArray[index]}' ";
				index++;
			}
			sql += $"WHERE {Table.Table.Columns[0].ColumnName} = {e.Row.ItemArray[0]}";
			if (SQLQueryes.ContainsKey(e.Row.ItemArray[0].ToString())) SQLQueryes[e.Row.ItemArray[0].ToString()] = sql;
			else SQLQueryes.Add(e.Row.ItemArray[0].ToString(), sql);
			Log.WriteLine(string.Join("\n", SQLQueryes.Values));
		}

		public AdminToolChangeVM()
		{

		}
		private string _Title = "Инструмент администратора";
		public string Title { get => _Title; set => Set(ref _Title, value); }

		private int _SelectedRow = -1;
		public int SelectedRow { get => _SelectedRow; set => Set(ref _SelectedRow, value); }

		private API.TableDataBase _Table;
		public API.TableDataBase Table { get => _Table; set => Set(ref _Table, value); }


		public Dictionary<string, string> SQLQueryes = new Dictionary<string, string>();

		#region Commnads
		#region RemoveRowCommand
		public ICommand RemoveRowCommand { get; set; }
		public bool CanRemoveRowCommand(object e) => true;
		public void OnRemoveRowCommand(object e)
		{
			if (SelectedRow == -1 || Table.Table.Rows.Count - 1 < SelectedRow) return;
			var x = SelectedRow;
			Table.Table.Rows.RemoveAt(x);
		}
		#endregion
		#region AddRowCommand
		public ICommand AddRowCommand { get; set; }
		public bool CanAddRowCommand(object e) => true;
		public void OnAddRowCommand(object e)
		{
			var row = Table.Table.NewRow();
			row[Table.Table.Columns[0]] = ClientViewModel.GetID(Table.Table);
			Table.Table.Rows.Add(row);
		}
		#endregion
		#endregion
	}
}
