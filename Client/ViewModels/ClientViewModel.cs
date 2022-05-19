using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DataBaseClientServer;
using DataBaseClientServer.Base.Command;
using API.XML;
using API.Logging;
using DataBaseClientServer.Models;
using DataBaseClientServer.Models.Settings;
using System.IO;
using Client;
using Client.Views.Windows;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Data;
using System.ComponentModel;

namespace DataBaseClientServer.ViewModels
{
	public class ClientViewModel : Base.ViewModel.BaseViewModel
	{
		private static object _lock = new object();
		private ClientSerttings _ClientSerttings = new ClientSerttings();
		public ClientSerttings ClientSerttings { get => _ClientSerttings; set => Set(ref _ClientSerttings, value); }

		private Models.Client _Client = new Models.Client();
		public Models.Client Client { get => _Client; set => Set(ref _Client, value); }
		private byte[] KEY_LOAD = new byte[16] { 0xaa, 0x11, 0x23, 0x54, 0x32, 0x40, 0x10, 0x01, 0xd, 0xdd, 0x23, 0x90, 0x01, 0x12, 0x11, 0x02 };
		private byte[] IV_LOAD = new byte[16] { 0xaa, 0x01, 0x0f, 0x00, 0x0b, 0x30, 0x03, 0x00, 0x60, 0x60, 0x40, 0x67, 0x01, 0x05, 0x80, 0x0f };

		#region работа с базами данных
		private bool BlockAllWorkInDataBase = false;
		private string _SelectPathToDataBase;
		public string SelectPathToDataBase { get => _SelectPathToDataBase; set => Set(ref _SelectPathToDataBase, value); }
		public ObservableCollection<string> PathsToDataBase { get; set; } = new ObservableCollection<string>();
		#endregion
		#region информация о клиенте
		private string _UserName;
		public string UserName { get => _UserName; set => Set(ref _UserName, value); }

		private string _UserSurname;
		public string UserSurname { get => _UserSurname; set => Set(ref _UserSurname, value); }

		private string _UserPatronymic;
		public string UserPatronymic { get => _UserPatronymic; set => Set(ref _UserPatronymic, value); }

		private string _UserInfo;
		public string UserInfo { get => _UserInfo; set => Set(ref _UserInfo, value); }
		#endregion
		#region Просмотр ключей шифрований
		private Visibility _VisibilityKeyAES = Visibility.Collapsed;
		public Visibility VisibilityKeyAES { get => _VisibilityKeyAES; set => Set(ref _VisibilityKeyAES, value); }

		private bool _CheckVisibilytiKeyAES = false;
		public bool CheckVisibilytiKeyAES { get => _CheckVisibilytiKeyAES; set { Set(ref _CheckVisibilytiKeyAES, value); if (value == true) VisibilityKeyAES = Visibility.Visible; else VisibilityKeyAES = Visibility.Collapsed; } }

		private Visibility _VisibilityIvAES = Visibility.Collapsed;
		public Visibility VisibilityIvAES { get => _VisibilityIvAES; set => Set(ref _VisibilityIvAES, value); }

		private bool _CheckVisibilytiIvAES = false;
		public bool CheckVisibilytiIvAES { get => _CheckVisibilytiIvAES; set { Set(ref _CheckVisibilytiIvAES, value); if (value == true) VisibilityIvAES = Visibility.Visible; else VisibilityIvAES = Visibility.Collapsed; } }

		#endregion
		#region Авторизация
		private string _PasswordBoxUser = "";
		public string PasswordBoxUser
		{
			get => _PasswordBoxUser; set
			{
				Set(ref _PasswordBoxUser, value);
				if (CheckBoxHide) ClientSerttings.KernelSettings.PasswordUser = value;
			}
		}

		private bool _CheckBoxHide = false;
		public bool CheckBoxHide
		{
			get => _CheckBoxHide; set
			{
				Set(ref _CheckBoxHide, value);
				if (value) PasswordBoxUser = ClientSerttings.KernelSettings.PasswordUser;
				else PasswordBoxUser = "";
			}
		}
		private string _InfoConnectText = "";
		public string InfoConnectText { get => _InfoConnectText; set => Set(ref _InfoConnectText, value); }

