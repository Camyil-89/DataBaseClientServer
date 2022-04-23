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
		public static void SendPacketClient(TcpClient client, API.Packet packet, CipherAES cipherAES)
		{
			NetworkStream networkStream = client.GetStream();
			var byte_packet = Packet.ToByteArray(packet, cipherAES);
			networkStream.Write(byte_packet, 0, byte_packet.Length);
		}
	}
	public enum TypePacket : int
	{
		Disconnect = 2,
		Termination = 3,
		Ping = 4,
		UpdateKey,
		ConfirmKey,
	}
	[Serializable]
	public class CipherAES
	{
		public byte[] AES_KEY;
		public byte[] AES_IV;
		public void BaseKey()
		{
			AES_KEY = new byte[16] { 0xaa, 0x11, 0x23, 0x00, 0x00, 0x40, 0x10, 0x00, 0xd, 0xdd, 0x00, 0x90, 0x00, 0x00, 0x00, 0x00 };
			AES_IV = new byte[16] { 0xaa, 0x00, 0x0f, 0x00, 0x0b, 0x00, 0x03, 0x00, 0x60, 0x00, 0x00, 0x67, 0x00, 0x00, 0x80, 0x00 };
		}
		public void CreateKey()
		{
			var aes = Aes.Create();
			aes.KeySize = 128;
			aes.BlockSize = 128;
			aes.Padding = PaddingMode.Zeros;
			aes.GenerateKey();
			aes.GenerateIV();
			AES_KEY = aes.Key;
			AES_IV = aes.IV;
		}
		private Aes GetAES()
		{
			Aes aes = Aes.Create();
			aes.KeySize = 128;
			aes.BlockSize = 128;
			aes.Padding = PaddingMode.Zeros;
			aes.Key = AES_KEY;
			aes.IV = AES_IV;
			return aes;
		}
		public byte[] Encrypt(byte[] data)
		{
			var aes = GetAES();

			using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
			{
				return PerformCryptography(data, encryptor);
			}
		}
		private byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
		{
			using (var ms = new MemoryStream())
			using (var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
			{
				cryptoStream.Write(data, 0, data.Length);
				cryptoStream.FlushFinalBlock();

				return ms.ToArray();
			}
		}

		public byte[] Decrypt(byte[] data)
		{
			var aes = GetAES();

			using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
			{
				return PerformCryptography(data, decryptor);
			}
		}
	}
	[Serializable]
	public class Packet
	{
		public TypePacket TypePacket { get; set; }
		public object Data { get; set; } = null;
		public override string ToString()
		{
			if (Data != null) return $"Packet type: {TypePacket}; Packet data: {Data.GetType()}";
			return $"Packet type: {TypePacket}; Packet data: null";
		}
		public static byte[] ToByteArray(object obj, CipherAES cipherAES)
		{
			if (obj == null)
				return null;
			BinaryFormatter bf = new BinaryFormatter();
			byte[] array;
			using (MemoryStream ms = new MemoryStream())
			{
				bf.Serialize(ms, obj);
				array = ms.ToArray();
			}
			return cipherAES.Encrypt(array);
		}
		public static Packet FromByteArray(byte[] data, CipherAES cipherAES)
		{
			if (data == null)
				return null;
			BinaryFormatter bf = new BinaryFormatter();
			var array = cipherAES.Decrypt(data);
			using (MemoryStream ms = new MemoryStream(array))
			{
				object obj = bf.Deserialize(ms);
				return (Packet)obj;
			}
		}
	}
}
