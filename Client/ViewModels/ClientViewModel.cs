using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DataBaseClientServer;
using DataBaseClientServer.Base.Command;
using API;
using API.XML;
using DataBaseClientServer.Models;
using DataBaseClientServer.Models.Settings;
using System.IO;
using Client;
using System.Windows;

namespace DataBaseClientServer.ViewModels
{
	class ClientViewModel : Base.ViewModel.BaseViewModel
	{
		private ClientSerttings _ClientSerttings = new ClientSerttings();
		public ClientSerttings ClientSerttings { get => _ClientSerttings; set => Set(ref _ClientSerttings, value); }

		private Models.Client _Client = new Models.Client();
		public Models.Client Client { get => _Client; set => Set(ref _Client, value); }
		private byte[] KEY_LOAD = new byte[16] { 0xaa, 0x11, 0x23, 0x54, 0x32, 0x40, 0x10, 0x01, 0xd, 0xdd, 0x23, 0x90, 0x01, 0x12, 0x11, 0x02 };
		private byte[] IV_LOAD = new byte[16] { 0xaa, 0x01, 0x0f, 0x00, 0x0b, 0x30, 0x03, 0x00, 0x60, 0x60, 0x40, 0x67, 0x01, 0x05, 0x80, 0x0f };

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

		#region Kernel
		public ClientViewModel()
		{
			ProviderXML.IV_AES = IV_LOAD;
			ProviderXML.KEY_AES = KEY_LOAD;
			ConnectServerCommand = new LambdaCommand(OnConnectServerCommand, CanConnectServerCommand);
			PingServerCommand = new LambdaCommand(OnPingServerCommand, CanPingServerCommand);
			AuthorizationServerCommand = new LambdaCommand(OnAuthorizationServerCommand, CanAuthorizationServerCommand);
			Client.CallAnswer += Answer;
			Client.ClientSerttings = ClientSerttings;
			App.Current.Exit += Current_Exit;
			Console.WriteLine("Start");
			Task.Run(() => { LoadXML(); });
		}

		private void Current_Exit(object sender, System.Windows.ExitEventArgs e)
		{
			SaveXML();
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
		}
		void SaveXML()
		{
			Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\Settings");
			try
			{
				ProviderXML.Save<ClientConnect>($"{Directory.GetCurrentDirectory()}\\Settings\\ConnectClient.xml", ClientSerttings.ClientConnect);
			}
			catch { }
		}
		#endregion

		#region Commands
		#region AuthorizationServerCommand
		public ICommand AuthorizationServerCommand { get; set; }
		public bool CanAuthorizationServerCommand(object e) => Client != null && Client.StatusClient == StatusClient.Connected && !Client.IsAuthorization;
		public void OnAuthorizationServerCommand(object e)
		{
			Console.WriteLine(Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.Authorization, Data = new API.Authorization() { Login = "test", Password = "test" } }));
			//Console.WriteLine(Client.Ping().TotalMilliseconds);
		}
		#endregion
		#region PingServerCommand
		public ICommand PingServerCommand { get; set; }
		public bool CanPingServerCommand(object e) => Client != null && Client.StatusClient == StatusClient.Connected;
		public void OnPingServerCommand(object e)
		{
			Console.WriteLine(Client.SendPacketAndWaitResponse(new API.Packet() { TypePacket = API.TypePacket.Ping }));
			//Console.WriteLine(Client.Ping().TotalMilliseconds);
		}
		#endregion
		#region ConnectServerCOmmand
		public ICommand ConnectServerCommand { get; set; }
		public bool CanConnectServerCommand(object e) => Client.StatusClient == StatusClient.Disconnected;
		public void OnConnectServerCommand(object e)
		{
			Task.Run(() => { ConnectClient(); });
		}
		#endregion
		#endregion
		private void Answer(API.Packet Packet)
		{
			Console.WriteLine(Packet);
		}
		private void ConnectClient()
		{
			Client.Connect();
		}
	}
}
