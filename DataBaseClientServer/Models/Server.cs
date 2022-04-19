using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace DataBaseClientServer.Models
{
	public class Server
	{
		public bool Connect { get; set; } = false;

		public int Port { get; set; } = 32001;
		public int CountClient { get; set; } = 0;

		public IPAddress IPAddress { get; set; } = IPAddress.Parse("127.0.0.0");

		public delegate void Answer(API.Packet Packet);
		public event Answer CallAnswer;
		public int SizeBuffer = 2048;
		
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
					CountClient++;
					Task.Run(() => { StartClient(client); });
				}
			});
		}

		void StartClient(TcpClient Client)
		{
			NetworkStream networkStream = Client.GetStream();
			while (Client.Connected)
			{
				try
				{
					byte[] myReadBuffer = new byte[SizeBuffer];
					do
					{
						networkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
					}
					while (networkStream.DataAvailable);
					API.Packet packet = API.Packet.FromByteArray(myReadBuffer);
					if (packet != null) CallAnswer.Invoke(packet);
				}
				catch { }
			}
			CountClient--;
		}
	}
}
