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

namespace DataBaseClientServer.ViewModels
{
	class ServerViewModel : Base.ViewModel.BaseViewModel
	{

		private Server _Server = new Server();
		public Server Server { get => _Server; set => Set(ref _Server, value); }

		public Settings _Settings = new Settings();
		public Settings Settings { get => _Settings; set => Set(ref _Settings, value); }

		private static object _lock = new object();
		#region Kernel
		public ServerViewModel()
		{
			Log.WriteLine("ServerViewModel");
			#region Commands
			LoadServerCommand = new LambdaCommand(OnLoadServerCommand, CanLoadServerCommand);
			StartServerListenerCommand = new LambdaCommand(OnStartServerListenerCommand, CanStartServerListenerCommand);
			StopServerListenerCommand = new LambdaCommand(OnStopServerListenerCommand, CanStopServerListenerCommand);
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
				if (File.Exists($"{Directory.GetCurrentDirectory()}\\Settings\\Clients.xml"))
				{
					Console.WriteLine($"{Directory.GetCurrentDirectory()}\\Settings\\Clients.xml");
					Settings.Clients = ProviderXML.Load<ObservableCollection<Client>>($"{Directory.GetCurrentDirectory()}\\Settings\\Clients.xml");
				}
			}
			catch { Settings.Clients = new ObservableCollection<Client>(); }
			try
			{
				if (File.Exists($"{Directory.GetCurrentDirectory()}\\Settings\\Settings.xml"))
				{
					Console.WriteLine($"{Directory.GetCurrentDirectory()}\\Settings\\Settings.xml");
					Settings.ServerSettings = ProviderXML.Load<ServerSettings>($"{Directory.GetCurrentDirectory()}\\Settings\\Settings.xml");
				}
			}
			catch { Settings.ServerSettings = new ServerSettings(); }
			Task.Run(() => { StartServer(); });
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
			Server.Start();

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
