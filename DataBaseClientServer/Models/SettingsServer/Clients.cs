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

		private string _Login;
		public string Login { get => _Login; set => Set(ref _Login, value); }

		private string _Password;
		public string Password { get => _Password; set => Set(ref _Password, value); }

		private API.AccessLevel _AccessLevel = API.AccessLevel.NonAuthorization;
		public API.AccessLevel AccessLevel { get => _AccessLevel; set => Set(ref _AccessLevel, value); }
		public int UID { get; set; } //Guid.NewGuid().ToString();


		public List<API.TypePacket> GetTypesDenayPacket()
		{
			switch (AccessLevel)
			{
				case API.AccessLevel.NonAuthorization:
					return new List<API.TypePacket>() {
							API.TypePacket.Ping,
							API.TypePacket.SQLQuery,
							API.TypePacket.GetPathsDataBase,
							API.TypePacket.ConnectDataBase,
							API.TypePacket.SQLQuery,
							API.TypePacket.AllocTable,
						};
				case API.AccessLevel.Worker:
					return new List<API.TypePacket>() { };
				case API.AccessLevel.User:
					return new List<API.TypePacket>() { API.TypePacket.SQLQuery };
				default:
					return new List<API.TypePacket>() { API.TypePacket.Disconnect,
							API.TypePacket.Termination,
							API.TypePacket.Ping,
							API.TypePacket.UpdateKey,
							API.TypePacket.ConfirmKey,
							API.TypePacket.Authorization,
							API.TypePacket.GetPathsDataBase,
							API.TypePacket.ConnectDataBase,
							API.TypePacket.SQLQuery,
							API.TypePacket.AllocTable,
						};
			}
		}
		public bool DenayPackages(API.TypePacket packet)
		{
			return GetTypesDenayPacket().Contains(packet);
		}
		

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
