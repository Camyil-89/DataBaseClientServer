﻿using System;
using System.Windows.Input;

namespace DataBaseClientServer.Base.Command
{
	/// <summary>
	/// класс для создания команды
	/// </summary>
	internal abstract class Command : ICommand
	{
		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public abstract bool CanExecute(object parameter);

		public abstract void Execute(object parameter);
	}
}