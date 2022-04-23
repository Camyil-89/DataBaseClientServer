using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace DataBaseClientServer.Models
{
	public class Server
	{
		~Server()
		{
			DisposeClients();
		}
		public bool Connect { get; set; } = false;

		public int Port { get; set; } = 32001;
		public int CountClient { get => tcpClients.Count; }

		public IPAddress IPAddress { get; set; } = IPAddress.Parse("127.0.0.0");

		public delegate void Answer(API.Packet Packet);
		public event Answer CallAnswer;
		public int SizeBuffer = 2048;
		private int CountPacketReciveToUpdateKey = 10;

		private List<TCPClient> tcpClients = new List<TCPClient>();
		
		public void Start()
		{
			TcpListener tcpListener = new TcpListener(IPAddress.Any, Port);
			tcpListener.Start();
			foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					IPAddress = ip;
					break;
				}
			}
			Log.WriteLine($"IP: {IPAddress}");
			Log.WriteLine($"Port: {Port}");
			Task.Run(() => {
				while (true)
				{
					var client = tcpListener.AcceptTcpClient();
					Log.WriteLine($"Connect client: {client.Client.RemoteEndPoint}");
					Task.Run(() => { StartClient(client); });
				}
			});
		}
		public void DisposeClients()
		{
			Log.WriteLine("Dispose Client");
			foreach (var i in tcpClients)
			{
				Log.WriteLine($"Dispose {i.Client.Client.RemoteEndPoint}");
				API.Base.SendPacketClient(i.Client, new API.Packet() { TypePacket = API.TypePacket.Termination }, i.CipherAES);
				//i.Dispose();
			}
			tcpClients.Clear();
		}
	
		void StartClient(TcpClient Client)
		{
			NetworkStream networkStream = Client.GetStream();
			API.CipherAES cipherAES = new API.CipherAES();
			API.CipherAES updateCipherAES = new API.CipherAES();
			cipherAES.BaseKey();
			TCPClient tCPClient = new TCPClient() { Client = Client, CipherAES = cipherAES};
			tcpClients.Add(tCPClient);
			int CountPacketRecive = 0;
			while (Client.Connected)
			{
				byte[] myReadBuffer = new byte[SizeBuffer];
				do
				{
					CountPacketRecive++;
					networkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
				}
				while (networkStream.DataAvailable);
				Log.WriteLine($"Packet lenght: {myReadBuffer.Length}");
				API.Packet packet = API.Packet.FromByteArray(myReadBuffer, cipherAES);
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
						Log.WriteLine($"{string.Join(";", tCPClient.CipherAES.AES_KEY)}");
						Log.WriteLine($"API.TypePacket.ConfirmKey: {tCPClient.CipherAES.AES_KEY.Length}");
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
				try
				{
					
				}
				catch { }
			}
			Log.WriteLine($"Client disconnect: {Client.Client.RemoteEndPoint}");
			tcpClients.Remove(tCPClient);
		}
	}
	class TCPClient
	{
		public TcpClient Client { get; set; }
		public API.CipherAES CipherAES { get; set; }
	}
}
