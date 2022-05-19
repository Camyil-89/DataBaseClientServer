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

		public delegate void Answer(API.Packet Packet,TcpClient client, API.CipherAES cipherAES, SettingsServer.Client client_auth);
		public event Answer CallAnswer;
		public int SizeBuffer = 65000;


		public DataBase AccessDataBase = new DataBase();

		public ServerViewModel ServerViewModel { get; set; }


		public ObservableCollection<TCPClient> tcpClients { get; set; } = new ObservableCollection<TCPClient>();
		public Dictionary<SettingsServer.Client, TCPClient> ConnectClientToDataBase { get; set; } = new Dictionary<SettingsServer.Client, TCPClient>();

		private TcpListener tcpListener;
		/// <summary>
		/// получение клиента из БД
		/// </summary>
		/// <param name="Login"></param>
		/// <param name="Password"></param>
		/// <returns></returns>
		public Client GetClientFromDataBase(string Login, string Password)
		{
			return ServerViewModel.Settings.Clients.FirstOrDefault((i) => i.Login == Login && i.Password == Password);
		}
		/// <summary>
		/// получение клиента из БД
		/// </summary>
		/// <param name="Login"></param>
		/// <returns></returns>
		public Client GetClientFromDataBaseLogin(string Login)
		{
			return ServerViewModel.Settings.Clients.FirstOrDefault((i) => i.Login == Login);
		}
		/// <summary>
		/// получение клиента из БД
		/// </summary>
		/// <param name="Password"></param>
		/// <returns></returns>
		public Client GetClientFromDataBasePassword(string Password)
		{
			return ServerViewModel.Settings.Clients.FirstOrDefault((i) => i.Password == Password);
		}
		/// <summary>
		/// поиск клиента по БД
		/// </summary>
		/// <param name="Login"></param>
		/// <param name="Password"></param>
		/// <returns></returns>
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
		/// <summary>
		/// Получение пакета данных с информации о подключении
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		private API.Packet Authorization(API.Packet packet)
		{
			var auth = (API.Authorization)packet.Data;
			var client = GetClientFromDataBaseLogin(auth.Login);
			API.InfoAboutClientPacket info = new API.InfoAboutClientPacket();

			if (client == null)
			{
				info.Error = API.TypeErrorAuthorization.Login;
				return new API.Packet() { TypePacket = API.TypePacket.AuthorizationFailed, Data = info, UID = packet.UID };
			}
			if (client.Password != auth.Password)
			{
				info.Error = API.TypeErrorAuthorization.Passsword;
				return new API.Packet() { TypePacket = API.TypePacket.AuthorizationFailed, Data = info, UID = packet.UID };
			}
			if (ConnectClientToDataBase.ContainsKey(client))
			{
				info.Error = API.TypeErrorAuthorization.ConnectNow;
				return new API.Packet() { TypePacket = API.TypePacket.AuthorizationFailed, Data = info, UID = packet.UID };
			}
			info.Name = client.Name;
			info.Surname = client.Surname;
			info.Patronymic = client.Patronymic;
			info.AccessLevel = client.AccessLevel;
			return new API.Packet() { TypePacket = API.TypePacket.Authorization, Data = info, UID = packet.UID };
		}
		/// <summary>
		/// Рассылка на всех подключенных клиентов
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="clients"></param>
		public void BroadcastSend(API.Packet packet, List<SettingsServer.Client> clients = null)
		{
			foreach (var client in ConnectClientToDataBase)
			{
				try
				{
					if (clients != null && clients.Contains(client.Key)) continue;
					if (client.Key.AccessLevel != API.AccessLevel.NonAuthorization)
					{
						Log.WriteLine($"Broadcast: {client.Key.Login} {client.Key.AccessLevel}");
						API.Base.SendPacketClient(client.Value.Client, packet, client.Value.CipherAES);
					}
				} catch (Exception ex) { Log.WriteLine(ex); }
			}
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

			SettingsServer.Client ConnectClient = new SettingsServer.Client();
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
					
					count_error_packet = 0;
					if (ConnectClient.DenayPackages(packet.TypePacket)) 
					{
						Log.WriteLine($"[{Client.Client.RemoteEndPoint}] packet denay: {packet}");
						packet.TypePacket = API.TypePacket.DenayPacket;
						API.Base.SendPacketClient(Client,packet, cipherAES);
						continue; 
					}
					Log.WriteLine($"[{Client.Client.RemoteEndPoint}] packet receive: {packet}");
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
							API.Base.SendPacketClient(Client, auth, cipherAES);
							if (auth.TypePacket == API.TypePacket.Authorization)
							{
								IsAuthorization = true;
								ConnectClient = GetClientFromDataBaseLogin(((API.Authorization)packet.Data).Login);
								ConnectClientToDataBase.Add(ConnectClient, tCPClient);
							}
							else Client.Close();
							Log.WriteLine($"[{Client.Client.RemoteEndPoint}] API.TypePacket.Authorization: {IsAuthorization}");
							break;
						default:
							CallAnswer.Invoke(packet, Client, cipherAES, ConnectClient);
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
			ConnectClientToDataBase.Remove(ConnectClient);
			Log.WriteLine($"Client disconnect: {endPoint}");
			tcpClients.Remove(tCPClient);
		}
	}
	/// <summary>
	/// Информация о клиенте
	/// </summary>
	public class TCPClient: Base.ViewModel.BaseViewModel
	{
		private TcpClient _Client;
		public TcpClient Client { get => _Client; set => Set(ref _Client, value); }

		private API.CipherAES _CipherAES;
		public API.CipherAES CipherAES { get => _CipherAES; set => Set(ref _CipherAES, value); }
	}
}
