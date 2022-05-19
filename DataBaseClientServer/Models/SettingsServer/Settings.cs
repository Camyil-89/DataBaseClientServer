using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace DataBaseClientServer.Models.SettingsServer
{
	/// <summary>
	/// Настройки
	/// </summary>
	[Serializable]
	public class Settings: Base.ViewModel.BaseViewModel
	{
		private ServerSettings _ServerSettings = new ServerSettings();
		public ServerSettings ServerSettings { get => _ServerSettings; set => Set(ref _ServerSettings, value); }
		public ObservableCollection<Client> Clients { get; set; } = new ObservableCollection<Client>();
	}
	/// <summary>
	/// Настройки
	/// </summary>
	[Serializable]
	public class ServerSettings: Base.ViewModel.BaseViewModel
	{
		private byte[] _IV_AES = API.CipherAES.IV_base;
		public byte[] IV_AES { get => _IV_AES; set => Set(ref _IV_AES, value);}


		private byte[] _Key_AES = API.CipherAES.KEY_base;
		public byte[] KeyAES { get => _Key_AES; set => Set(ref _Key_AES, value); }


		private bool _AutoStartServer = false;
		public bool AutoStartServer { get => _AutoStartServer; set => Set(ref _AutoStartServer, value); }

		private ObservableCollection<string> _PathsToDataBase = new ObservableCollection<string>();
		public ObservableCollection<string> PathsToDataBase { get => _PathsToDataBase; set => Set(ref _PathsToDataBase, value); }
	}
}
