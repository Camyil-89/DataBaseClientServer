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
		#region Commands
		#region LoadServerCOmmand
		public ServerViewModel()
		{
			#region Commands
			LoadServerCommand = new LambdaCommand(OnLoadServerCommand, CanLoadServerCommand);
			#endregion

			Console.WriteLine("Start server");
		}
		public ICommand LoadServerCommand { get; set; }
		public bool CanLoadServerCommand(object e) => true;
		public void OnLoadServerCommand(object e)
		{
			
		}
		#endregion
		#endregion
	}
}
