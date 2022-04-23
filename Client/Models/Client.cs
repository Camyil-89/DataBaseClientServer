using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace DataBaseClientServer.Models
{
	internal class Client
	{
		public IPAddress IPAddress { get; set; } = IPAddress.Parse("127.0.0.0");
		public int Port { get; set; } = 32001;
		private TcpClient _Client { get; set; }
		public int SizeBuffer { get; set; } = 2048;
		public bool Connected { get => _Client.Connected; }

		public delegate void Answer(API.Packet Packet);
		private DateTime LastAnswer;
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
				_Client = new TcpClient();
				_Client.Connect(IPAddress, Port);
				Task.Run(() => { ClientListener(); });
				return true;
			} catch (Exception ex) { }

			return false;
			
		}
		public void ClientListener()
		{
			NetworkStream networkStream = _Client.GetStream();
			cipherAES.BaseKey();
			while (_Client.Connected)
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
						case API.TypePacket.Ping:
							break;
						case API.TypePacket.Termination:
							_Client.Dispose();
							return;
						case API.TypePacket.UpdateKey:
							API.Base.SendPacketClient(_Client, new API.Packet() { TypePacket = API.TypePacket.ConfirmKey }, cipherAES);
							cipherAES = (API.CipherAES)packet.Data;
							break;
						default:
							if (packet != null) CallAnswer.Invoke(packet);
							break;
					}
				}
				catch { }
			}
		}
	}
}
