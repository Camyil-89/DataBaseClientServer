using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DataBaseClientServer;
using DataBaseClientServer.Base.Command;

namespace DataBaseClientServer.ViewModels
{
	class ServerViewModel: Base.ViewModel.BaseViewModel
	{

		private Models.Server Server;
		#region Commands
		#region LoadServerCOmmand
		public ServerViewModel()
		{
			Log.WriteLine("ServerViewModel");
			#region Commands
			LoadServerCommand = new LambdaCommand(OnLoadServerCommand, CanLoadServerCommand);
			#endregion
			Log.WriteLine("Start server");
			StartServer();
		}
		public ICommand LoadServerCommand { get; set; }
		public bool CanLoadServerCommand(object e) => true;
		public void OnLoadServerCommand(object e)
		{
			
		}
		#endregion
		#endregion
		private void StartServer()
		{
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
