using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Windows;
using DataBaseClientServer.Models.Settings;
using API.Logging;

namespace DataBaseClientServer.Models
{
	public enum StatusClient: int
	{
		Connected = 1,
		Disconnected = 2,
		Connecting = 3,
	}
	public enum AddType : int
	{
		AddBook = 1,
	}
	public class Client: Base.ViewModel.BaseViewModel
	{
		
		private TcpClient _Client { get; set; } = new TcpClient();
		public int TimeOutUpdateKeyAES { get; set; } = 30000; // msc
		public int TimeOut { get; set; } = 5000; // msc	
		public int TimeOutConnect { get; set; } = 5000; // msc	
		public int SizeBuffer { get; set; } = 2048;
		public ClientSerttings ClientSerttings { get; set; }

		private StatusClient _StatusClient = StatusClient.Disconnected;
		public StatusClient StatusClient
		{ get => _StatusClient;
			set 
			{ 
				Set(ref _StatusClient, value);
				if (value == StatusClient.Connecting) VisibilityConnecting = Visibility.Visible;
				else
				{
					VisibilityConnecting = Visibility.Collapsed;
					IsConnected = false;
				}
				if (value == StatusClient.Connected) IsConnected = true;
			} 
		}
		#region Connecting
		private Visibility _VisibilityConnecting = Visibility.Collapsed;
		public Visibility VisibilityConnecting { get => _VisibilityConnecting; set => Set(ref _VisibilityConnecting, value); }

		#endregion

		private bool _IsConnected = false;
		public bool IsConnected { get => _IsConnected; set => Set(ref _IsConnected, value); }

		private bool _IsAuthorization = false;
		public bool IsAuthorization { get => _IsAuthorization; set => Set(ref _IsAuthorization, value); }
		private DateTime LastAnswer;
		public bool UpdateKey = false;
		public bool FirstUpdateKey = false;

