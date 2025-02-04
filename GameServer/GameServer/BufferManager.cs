using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	internal class BufferManager
	{
		private int _numByte;
		private Stack<int> _freeIndexPool;
		private int _currentIndex;
		private int _bufferSize;
		private byte[] _buffer;

		public BufferManager(int totalByte, int bufferSize)
		{
			_numByte = totalByte;
			_currentIndex = 0;
			_bufferSize = bufferSize;
			_freeIndexPool = new Stack<int>();
		}

		public void InitBuffer()
		{
			_buffer = new byte[_bufferSize];
		}

		public bool SetBuffer(SocketAsyncEventArgs args)
		{
			if(_freeIndexPool.Count > 0)
			{
				args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
			}
			else
			{
				if((_numByte - _bufferSize) < _currentIndex)
				{
					return false;
				}

				args.SetBuffer(_buffer, _currentIndex, _bufferSize);
				_currentIndex += _bufferSize;
			}

			return true;
		}

		public void FlushBuffer(SocketAsyncEventArgs args)
		{
			_freeIndexPool.Push(args.Offset);
			args.SetBuffer(null, 0, 0);
		}
	}
}
