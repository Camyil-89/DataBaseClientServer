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
		public static void Save<T>(string path, object obj)
		{
			using (StreamWriter sw = new StreamWriter(path))
			{
				XmlSerializer xmls = new XmlSerializer(typeof(T));
				xmls.Serialize(sw, obj);
			}
		}
		public static T Load<T>(string filename)
		{
			using (StreamReader sw = new StreamReader(filename))
			{
				XmlSerializer xmls = new XmlSerializer(typeof(T));
				return (T)xmls.Deserialize(sw);
			}
		}
	}
}
