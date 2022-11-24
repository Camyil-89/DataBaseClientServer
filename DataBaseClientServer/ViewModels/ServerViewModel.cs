using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using DataBaseClientServer;
using DataBaseClientServer.Base.Command;
using DataBaseClientServer.Models;
using DataBaseClientServer.Models.SettingsServer;
using DataBaseClientServer.Models.database;
using System.Windows;
using API;
using API.XML;
using API.Logging;
using System.Windows.Media;

namespace DataBaseClientServer.ViewModels
{
	public class ServerViewModel : Base.ViewModel.BaseViewModel
	{

		private Server _Server = new Server();
		public Server Server { get => _Server; set => Set(ref _Server, value); }

		private Settings _Settings = new Settings();
		public Settings Settings { get => _Settings; set => Set(ref _Settings, value); }
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
		#region Создание пользователей
		private string _TextButtonAddDeleteChange = "Довавить пользователя";
		public string TextButtonAddDeleteChange { get => _TextButtonAddDeleteChange; set => Set(ref _TextButtonAddDeleteChange, value); }

		private Visibility _VisibilitySelectUser = Visibility.Collapsed;
		public Visibility VisibilitySelectUser { get => _VisibilitySelectUser; set => Set(ref _VisibilitySelectUser, value); }

		public Dictionary<string, DateTime> BroadcastTable = new Dictionary<string, DateTime>();

		/// <summary>
		/// выбор режима работы (изменить, добамить, удалить)
		/// </summary>
		private int _SelectMode = 0;
		public int SelectMode
		{
			get => _SelectMode;
			set
			{
				Set(ref _SelectMode, value);
				if (value == 0)
				{
					UserLogin = "";
					UserPassword = "";
					AccessLevelUser = -1;
					UserName = "";
					UserSurname = "";
					UserPatronymic = "";
				}
				if (value != 0) VisibilitySelectUser = Visibility.Visible;
				else VisibilitySelectUser = Visibility.Collapsed;
				switch (SelectMode)
				{
					case 0:
						TextButtonAddDeleteChange = "Довавить пользователя";
						break;
					case 1:
						TextButtonAddDeleteChange = "Удалить";
						break;
					case 2:
						TextButtonAddDeleteChange = "Измененить";
						break;
					default:
						TextButtonAddDeleteChange = "";
						break;
				}
			}
		}

		/// <summary>
		/// Выбор пользователя из списка и отображение информации о нем
		/// </summary>
		private int _SelectUser = -1;
		public int SelectUser
		{
			get => _SelectUser;
			set
			{
				Set(ref _SelectUser, value);
				if (SelectUser == -1) return;
				UserLogin = Settings.Clients[SelectUser].Login;
				UserPassword = Settings.Clients[SelectUser].Password;
				UserName = Settings.Clients[SelectUser].Name;
				UserSurname = Settings.Clients[SelectUser].Surname;
				UserPatronymic = Settings.Clients[SelectUser].Patronymic;
				switch (Settings.Clients[SelectUser].AccessLevel)
				{
					case API.AccessLevel.User:
						AccessLevelUser = 0;
						break;
					case API.AccessLevel.Worker:
						AccessLevelUser = 1;
						break;
					case API.AccessLevel.Admin:
						AccessLevelUser = 2;
						break;
				}
			}
		}
		/// <summary>
		/// конвертер из числа в уровень достпа
		/// </summary>
		private API.AccessLevel _AccessLevelUser;
		private int _AccessLevelUserComboBox = -1;
		public int AccessLevelUser
		{
			get => _AccessLevelUserComboBox;
			set
			{
				Set(ref _AccessLevelUserComboBox, value);
				switch (value)
				{
					case 0:
						_AccessLevelUser = API.AccessLevel.User;
						break;
					case 1:
						_AccessLevelUser = API.AccessLevel.Worker;
						break;
					case 2:
						_AccessLevelUser = API.AccessLevel.Admin;
						break;
				}
			}
		}
		private Client _SelectedUser;
		public Client SelectedUser
		{
			get => _SelectedUser; set
			{
				Set(ref _SelectedUser, value);
				if (SelectMode != 0) SelectUser = Settings.Clients.IndexOf(SelectedUser);
			}
		}

