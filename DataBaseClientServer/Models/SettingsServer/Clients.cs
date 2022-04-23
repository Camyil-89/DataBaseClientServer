using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseClientServer.Models.SettingsServer
{
	public class Client: Base.ViewModel.BaseViewModel
	{

		private string _Name;
		public string Name { get => _Name; set => Set(ref _Name, value); }

		private string _Password;
		public string Password { get => _Password; set => Set(ref _Password, value); }

		private API.AccessLevel _AccessLevel;
		public API.AccessLevel AccessLevel { get => _AccessLevel; set => Set(ref _AccessLevel, value); }
		public int UID { get; set; } //Guid.NewGuid().ToString();

		public static int GenerateUIDClient(ObservableCollection<Client> clients)
		{
			int uid = 0;
			foreach (var i in clients)
			{
				foreach (var j in clients)
				{
					if (j.UID == uid) uid++;
				}
			}
			return uid;
		}
	}
}
