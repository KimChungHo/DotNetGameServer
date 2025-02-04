using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	internal class SocketAsyncEventArgsPool
	{
		private Stack<SocketAsyncEventArgs> _pool;

		public SocketAsyncEventArgsPool(int capacity)
		{
			_pool = new Stack<SocketAsyncEventArgs>(capacity);
		}

		public void Push(SocketAsyncEventArgs item)
		{
			if(item == null)
			{
				throw new ArgumentNullException("Item은 null이 될 수 없습니다.");
			}

			lock(_pool)
			{
				if(_pool.Contains(item))
				{
					throw new Exception("존재하는 Item입니다.");
				}

				_pool.Push(item);
			}
		}

		public SocketAsyncEventArgs Pop()
		{
			lock(_pool)
			{
				return _pool.Pop();
			}
		}

		public int Count
		{
			get
			{
				return _pool.Count;
			}
		}
	}
}
