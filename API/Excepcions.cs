using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Excepcion
{
	public class ExcepcionClientConnectLose : Exception
	{
		public override string Message => "Подключение разорвано.";
	}
	public class ExcepcionTimeOut : Exception
	{
		public override string Message => "Превышено время ожидания";
	}
	public class ExcepcionIsAuthorizationClientUse : Exception
	{
		public override string Message => "Для использования этого запроса требуется авторизация со стороны сервера.";
	}
}
