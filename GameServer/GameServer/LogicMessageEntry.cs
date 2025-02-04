using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public class LogicMessageEntry : IMessageDispatcher
	{
		private NetworkService _service;
		private ILogicQueue _messageQueue;
		private AutoResetEvent _logicEvent;

		public LogicMessageEntry(NetworkService service)
		{
			_service = service;
			_messageQueue = new DoubleBufferingQueue();
			_logicEvent = new AutoResetEvent(false);
		}

		void IMessageDispatcher.OnMessage(UserToken user, ArraySegment<byte> buffer)
		{
			Packet packet = new Packet(buffer, user);

			_messageQueue.Enqueue(packet);
			_logicEvent.Set();
		}

		public void Start()
		{
			Thread thread = new Thread(DoLogic);
			thread.Start();
		}

		private void DoLogic()
		{
			while(true)
			{
				_logicEvent.WaitOne();

				DispatchAll(_messageQueue.GetAll());
			}
		}

		private void DispatchAll(Queue<Packet> queue)
		{
			while(queue.Count > 0)
			{
				Packet packet = queue.Dequeue();

				if(packet.Owner != null)
				{
					if(_service.ServerUserManager.IsExist(packet.Owner))
					{
						continue;
					}

					packet.Owner.OnMessage(packet);
				}
			}
		}
	}
}
