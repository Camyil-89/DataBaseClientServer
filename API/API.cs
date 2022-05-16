using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using API.Logging;

namespace API
{
	#region авторизация
	public enum AccessLevel : int
	{
		NonAuthorization = 0,
		User = 1,
		Worker = 10,
		Admin = 100,
	}
	public enum TypeErrorAuthorization : int
	{
		Login = 1,
		Passsword = 2,
	}
	#endregion

	#region база данных
	public enum TypeStatusTable : int
	{
		Read = 1,
		WriteAndRead = 2,
	}
	public enum TypeDataBasePacket : int
	{
		GetTables = 1,
	}
	public enum InfoDataBasePacket : int
	{
		OK = 0,
		AllocTrue = 1,
		AllocFalse = 2,
		NotExistsFile = 3,
		IsNotWork = 4,
	}
	#endregion

	public enum TypePacket : int
	{
		// NonAuthorization
		Disconnect = 2,
		Termination = 3,
		UpdateKey = 5,
		ConfirmKey = 6,
		Authorization = 7,
		AuthorizationFailed = 8,
		// NonAuthorization

		// Authorization
		Ping = 4,

		GetPathsDataBase = 9,
		ConnectDataBase = 10,

		AllocTable = 12,
		DenayPacket = 13,
		// query
		SQLQuery = 11,
		SQLQueryDenay = 14,
		SQLQueryError = 15,
		SQLQueryOK = 16,
		// query
		// Authorization
	}
	[Serializable]
	public class TableDataBase
	{
		#region Base
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
		{
			//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
			var handlers = PropertyChanged;
			if (handlers is null) return;

			var invocation_list = handlers.GetInvocationList();

			var arg = new PropertyChangedEventArgs(PropertyName);
			foreach (var action in invocation_list)
				if (action.Target is DispatcherObject disp_object)
					disp_object.Dispatcher.Invoke(action, this, arg);
				else
					action.DynamicInvoke(this, arg);
		}

		protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string PropertyName = null)
		{
			if (Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(PropertyName);
			return true;
		}
		#endregion

		private TypeStatusTable _Status = TypeStatusTable.Read;
		public TypeStatusTable Status { get => _Status; set => Set(ref _Status, value); }
		private DataTable _Table;
		public DataTable Table { get => _Table; set => Set(ref _Table, value); }
	}

	[Serializable]
	public class DataBasePacket
	{
		public string Path { get; set; }
		public dynamic Data { get; set; }
		public TypeDataBasePacket Type { get; set; }
		public InfoDataBasePacket Info { get; set; } = InfoDataBasePacket.OK;
	}
	public static class Base
	{
		public static bool LogPing = false;
		public static List<API.TypePacket> IsAuthorizationClientUse = new List<API.TypePacket>() { API.TypePacket.Ping };
		public static void SendPacketClient(TcpClient client, API.Packet packet, CipherAES cipherAES)
		{
			NetworkStream networkStream = client.GetStream();
			var byte_packet = Packet.ToByteArray(packet, cipherAES);
			LogWr(packet, client, byte_packet.Length);
			networkStream.Write(byte_packet, 0, byte_packet.Length);
		}
		private static void LogWr(Packet packet, TcpClient client, int size)
		{
			if (!LogPing && packet.TypePacket == TypePacket.Ping) return;
			Log.WriteLine($"[{client.Client.LocalEndPoint} > {client.Client.RemoteEndPoint} [{size}]] {packet.TypePacket} {packet.UID}");
		}
	}

	[Serializable]
	public class CipherAES
	{
		public byte[] AES_KEY;
		public byte[] AES_IV;

		public static byte[] KEY_base = new byte[16] { 0xaa, 0x11, 0x23, 0x00, 0x00, 0x40, 0x10, 0x00, 0xd, 0xdd, 0x00, 0x90, 0x00, 0x00, 0x00, 0x00 };
		public static byte[] IV_base = new byte[16] { 0xaa, 0x00, 0x0f, 0x00, 0x0b, 0x00, 0x03, 0x00, 0x60, 0x00, 0x00, 0x67, 0x00, 0x00, 0x80, 0x00 };
		public void BaseKey()
		{
			AES_KEY = KEY_base;
			AES_IV = IV_base;
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
	public class Authorization
	{
		public string Login { get; set; }
		public string Password { get; set; }
	}
	[Serializable]
	public class Packet
	{
		public TypePacket TypePacket { get; set; }
		public Guid UID { get; set; } = Guid.NewGuid();
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
