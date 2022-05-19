using API.Logging;
using DataBaseClientServer.Base.Command;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

		

		public AdminToolChangeVM()
		{

		}
		private string _Title = "Инструмент администратора";
		public string Title { get => _Title; set => Set(ref _Title, value); }

		private int _SelectedRow = -1;
		public int SelectedRow { get => _SelectedRow; set => Set(ref _SelectedRow, value); }

		private API.TableDataBase _Table;
		public API.TableDataBase Table { get => _Table; set => Set(ref _Table, value); }


		public Dictionary<string, Query> SQLQueryes = new Dictionary<string, Query>();
		public Dictionary<string, Query> SQLQueryesInsert = new Dictionary<string, Query>();

		public List<Query> Queries = new List<Query>();

		/// <summary>
		/// Вызов при удалении строки
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Table_RowDeleted(object sender, DataRowChangeEventArgs e)
		{
			string sql = $"DELETE FROM [{Table.Table.TableName}] WHERE {Table.Table.Columns[0].ColumnName} = {e.Row.ItemArray[0]}";
			Query query = new Query() { SQL = sql, Command = $"DELETE FROM [{Table.Table.TableName}]" };
			if (SQLQueryes.ContainsKey(e.Row.ItemArray[0].ToString()))
			{
				Queries.Remove(SQLQueryes[e.Row.ItemArray[0].ToString()]);
				SQLQueryes[e.Row.ItemArray[0].ToString()] = query;
			}
			else SQLQueryes.Add(e.Row.ItemArray[0].ToString(), query);
			Queries.Add(query);
			Log.WriteLine($"DELETE>{string.Join("\n", Queries)}");
		}
		/// <summary>
		/// Вызов при изменении строки
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
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
			Query query = new Query() { SQL = sql, Command = $"UPDATE [{Table.Table.TableName}]" };
			if (SQLQueryes.ContainsKey(e.Row.ItemArray[0].ToString()))
			{
				Queries.Remove(SQLQueryes[e.Row.ItemArray[0].ToString()]);
				SQLQueryes[e.Row.ItemArray[0].ToString()] = query;
			}
			else SQLQueryes.Add(e.Row.ItemArray[0].ToString(),query);
			Queries.Add(query);
			Log.WriteLine(string.Join("\n", Queries));
		}

		#region Commnads
		#region RemoveRowCommand
		/// <summary>
		/// Удаление строки в БД
		/// </summary>
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
		/// <summary>
		/// Добавление строки в БД
		/// </summary>
		public ICommand AddRowCommand { get; set; }
		public bool CanAddRowCommand(object e) => true;
		public void OnAddRowCommand(object e)
		{
			Query query = new Query() { Command = $"INSERT INTO [{Table.Table.TableName}]" };
			Queries.Add(query);
			var row = Table.Table.NewRow();
			foreach (DataColumn i in Table.Table.Columns) row[i] = DBNull.Value;
			row[Table.Table.Columns[0]] = ClientViewModel.GetID(Table.Table);
			Table.Table.Rows.Add(row);
			string sql = $"INSERT INTO [{Table.Table.TableName}](";//$"INSERT INTO таблица(название, размер) VALUES('Михаил', 20);"
			int last = 1;
			foreach (DataColumn i in Table.Table.Columns)
			{
				if (Table.Table.Columns.Count == last) sql += $"[{i.ColumnName}])";
				else sql += $"[{i.ColumnName}],";
				last++;
			}
			sql += $" VALUES(";
			last = 1;
			foreach (var i in row.ItemArray)
			{
				if (last == row.ItemArray.Length)
				{
					if (i == DBNull.Value) sql += $"NULL);";
					else sql += $"{i});";
				}
				else
				{
					if (i == DBNull.Value) sql += $"NULL,";
					else sql += $"{i},";
				}
				last++;
			}
			query.SQL = sql;
			SQLQueryesInsert.Add(row[Table.Table.Columns[0]].ToString(), query);
			
			Log.WriteLine(string.Join("\n", Queries));
		}
		#endregion
		#endregion
	}
	/// <summary>
	/// Информация о запросах
	/// </summary>
	public class Query
	{
		public string SQL { get; set; }
		public string Command { get; set; }

		public override string ToString()
		{
			return $"{SQL}";
		}
	}
}
