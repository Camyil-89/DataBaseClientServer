using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataBaseClientServer.Models.SettingsServer
{
	internal static class ProviderXML
	{
		public static byte[] KEY_AES = API.CipherAES.KEY_base;
		public static byte[] IV_AES = API.CipherAES.IV_base;
		public static void Save<T>(string path, object obj)
		{
			API.CipherAES cipherAES = new API.CipherAES();
			cipherAES.AES_KEY = KEY_AES;
			cipherAES.AES_IV = IV_AES;
			using (MemoryStream ms = new MemoryStream())
			{
				XmlSerializer xmls = new XmlSerializer(typeof(T));
				xmls.Serialize(ms, obj);
				using (FileStream sw = new FileStream(path, FileMode.Open))
				{
					var array = cipherAES.Encrypt(ms.ToArray());
					sw.Write(array, 0, array.Length);
				}
			}
		}
		public static T Load<T>(string filename)
		{
			API.CipherAES cipherAES = new API.CipherAES();
			cipherAES.AES_KEY = KEY_AES;
			cipherAES.AES_IV = IV_AES;
			using (FileStream sw = new FileStream(filename, FileMode.Open))
			{
				byte[] array = new byte[sw.Length];
				sw.Read(array, 0, array.Length);
				using (MemoryStream ms= new MemoryStream(cipherAES.Decrypt(array)))
				{
					XmlSerializer xmls = new XmlSerializer(typeof(T));
					return (T)xmls.Deserialize(ms);
				}
			}
		}
	}
}
