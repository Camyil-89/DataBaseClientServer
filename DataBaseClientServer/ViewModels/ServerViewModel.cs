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

namespace DataBaseClientServer.ViewModels
{
	class ServerViewModel : Base.ViewModel.BaseViewModel
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

		private int _SelectMode = 0;
		public int SelectMode
		{
			get => _SelectMode;
			set
			{
				Set(ref _SelectMode, value);
				if (value == 0)
				{
					UserName = "";
					UserPassword = "";
					AccessLevelUser = -1;
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

		private int _SelectUser = -1;
		public int SelectUser
		{
			get => _SelectUser;
			set
			{
				Set(ref _SelectUser, value);
				if (SelectUser == -1) return;
				UserName = Settings.Clients[SelectUser].Name;
				UserPassword = Settings.Clients[SelectUser].Password;
				switch (Settings.Clients[SelectUser].AccessLevel)
				{
					case API.AccessLevel.User:
						AccessLevelUser = 0;
						break;
					case API.AccessLevel.Admin:
						AccessLevelUser = 1;
						break;
				}
			}
		}

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
						_AccessLevelUser = API.AccessLevel.Admin;
						break;
				}
			}
		}
		private string _UserName = "";
		public string UserName { get => _UserName; set => Set(ref _UserName, value); }

		private string _UserPassword = "";
		public string UserPassword { get => _UserPassword; set => Set(ref _UserPassword, value); }
		#endregion


		private static object _lock = new object();
		#region Kernel
		public ServerViewModel()
		{
			Log.WriteLine("ServerViewModel");
			#region Commands
			LoadServerCommand = new LambdaCommand(OnLoadServerCommand, CanLoadServerCommand);
			StartServerListenerCommand = new LambdaCommand(OnStartServerListenerCommand, CanStartServerListenerCommand);
			StopServerListenerCommand = new LambdaCommand(OnStopServerListenerCommand, CanStopServerListenerCommand);
			AddUserCommand = new LambdaCommand(OnAddUserCommand, CanAddUserCommand);
			#endregion

			BindingOperations.EnableCollectionSynchronization(Server.tcpClients, _lock); // доступ из всех потоков

			Server.CallAnswer += Answer;
			foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					Server.IPAddress = ip;
					break;
				}
			}
			App.Current.Exit += Current_Exit;
			Task.Run(() => { LoadXML(); });
		}

		private void Current_Exit(object sender, System.Windows.ExitEventArgs e)
		{
			Server.DisposeClients();
			SaveXML();
		}
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
			if (Settings.ServerSettings.AutoStartServer) Task.Run(() => { StartServer(); });
			Log.WriteLine("LoadXML: true");
			//Settings.Clients.Add(new Client() { AccessLevel = API.AccessLevel.Admin, Name = "test2", Password = "123", UID = Client.GenerateUIDClient(Settings.Clients)});
			//Settings.Clients.Add(new Client() { AccessLevel = API.AccessLevel.User, Name = "test2", Password = "123", UID = Client.GenerateUIDClient(Settings.Clients)});
		}
		private void SaveXML()
		{
			Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\Settings");
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
		#region AddUser
		public ICommand AddUserCommand { get; set; }
		public bool CanAddUserCommand(object e)
		{
			switch (SelectMode)
			{
				case 0:
					return UserName != "" && UserPassword != "" && AccessLevelUser != -1;
				case 1:
					return SelectUser != -1 && SelectUser < Settings.Clients.Count;
				case 2:
					return SelectUser != -1 && UserName != "" && UserPassword != "" && AccessLevelUser != -1;
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
		private void ChangeUser()
		{
			var find = Settings.Clients.FirstOrDefault((i) => i.Name == UserName);
			if (find == null)
			{
				Settings.Clients[SelectUser].Name = UserName;
				Settings.Clients[SelectUser].Password = UserPassword;
				Settings.Clients[SelectUser].AccessLevel = _AccessLevelUser;
			}
			else MessageBox.Show($"Пользователь с именем \"{UserName}\" уже добавлен", "Уведомление");
		}
		private void RemoveUser()
		{
			var answer = MessageBox.Show("Вы уверены что хотите удалить пользователя?", "Уведомление", MessageBoxButton.YesNo);
			if (answer != MessageBoxResult.Yes) return;
			Settings.Clients.RemoveAt(SelectUser);
		}
		private void AddUser()
		{
			var find = Settings.Clients.FirstOrDefault((i) => i.Name == UserName);
			if (find == null)
			{
				Settings.Clients.Add(new Client() { Name = UserName, Password = UserPassword, AccessLevel = _AccessLevelUser, UID = Client.GenerateUIDClient(Settings.Clients) });
				UserName = "";
				UserPassword = "";
				AccessLevelUser = -1;
			}
			else MessageBox.Show($"Пользователь с именем \"{UserName}\" уже добавлен", "Уведомление");
		}
		#endregion
		#region StopServerListenerCommand
		public ICommand StopServerListenerCommand { get; set; }
		public bool CanStopServerListenerCommand(object e) => Server.Work;
		public void OnStopServerListenerCommand(object e)
		{
			Log.WriteLine("Stop server");
			Server.DisposeClients();
		}
		#endregion
		#region StartServerListener
		public ICommand StartServerListenerCommand { get; set; }
		public bool CanStartServerListenerCommand(object e) => !Server.Work;
		public void OnStartServerListenerCommand(object e)
		{
			StartServer();
		}
		#endregion
		#region LoadServerCOmmand

		public ICommand LoadServerCommand { get; set; }
		public bool CanLoadServerCommand(object e) => true;
		public void OnLoadServerCommand(object e)
		{

		}
		#endregion
		#endregion
		private void StartServer()
		{
			Log.WriteLine("Start server");
			Server.Start(Settings.ServerSettings.KeyAES, Settings.ServerSettings.IV_AES);

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
		private void Answer(API.Packet Packet)
		{
			Log.WriteLine(Packet);
		}
	}
}
