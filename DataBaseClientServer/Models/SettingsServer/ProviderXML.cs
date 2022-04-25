using System.IO;
using System.Xml.Serialization;

namespace DataBaseClientServer.Models.SettingsServer
{
	internal static class ProviderXML
	{
		private static byte[] KEY_AES = new byte[16] { 0xaa, 0x11, 0x23, 0x54, 0x32, 0x40, 0x10, 0x01, 0xd, 0xdd, 0x23, 0x90, 0x01, 0x12, 0x11, 0x02 };
		private static byte[] IV_AES = { 0xaa, 0x01, 0x0f, 0x00, 0x0b, 0x30, 0x03, 0x00, 0x60, 0x60, 0x40, 0x67, 0x01, 0x05, 0x80, 0x0f };

		public static bool Encrypt = true;
		public static void Save<T>(string path, object obj)
		{
			Log.WriteLine($"Save: {path}");
			if (Encrypt)
			{
				API.CipherAES cipherAES = new API.CipherAES();
				cipherAES.AES_KEY = KEY_AES;
				cipherAES.AES_IV = IV_AES;
				using (MemoryStream ms = new MemoryStream())
				{
					using (StreamWriter sw = new StreamWriter(ms))
					{
						XmlSerializer xmls = new XmlSerializer(typeof(T));
						xmls.Serialize(sw, obj);
					}
					using (FileStream sw = new FileStream(path, FileMode.OpenOrCreate))
					{
						var array = cipherAES.Encrypt(ms.ToArray());
						sw.Write(array, 0, array.Length);
					}
				}
			}
			else
			{
				using (StreamWriter sw = new StreamWriter(path))
				{
					XmlSerializer xmls = new XmlSerializer(typeof(T));
					xmls.Serialize(sw, obj);
				}
			}
		}
		public static T Load<T>(string filename)
		{
			Log.WriteLine($"Load: {filename}");
			if (Encrypt)
			{
				try
				{
					API.CipherAES cipherAES = new API.CipherAES();
					cipherAES.AES_KEY = KEY_AES;
					cipherAES.AES_IV = IV_AES;
					using (FileStream sw = new FileStream(filename, FileMode.Open))
					{
						byte[] array = new byte[sw.Length];
						sw.Read(array, 0, array.Length);
						using (MemoryStream ms = new MemoryStream(cipherAES.Decrypt(array)))
						{
							using (StreamReader reader = new StreamReader(ms))
							{
								XmlSerializer xmls = new XmlSerializer(typeof(T));
								return (T)xmls.Deserialize(reader);
							}
						}
					}
				}
				catch
				{
					using (StreamReader sw = new StreamReader(filename))
					{
						XmlSerializer xmls = new XmlSerializer(typeof(T));
						return (T)xmls.Deserialize(sw);
					}
				}
			}
			else
			{
				try
				{
					using (StreamReader sw = new StreamReader(filename))
					{
						XmlSerializer xmls = new XmlSerializer(typeof(T));
						return (T)xmls.Deserialize(sw);
					}
				}
				catch
				{
					API.CipherAES cipherAES = new API.CipherAES();
					cipherAES.AES_KEY = KEY_AES;
					cipherAES.AES_IV = IV_AES;
					using (FileStream sw = new FileStream(filename, FileMode.Open))
					{
						byte[] array = new byte[sw.Length];
						sw.Read(array, 0, array.Length);
						using (MemoryStream ms = new MemoryStream(cipherAES.Decrypt(array)))
						{
							using (StreamReader reader = new StreamReader(ms))
							{
								XmlSerializer xmls = new XmlSerializer(typeof(T));
								return (T)xmls.Deserialize(reader);
							}
						}
					}
				}
			}
			
		}
	}
}
