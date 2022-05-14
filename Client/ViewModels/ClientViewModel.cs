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
		public string PasswordBoxUser { get => _PasswordBoxUser; set
			{
				Set(ref _PasswordBoxUser, value);
				if (CheckBoxHide) ClientSerttings.KernelSettings.PasswordUser = value;
			} }

		private bool _CheckBoxHide = false;
		public bool CheckBoxHide { get => _CheckBoxHide; set {
				Set(ref _CheckBoxHide, value);
				if (value) PasswordBoxUser = ClientSerttings.KernelSettings.PasswordUser;
				else PasswordBoxUser = "";
			} }
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


		public AddToDataBaseVM AddToDataBaseVM;

		private bool PingWork = false;
		private bool PingStop = false;
		#endregion
		#region Kernel
		public ClientViewModel()
		{
			AddToDataBaseVM = new AddToDataBaseVM(this);
			BindingOperations.EnableCollectionSynchronization(PathsToDataBase, _lock); // доступ из всех потоков
			ProviderXML.IV_AES = IV_LOAD;
			ProviderXML.KEY_AES = KEY_LOAD;
			ConnectServerCommand = new LambdaCommand(OnConnectServerCommand, CanConnectServerCommand);
			PingServerCommand = new LambdaCommand(OnPingServerCommand, CanPingServerCommand);
			AuthorizationServerCommand = new LambdaCommand(OnAuthorizationServerCommand, CanAuthorizationServerCommand);
			DisconnectServerCommand = new LambdaCommand(OnDisconnectServerCommand, CanDisconnectServerCommand);
			ConnectDataBaseCommand = new LambdaCommand(OnConnectDataBaseCommand, CanConnectDataBaseCommand);
			AddDBBookCommand = new LambdaCommand(OnAddDBBookCommand, CanAddDBBookCommand);
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
		private void SetSettingsClient()
		{
			SelectPathToDataBase = null;
			PathsToDataBase.Clear();
			Client.CallAnswer += Answer;
			Client.CallDisconnect += Disconnect;
			Client.ClientSerttings = ClientSerttings;
		}
		private void Current_Exit(object sender, System.Windows.ExitEventArgs e)
		{
			SaveXML();
		}
		private void InfoConnectTextUpdate()
		{
			while (true)
			{
				switch (Client.StatusClient)
				{
					case StatusClient.Connected:
						InfoConnectText = "Подключение установлено!";
						break;
					case StatusClient.Disconnected:
						InfoConnectText = "Подключение не установлено!";
						break;
					case StatusClient.Connecting:
						InfoConnectText = "Подключение..";
						break;
				}
				Thread.Sleep(250);
			}
		}
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
			catch (Exception ex) { ClientSerttings.ClientConnect= new ClientConnect(); Log.WriteLine(ex); }
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
		#region AddDBBookCommand
		public ICommand AddDBBookCommand { get; set; }
		public bool CanAddDBBookCommand(object e) => true;
		public void OnAddDBBookCommand(object e)
		{
			OpenAddDBWindow(AddType.AddBook);
		}
		#endregion
		public void OpenAddDBWindow(AddType type)
		{
			AddToDataBaseWindow window = new AddToDataBaseWindow();
			window.DataContext = AddToDataBaseVM;
			AddToDataBaseVM.AddDBType = type;
			AddToDataBaseVM.Window = window;
			window.ShowInTaskbar = false;
			window.ShowDialog();
		}
		#region ConnectDataBaseCommand
		public ICommand ConnectDataBaseCommand { get; set; }
		public bool CanConnectDataBaseCommand(object e) => Client != null && 
			Client.StatusClient == StatusClient.Connected &&
			Client.IsAuthorization &&
			!BlockAllWorkInDataBase;
		public void OnConnectDataBaseCommand(object e)
		{
			Task.Run(() => {
				ConnectDataBase();
			});
		}
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
		#region AuthorizationServerCommand
		public ICommand AuthorizationServerCommand { get; set; }
		public bool CanAuthorizationServerCommand(object e) => Client != null && Client.StatusClient == StatusClient.Connected && !Client.IsAuthorization;
		public void OnAuthorizationServerCommand(object e)
		{
			Console.WriteLine(Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.Authorization, Data = new API.Authorization() { Login = "test", Password = "test" } }, 1));
			//Console.WriteLine(Client.Ping().TotalMilliseconds);
		}
		#endregion
		#region DisconnectServerCommand
		public ICommand DisconnectServerCommand { get; set; }
		public bool CanDisconnectServerCommand(object e) => Client != null && Client.StatusClient == StatusClient.Connected;
		public void OnDisconnectServerCommand(object e)
		{
			DisconnectClient();
		}
		#endregion
		#region PingServerCommand
		public ICommand PingServerCommand { get; set; }
		public bool CanPingServerCommand(object e) => Client != null && Client.StatusClient == StatusClient.Connected;
		public void OnPingServerCommand(object e)
		{
			Console.WriteLine(Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.Ping }, 1));
			//Console.WriteLine(Client.Ping().TotalMilliseconds);
		}
		#endregion
		#region ConnectServerCOmmand
		public ICommand ConnectServerCommand { get; set; }
		public bool CanConnectServerCommand(object e) => Client != null && Client.StatusClient == StatusClient.Disconnected;
		public void OnConnectServerCommand(object e)
		{
			Task.Run(() => { ConnectClient(); });
		}
		#endregion
		#endregion
		private void Disconnect(string message)
		{
			PingToServer = -1;
			if (message != null) InfoConnectText = message;
		}
		private void Answer(API.Packet Packet)
		{
			Console.WriteLine(Packet);
		}
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
		private void DisconnectClient()
		{
			Client.Disconnect();
			Client = new Models.Client();
			Disconnect(null);
		}
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
					var packet = Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.Authorization,
						Data = new API.Authorization() { Login = ClientSerttings.KernelSettings.LoginUser, Password = ClientSerttings.KernelSettings.PasswordUser} }, 1);
					if (packet.Packets[0].TypePacket == API.TypePacket.AuthorizationFailed)
					{
						//var data_answer = (API.TypeErrorAuthorization)packet.Packets[0].Data;
						//if (data_answer == API.TypeErrorAuthorization.Login) InfoConnectText = "Неверный логин!";
						//else if (data_answer == API.TypeErrorAuthorization.Passsword) InfoConnectText = "Неверный пароль!";
						Client = new Models.Client();
					}
					else
					{
						//Thread.Sleep(1000);
						Task.Run(() => { 
							while (PingWork)
							{
								PingStop = true;
								Thread.Sleep(1);
							}
							PingStop = false;
							PingServer();
						});
						ConnectDataBase();
						//foreach (var i in (List<string>)Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.GetPathsDataBase }, 1).Packets[0].Data)
						//{
						//	PathsToDataBase.Add(i);
						//}

					}
				}
			}
			GC.Collect();
		}
	}
}
