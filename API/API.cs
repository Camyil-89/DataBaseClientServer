using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace API
{
    public class Server
    {

    }
    public class Client
	{

	}
   
    public enum TypePacket: int
	{
        Connect = 1,
        Disconnect = 2,
	}
    [Serializable]
    public class Packet
	{
        public TypePacket TypePacket { get; set; }
        public object Data { get; set; }

		public override string ToString()
		{
			return $"Packet type: {TypePacket}; Packet data: {Data.GetType()}";
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
