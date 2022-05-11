using System;
using System.Collections.Generic;
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
		Disconnect = 2,
		Termination = 3,
		Ping = 4,
		UpdateKey = 5,
		ConfirmKey = 6,
		Authorization = 7,
		AuthorizationFailed = 8,


		GetPathsDataBase = 9,
		ConnectDataBase = 10,

		SQLQuery = 11,
		AllocTable = 12,
	}

	[Serializable]
	public class LibraryDataBase
	{
		#region BaseClass
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

		/// <summary>
		/// Абонементы
		/// </summary>
		private DataTable _Subscriptions;
		public DataTable Subscriptions { get => _Subscriptions; set => Set(ref _Subscriptions, value); }
		/// <summary>
		/// Авторы
		/// </summary>
		private DataTable _Authors;
		public DataTable Authors { get => _Authors; set => Set(ref _Authors, value); }
		/// <summary>
		/// Библиотеки
		/// </summary>
		private DataTable _Libraries;
		public DataTable Libraries { get => _Libraries; set => Set(ref _Libraries, value); }
		/// <summary>
		/// Хранилище книг
		/// </summary>
		private DataTable _BookStorage;
		public DataTable BookStorage { get => _BookStorage; set => Set(ref _BookStorage, value); }
		/// <summary>
		/// Выданные книги
		/// </summary>
		private DataTable _IssuedBooks;
		public DataTable IssuedBooks { get => _IssuedBooks; set => Set(ref _IssuedBooks, value); }
		/// <summary>
		/// Должности
		/// </summary>
		private DataTable _Positions;
		public DataTable Positions { get => _Positions; set => Set(ref _Positions, value); }
		/// <summary>
		///  Жанр авторов
		/// </summary>
		private DataTable _GenreAuthors;
		public DataTable GenreAuthors { get => _GenreAuthors; set => Set(ref _GenreAuthors, value); }
		/// <summary>
		///  жанр книг
		/// </summary>
		private DataTable _BookGenre;
		public DataTable BookGenre { get => _BookGenre; set => Set(ref _BookGenre, value); }
		/// <summary>
		///  Зарегистрированные читатели
		/// </summary>
		private DataTable _RegisteredReaders;
		public DataTable RegisteredReaders { get => _RegisteredReaders; set => Set(ref _RegisteredReaders, value); }
		/// <summary>
		///  Категория читателей
		/// </summary>
		private DataTable _CategoryReaders;
		public DataTable CategoryReaders { get => _CategoryReaders; set => Set(ref _CategoryReaders, value); }
		/// <summary>
		///  Книги
		/// </summary>
		private DataTable _Books;
		public DataTable Books { get => _Books; set => Set(ref _Books, value); }
		/// <summary>
		///  обслуженные читатели
		/// </summary>
		private DataTable _ServedReaders;
		public DataTable ServedReaders { get => _ServedReaders; set => Set(ref _ServedReaders, value); }
		/// <summary>
		///  Обязанности
		/// </summary>
		private DataTable _Responsibilities;
		public DataTable Responsibilities { get => _Responsibilities; set => Set(ref _Responsibilities, value); }
		/// <summary>
		///  Полка
		/// </summary>
		private DataTable _Shelf;
		public DataTable Shelf { get => _Shelf; set => Set(ref _Shelf, value); }
		/// <summary>
		///  Работники
		/// </summary>
		private DataTable _Employees;
		public DataTable Employees { get => _Employees; set => Set(ref _Employees, value); }
		/// <summary>
		///  Стелаж
		/// </summary>
		private DataTable _Shelving;
		public DataTable Shelving { get => _Shelving; set => Set(ref _Shelving, value); }
		/// <summary>
		///  Тип книги
		/// </summary>
		private DataTable _BookType;
		public DataTable BookType { get => _BookType; set => Set(ref _BookType, value); }
		/// <summary>
		///  Читатели
		/// </summary>
		private DataTable _Readers;
		public DataTable Readers { get => _Readers; set => Set(ref _Readers, value); }
		/// <summary>
		///  Читательский зал
		/// </summary>
		private DataTable _ReadingRoom;
		public DataTable ReadingRoom { get => _ReadingRoom; set => Set(ref _ReadingRoom, value); }
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
