using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	internal class DoubleBufferingQueue : ILogicQueue
	{
		private Queue<Packet> _queue1;
		private Queue<Packet> _queue2;

		private Queue<Packet> _refInput;
		private Queue<Packet> _refOutput;

		private object _csWrite;

		public DoubleBufferingQueue()
		{
			_queue1 = new Queue<Packet>();
			_queue2 = new Queue<Packet>();
			_refInput = new Queue<Packet>();
			_refOutput = new Queue<Packet>();

			_csWrite = new object();
		}

		void ILogicQueue.Enqueue(Packet msg)
		{
			lock(_csWrite)
			{
				_refInput.Enqueue(msg);
			}
		}

		Queue<Packet> ILogicQueue.GetAll()
		{
			Swap();
			return _refOutput;
		}

		private void Swap()
		{
			lock(_csWrite)
			{
				Queue<Packet> tmp = _refInput;
				_refInput = _refOutput;
				_refOutput = tmp;
			}
		}
	}
}
