using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	internal interface IPeer
	{
		public void OnMessage(Packet packet);
		public void OnRemoved();
		public void Send(Packet packet);
		public void Disconnect();
	}
}
