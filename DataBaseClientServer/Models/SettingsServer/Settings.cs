using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseClientServer.Models.SettingsServer
{
	public class Settings: Base.ViewModel.BaseViewModel
	{
		private ServerSettings _ServerSettings = new ServerSettings();
		public ServerSettings ServerSettings { get => _ServerSettings; set => Set(ref _ServerSettings, value); }
		public ObservableCollection<Client> Clients { get; set; } = new ObservableCollection<Client>();
	}
	public class ServerSettings: Base.ViewModel.BaseViewModel
	{
		private bool _AutoStartServer;
		public bool AutoStartServer { get => _AutoStartServer; set => Set(ref _AutoStartServer, value); }
	}
}
