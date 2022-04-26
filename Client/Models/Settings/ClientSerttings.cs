using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBaseClientServer.Base;
using DataBaseClientServer.Base.ViewModel;

namespace DataBaseClientServer.Models.Settings
{
	public class ClientSerttings : BaseViewModel
	{
		private ClientConnect _ClientConnect = new ClientConnect();
		public ClientConnect ClientConnect { get => _ClientConnect; set => Set(ref _ClientConnect, value); }

		private KernelSettings _KernelSettings = new KernelSettings();
		public KernelSettings KernelSettings { get => _KernelSettings; set => Set(ref _KernelSettings, value); }
	}
	public class KernelSettings: BaseViewModel
	{
		private byte[] _IV_AES = API.CipherAES.IV_base;
		public byte[] IV_AES { get => _IV_AES; set => Set(ref _IV_AES, value); }


		private byte[] _Key_AES = API.CipherAES.KEY_base;
		public byte[] KeyAES { get => _Key_AES; set => Set(ref _Key_AES, value); }
	}
	public class ClientConnect : BaseViewModel
	{
		private byte[] _IPAddressServer = new byte[] { 0, 0, 0, 0 };
		public byte[] IPAddressServer { get => _IPAddressServer; set => Set(ref _IPAddressServer, value); }

		private int _Port = 32001;
		public int Port { get => _Port; set => Set(ref _Port, value); }
	}
}
