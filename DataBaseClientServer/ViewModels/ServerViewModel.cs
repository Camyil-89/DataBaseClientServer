using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DataBaseClientServer;
using DataBaseClientServer.Base.Command;
using DataBaseClientServer.Models.database;

namespace DataBaseClientServer.ViewModels
{
	class ServerViewModel: Base.ViewModel.BaseViewModel
	{

		private Models.Server _Server = new Models.Server();
		public Models.Server Server { get => _Server; set => Set(ref _Server, value); }
		public ServerViewModel()
		{
			Log.WriteLine("ServerViewModel");
			#region Commands
			LoadServerCommand = new LambdaCommand(OnLoadServerCommand, CanLoadServerCommand);
			StartServerListenerCommand = new LambdaCommand(OnStartServerListenerCommand, CanStartServerListenerCommand);
			StopServerListenerCommand = new LambdaCommand(OnStopServerListenerCommand, CanStopServerListenerCommand);
			#endregion


			Server.CallAnswer += Answer;
			App.Current.Exit += Current_Exit;
		}

		private void Current_Exit(object sender, System.Windows.ExitEventArgs e)
		{
			Server.DisposeClients();
		}
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

			Task.Run(() => {
				Thread.Sleep(1000);
				try
				{
					DataBase dataBase = new DataBase();
					dataBase.Connect();
					dataBase.SendQuery("SELECT * FROM таблица"); //ID_таблица
					//dataBase.SendQuery("DELETE FROM таблица WHERE ID_таблица = 4"); //ID_таблица
					//dataBase.SendQuery("INSERT INTO таблица(название, размер) VALUES('Михаил', 20);"); //ID_таблица
					//dataBase.SendQuery("SELECT * FROM таблица"); //ID_таблица
				}
				catch (Exception ex) { Console.WriteLine(ex); }	
			});
		}
		private void Answer(API.Packet Packet)
		{
			Log.WriteLine(Packet);
		}
	}
}
