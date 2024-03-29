﻿using System;
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
		public int SizeBuffer { get; set; } = 65000;
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
		/// <summary>
		/// Подключение к серверу
		/// </summary>
		/// <returns></returns>
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
		/// <summary>
		/// Отключение при истечение времени оэидания
		/// </summary>
		/// <exception cref="API.Excepcion.ExcepcionTimeOut"></exception>
		private void TimeOutException()
		{
			try
			{
				Disconnect();
			}
			catch { }
			throw new API.Excepcion.ExcepcionTimeOut();
		}
		/// <summary>
		/// отправка пакета данных и ожидания его получения
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="CountNeedReceive"></param>
		/// <returns></returns>
		/// <exception cref="API.Excepcion.ExcepcionClientConnectLose"></exception>
		/// <exception cref="API.Excepcion.ExcepcionIsAuthorizationClientUse"></exception>
		public AwaitPackets SendPacketAndWaitResponse(API.Packet packet, int CountNeedReceive)
		{
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
			if (x.Packets[0].TypePacket == API.TypePacket.DenayPacket) throw new API.Excepcion.ExcepcionIsAuthorizationClientUse();
			return x;
		}
		/// <summary>
		/// Отключение от сервера
		/// </summary>
		public void Disconnect()
		{
			API.Base.SendPacketClient(_Client, new API.Packet() { TypePacket = API.TypePacket.Disconnect}, cipherAES);
			_Client.Dispose();
		}
		/// <summary>
		/// Главный цикл клиента
		/// </summary>
		public void ClientListener()
		{
			StatusClient = StatusClient.Connecting;
			IsAuthorization = false;
			cipherAES.AES_KEY = ClientSerttings.KernelSettings.KeyAES;
			cipherAES.AES_IV = ClientSerttings.KernelSettings.IV_AES;
			NetworkStream networkStream = _Client.GetStream();
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

			int ReceiveBytesNeeds = 0;
			int ReceiveBytesNow = 0;
			while (_Client.Connected && _Client.Client.Connected)
			{
				try
				{
					byte[] myReadBuffer = new byte[SizeBuffer];
					List<byte> allData = new List<byte>();
					if (ReceiveBytesNeeds != 0)
					{
						while (ReceiveBytesNeeds != ReceiveBytesNow)
						{
							int numBytesRead = networkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
							if (numBytesRead == myReadBuffer.Length) allData.AddRange(myReadBuffer);
							else if (numBytesRead > 0) allData.AddRange(myReadBuffer.Take(numBytesRead));
							ReceiveBytesNow += numBytesRead;
						}
					}
					else
					{
						do
						{
							int numBytesRead = networkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
							if (numBytesRead == myReadBuffer.Length) allData.AddRange(myReadBuffer);
							else if (numBytesRead > 0) allData.AddRange(myReadBuffer.Take(numBytesRead));
							ReceiveBytesNow += numBytesRead;
						} while (networkStream.DataAvailable);
					}
					if (allData.Count == 0) continue;
					ReceiveBytesNow = 0;
					ReceiveBytesNeeds = 0;
					API.Packet packet = API.Packet.FromByteArray(allData.ToArray(), cipherAES);
					LastAnswer = DateTime.Now;
					switch (packet.TypePacket)
					{
						case API.TypePacket.Header:
							ReceiveBytesNeeds = (int)packet.Data;
							break;
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
	/// <summary>
	/// класс нужен для ожидания пакетов которые были отправлены из SendPacketAndWaitResponse
	/// </summary>
	public class AwaitPackets
	{
		public List<API.Packet> Packets { get; set; } = new List<API.Packet>();
		public int CountNeedReceive { get; set; }
		public int CountReceive { get; set; }

	}
}
