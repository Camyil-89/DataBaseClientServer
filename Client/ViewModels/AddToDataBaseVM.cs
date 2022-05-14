using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using API.Logging;
using Client;
using DataBaseClientServer.Base.Command;
using DataBaseClientServer.Models;
using DataBaseClientServer.Models.Settings;

namespace DataBaseClientServer.ViewModels
{
	public class AddToDataBaseVM : Base.ViewModel.BaseViewModel
	{
		private ClientViewModel ClientViewModel { get; set; }
		public Client.Views.Windows.AddToDataBaseWindow Window { get; set; }
		public AddToDataBaseVM(ClientViewModel clientViewModel)
		{
			#region Commands
			CloseCommand = new LambdaCommand(OnCloseCommand, CanCloseCommand);
			#endregion
			ClientViewModel = clientViewModel;
		}

		public AddToDataBaseVM()
		{
			
		}

		private AddType _AddDBType;
		public AddType AddDBType { get => _AddDBType; set => Set(ref _AddDBType, value); }

		private string _Title;
		public string Title { 
			get 
			{ 
				switch (AddDBType)
				{
					case AddType.AddBook:
						return "Добавление книги";
					default:
						return "Добавление";
				}
			} set => Set(ref _Title, value);
		}

		#region Commnads
		#region CloseCommand
		public ICommand CloseCommand { get; set; }
		public bool CanCloseCommand(object e) => true;
		public void OnCloseCommand(object e)
		{
			Window.Close();
		}
		#endregion
		#endregion
	}
}
