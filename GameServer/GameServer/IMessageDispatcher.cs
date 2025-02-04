using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public interface IMessageDispatcher
	{
		internal void OnMessage(UserToken user, ArraySegment<byte> buffer);
	}
}
