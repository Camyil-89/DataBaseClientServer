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

namespace DataBaseClientServer.Models
{
	enum StatusClient: int
	{
		Connected = 1,
		Disconnected = 2,
		Connecting = 3,
	}
	internal class Client: Base.ViewModel.BaseViewModel
	{
		public IPAddress IPAddress { get; set; } = IPAddress.Parse("127.0.0.0");
		public int Port { get; set; } = 32001;
		private TcpClient _Client { get; set; } = new TcpClient();
		public int TimeOut { get; set; } = 5000; // msc	
		public int TimeOutConnect { get; set; } = 5000; // msc	
		public int SizeBuffer { get; set; } = 2048;

		private StatusClient _StatusClient = StatusClient.Disconnected;
		public StatusClient StatusClient
		{ get => _StatusClient;
			set 
			{ 
				Set(ref _StatusClient, value); 
				if (value == StatusClient.Connecting) VisibilityConnecting = Visibility.Visible;
				else VisibilityConnecting = Visibility.Collapsed;
			} 
		}
		#region Connecting
		private Visibility _VisibilityConnecting = Visibility.Collapsed;
		public Visibility VisibilityConnecting { get => _VisibilityConnecting; set => Set(ref _VisibilityConnecting, value); }

		#endregion
		public bool IsAuthorization { get; set; } = false;
		public delegate void Answer(API.Packet Packet);
		private DateTime LastAnswer;
		private API.Packet LastAnswerPacket;
		private bool AwaitPacketResponse = false;

		public event Answer CallAnswer;
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
				if (!_Client.ConnectAsync(IPAddress, Port).Wait(TimeOutConnect))
				{
					StatusClient = StatusClient.Connecting;
					return false;
				}
				Task.Run(() => { ClientListener(); });
				return true;
			} catch (Exception ex) { Console.WriteLine(ex); }
			StatusClient = StatusClient.Disconnected;
			return false;
			
		}
		public API.Packet SendPacketAndWaitResponse(API.Packet packet)
		{
			if (API.Base.IsAuthorizationClientUse.Contains(packet.TypePacket) && !IsAuthorization) throw new API.Excepcion.ExcepcionIsAuthorizationClientUse();
			AwaitPacketResponse = true;
			LastAnswerPacket = null;
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			API.Base.SendPacketClient(_Client, packet, cipherAES);
			while (LastAnswerPacket == null)
			{
				if (stopwatch.ElapsedMilliseconds > TimeOut) throw new API.Excepcion.ExcepcionTimeOut();
				if (!_Client.Connected) throw new API.Excepcion.ExcepcionClientConnectLose();
				Thread.Sleep(1);
			}
			return LastAnswerPacket;
		}
		public void ClientListener()
		{
			StatusClient = StatusClient.Connected;
			IsAuthorization = false;
			NetworkStream networkStream = _Client.GetStream();
			cipherAES.BaseKey();
			API.Base.SendPacketClient(_Client, new API.Packet() { TypePacket = API.TypePacket.UpdateKey }, cipherAES);
			Task.Run(() => {
				while(true)
				{
					if (!_Client.Client.Connected)
					{
						StatusClient = StatusClient.Disconnected;
						break;
					}
				}
				
			});
			while (_Client.Connected && _Client.Client.Connected)
			{
				try
				{
					byte[] myReadBuffer = new byte[SizeBuffer];
					do
					{
						networkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
					}
					while (networkStream.DataAvailable);
					API.Packet packet = API.Packet.FromByteArray(myReadBuffer, cipherAES);
					LastAnswer = DateTime.Now;
					switch (packet.TypePacket)
					{
						case API.TypePacket.Termination:
							_Client.Dispose();
							return;
						case API.TypePacket.Authorization:
							LastAnswerPacket = packet;
							if (AwaitPacketResponse) AwaitPacketResponse = false;
							IsAuthorization = true;
							break;
						case API.TypePacket.UpdateKey:
							API.Base.SendPacketClient(_Client, new API.Packet() { TypePacket = API.TypePacket.ConfirmKey }, cipherAES);
							cipherAES = (API.CipherAES)packet.Data;
							break;
						default:
							LastAnswerPacket = packet;
							if (AwaitPacketResponse) AwaitPacketResponse = false;
							else if (packet != null) CallAnswer.Invoke(packet);
							break;
					}
				}
				catch (Exception ex) { Log.WriteLine(ex); }
			}
			Log.WriteLine("StatusClient.Disconnected");
			StatusClient = StatusClient.Disconnected;
		}
	}
}
