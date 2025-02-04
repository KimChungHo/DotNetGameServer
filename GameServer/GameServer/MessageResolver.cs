using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	internal class Defines
	{
		public static readonly short HEADER_SIZE = 4;
	}

	public delegate void CompletedMessageCallback(ArraySegment<byte> buffer);

	internal class MessageResolver
	{
		private int _messageSize;
		private byte[] _buffer = new byte[1024];
		private int _currentPosition;
		private int _positionToRead;
		private int _remainByte;

		public MessageResolver()
		{
			_messageSize = 0;
			_currentPosition = 0;
			_positionToRead = 0;
			_remainByte = 0;
		}

		private bool ReadUntil(byte[] buffer, ref int srcPosition)
		{
			int copySize = _positionToRead - _currentPosition;

			if(_remainByte < copySize)
			{
				copySize = _remainByte;
			}

			Array.Copy(buffer, srcPosition, _buffer, _currentPosition, copySize);

			srcPosition += copySize;
			_currentPosition += copySize;
			_remainByte -= copySize;

			if(_currentPosition < _positionToRead)
			{
				return false;
			}

			return true;
		}

		public void OnReceive(byte[] buffer, int offset, int transffered, CompletedMessageCallback callback)
		{
			_remainByte = transffered;

			int srcPosition = offset;

			while(_remainByte > 0)
			{
				bool completed = false;

				if(_currentPosition < Defines.HEADER_SIZE)
				{
					_positionToRead = Defines.HEADER_SIZE;
					completed = ReadUntil(buffer, ref srcPosition);

					if(completed == false)
					{
						return;
					}

					_messageSize = GetTotalMessageSize();

					if(_messageSize <= 0)
					{
						ClearBuffer();

						return;
					}

					_positionToRead = _messageSize;

					if(_remainByte <= 0)
					{
						return;
					}
				}

				completed = ReadUntil(buffer, ref srcPosition);

				if(completed == false)
				{
					byte[] clone = new byte[_positionToRead];

					Array.Copy(_buffer, clone, _positionToRead);
					ClearBuffer();
					callback(new ArraySegment<byte>(clone, 0, _positionToRead));
				}
			}
		}

		private int GetTotalMessageSize()
		{
			int result = BitConverter.ToInt32(_buffer, 0);

			return result;
		}

		public void ClearBuffer()
		{
			Array.Clear(_buffer, 0, _buffer.Length);

			_currentPosition = 0;
			_messageSize = 0;
		}
	}
}