		private int _PingToServer = -1;
		public int PingToServer { get => _PingToServer; set => Set(ref _PingToServer, value); }
		#endregion
		#region база данных
		private bool _IsConnectDataBase = false;
		public bool IsConnectDataBase { get => _IsConnectDataBase; set => Set(ref _IsConnectDataBase, value); }

		private ObservableCollection<API.TableDataBase> _TablesDataBase = new ObservableCollection<API.TableDataBase>();
		public ObservableCollection<API.TableDataBase> TablesDataBase { get => _TablesDataBase; set => Set(ref _TablesDataBase, value); }

		private API.TableDataBase _SelectedTableDataBase;
		public API.TableDataBase SelectedTableDataBase { get => _SelectedTableDataBase; set => Set(ref _SelectedTableDataBase, value); }

		private DataTable _Books = null;
		public DataTable Books { get => _Books; set => Set(ref _Books, value); }

		private bool _IsAccessLevelAdmin = false;
		public bool IsAccessLevelAdmin { get => _IsAccessLevelAdmin; set => Set(ref _IsAccessLevelAdmin, value); }

		private bool _WriteToTable = false;
		public bool WriteToTable { get => _WriteToTable; set => Set(ref _WriteToTable, value); }

		private int _IndexSelectRow = -1;
		public int IndexSelectRow { get => _IndexSelectRow; set => Set(ref _IndexSelectRow, value); }

		private API.AccessLevel _AccessLevel = API.AccessLevel.NonAuthorization;
		public API.AccessLevel AccessLevel
		{
			get => _AccessLevel; set

			{
				Set(ref _AccessLevel, value);
				switch (_AccessLevel)
				{
					case API.AccessLevel.Worker:
						WriteToTable = true;
						IsAccessLevelAdmin = false;
						break;
					case API.AccessLevel.Admin:
						WriteToTable = true;
						IsAccessLevelAdmin = true;
						break;
					default:
						WriteToTable = false;
						IsAccessLevelAdmin = false;
						break;
				}
			}
		}

		public AddToDataBaseVM AddToDataBaseVM;
		public AdminToolChangeVM AdminToolChangeVM;

