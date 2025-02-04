using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	internal class Peer
	{
		public void OnMessage(Const<byte[]> buffer)
		{
			Packet packet = new Packet(buffer.Value, null);
			short protocolId = packet.PopInt16();

			switch(protocolId)
			{
				case 1:
					int number = packet.PopInt32();
					string text = packet.PopString();

					Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId})] [Received] {protocolId} : {number}, {text}");
					break;
			}
		}
	}
}