		private string _UserLogin = "";
		public string UserLogin { get => _UserLogin; set => Set(ref _UserLogin, value); }

		private string _UserName = "";
		public string UserName { get => _UserName; set => Set(ref _UserName, value); }

		private string _UserSurname = "";
		public string UserSurname { get => _UserSurname; set => Set(ref _UserSurname, value); }

		private string _UserPatronymic = "";
		public string UserPatronymic { get => _UserPatronymic; set => Set(ref _UserPatronymic, value); }


		private string _UserPassword = "";
		public string UserPassword { get => _UserPassword; set => Set(ref _UserPassword, value); }
		#endregion
		#region добавление файлов баз данных и удаление
		private DataBaseConnectPath _SelectPathDataBase;
		public DataBaseConnectPath SelectPathDataBase { get => _SelectPathDataBase; set => Set(ref _SelectPathDataBase, value); }

		private ObservableCollection<DataBaseConnectPath> _PathsToDataBase = new ObservableCollection<DataBaseConnectPath>();
		public ObservableCollection<DataBaseConnectPath> PathsToDataBase { get => _PathsToDataBase; set => Set(ref _PathsToDataBase, value); }
		#endregion
		private byte[] KEY_LOAD = new byte[16] { 0xaa, 0x11, 0x23, 0x54, 0x32, 0x40, 0x10, 0x01, 0xd, 0xdd, 0x23, 0x90, 0x01, 0x12, 0x11, 0x02 };
		private byte[] IV_LOAD = new byte[16] { 0xaa, 0x01, 0x0f, 0x00, 0x0b, 0x30, 0x03, 0x00, 0x60, 0x60, 0x40, 0x67, 0x01, 0x05, 0x80, 0x0f };
		private static object _lock = new object();
		#region Kernel

		/// <summary>
		/// Запуск программы
		/// </summary>
		public ServerViewModel()
		{
			BindingOperations.EnableCollectionSynchronization(Server.tcpClients, _lock); // доступ из всех потоков
			BindingOperations.EnableCollectionSynchronization(PathsToDataBase, _lock); // доступ из всех потоков
			Log.WriteLine("ServerViewModel");
			#region Commands
			StartServerListenerCommand = new LambdaCommand(OnStartServerListenerCommand, CanStartServerListenerCommand);
			StopServerListenerCommand = new LambdaCommand(OnStopServerListenerCommand, CanStopServerListenerCommand);
			AddUserCommand = new LambdaCommand(OnAddUserCommand, CanAddUserCommand);
			AddDataBasePathCommand = new LambdaCommand(OnAddDataBasePathCommand, CanAddDataBasePathCommand);
			RemoveDataBasePathCommand = new LambdaCommand(OnRemoveDataBasePathCommand, CanRemoveDataBasePathCommand);
			#endregion
			Server.ServerViewModel = this;
			ProviderXML.IV_AES = IV_LOAD;
			ProviderXML.KEY_AES = KEY_LOAD;

			Server.CallAnswer += Answer;
			Server.IPAddress = API.NetFind.Utility.GetLocalIPAddress();
			App.Current.Exit += Current_Exit;
			Task.Run(() => { LoadXML(); });
			Task.Run(() =>
			{
				API.NetFind.Server server = new API.NetFind.Server();
				server.Start(Server.Port + 10, "ServerDataBase");
			});
		}
		/// <summary>
		/// Выход из программы
		/// </summary>
		private void Current_Exit(object sender, System.Windows.ExitEventArgs e)
		{
			Server.DisposeClients();
			SaveXML();
		}
		/// <summary>
		/// подгрузка данных
		/// </summary>
		private void LoadXML()
		{
			Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\Settings");
			try
			{
				if (File.Exists($"{Directory.GetCurrentDirectory()}\\Settings\\Settings.xml"))
				{
					Settings.ServerSettings = ProviderXML.Load<ServerSettings>($"{Directory.GetCurrentDirectory()}\\Settings\\Settings.xml");
				}
			}
			catch (Exception ex) { Settings.ServerSettings = new ServerSettings(); Log.WriteLine(ex); }
			try
			{
				if (File.Exists($"{Directory.GetCurrentDirectory()}\\Settings\\Clients.xml"))
				{
					Settings.Clients = ProviderXML.Load<ObservableCollection<Client>>($"{Directory.GetCurrentDirectory()}\\Settings\\Clients.xml");
				}
			}
			catch (Exception ex) { Settings.Clients = new ObservableCollection<Client>(); Log.WriteLine(ex); }
			Log.WriteLine("LoadXML: true");
			Task.Run(() =>
			{
				try
				{
					foreach (var i in Settings.ServerSettings.PathsToDataBase)
					{
						var x = new DataBaseConnectPath() { Path = i };
						if (File.Exists(i)) x.IsEnableStatus = StatusConnectDataBase.CanConnect;
						else x.IsEnableStatus = 0;
						PathsToDataBase.Add(x);
						if (x.DataBase.Connect()) x.IsEnableStatus = StatusConnectDataBase.ConnectAccess;
						else x.IsEnableStatus = StatusConnectDataBase.NotWork;
					}
				}
				catch (Exception e) { Log.WriteLine(e); }
				if (Settings.ServerSettings.AutoStartServer) Task.Run(() => { StartServer(); });
			});
		}
		/// <summary>
		/// Сохранение всех необходимых данных
		/// </summary>
		private void SaveXML()
		{
			Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\Settings");
			Settings.ServerSettings.PathsToDataBase.Clear();
			foreach (var i in PathsToDataBase) Settings.ServerSettings.PathsToDataBase.Add(i.Path);
			try
			{
				ProviderXML.Save<ObservableCollection<Client>>($"{Directory.GetCurrentDirectory()}\\Settings\\Clients.xml", Settings.Clients);
			}
			catch { }
			try
			{
				ProviderXML.Save<ServerSettings>($"{Directory.GetCurrentDirectory()}\\Settings\\Settings.xml", Settings.ServerSettings);
			}
			catch { }
			Log.WriteLine("SaveXML: true");
		}
		#endregion


