using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API
{
    public static class Base
	{
		public static void SendPacketClient(TcpClient client, API.Packet packet)
		{
			NetworkStream networkStream = client.GetStream();
			var byte_packet = Packet.ToByteArray(packet);
			networkStream.Write(byte_packet, 0, byte_packet.Length);
		}
	}
    public enum TypePacket: int
	{
        Disconnect = 2,
		Termination = 3,
		Ping = 4,
	}
    [Serializable]
    public class Packet
	{
        public TypePacket TypePacket { get; set; }
        public object Data { get; set; } = null;

		private static byte[] AES_KEY = new byte[16];
		public override string ToString()
		{
			if (Data != null) return $"Packet type: {TypePacket}; Packet data: {Data.GetType()}";
			return $"Packet type: {TypePacket}; Packet data: null";
		}

		public static byte[] ToByteArray(object obj)
        {
            if (obj == null)
                return null;
			BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
				bf.Serialize(ms, obj);
				return ms.ToArray();
            }
        }
        public static Packet FromByteArray(byte[] data)
        {
            if (data == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
				object obj = bf.Deserialize(ms);
				return (Packet)obj;
			}
        }
    }
}
