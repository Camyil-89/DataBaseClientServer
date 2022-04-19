using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DataBaseClientServer;
using DataBaseClientServer.Base.Command;

namespace DataBaseClientServer.ViewModels
{
	class ClientViewModel : Base.ViewModel.BaseViewModel
	{
		private Models.Client Client;
		#region Commands
		#region LoadServerCOmmand
		public ClientViewModel()
		{
			ConnectServerCommand = new LambdaCommand(OnConnectServerCommand, CanConnectServerCommand);
			Console.WriteLine("Start");
		}
		public ICommand ConnectServerCommand { get; set; }
		public bool CanConnectServerCommand(object e) => Client == null || !Client.Connected;
		public void OnConnectServerCommand(object e)
		{
			ConnectClient();
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
			Client.IPAddress = IPAddress.Parse("192.168.1.79");
			Client.CallAnswer += Answer;
			Client.Connect();
		}
	}
}