		private Dictionary<Guid, AwaitPackets> PacketsAwait = new Dictionary<Guid, AwaitPackets>();
		public delegate void Answer(API.Packet Packet);
		public delegate void DisconnectFromServer(string message);
		public event Answer CallAnswer;
		public event DisconnectFromServer CallDisconnect;
		private API.CipherAES cipherAES = new API.CipherAES();
		public TimeSpan Ping()
		{
			DateTime last_answer = LastAnswer;
			DateTime dateTime = DateTime.Now;
			API.Base.SendPacketClient(_Client, new API.Packet() { TypePacket = API.TypePacket.Ping }, cipherAES);
			while (last_answer == LastAnswer) { }
			return LastAnswer - dateTime;
		}
		public bool Connect()
		{
			try
			{
				StatusClient = StatusClient.Connecting;
				_Client = new TcpClient();
				if (!_Client.ConnectAsync(new IPAddress(ClientSerttings.ClientConnect.IPAddressServer), ClientSerttings.ClientConnect.Port).Wait(TimeOutConnect))
				{
					StatusClient = StatusClient.Disconnected;
					return false;
				}
				UpdateKey = true;
				Task.Run(() => { ClientListener(); });
				return true;
			} catch (Exception ex) { Console.WriteLine(ex); }
			StatusClient = StatusClient.Disconnected;
			return false;
			
		}
		private void TimeOutException()
		{
			try
			{
				Disconnect();
			}
			catch { }
			throw new API.Excepcion.ExcepcionTimeOut();
		}
		public AwaitPackets SendPacketAndWaitResponse(API.Packet packet, int CountNeedReceive)
		{
			if (API.Base.IsAuthorizationClientUse.Contains(packet.TypePacket) && !IsAuthorization) throw new API.Excepcion.ExcepcionIsAuthorizationClientUse();
			//if (!FirstUpdateKey) throw new API.Excepcion.ExcepcionFirstUpdateKey();

			if (PacketsAwait.ContainsKey(packet.UID)) PacketsAwait.Remove(packet.UID);
			PacketsAwait.Add(packet.UID, new AwaitPackets() { CountNeedReceive = CountNeedReceive });
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			while (UpdateKey || !FirstUpdateKey)
			{
				if (stopwatch.ElapsedMilliseconds > TimeOut) TimeOutException();
				Thread.Sleep(1);
			}
			stopwatch.Restart();
			API.Base.SendPacketClient(_Client, packet, cipherAES);
			while (PacketsAwait[packet.UID].CountNeedReceive > PacketsAwait[packet.UID].CountReceive)
			{
				if (stopwatch.ElapsedMilliseconds > TimeOut) TimeOutException();
				if (!_Client.Connected) throw new API.Excepcion.ExcepcionClientConnectLose();
				Thread.Sleep(1);
			}
			var x = PacketsAwait[packet.UID];
			PacketsAwait.Remove(packet.UID);
			return x;
		}
		public void Disconnect()
		{
			API.Base.SendPacketClient(_Client, new API.Packet() { TypePacket = API.TypePacket.Disconnect}, cipherAES);
			_Client.Dispose();
		}
		public void ClientListener()
		{
			StatusClient = StatusClient.Connecting;
			IsAuthorization = false;
			NetworkStream networkStream = _Client.GetStream();
			cipherAES.AES_KEY = ClientSerttings.KernelSettings.KeyAES;
			cipherAES.AES_IV = ClientSerttings.KernelSettings.IV_AES;
			API.Base.SendPacketClient(_Client, new API.Packet() { TypePacket = API.TypePacket.UpdateKey }, cipherAES);
			Task.Run(() => {
				while(StatusClient != StatusClient.Disconnected)
				{
					if (!_Client.Client.Connected)
					{
						StatusClient = StatusClient.Disconnected;
						CallDisconnect.Invoke(null);
						break;
					}
					Thread.Sleep(1);
				}
				Log.WriteLine("Checker connect: Dispose");
			});
			int TimeOutFirstUpdateKey = 3000;
			Task.Run(() => {
				Stopwatch sw = new Stopwatch();
				sw.Start();
				while (StatusClient != StatusClient.Disconnected)
				{
					//Console.WriteLine($"{FirstUpdateKey && sw.ElapsedMilliseconds >= TimeOutUpdateKeyAES && PacketsAwait.Count == 0} - {sw.ElapsedMilliseconds}");
					if (FirstUpdateKey && sw.ElapsedMilliseconds >= TimeOutUpdateKeyAES && PacketsAwait.Count == 0 && !UpdateKey)
					{
						UpdateKey = true;
						API.Base.SendPacketClient(_Client, new API.Packet() { TypePacket = API.TypePacket.UpdateKey }, cipherAES);
						sw.Restart();
					}
					Thread.Sleep(1);
				}
			});
			Task.Run(() => {
				Stopwatch stopwatch= new Stopwatch();
				stopwatch.Start();
				while (!FirstUpdateKey)
				{
					if (stopwatch.ElapsedMilliseconds > TimeOutFirstUpdateKey)
					{
						_Client.Close();
						CallDisconnect.Invoke("Неверные ключи подключения!");
						break;
					}
				}
			});

			API.CipherAES old_cipherAES = cipherAES;
			while (_Client.Connected && _Client.Client.Connected)
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
					LastAnswer = DateTime.Now;
					switch (packet.TypePacket)
					{
						case API.TypePacket.Termination:
							_Client.Dispose();
							return;
						case API.TypePacket.Authorization:
							StatusClient = StatusClient.Connected;
							IsAuthorization = true;
							if (PacketsAwait.ContainsKey(packet.UID)) { PacketsAwait[packet.UID].Packets.Add(packet); PacketsAwait[packet.UID].CountReceive++; }
							break;
						case API.TypePacket.UpdateKey:
							UpdateKey = true;
							old_cipherAES = cipherAES;
							API.Base.SendPacketClient(_Client, new API.Packet() { TypePacket = API.TypePacket.ConfirmKey }, cipherAES);
							cipherAES = (API.CipherAES)packet.Data;
							FirstUpdateKey = true;
							break;
						case API.TypePacket.ConfirmKey:
							UpdateKey = false;
							break;
						default:
							if (PacketsAwait.ContainsKey(packet.UID)) { PacketsAwait[packet.UID].Packets.Add(packet); PacketsAwait[packet.UID].CountReceive++; }
							else if (packet != null) CallAnswer.Invoke(packet);
							break;
					}
				}
				catch (Exception ex) { Log.WriteLine(ex); }
			}
			Log.WriteLine("StatusClient.Disconnected");
			StatusClient = StatusClient.Disconnected;
			CallDisconnect.Invoke(null);
		}
	}
	public class AwaitPackets
	{
		public List<API.Packet> Packets { get; set; } = new List<API.Packet>();
		public int CountNeedReceive { get; set; }
		public int CountReceive { get; set; }

	}
}
