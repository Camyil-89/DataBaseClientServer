using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ExplorerDialog.FolderPicker;

namespace ExplorerDialog
{
	public class FilePicker
	{
		public static string ShowDialog(bool Multiselect, string Spliter)
		{
			OpenFileDialog fileDialog = new OpenFileDialog();
			fileDialog.Multiselect = Multiselect;

			fileDialog.ShowDialog();

			if (Multiselect)
			{
				return string.Join(Spliter, fileDialog.FileNames);
			}
			else
			{
				return fileDialog.FileName;
			}
		}
		public static string ShowDialog(bool Multiselect)
		{
			OpenFileDialog fileDialog = new OpenFileDialog();
			fileDialog.Multiselect = Multiselect;

			fileDialog.ShowDialog(new WindowWrapper(IntPtr.Zero));

			if (Multiselect)
			{
				return string.Join(",", fileDialog.FileNames);
			}
			else
			{
				return fileDialog.FileName;
			}
		}
	}
}
