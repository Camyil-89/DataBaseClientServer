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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using API.Logging;

namespace API
{
	#region авторизация
	/// <summary>
	/// Уровень доступа
	/// </summary>
	public enum AccessLevel : int
	{
		NonAuthorization = 0,
		User = 1,
		Worker = 10,
		Admin = 100,
	}
	/// <summary>
	/// Ошибки при авторизации
	/// </summary>
	public enum TypeErrorAuthorization : int
	{
		Login = 1,
		Passsword = 2,
		ConnectNow = 4,
		OK = 3,
	}
	#endregion

	#region база данных
	/// <summary>
	/// Разрешение на запись и чтение таблицы (наверное уже не используется)
	/// </summary>
	public enum TypeStatusTable : int
	{
		Read = 1,
		WriteAndRead = 2,
	}
	/// <summary>
	/// уже не используется
	/// </summary>
	public enum TypeDataBasePacket : int
	{
		GetTables = 1,
	}
	/// <summary>
	/// информация о подключении БД
	/// </summary>
	public enum InfoDataBasePacket : int
	{
		OK = 0,
		AllocTrue = 1,
		AllocFalse = 2,
		NotExistsFile = 3,
		IsNotWork = 4,
	}
	/// <summary>
	/// Информация о широковещательной рассылке (тоже особо не используется)
	/// </summary>
	public enum TypeSQLQuery: int
	{
		BroadcastMe = 1,
		Broadcast = 2,
	}
	#endregion
	/// <summary>
	/// Тип пакета
	/// </summary>
	public enum TypePacket : int
	{
		// NonAuthorization
		Disconnect = 2,
		Termination = 3,
		UpdateKey = 5,
		ConfirmKey = 6,
		Authorization = 7,
		AuthorizationFailed = 8,
		Header = 18,
		// NonAuthorization

		// Authorization
		Ping = 4,

		GetPathsDataBase = 9,
		ConnectDataBase = 10,

		AllocTable = 12,
		UpdateTable = 17,
		DenayPacket = 13,
		// query
		SQLQuery = 11,
		SQLQueryDenay = 14,
		SQLQueryError = 15,
		SQLQueryOK = 16,
		// query
		// Authorization
	}
	/// <summary>
	/// информация о клиенте
	/// </summary>
	[Serializable]
	public class InfoAboutClientPacket
	{
		public TypeErrorAuthorization Error = TypeErrorAuthorization.OK;
		public AccessLevel AccessLevel { get; set; } = AccessLevel.NonAuthorization;
		public string Name { get; set; }
		public string Surname { get; set; }
		public string Patronymic { get; set; }
	}
	/// <summary>
	/// Пакет с БД
	/// </summary>
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
	/// <summary>
	/// Пакет для отправки sql запросов
	/// </summary>
	[Serializable]
	public class SQLQueryPacket
	{
		public object Data { get; set; }
		public string TableName { get; set; } = "";
		public TypeSQLQuery TypeSQLQuery { get; set; } = TypeSQLQuery.Broadcast;
	}
	
	/// <summary>
	/// служит для подключения БД
	/// </summary>
	[Serializable]
	public class DataBasePacket
	{
		public string Path { get; set; }
		public object Data { get; set; }
		public TypeDataBasePacket Type { get; set; }
		public InfoDataBasePacket Info { get; set; } = InfoDataBasePacket.OK;
	}
	public static class Base
	{
		public static bool LogPing = false;
		public static List<API.TypePacket> IsAuthorizationClientUse = new List<API.TypePacket>() { API.TypePacket.Ping };
		/// <summary>
		/// Отправка пакета данных и отправка хейдера для больших пакетов.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="packet"></param>
		/// <param name="cipherAES"></param>
		public static void SendPacketClient(TcpClient client, API.Packet packet, CipherAES cipherAES)
		{
			NetworkStream networkStream = client.GetStream();
			var byte_packet = Packet.ToByteArray(packet, cipherAES);
			LogWr(packet, client, byte_packet.Length);
			if (byte_packet.Length > 8192)
			{
				Packet packet1 = new Packet() { TypePacket = TypePacket.Header, Data = byte_packet.Length};
				var byte_packet1 = Packet.ToByteArray(packet1, cipherAES);
				LogWr(packet1, client, byte_packet1.Length);
				networkStream.Write(byte_packet1, 0, byte_packet1.Length);
				Thread.Sleep(60);
			}
			networkStream.Write(byte_packet, 0, byte_packet.Length);
		}
		/// <summary>
		/// логирование
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="client"></param>
		/// <param name="size"></param>
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
		/// <summary>
		/// установка базовых ключей
		/// </summary>
		public void BaseKey()
		{
			AES_KEY = KEY_base;
			AES_IV = IV_base;
		}
		/// <summary>
		/// Создание нового ключа
		/// </summary>
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
		/// <summary>
		/// получение нового экземпляра AES
		/// </summary>
		/// <returns></returns>
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
		/// <summary>
		/// Шифрование даных
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public byte[] Encrypt(byte[] data)
		{
			var aes = GetAES();

			using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
			{
				return PerformCryptography(data, encryptor);
			}
		}
		/// <summary>
		/// Шифрование данных
		/// </summary>
		/// <param name="data"></param>
		/// <param name="cryptoTransform"></param>
		/// <returns></returns>
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
		/// <summary>
		/// расшифровка даных
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public byte[] Decrypt(byte[] data)
		{
			var aes = GetAES();

			using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
			{
				return PerformCryptography(data, decryptor);
			}
		}
	}
	/// <summary>
	/// Пакет авторизации
	/// </summary>
	[Serializable]
	public class Authorization
	{
		public string Login { get; set; }
		public string Password { get; set; }
	}
	/// <summary>
	/// Стандартный пакет
	/// </summary>
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
		/// <summary>
		/// преобразование объекта в массив байт
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="cipherAES"></param>
		/// <returns></returns>
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
		/// <summary>
		/// из массива байт в пакет
		/// </summary>
		/// <param name="data"></param>
		/// <param name="cipherAES"></param>
		/// <returns></returns>
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
