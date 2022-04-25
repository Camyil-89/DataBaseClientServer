using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using DataBaseClientServer.Models.database;
using System.Collections.ObjectModel;
using DataBaseClientServer.Models.SettingsServer;
using DataBaseClientServer.ViewModels;

namespace DataBaseClientServer.Models
{
	public class Server: Base.ViewModel.BaseViewModel
	{
		public bool Work { get; set; } = false;

		private int _port = 32001;
		public int Port { get => _port; set => Set(ref _port, value); }
		public int CountClient { get => tcpClients.Count; }

		private IPAddress _IPAddress = IPAddress.Parse("127.0.0.0");
		public IPAddress IPAddress { get => _IPAddress; set => Set(ref _IPAddress, value); }

		public delegate void Answer(API.Packet Packet);
		public event Answer CallAnswer;
		public int SizeBuffer = 2048;


		public DataBase AccessDataBase = new DataBase();

		public ServerViewModel ServerViewModel { get; set; }

		private int CountPacketReciveToUpdateKey = 10; // min 2

		public ObservableCollection<TCPClient> tcpClients { get; set; } = new ObservableCollection<TCPClient>();

		private TcpListener tcpListener;

		public Client GetClientFromDataBase(string Login, string Password)
		{
			return ServerViewModel.Settings.Clients.FirstOrDefault((i) => i.Name == Login && i.Password == Password);
		}
		public bool FindUserInDataBase(string Login, string Password)
		{
			return GetClientFromDataBase(Login, Password) != null;
		}

		/// <summary>
		/// Запуск сервера и сокета для прослушивания
		/// </summary>
		public void Start(byte[] Key_aes, byte[] IV_aes)
		{
			tcpListener = new TcpListener(IPAddress.Any, Port);
			tcpListener.Start();
	
			Log.WriteLine($"IP: {IPAddress}");
			Log.WriteLine($"Port: {Port}");
			Work = true;
			Task.Run(() => {
				while (Work)
				{
					var client = tcpListener.AcceptTcpClient();
					Log.WriteLine($"Connect client: {client.Client.RemoteEndPoint}");
					Task.Run(() => { StartClient(client, Key_aes, IV_aes); });
				}
			});
		}
		/// <summary>
		/// Освобождение ресурсов и отключение всех клиетов
		/// </summary>
		public void DisposeClients()
		{
			Log.WriteLine("Dispose Client");
			if (tcpListener != null) tcpListener.Stop();
			Work = false;
			foreach (var i in tcpClients)
			{
				Log.WriteLine($"Dispose {i.Client.Client.RemoteEndPoint}");
				if (i.Client.Connected) API.Base.SendPacketClient(i.Client, new API.Packet() { TypePacket = API.TypePacket.Termination }, i.CipherAES);
				i.Client.Dispose();
			}
			tcpClients.Clear();
		}
		/// <summary>
		/// Подключение клиента
		/// </summary>
		void StartClient(TcpClient Client, byte[] Key_aes, byte[] IV_aes)
		{
			NetworkStream networkStream = Client.GetStream();
			API.CipherAES cipherAES = new API.CipherAES();
			API.CipherAES updateCipherAES = new API.CipherAES();
			cipherAES.AES_KEY = Key_aes;
			cipherAES.AES_IV = IV_aes;
			TCPClient tCPClient = new TCPClient() { Client = Client, CipherAES = cipherAES};
			tcpClients.Add(tCPClient);
			bool IsAuthorization = false;
			int CountPacketRecive = 0;
			while (Client.Connected)
			{
				try
				{
					byte[] myReadBuffer = new byte[SizeBuffer];
					do
					{
						CountPacketRecive++;
						networkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
					}
					while (networkStream.DataAvailable);
					Log.WriteLine($"[{Client.Client.RemoteEndPoint}] Packet lenght: {myReadBuffer.Length}");
					API.Packet packet = API.Packet.FromByteArray(myReadBuffer, cipherAES);
					if (!IsAuthorization && API.Base.IsAuthorizationClientUse.Contains(packet.TypePacket)) continue; 
					switch (packet.TypePacket)
					{
						case API.TypePacket.Ping:
							API.Base.SendPacketClient(Client, packet, cipherAES);
							break;
						case API.TypePacket.UpdateKey:
							updateCipherAES = new API.CipherAES();
							updateCipherAES.CreateKey();
							API.Base.SendPacketClient(Client, new API.Packet() { Data = updateCipherAES, TypePacket = API.TypePacket.UpdateKey }, cipherAES);
							CountPacketRecive = 0;
							break;
						case API.TypePacket.ConfirmKey:
							cipherAES = updateCipherAES;
							tCPClient.CipherAES = cipherAES;
							Log.WriteLine($"[{Client.Client.RemoteEndPoint}] API.TypePacket.ConfirmKey: {tCPClient.CipherAES.AES_KEY.Length * 8} | {string.Join(";", tCPClient.CipherAES.AES_KEY)}");
							break;
						case API.TypePacket.Authorization:
							IsAuthorization = FindUserInDataBase((packet.Data as API.Authorization).Login, (packet.Data as API.Authorization).Password);
							if (IsAuthorization) API.Base.SendPacketClient(Client, new API.Packet() { TypePacket = API.TypePacket.Authorization }, cipherAES);
							else API.Base.SendPacketClient(Client, new API.Packet() { TypePacket = API.TypePacket.AuthorizationFailed }, cipherAES);
							Log.WriteLine($"[{Client.Client.RemoteEndPoint}] API.TypePacket.Authorization: {IsAuthorization}");
							break;
						default:
							if (packet != null) CallAnswer.Invoke(packet);
							break;
					}
					if (CountPacketRecive >= CountPacketReciveToUpdateKey)
					{
						updateCipherAES = new API.CipherAES();
						updateCipherAES.CreateKey();
						API.Base.SendPacketClient(Client, new API.Packet() { Data = updateCipherAES, TypePacket = API.TypePacket.UpdateKey }, cipherAES);
						CountPacketRecive = 0;
					}
				}
				catch { }
			}
			Log.WriteLine($"Client disconnect: {Client.Client.RemoteEndPoint}");
			tcpClients.Remove(tCPClient);
		}
	}
	public class TCPClient: Base.ViewModel.BaseViewModel
	{
		private TcpClient _Client;
		public TcpClient Client { get => _Client; set => Set(ref _Client, value); }

		private API.CipherAES _CipherAES;
		public API.CipherAES CipherAES { get => _CipherAES; set => Set(ref _CipherAES, value); }
	}
}