		private bool PingWork = false;
		private bool PingStop = false;
		#endregion
		#region Kernel
		/// <summary>
		/// Запуск клиента
		/// </summary>
		public ClientViewModel()
		{
			AddToDataBaseVM = new AddToDataBaseVM(this);
			AdminToolChangeVM = new AdminToolChangeVM(this);
			BindingOperations.EnableCollectionSynchronization(PathsToDataBase, _lock); // доступ из всех потоков
			ProviderXML.IV_AES = IV_LOAD;
			ProviderXML.KEY_AES = KEY_LOAD;
			ConnectServerCommand = new LambdaCommand(OnConnectServerCommand, CanConnectServerCommand);
			DisconnectServerCommand = new LambdaCommand(OnDisconnectServerCommand, CanDisconnectServerCommand);
			ConnectDataBaseCommand = new LambdaCommand(OnConnectDataBaseCommand, CanConnectDataBaseCommand);
			AddDBBookCommand = new LambdaCommand(OnAddDBBookCommand, CanAddDBBookCommand);
			DeleteFromDBCommand = new LambdaCommand(OnDeleteFromDBCommand, CanDeleteFromDBCommand);
			ChangeAdminToolCommnad = new LambdaCommand(OnChangeAdminToolCommnad, CanChangeAdminToolCommnad);
			SetSettingsClient();
			App.Current.Exit += Current_Exit;
			Console.WriteLine("Start");
			Task.Run(() => { LoadXML(); });
			Task.Run(() => { InfoConnectTextUpdate(); });

			if ((bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue))
			{
				for (int i = 0; i < 10; i++)
				{
					TablesDataBase.Add(new API.TableDataBase() { Table = new DataTable() { TableName = $"{i}" } });
				}
			}
		}
		/// <summary>
		/// Установка стандартных значений для сетевого клиента
		/// </summary>
		private void SetSettingsClient()
		{
			SelectPathToDataBase = null;
			PathsToDataBase.Clear();
			Client.CallAnswer += Answer;
			Client.CallDisconnect += Disconnect;
			Client.ClientSerttings = ClientSerttings;
		}
		/// <summary>
		/// Поиск БД
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public API.TableDataBase GetTableFromName(string name)
		{
			return TablesDataBase.FirstOrDefault((i) => i.Table.TableName == name);
		}
		/// <summary>
		/// Завершение работы 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Current_Exit(object sender, System.Windows.ExitEventArgs e)
		{
			SaveXML();
		}
		/// <summary>
		/// Обновление информации о подключении
		/// </summary>
		private void InfoConnectTextUpdate()
		{
			while (true)
			{
				if (InfoConnectText == "Неверный логин!" || InfoConnectText == "Неверный пароль!" || InfoConnectText == "Такой пользователь уже подключен!") continue;
				switch (Client.StatusClient)
				{
					case StatusClient.Connected:
						InfoConnectText = "Подключение установлено!";
						UserInfo = $"Добро пожаловать {UserSurname} {UserName} {UserPatronymic}";
						break;
					case StatusClient.Disconnected:
						AccessLevel = API.AccessLevel.NonAuthorization;
						InfoConnectText = "Подключение не установлено!";
						UserName = "";
						UserSurname = "";
						UserPatronymic = "";
						UserInfo = "";
						break;
					case StatusClient.Connecting:
						InfoConnectText = "Подключение..";
						break;
				}
				Thread.Sleep(250);
			}
		}
		/// <summary>
		/// Загрузка данных
		/// </summary>
		void LoadXML()
		{
			Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\Settings");
			try
			{
				if (File.Exists($"{Directory.GetCurrentDirectory()}\\Settings\\ConnectClient.xml"))
				{
					ClientSerttings.ClientConnect = ProviderXML.Load<ClientConnect>($"{Directory.GetCurrentDirectory()}\\Settings\\ConnectClient.xml");
				}
			}
			catch (Exception ex) { ClientSerttings.ClientConnect = new ClientConnect(); Log.WriteLine(ex); }
			try
			{
				if (File.Exists($"{Directory.GetCurrentDirectory()}\\Settings\\KernelSettings.xml"))
				{
					ClientSerttings.KernelSettings = ProviderXML.Load<KernelSettings>($"{Directory.GetCurrentDirectory()}\\Settings\\KernelSettings.xml");
				}
			}
			catch (Exception ex) { ClientSerttings.ClientConnect = new ClientConnect(); Log.WriteLine(ex); }
			if (ClientSerttings.KernelSettings.AutoStartClient) Task.Run(() => { ConnectClient(); });
		}
		/// <summary>
		/// Сохранение данных
		/// </summary>
		void SaveXML()
		{
			Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\Settings");
			try
			{
				ProviderXML.Save<ClientConnect>($"{Directory.GetCurrentDirectory()}\\Settings\\ConnectClient.xml", ClientSerttings.ClientConnect);
			}
			catch { }
			try
			{
				ProviderXML.Save<KernelSettings>($"{Directory.GetCurrentDirectory()}\\Settings\\KernelSettings.xml", ClientSerttings.KernelSettings);
			}
			catch { }
		}
		#endregion

