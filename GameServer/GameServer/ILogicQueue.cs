using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public interface ILogicQueue
	{
		internal void Enqueue(Packet msg);
		internal Queue<Packet> GetAll();
	}
}
