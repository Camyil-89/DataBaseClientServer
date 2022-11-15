using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
		public readonly ClientViewModel ClientViewModel;
		public Client.Views.Windows.AddToDataBaseWindow Window { get; set; }
	
		private AddType _AddDBType = AddType.AddBook;
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

		#region добавление книг
		private Visibility _VisibilityAddBook = Visibility.Collapsed;
		public Visibility VisibilityAddBook {
			get
			{
				switch (AddDBType)
				{
					case AddType.AddBook:
						return Visibility.Visible;
					default:
						return Visibility.Collapsed;
				}
				
			}
			set => Set(ref _VisibilityAddBook, value);
		}
		private ObservableCollection<DataRow> _ComboBoxTypeBook = new ObservableCollection<DataRow>();
		public ObservableCollection<DataRow> ComboBoxTypeBook { get => _ComboBoxTypeBook; set => Set(ref _ComboBoxTypeBook, value); }


		private DataRow _SelectedTypeBook;
		public DataRow SelectedTypeBook { get => _SelectedTypeBook; set => Set(ref _SelectedTypeBook, value); }

		private ObservableCollection<DataRow> _ComboBoxAuthor = new ObservableCollection<DataRow>();
		public ObservableCollection<DataRow> ComboBoxAuthor { get => _ComboBoxAuthor; set => Set(ref _ComboBoxAuthor, value); }

		private DataRow _SelectedAuthor;
		public DataRow SelectedAuthor { get => _SelectedAuthor; set => Set(ref _SelectedAuthor, value); }

		private ObservableCollection<DataRow> _ComboBoxGenre = new ObservableCollection<DataRow>();
		public ObservableCollection<DataRow> ComboBoxGenre { get => _ComboBoxGenre; set => Set(ref _ComboBoxGenre, value); }

		private DataRow _SelectedGenre;
		public DataRow SelectedGenre { get => _SelectedGenre; set => Set(ref _SelectedGenre, value); }

		private string _NameBook = string.Empty;
		public string NameBook { get => _NameBook; set => Set(ref _NameBook, value); }

		private float _Rating = -1;
		public float Rating { get => _Rating; set => Set(ref _Rating, value); }

		private bool AddRow = false;
		/// <summary>
		/// Запуск приложения
		/// </summary>
		/// <param name="clientViewModel"></param>
		public AddToDataBaseVM(ClientViewModel clientViewModel)
		{
			#region Commands
			CloseCommand = new LambdaCommand(OnCloseCommand, CanCloseCommand);
			AddRowCommand = new LambdaCommand(OnAddRowCommand, CanAddRowCommand);
			#endregion
			ClientViewModel = clientViewModel;
		}

		public AddToDataBaseVM()
		{

		}

		#endregion
		/// <summary>
		/// устанавливает значения
		/// </summary>
		public void FillProperty()
		{
			switch (AddDBType)
			{
				case AddType.AddBook:
					var table = ClientViewModel.GetTableFromName("Тип книги");
					ComboBoxTypeBook.Clear();
					foreach (DataRow i in table.Table.Rows) ComboBoxTypeBook.Add(i);

					table = ClientViewModel.GetTableFromName("Авторы");
					ComboBoxAuthor.Clear();
					foreach (DataRow i in table.Table.Rows) ComboBoxAuthor.Add(i);

					table = ClientViewModel.GetTableFromName("Жанры");
					ComboBoxGenre.Clear();
					foreach (DataRow i in table.Table.Rows) ComboBoxGenre.Add(i);

					NameBook = string.Empty;
					Rating = -1;
					SelectedAuthor = null;
					SelectedGenre = null;
					SelectedTypeBook = null;
					break;
			}
		}
		#region Commnads
		#region AddRowCommand
		/// <summary>
		/// команда для кнопки добавления
		/// </summary>
		public ICommand AddRowCommand { get; set; }
		public bool CanAddRowCommand(object e)
		{
			switch (AddDBType)
			{
				case AddType.AddBook:
					return NameBook != string.Empty &&
					Rating >= 0 &&
					SelectedAuthor != null &&
					SelectedGenre != null &&
					SelectedTypeBook != null;
				default:
					return false;
			}
		}
		public void OnAddRowCommand(object e)
		{
			AddRow = true;
			Window.Close();
		}
		/// <summary>
		/// получение новой записи
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, object> GetDataRow()
		{
			switch (AddDBType)
			{
				case AddType.AddBook:
					DataTable dataTable = ClientViewModel.GetTableFromName("Книги").Table;
					return new Dictionary<string, object>() {
						{ "!TableName!", "Книги"},
						{ "ID_книга", ClientViewModel.GetID(dataTable)},
						{ "Название", NameBook},
						{ "ID_жанр", SelectedGenre.ItemArray[0]},
						{ "Рейтинг", Rating},
						{ "ID_автор", SelectedAuthor.ItemArray[0]},
						{ "ID_тип", SelectedTypeBook.ItemArray[0]},
					};
			}
			return null;
		}
		/// <summary>
		/// получение sql запроса
		/// </summary>
		/// <returns></returns>
		public List<string> GetSQLQuery()
		{
			var sqls = new List<string>();
			//string sql = null;
			if (!AddRow) return null;
			switch (AddDBType)
			{
				case AddType.AddBook:
					DataTable dataTable = ClientViewModel.GetTableFromName("Книги").Table;
					//sql = $"INSERT INTO Книги(ID_книга, Название, ID_жанр, Рейтинг, ID_автор, ID_тип) " +
					//	$"VALUES({ClientViewModel.GetID(dataTable)}, '{NameBook}', {SelectedGenre.ItemArray[0]}, {Rating}, {SelectedAuthor.ItemArray[0]}, {SelectedTypeBook.ItemArray[0]});";
					sqls.Add($"INSERT INTO Книги(ID_книга, Название, Рейтинг, ID_автор, ID_тип) " +
						$"VALUES({ClientViewModel.GetID(dataTable)}, '{NameBook}', {Rating}, {SelectedAuthor.ItemArray[0]}, {SelectedTypeBook.ItemArray[0]});");
					sqls.Add($"INSERT INTO `жанр книг` (`ID_жанр_книги`, `ID_книга`, `ID_жанра`) VALUES (NULL, {ClientViewModel.GetID(dataTable)}, {SelectedGenre.ItemArray[0]});");
					break;
			}
			AddRow = false;
			return sqls;
		}
		#endregion
		#region CloseCommand
		/// <summary>
		/// Закрытие окна
		/// </summary>
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