		#region Commands
		#region ChangeAdminToolCommnad
		/// <summary>
		/// Открытие окна редактирования БД для администратора и отправка на сервер данных
		/// </summary>
		public ICommand ChangeAdminToolCommnad { get; set; }
		public bool CanChangeAdminToolCommnad(object e) => true;
		public void OnChangeAdminToolCommnad(object e)
		{
			if (SelectedTableDataBase == null) return;
			AdminToolChangeWindow window = new AdminToolChangeWindow();
			window.DataContext = AdminToolChangeVM;
			AdminToolChangeVM.SQLQueryes.Clear();
			AdminToolChangeVM.SQLQueryesInsert.Clear();
			AdminToolChangeVM.Queries.Clear();
			AdminToolChangeVM.Window = window;
			AdminToolChangeVM.Table = new API.TableDataBase() { Table = SelectedTableDataBase.Table.Copy(), Status = SelectedTableDataBase.Status };
			AdminToolChangeVM.Table.Table.RowChanged += AdminToolChangeVM.Table_RowChanged;
			AdminToolChangeVM.Table.Table.RowDeleting += AdminToolChangeVM.Table_RowDeleted;
			AdminToolChangeVM.Table.Table.Columns[0].ReadOnly = true;
			AdminToolChangeVM.Title = $"Инструмент администратора - {AdminToolChangeVM.Table.Table.TableName}";
			window.ShowInTaskbar = false;
			window.ShowDialog();

			if (AdminToolChangeVM.Queries.Count == 0) return;
			if (AdminToolChangeVM.Queries.Count > 0)
			{
				string changes = "";
				int count1 = 1;
				foreach (var i in AdminToolChangeVM.SQLQueryesInsert)
				{
					changes += $"[{AdminToolChangeVM.Table.Table.Columns[0].ColumnName} {i.Key}] Транзакция {count1} - {i.Value.Command}\n";
					count1++;
				}
				foreach (var i in AdminToolChangeVM.SQLQueryes)
				{
					changes += $"[{AdminToolChangeVM.Table.Table.Columns[0].ColumnName} {i.Key}] Транзакция {count1} - {i.Value.Command}\n";
					count1++;
				}
				var answer1 = MessageBox.Show($"Вы уверены что хотите применить все изменения?\n{changes}", "Изменения", MessageBoxButton.YesNo);
				if (answer1 == MessageBoxResult.No)
				{
					AdminToolChangeVM.SQLQueryes.Clear();
					return;
				}

			}

			BlockAllWorkInDataBase = true;
			Dictionary<string, string> queryes = new Dictionary<string, string>();
			int count = 0;
			foreach (var sql in AdminToolChangeVM.Queries)
			{
				try
				{
					//Console.WriteLine($"<>>>>{sql.Value}");
					count++;
					var packet = Client.SendPacketAndWaitResponse(GetPacketSQLQuery(sql.SQL, SelectedTableDataBase.Table.TableName, API.TypeSQLQuery.BroadcastMe), 1).Packets[0];
					//Console.WriteLine(packet);
					if (packet.TypePacket == API.TypePacket.SQLQueryOK)
					{
						queryes.Add($"{count} - {sql.Command}", "Транзакция успешно выполнена!");
					}
					else if (packet.TypePacket == API.TypePacket.SQLQueryDenay) queryes.Add($"{count}", "У вас недостаточно прав для выполнения данной операции!");
					else if (packet.TypePacket == API.TypePacket.SQLQueryError) queryes.Add($"{count}", $"Произошла ошибка на стороне сервера!\n{packet.Data}");
					else queryes.Add($"{count}", $"Сервер вернул что то непонятное:(");
				}
				catch (Exception er) { queryes.Add($"{count}", $"Произошла непредвиденная ошибка!\n{er}"); }

			}
			string answer = "";
			foreach (var i in queryes)
			{
				answer += $"Запрос: {i.Key}\n{i.Value}\n\n";
			}
			MessageBox.Show(answer, "Уведомление");
			BlockAllWorkInDataBase = false;
			//Console.WriteLine("---------");
			//foreach (DataRow i in AdminToolChangeVM.Table.Table.Rows) Console.WriteLine($"{string.Join(";", i.ItemArray)}");
			//Console.WriteLine("---------");
			//foreach (DataRow i in SelectedTableDataBase.Table.Rows) Console.WriteLine($"{string.Join(";", i.ItemArray)}");
		}
		#endregion
		/// <summary>
		/// Создание пакета данных для отправки sql запроса
		/// </summary>
		/// <param name="data"></param>
		/// <param name="TableName"></param>
		/// <param name="typeSQLQuery"></param>
		/// <returns></returns>
		public API.Packet GetPacketSQLQuery(object data, string TableName, API.TypeSQLQuery typeSQLQuery = API.TypeSQLQuery.Broadcast)
		{
			return new API.Packet() { TypePacket = API.TypePacket.SQLQuery, Data = new API.SQLQueryPacket() { TableName = TableName, Data = data, TypeSQLQuery = typeSQLQuery } };
		}
		/// <summary>
		/// Получает ид в бд
		/// </summary>
		/// <param name="dataTable"></param>
		/// <returns></returns>
		public int GetID(DataTable dataTable)
		{
			int max_index = 0;
			foreach (DataRow i in dataTable.Rows)
			{
				int temp = (int)i.ItemArray[0];
				if (temp >= max_index) max_index = temp + 1;
			}
			return max_index;
		}
		#region DeleteFromDBCommand
		/// <summary>
		/// Удаление из БД строки
		/// </summary>
		public ICommand DeleteFromDBCommand { get; set; }
		public bool CanDeleteFromDBCommand(object e) => true;
		public void OnDeleteFromDBCommand(object e)
		{
			if (IndexSelectRow == -1) return;
			var row = "";
			int index = 0;
			foreach (DataColumn i in SelectedTableDataBase.Table.Columns)
			{
				row += $"{i.ColumnName}={SelectedTableDataBase.Table.Rows[IndexSelectRow][index]}; ";
				index++;
			}
			var answer = MessageBox.Show($"Вы уверены что хотите удалить эту запись?\n" +
				$"Таблица: {SelectedTableDataBase.Table.TableName}\n" +
				$"Поле: {row}", "Удаление", MessageBoxButton.YesNo);
			if (answer == MessageBoxResult.Yes)
			{
				BlockAllWorkInDataBase = true;
				try
				{
					var packet = Client.SendPacketAndWaitResponse(GetPacketSQLQuery($"DELETE FROM [{SelectedTableDataBase.Table.TableName}]" +
						$" WHERE {SelectedTableDataBase.Table.Columns[0].ColumnName} = {SelectedTableDataBase.Table.Rows[IndexSelectRow].ItemArray[0]}", SelectedTableDataBase.Table.TableName, API.TypeSQLQuery.BroadcastMe), 1).Packets[0];
					Console.WriteLine(packet);
					if (packet.TypePacket == API.TypePacket.SQLQueryOK)
					{
						MessageBox.Show($"Транзакция успешно выполнена!", "Уведомление");
					}
					else if (packet.TypePacket == API.TypePacket.SQLQueryDenay) MessageBox.Show($"У вас недостаточно прав для выполнения данной операции!");
					else if (packet.TypePacket == API.TypePacket.SQLQueryError) MessageBox.Show($"Произошла ошибка на стороне сервера!\n{packet.Data}");
					else MessageBox.Show($"Сервер вернул что то непонятное:(", "Ошибка");
				}
				catch (Exception er) { MessageBox.Show($"Произошла непредвиденная ошибка!\n{er}", "Ошибка"); }
			}
			BlockAllWorkInDataBase = false;
		}
		#endregion
		#region AddDBBookCommand
		/// <summary>
		/// Открытие окна для добавления книг в таблицу БД
		/// </summary>
		public ICommand AddDBBookCommand { get; set; }
		public bool CanAddDBBookCommand(object e) => true;
		public void OnAddDBBookCommand(object e)
		{
			OpenAddDBWindow(AddType.AddBook);
		}
		#endregion
		/// <summary>
		/// Открытие окна для добавления книг в таблицу БД и отправка на сервер данных
		/// </summary>
		public void OpenAddDBWindow(AddType type)
		{
			AddToDataBaseWindow window = new AddToDataBaseWindow();
			AddToDataBaseVM.AddDBType = type;
			AddToDataBaseVM.Window = window;
			window.ShowInTaskbar = false;
			AddToDataBaseVM.FillProperty();
			window.DataContext = AddToDataBaseVM;
			window.ShowDialog();
			string sql_query = AddToDataBaseVM.GetSQLQuery();
			if (sql_query != null && sql_query != "" && sql_query != "exit")
			{
				BlockAllWorkInDataBase = true;
				try
				{
					var dict = AddToDataBaseVM.GetDataRow();
					var packet = Client.SendPacketAndWaitResponse(GetPacketSQLQuery(sql_query, dict["!TableName!"].ToString(), API.TypeSQLQuery.BroadcastMe), 1).Packets[0];
					Console.WriteLine(packet);
					if (packet.TypePacket == API.TypePacket.SQLQueryOK)
					{
						//var table = GetTableFromName(dict["!TableName!"].ToString());
						//dict.Remove("!TableName!");
						//var row = table.Table.NewRow();
						//foreach (var i in dict) row[i.Key] = i.Value;
						//table.Table.Rows.Add(row);
						MessageBox.Show($"Транзакция успешно выполнена!", "Уведомление");
					}
					else if (packet.TypePacket == API.TypePacket.SQLQueryDenay) MessageBox.Show($"У вас недостаточно прав для выполнения данной операции!");
					else if (packet.TypePacket == API.TypePacket.SQLQueryError) MessageBox.Show($"Произошла ошибка на стороне сервера!\n{packet.Data}");
					else MessageBox.Show($"Сервер вернул что то непонятное:(", "Ошибка");
				}
				catch (Exception e) { MessageBox.Show($"Произошла непредвиденная ошибка!\n{e}", "Ошибка"); }
			}
			BlockAllWorkInDataBase = false;
		}
		#region ConnectDataBaseCommand
		/// <summary>
		/// Подключение к серверу
		/// </summary>
		public ICommand ConnectDataBaseCommand { get; set; }
		public bool CanConnectDataBaseCommand(object e) => Client != null &&
			Client.StatusClient == StatusClient.Connected &&
			Client.IsAuthorization &&
			!BlockAllWorkInDataBase;
		/// <summary>
		/// Получение БД
		/// </summary>
		/// <param name="e"></param>
		public void OnConnectDataBaseCommand(object e)
		{
			Task.Run(() =>
			{
				ConnectDataBase();
			});
		}
		/// <summary>
		/// Получение БД
		/// </summary>
		public void ConnectDataBase()
		{
			BlockAllWorkInDataBase = true;
			IsConnectDataBase = false;
			try
			{
				var packet = (API.DataBasePacket)Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.ConnectDataBase }, 1).Packets[0].Data;
				if (packet.Info == API.InfoDataBasePacket.OK)
				{
					TablesDataBase = (ObservableCollection<API.TableDataBase>)packet.Data;
					IsConnectDataBase = true;
				}
				else
				{
					switch (packet.Info)
					{
						case API.InfoDataBasePacket.NotExistsFile:
							MessageBox.Show("База данных в данных момент не доступна!", "Уведомление");
							break;
						default:
							MessageBox.Show($"Не удалось получить доступ к базе данных!\n{packet.Info}", "Уведомление");
							break;
					}
					IsConnectDataBase = false;
				}
			}
			catch (Exception ex) { Console.WriteLine(ex); }
			BlockAllWorkInDataBase = false;
		}
		#endregion
		#region DisconnectServerCommand
		/// <summary>
		/// Отключение от сервера
		/// </summary>
		public ICommand DisconnectServerCommand { get; set; }
		public bool CanDisconnectServerCommand(object e) => Client != null && Client.StatusClient == StatusClient.Connected;
		public void OnDisconnectServerCommand(object e)
		{
			DisconnectClient();
		}
		#endregion
		#region ConnectServerCOmmand
		/// <summary>
		/// подключение к БД
		/// </summary>
		public ICommand ConnectServerCommand { get; set; }
		public bool CanConnectServerCommand(object e) => Client != null && Client.StatusClient == StatusClient.Disconnected;
		public void OnConnectServerCommand(object e)
		{
			Task.Run(() => { ConnectClient(); });
		}
		#endregion
		#endregion
		/// <summary>
		/// делегат для сетевого клиента
		/// </summary>
		/// <param name="message"></param>
		private void Disconnect(string message)
		{
			PingToServer = -1;
			if (message != null) InfoConnectText = message;
		}
		/// <summary>
		/// получение и обработка ответа от сервера
		/// </summary>
		/// <param name="Packet"></param>
		private void Answer(API.Packet Packet)
		{
			switch (Packet.TypePacket)
			{
				case API.TypePacket.UpdateTable:
					API.SQLQueryPacket queryPacket = (API.SQLQueryPacket)Packet.Data;
					var table = GetTableFromName(queryPacket.TableName);
					table.Table = (DataTable)queryPacket.Data;
					SelectedTableDataBase = GetTableFromName(TablesDataBase.FirstOrDefault((i) => i != table).Table.TableName);
					SelectedTableDataBase = table;
					break;
			}
			Console.WriteLine(Packet);
		}
		/// <summary>
		/// Получение времени отклика сервера (пинг)
		/// </summary>
		private void PingServer()
		{
			PingWork = true;
			while (Client != null && Client.StatusClient == StatusClient.Connected && !PingStop)
			{
				try
				{
					var packet = Client.SendPacketAndWaitResponse(new API.Packet()
					{
						TypePacket = API.TypePacket.Ping,
						Data = DateTime.Now,
					}, 1);
					PingToServer = (int)((DateTime.Now - (DateTime)packet.Packets[0].Data).TotalMilliseconds);
				}
				catch { }
				Thread.Sleep(1000);
			}
			PingWork = false;
			Log.WriteLine("PingServer: Dispose");
		}
		/// <summary>
		/// Отключение от сервера
		/// </summary>
		private void DisconnectClient()
		{
			Client.Disconnect();
			Client = new Models.Client();
			Disconnect(null);
		}
		/// <summary>
		/// Подключение к серверу и получение БД
		/// </summary>
		private void ConnectClient()
		{
			Client = new Models.Client();
			SetSettingsClient();
			InfoConnectText = "";
			if (ClientSerttings.KernelSettings.LoginUser != "" && ClientSerttings.KernelSettings.PasswordUser != "")
			{
				if (Client.Connect())
				{
					while (!Client.FirstUpdateKey) Thread.Sleep(1);
					var packet = Client.SendPacketAndWaitResponse(new API.Packet()
					{
						TypePacket = API.TypePacket.Authorization,
						Data = new API.Authorization() { Login = ClientSerttings.KernelSettings.LoginUser, Password = ClientSerttings.KernelSettings.PasswordUser }
					}, 1);

					API.InfoAboutClientPacket info_client = (API.InfoAboutClientPacket)packet.Packets[0].Data;
					if (packet.Packets[0].TypePacket == API.TypePacket.AuthorizationFailed)
					{
						//var data_answer = (API.TypeErrorAuthorization)packet.Packets[0].Data;
						if (info_client.Error == API.TypeErrorAuthorization.Login) InfoConnectText = "Неверный логин!";
						else if (info_client.Error == API.TypeErrorAuthorization.Passsword) InfoConnectText = "Неверный пароль!";
						else if (info_client.Error == API.TypeErrorAuthorization.ConnectNow) InfoConnectText = "Такой пользователь уже подключен!";
						Client.Disconnect();
						Client = new Models.Client();
					}
					else
					{
						Task.Run(() =>
						{
							while (PingWork)
							{
								PingStop = true;
								Thread.Sleep(1);
							}
							PingStop = false;
							PingServer();
						});
						AccessLevel = info_client.AccessLevel;
						UserName = info_client.Name;
						UserSurname = info_client.Surname;
						UserPatronymic = info_client.Patronymic;
						ConnectDataBase();

					}
				}
			}
			GC.Collect();
		}
	}
}
