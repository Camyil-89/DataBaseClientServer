using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DataBaseClientServer;
using DataBaseClientServer.Base.Command;
using DataBaseClientServer.Models;

namespace DataBaseClientServer.ViewModels
{
	class ClientViewModel : Base.ViewModel.BaseViewModel
	{
		private Models.Client _Client = new Models.Client();
		public Models.Client Client { get => _Client; set => Set(ref _Client, value); }
		public ClientViewModel()
		{
			ConnectServerCommand = new LambdaCommand(OnConnectServerCommand, CanConnectServerCommand);
			PingServerCommand = new LambdaCommand(OnPingServerCommand, CanPingServerCommand);
			AuthorizationServerCommand = new LambdaCommand(OnAuthorizationServerCommand, CanAuthorizationServerCommand);
			Console.WriteLine("Start");
		}
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
			Client = new Models.Client();
			//Client.IPAddress = IPAddress.Parse("10.102.250.252");
			Client.IPAddress = IPAddress.Parse("192.168.1.79");
			Client.CallAnswer += Answer;
			Client.Connect();
		}
	}
}
