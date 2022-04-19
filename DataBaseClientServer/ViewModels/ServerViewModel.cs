using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DataBaseClientServer;
using DataBaseClientServer.Base.Command;

namespace DataBaseClientServer.ViewModels
{
	class ServerViewModel: Base.ViewModel.BaseViewModel
	{

		private Models.Server Server;
		public ServerViewModel()
		{
			Log.WriteLine("ServerViewModel");
			#region Commands
			LoadServerCommand = new LambdaCommand(OnLoadServerCommand, CanLoadServerCommand);
			#endregion
			
			

			App.Current.Exit += Current_Exit;
			Log.WriteLine("Start server");
			StartServer();
		}

		private void Current_Exit(object sender, System.Windows.ExitEventArgs e)
		{
			Server.DisposeClients();
		}
		#region Commands
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
			Task.Run(() => {
				Thread.Sleep(1000);
				try
				{
					API.Packet packet = new API.Packet() { TypePacket = API.TypePacket.Ping };
					var x = API.Packet.ToByteArray(packet);
					Console.WriteLine($">{x.Length}");

					var g = API.Packet.FromByteArray(x);
					Console.WriteLine(1312312);
					Console.WriteLine(g);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			});
			Server = new Models.Server();
			Server.CallAnswer += Answer;
			Server.Start();
		}
		private void Answer(API.Packet Packet)
		{
			Log.WriteLine(Packet);
		}
	}
}