		#region Commands
		#region RemoveDataBasePathCommand
		/// <summary>
		/// Удаление базы данных
		/// </summary>
		public ICommand RemoveDataBasePathCommand { get; set; }
		public bool CanRemoveDataBasePathCommand(object e) => true;
		public void OnRemoveDataBasePathCommand(object e)
		{
			if (SelectPathDataBase != null) PathsToDataBase.Remove(SelectPathDataBase);
		}
		#endregion
		#region AddDataBasePath
		/// <summary>
		/// Добавление базы данных
		/// </summary>
		public ICommand AddDataBasePathCommand { get; set; }
		public bool CanAddDataBasePathCommand(object e) => true;
		public void OnAddDataBasePathCommand(object e)
		{
			if (PathsToDataBase.Count > 0)
			{
				MessageBox.Show($"Сначала удалите базу данных текущею!\n{PathsToDataBase[0].Path}", "Уведомление");
				return;
			}
			var list_paths = ExplorerDialog.FilePicker.ShowDialog(false, ";").Split(';');
			if (list_paths.Length == 0) return;
			foreach (var path in list_paths)
			{
				if (path.Split('.').Last() != "mdb") continue;
				var find = PathsToDataBase.FirstOrDefault((i) => i.Path == path);
				if (find != null) continue;
				PathsToDataBase.Add(new DataBaseConnectPath() { Path = path, IsEnableStatus = StatusConnectDataBase.CanConnect });
			}
		}
		#endregion
		#region AddUser
		/// <summary>
		/// Выбор неоюходимой функции в зависимости от режима работы
		/// </summary>
		public ICommand AddUserCommand { get; set; }
		public bool CanAddUserCommand(object e)
		{
			switch (SelectMode)
			{
				case 0:
					return UserLogin != "" && UserPassword != "" && AccessLevelUser != -1 && UserName != "" && UserSurname != "" && UserPatronymic != "";
				case 1:
					return SelectUser != -1 && SelectUser < Settings.Clients.Count;
				case 2:
					return SelectUser != -1 && UserLogin != "" && UserPassword != "" && AccessLevelUser != -1 && UserName != "" && UserSurname != "" && UserPatronymic != "";
				default:
					return false;
			}
		}
		public void OnAddUserCommand(object e)
		{
			switch (SelectMode)
			{
				case 0:
					AddUser();
					break;
				case 1:
					RemoveUser();
					break;
				case 2:
					ChangeUser();
					break;
			}

		}
		/// <summary>
		/// Изменение данных у пользователя
		/// </summary>
		private void ChangeUser()
		{
			var find = Settings.Clients.FirstOrDefault((i) => i.Login == UserLogin && i.UID != Settings.Clients[SelectUser].UID);
			if (find == null)
			{
				Settings.Clients[SelectUser].Login = UserLogin;
				Settings.Clients[SelectUser].Password = UserPassword;
				Settings.Clients[SelectUser].AccessLevel = _AccessLevelUser;
				Settings.Clients[SelectUser].Name = UserName;
				Settings.Clients[SelectUser].Surname = UserSurname;
				Settings.Clients[SelectUser].Patronymic = UserPatronymic;
				MessageBox.Show($"Изменения внесены!", "Уведомление");
			}
			else MessageBox.Show($"Пользователь с именем \"{UserLogin}\" уже добавлен", "Уведомление");
		}
		/// <summary>
		/// Удаление пользователя из базы
		/// </summary>
		private void RemoveUser()
		{
			var answer = MessageBox.Show("Вы уверены что хотите удалить пользователя?", "Уведомление", MessageBoxButton.YesNo);
			if (answer != MessageBoxResult.Yes) return;
			Settings.Clients.RemoveAt(SelectUser);
		}
		/// <summary>
		/// Добавление пользователя в базу
		/// </summary>
		private void AddUser()
		{
			var find = Settings.Clients.FirstOrDefault((i) => i.Login == UserLogin);
			if (find == null)
			{
				Settings.Clients.Add(new Client()
				{
					Login = UserLogin,
					Password = UserPassword,
					Name = UserName,
					Surname = UserSurname,
					Patronymic = UserPatronymic,
					AccessLevel = _AccessLevelUser,
					UID = Client.GenerateUIDClient(Settings.Clients)
				});
				UserLogin = "";
				UserPassword = "";
				AccessLevelUser = -1;
				UserName = "";
				UserSurname = "";
				UserPatronymic = "";
				MessageBox.Show($"Пользователь успешно добавлен!", "Уведомление");
			}
			else MessageBox.Show($"Пользователь с именем \"{UserLogin}\" уже добавлен", "Уведомление");
		}
		#endregion
		#region StopServerListenerCommand
		/// <summary>
		/// Остановка сервера
		/// </summary>
		public ICommand StopServerListenerCommand { get; set; }
		public bool CanStopServerListenerCommand(object e) => Server.Work;
		public void OnStopServerListenerCommand(object e)
		{
			Log.WriteLine("Stop server");
			Server.DisposeClients();
		}
		#endregion
		#region StartServerListener
		/// <summary>
		/// Запуск сервера
		/// </summary>
		public ICommand StartServerListenerCommand { get; set; }
		public bool CanStartServerListenerCommand(object e) => !Server.Work;
		public void OnStartServerListenerCommand(object e)
		{
			StartServer();
		}
		#endregion
		#endregion
		/// <summary>
		/// Запуск сервера
		/// </summary>
		private void StartServer()
		{
			Log.WriteLine("Start server");
			Server.Start(Settings.ServerSettings.KeyAES, Settings.ServerSettings.IV_AES);
			Task.Run(() => { SendBroadcast(); });
			//Task.Run(() =>
			//{
			//	Thread.Sleep(1000);
			//	try
			//	{
			//		DataBase dataBase = new DataBase();
			//		dataBase.Connect();
			//		dataBase.SendQuery("SELECT * FROM таблица"); //ID_таблица
			//													 //dataBase.SendQuery("DELETE FROM таблица WHERE ID_таблица = 4"); //ID_таблица
			//													 //dataBase.SendQuery("INSERT INTO таблица(название, размер) VALUES('Михаил', 20);"); //ID_таблица
			//													 //dataBase.SendQuery("SELECT * FROM таблица"); //ID_таблица
			//	}
			//	catch (Exception ex) { Console.WriteLine(ex); }
			//});
		}
		/// <summary>
		/// Создание пакета данных с БД
		/// </summary>
		/// <param name="dataBasePacket"></param>
		/// <returns></returns>
		private DataBasePacket CreateDataBasePacketDataTable(DataBasePacket dataBasePacket)
		{
			var dt = PathsToDataBase.FirstOrDefault((i) => i.Path == dataBasePacket.Path);
			if (dt == null)
			{
				dataBasePacket.Info = InfoDataBasePacket.NotExistsFile;
				return dataBasePacket;
			}
			if (dt.IsEnableStatus != StatusConnectDataBase.ConnectAccess)
			{
				dataBasePacket.Info = InfoDataBasePacket.IsNotWork;
				return dataBasePacket;
			}
			dataBasePacket.Type = TypeDataBasePacket.GetTables;
			dataBasePacket.Data = dt.DataBase.SendQuery($"SELECT * FROM {dataBasePacket.Data}");
			Console.WriteLine(dataBasePacket.Data);
			return dataBasePacket;
		}
		/// <summary>
		/// Создание пакета данных с всей БД
		/// </summary>
		/// <param name="dataBasePacket"></param>
		/// <returns></returns>
		private DataBasePacket CreateDataBasePacketTables(DataBasePacket dataBasePacket)
		{
			if (PathsToDataBase.Count == 0)
			{
				dataBasePacket.Info = InfoDataBasePacket.NotExistsFile;
				return dataBasePacket;
			}
			if (PathsToDataBase[0].IsEnableStatus != StatusConnectDataBase.ConnectAccess)
			{
				dataBasePacket.Info = InfoDataBasePacket.IsNotWork;
				return dataBasePacket;
			}
			dataBasePacket.Data = PathsToDataBase[0].DataBase.GetTablesDT();
			dataBasePacket.Type = API.TypeDataBasePacket.GetTables;
			return dataBasePacket;
		}
		/// <summary>
		/// Отправка пользователям измененую таблицу
		/// </summary>
		private void SendBroadcast()
		{
			Log.WriteLine($"Start SendBroadcast");
			while (Server.Work)
			{
				List<string> remove = new List<string>();
				try
				{
					foreach (var i in BroadcastTable)
					{
						if ((DateTime.Now - i.Value).TotalMilliseconds > 250)
						{
							var table = PathsToDataBase[0].DataBase.SendQuery($"SELECT * FROM [{i.Key}]");
							table.TableName = i.Key;
							Server.BroadcastSend(new Packet() { TypePacket = TypePacket.UpdateTable, Data = new API.SQLQueryPacket() { Data = table, TableName = i.Key } });
							remove.Add(i.Key);
						}
					}
				}
				catch (Exception e) { Log.WriteLine(e); }
				foreach (var i in remove) BroadcastTable.Remove(i);
				Thread.Sleep(250);
			}
			Log.WriteLine($"End SendBroadcast");
		}
		/// <summary>
		///  Получение ответа от клиента
		/// </summary>
		/// <param name="Packet"></param>
		private void Answer(API.Packet Packet, TcpClient client, API.CipherAES cipherAES, Models.SettingsServer.Client client_auth)
		{
			switch (Packet.TypePacket)
			{
				case TypePacket.GetPathsDataBase:
					List<string> paths = new List<string>();
					foreach (var i in PathsToDataBase) paths.Add(i.Path);
					Packet.Data = paths;
					API.Base.SendPacketClient(client, Packet, cipherAES);
					break;
				case TypePacket.ConnectDataBase:
					try
					{
						Packet.Data = CreateDataBasePacketTables(new DataBasePacket());
						API.Base.SendPacketClient(client, Packet, cipherAES);
					}
					catch (Exception ex) { Console.WriteLine(ex); }
					break;
				case TypePacket.AllocTable:
					try
					{
						//Packet.Data = CreateDataBasePacketDataTable((API.DataBasePacket)Packet.Data);
						//API.Base.SendPacketClient(client, Packet, cipherAES);
					}
					catch (Exception ex) { Console.WriteLine(ex); }
					break;
				case TypePacket.SQLQuery:
					try
					{
						var sql_packet = (API.SQLQueryPacket)Packet.Data;
						Packet.Data = PathsToDataBase[0].DataBase.SendQuery(sql_packet.Data.ToString()); ;
						Packet.TypePacket = TypePacket.SQLQueryOK;
						API.Base.SendPacketClient(client, Packet, cipherAES);

						//if (sql_packet.TypeSQLQuery == TypeSQLQuery.Broadcast) Server.BroadcastSend(new Packet() { TypePacket = TypePacket.UpdateTable, Data = new API.SQLQueryPacket() {Data = table, TableName = sql_packet.TableName } }, new List<Client>() { client_auth});
						//else Server.BroadcastSend(new Packet() { TypePacket = TypePacket.UpdateTable, Data = new API.SQLQueryPacket() { Data = table, TableName = sql_packet.TableName } });
						if (!BroadcastTable.ContainsKey(sql_packet.TableName)) BroadcastTable.Add(sql_packet.TableName, DateTime.Now);
						else BroadcastTable[sql_packet.TableName] = DateTime.Now;
						//Task.Run(() =>
						//{
						//	var table = PathsToDataBase[0].DataBase.SendQuery($"SELECT * FROM [{sql_packet.TableName}]");
						//	table.TableName = sql_packet.TableName;
						//	Thread.Sleep(20);
						//	Server.BroadcastSend(new Packet() { TypePacket = TypePacket.UpdateTable, Data = new API.SQLQueryPacket() { Data = table, TableName = sql_packet.TableName } });
						//});
					}
					catch (Exception e)
					{
						Packet.Data = e;
						Packet.TypePacket = TypePacket.SQLQueryError;
						API.Base.SendPacketClient(client, Packet, cipherAES);
					}
					break;
			}
			Log.WriteLine(Packet);
		}
	}
	/// <summary>
	/// Информация о подключенной БД
	/// </summary>
	public enum StatusConnectDataBase : int
	{
		CanConnect = 3,
		ConnectAccess = 1,
		NotWork = 0,
		FileIsAlloc = 2,
		TableIsAlloc = 4,
	}
	/// <summary>
	/// Класс представления подключеной БД
	/// </summary>
	public class DataBaseConnectPath : Base.ViewModel.BaseViewModel
	{
		private string _Path;
		public string Path
		{
			get => _Path;
			set
			{
				Set(ref _Path, value);
				DataBase.SetPath(Path);
			}
		}

		public IDataBase DataBase { get; set; } = new SQLDataBase();//new DataBase();

		private StatusConnectDataBase _IsEnableStatus;
		public StatusConnectDataBase IsEnableStatus
		{
			get => _IsEnableStatus; set
			{
				Set(ref _IsEnableStatus, value);
				switch (IsEnableStatus)
				{
					case StatusConnectDataBase.CanConnect:
						Foreground = Brushes.YellowGreen;
						break;
					case StatusConnectDataBase.ConnectAccess:
						Foreground = Brushes.Green;
						break;
					case StatusConnectDataBase.NotWork:
						Foreground = Brushes.Red;
						break;
					case StatusConnectDataBase.FileIsAlloc:
						Foreground = Brushes.Orange;
						break;
					case StatusConnectDataBase.TableIsAlloc:
						Foreground = Brushes.Orange;
						break;
				}
			}
		}

		public string ShortPath
		{
			get
			{
				var split_list = Path.Split('\\');
				if (split_list.Length <= 2) return Path;
				split_list = split_list.Skip(split_list.Length - 2).ToArray();
				return String.Join("\\", split_list);
			}
		}
		private Brush _Foreground;
		public Brush Foreground { get => _Foreground; set => Set(ref _Foreground, value); }
	}
}
