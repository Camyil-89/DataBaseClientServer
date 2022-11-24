using DataBaseClientServer.Base.Command;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DataBaseClientServer.ViewModels
{

	class SqlQueryWindowVM : Base.ViewModel.BaseViewModel
	{
		public SqlQueryWindowVM()
		{
			#region Commands
			SendQueryCommand = new LambdaCommand(OnSendQueryCommandExecuted, CanSendQueryCommandExecute);
			#endregion
		}

		private Models.Client _Client;
		public Models.Client Client { get => _Client; set => Set(ref _Client, value); }

		#region SqlQueryText: Description
		/// <summary>Description</summary>
		private string _SqlQueryText;
		/// <summary>Description</summary>
		public string SqlQueryText { get => _SqlQueryText; set => Set(ref _SqlQueryText, value); }
		#endregion

		#region Parametrs
		private DataTable _Table;
		public DataTable Table { get => _Table; set => Set(ref _Table, value); }
		#endregion

		#region Commands

		#region SendQueryCommand: Description
		//SendQueryCommand = new LambdaCommand(OnSendQueryCommandExecuted, CanSendQueryCommandExecute);
		public ICommand SendQueryCommand { get; set; }
		private bool CanSendQueryCommandExecute(object e) => !string.IsNullOrEmpty(SqlQueryText);
		private void OnSendQueryCommandExecuted(object e)
		{
			try
			{
				var packet = Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.SQLQuery, Data = new API.SQLQueryPacket() { TableName = null, Data = SqlQueryText, TypeSQLQuery = API.TypeSQLQuery.BroadcastMe } }, 1).Packets[0];
				Console.WriteLine(packet);
				if (packet.TypePacket == API.TypePacket.SQLQueryOK)
				{
					Table = (DataTable)packet.Data;
					Console.WriteLine(Table == null);
					MessageBox.Show($"Транзакция успешно выполнена!", "Уведомление");
				}
				else if (packet.TypePacket == API.TypePacket.SQLQueryDenay) MessageBox.Show($"У вас недостаточно прав для выполнения данной операции!");
				else if (packet.TypePacket == API.TypePacket.SQLQueryError) MessageBox.Show($"Произошла ошибка на стороне сервера!\n{packet.Data}");
				else MessageBox.Show($"Сервер вернул что то непонятное:(", "Ошибка");
			}
			catch (Exception er) { MessageBox.Show($"Произошла непредвиденная ошибка!\n{er}", "Ошибка"); }
		}
		#endregion
		#endregion

		#region Functions
		#endregion
	}
}
