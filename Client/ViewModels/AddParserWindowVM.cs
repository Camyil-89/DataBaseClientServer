using Client.Views.Windows;
using DataBaseClientServer.Base.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace DataBaseClientServer.ViewModels
{

	internal class AddParserWindowVM : Base.ViewModel.BaseViewModel
	{
		public AddParserWindowVM()
		{
			#region Commands
			ExitCommand = new LambdaCommand(OnExitCommandExecuted, CanExitCommandExecute);
			AddCommand = new LambdaCommand(OnAddCommandExecuted, CanAddCommandExecute);
			#endregion
		}

		#region Parametrs

		private string field_sql = "";

		private bool IsEmpryField = false;

		public bool AddToDataBase = false;

		#region Window: Description
		/// <summary>Description</summary>
		private AddParserWindow _Window;
		/// <summary>Description</summary>
		public AddParserWindow Window { get => _Window; set => Set(ref _Window, value); }
		#endregion

		#region Table: Description
		/// <summary>Description</summary>
		private DataTable _Table;
		/// <summary>Description</summary>
		public DataTable Table { get => _Table; set => Set(ref _Table, value); }
		#endregion


		#region Blocks: Description
		/// <summary>Description</summary>
		private ObservableCollection<StackPanel> _Blocks = new ObservableCollection<StackPanel>();
		/// <summary>Description</summary>
		public ObservableCollection<StackPanel> Blocks { get => _Blocks; set => Set(ref _Blocks, value); }
		#endregion

		private Models.Client _Client;
		public Models.Client Client { get => _Client; set => Set(ref _Client, value); }

		#endregion

		#region Commands

		#region ExitCommand: Description
		//ExitCommand = new LambdaCommand(OnExitCommandExecuted, CanExitCommandExecute);
		public ICommand ExitCommand { get; set; }
		private bool CanExitCommandExecute(object e) => true;
		private void OnExitCommandExecuted(object e)
		{
			Window.Close();
		}
		#endregion

		#region AddCommand: Description
		//AddCommand = new LambdaCommand(OnAddCommandExecuted, CanAddCommandExecute);
		public ICommand AddCommand { get; set; }
		private bool CanAddCommandExecute(object e) => true;
		private void OnAddCommandExecuted(object e)
		{
			//INSERT INTO `книги` (`ID_книга`, `Название`, `Рейтинг`, `ID_автор`, `ID_тип`) VALUES (NULL, 'test', '4', '2', '1');
			string sql = $"INSERT INTO `{Table.TableName}` {field_sql} VALUES (NULL ";

			foreach (var panel in Blocks)
			{
				foreach (UIElement child in panel.Children)
				{
					if (child.GetType() == typeof(TextBox))
					{
						if ((child as TextBox).Text == "" && !IsEmpryField)
						{
							MessageBox.Show("У вас не все поля заполнены!", "Уведомление");
							return;
						}
						sql += $",'{(child as TextBox).Text}'";
					}
					else if (child.GetType() == typeof(ComboBox))
					{
						if ((child as ComboBox).SelectedItem == null)
						{
							MessageBox.Show("У вас не все поля заполнены!", "Уведомление");
							return;
						}
						sql += $",'{(child as ComboBox).SelectedItem.ToString().Split('|')[0].Trim()}'";
					}
				}
			}
			sql += ");";
			try
			{
				var packet = Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.SQLQuery, Data = new API.SQLQueryPacket() { TableName = null, Data = sql, TypeSQLQuery = API.TypeSQLQuery.Broadcast } }, 1).Packets[0];
				if (packet.TypePacket == API.TypePacket.SQLQueryOK)
				{
					MessageBox.Show($"Транзакция успешно выполнена!", "Уведомление");
					AddToDataBase = true;
					Window.Close();
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
		public void Generate()
		{
			field_sql = "(";
			bool first = true;
			foreach (DataColumn i in Table.Columns)
			{
				if (first)
				{
					first = false;
					field_sql += $"`{i.ColumnName}`";
					continue;
				}
				else
					field_sql += $",`{i.ColumnName}`";


				if (i.ColumnName == "ID_тип" && i.Table.TableName != "тип книги")
					CreateComboBox(i.ColumnName, $"SELECT * FROM `тип книги`");
				else if (i.ColumnName == "ID_автор" && i.Table.TableName != "авторы")
					CreateComboBox(i.ColumnName, $"SELECT `ID_автор`, CONCAT(`Фамилия`, ' ', `Имя`, ' ', `Отчество`) AS ФИО FROM `авторы`");
				else
					CreateTextBox(i.ColumnName);

			}
			field_sql += ")";
		}
		private DataTable GetTable(string sql)
		{
			try
			{
				var packet = Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.SQLQuery, Data = new API.SQLQueryPacket() { TableName = null, Data = sql, TypeSQLQuery = API.TypeSQLQuery.BroadcastMe } }, 1).Packets[0];
				if (packet.TypePacket == API.TypePacket.SQLQueryOK)
					return (DataTable)packet.Data;
			}
			catch (Exception er) { MessageBox.Show($"Произошла непредвиденная ошибка!\n{er}", "Ошибка"); }
			return null;
		}
		private void CreateComboBox(string name, string sql)
		{

			var panel = new StackPanel();
			panel.Orientation = Orientation.Horizontal;
			panel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
			panel.Margin = new System.Windows.Thickness(5);

			var comboBox = new ComboBox();
			comboBox.Width = 150;
			comboBox.FontSize = 14;
			comboBox.Tag = name;

			var table = GetTable(sql);
			if (table == null)
			{
				Window.Close();
				return;
			}

			foreach (DataRow i in table.Rows)
				comboBox.Items.Add(string.Join(" | ", i.ItemArray));

			TextBlock textBlock = new TextBlock();
			textBlock.Text = name;
			textBlock.FontSize = 14;
			textBlock.Margin = new System.Windows.Thickness(0, 0, 10, 0);


			panel.Children.Add(textBlock);
			panel.Children.Add(comboBox);

			Blocks.Add(panel);
		}
		private void CreateTextBox(string name)
		{
			var panel = new StackPanel();
			panel.Orientation = Orientation.Horizontal;
			panel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
			panel.Margin = new System.Windows.Thickness(5);

			var text_box = new TextBox();
			text_box.Width = 150;
			text_box.FontSize = 14;
			text_box.Tag = name;

			TextBlock textBlock = new TextBlock();
			textBlock.Text = name;
			textBlock.FontSize = 14;
			textBlock.Margin = new System.Windows.Thickness(0, 0, 10, 0);


			panel.Children.Add(textBlock);
			panel.Children.Add(text_box);

			Blocks.Add(panel);
		}
		#endregion
	}
}
