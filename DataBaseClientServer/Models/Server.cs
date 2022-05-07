using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using API.Logging;
using DataBaseClientServer.Models.database;
using System.Collections.ObjectModel;
using DataBaseClientServer.Models.SettingsServer;
using DataBaseClientServer.ViewModels;
using System.Threading;

namespace DataBaseClientServer.Models
{
	public class Server: Base.ViewModel.BaseViewModel
	{
		public bool Work { get; set; } = false;

		private int _port = 32001;
		public int Port { get => _port; set => Set(ref _port, value); }
		public int CountClient { get => tcpClients.Count; }

		public int MaxCountErrorPacket = 10;

		private IPAddress _IPAddress = IPAddress.Parse("127.0.0.0");
		public IPAddress IPAddress { get => _IPAddress; set => Set(ref _IPAddress, value); }

		public delegate void Answer(API.Packet Packet,TcpClient client, API.CipherAES cipherAES);
		public event Answer CallAnswer;
		public int SizeBuffer = 2048;


		public DataBase AccessDataBase = new DataBase();

		public ServerViewModel ServerViewModel { get; set; }

		private int CountPacketReciveToUpdateKey = 3; // min 2

		public ObservableCollection<TCPClient> tcpClients { get; set; } = new ObservableCollection<TCPClient>();

		private TcpListener tcpListener;

		public Client GetClientFromDataBase(string Login, string Password)
		{
			return ServerViewModel.Settings.Clients.FirstOrDefault((i) => i.Name == Login && i.Password == Password);
		}
		public Client GetClientFromDataBaseLogin(string Login)
		{
			return ServerViewModel.Settings.Clients.FirstOrDefault((i) => i.Name == Login);
		}
		public Client GetClientFromDataBasePassword(string Password)
		{
			return ServerViewModel.Settings.Clients.FirstOrDefault((i) => i.Password == Password);
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
				try
				{
					while (Work)
					{
						var client = tcpListener.AcceptTcpClient();
						Log.WriteLine($"Connect client: {client.Client.RemoteEndPoint}");
						Task.Run(() => { StartClient(client, Key_aes, IV_aes); });
					}
				} catch { Work = false; }
				
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
		private API.Packet Authorization(API.Packet packet)
		{
			API.Packet authorization = new API.Packet();
			var auth = (API.Authorization)packet.Data;
			var client = GetClientFromDataBaseLogin(auth.Login);
			if (client == null) return new API.Packet() { TypePacket = API.TypePacket.AuthorizationFailed, Data = API.TypeErrorAuthorization.Login, UID = packet.UID };
			if (client.Password != auth.Password) return new API.Packet() { TypePacket = API.TypePacket.AuthorizationFailed, Data = API.TypeErrorAuthorization.Passsword, UID = packet.UID };
			return new API.Packet() { TypePacket = API.TypePacket.Authorization, UID = packet.UID };
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
			int count_error_packet = 0;
			EndPoint endPoint = Client.Client.RemoteEndPoint;
			while (Client.Connected)
			{
				try
				{
					byte[] myReadBuffer = new byte[SizeBuffer];
					List<byte> allData = new List<byte>();
					do
					{
						int numBytesRead = networkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
						if (numBytesRead == myReadBuffer.Length) allData.AddRange(myReadBuffer);
						else if (numBytesRead > 0) allData.AddRange(myReadBuffer.Take(numBytesRead));
					} while (networkStream.DataAvailable);

					API.Packet packet = API.Packet.FromByteArray(allData.ToArray(), cipherAES);
					Log.WriteLine($"[{Client.Client.RemoteEndPoint}] packet receive: {packet}");
					count_error_packet = 0;
					if (!IsAuthorization && API.Base.IsAuthorizationClientUse.Contains(packet.TypePacket)) continue;
					switch (packet.TypePacket)
					{
						case API.TypePacket.Disconnect:
							Client.Close();
							break;
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
							API.Base.SendPacketClient(Client, new API.Packet() { TypePacket = API.TypePacket.ConfirmKey }, cipherAES);
							break;
						case API.TypePacket.Authorization:
							var auth = Authorization(packet);
							if (auth.TypePacket == API.TypePacket.Authorization) IsAuthorization = true;
							API.Base.SendPacketClient(Client, auth, cipherAES);
							Log.WriteLine($"[{Client.Client.RemoteEndPoint}] API.TypePacket.Authorization: {IsAuthorization}");
							break;
						default:
							CallAnswer.Invoke(packet, Client, cipherAES);
							break;
					}
					//if (CountPacketRecive >= CountPacketReciveToUpdateKey)
					//{
					//	updateCipherAES = new API.CipherAES();
					//	updateCipherAES.CreateKey();
					//	API.Base.SendPacketClient(Client, new API.Packet() { Data = updateCipherAES, TypePacket = API.TypePacket.UpdateKey }, cipherAES);
					//	CountPacketRecive = 0;
					//}
				}
				catch (Exception e) { count_error_packet++; if (count_error_packet == MaxCountErrorPacket) Client.Close(); Log.WriteLine(e); }
			}
			Log.WriteLine($"Client disconnect: {endPoint}");
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
