using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Logging
{
	public static class Log
	{
		public static void WriteLine(object obj)
		{
			Console.WriteLine($"[{DateTime.Now}.{DateTime.Now.Millisecond}] {obj}");
		}
	}
}
