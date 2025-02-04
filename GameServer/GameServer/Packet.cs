using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public class Packet
	{
		public UserToken? Owner { get; private set; }
		public byte[]? Buffer { get; private set; }
		public int Position { get; private set; }
		public int Size { get; private set; }

		public short ProtocolId { get; private set; }

		public Packet()
		{
			Buffer = new byte[1024];
		}

		public Packet(ArraySegment<byte> buffer, UserToken? owner)
		{
			Buffer = buffer.Array;
			Position = Defines.HEADER_SIZE;
			Size = buffer.Count;
			ProtocolId = PopProtocolId();
			Position = Defines.HEADER_SIZE;
			Owner = owner;
		}

		public Packet(byte[] buffer, UserToken owner)
		{
			Buffer = buffer;
			Position = Defines.HEADER_SIZE;
			Owner = owner;
		}

		public static Packet Create(short protocolId)
		{
			Packet packet = new Packet();
			packet.SetProtocol(protocolId);

			return packet;
		}

		public void Destroy(Packet packet)
		{
			//PacketBufferManager.Push(packet);
		}

		public void RecoredSize()
		{
			byte[] header = BitConverter.GetBytes(Position);

			if(Buffer != null)
			{
				header.CopyTo(Buffer, 0);
			}
		}

		public short PopProtocolId()
		{
			return PopInt16();
		}

		public void CopyTo(Packet target)
		{
			if(Buffer != null)
			{
				target.SetProtocol(ProtocolId);
				target.Overwrite(Buffer, Position);
			}
		}

		public void Overwrite(byte[] src, int position)
		{
			if(Buffer != null)
			{
				Array.Copy(src, Buffer, src.Length);
				Position = position;
			}
		}

		public byte PopByte()
		{
			byte data = 0;

			if(Buffer != null)
			{
				data = Buffer[Position];
				Position += sizeof(byte);
			}

			return data;
		}

		public short PopInt16()
		{
			short data = 0;

			if(Buffer != null)
			{
				data = BitConverter.ToInt16(Buffer, Position);
				Position += sizeof(short);
			}

			return data;
		}

		public int PopInt32()
		{
			int data = 0;

			if(Buffer != null)
			{
				data = BitConverter.ToInt32(Buffer, Position);
				Position += sizeof(short);
			}

			return data;
		}

		public string PopString()
		{
			string data = string.Empty;
			
			if(Buffer != null)
			{
				short len = BitConverter.ToInt16(Buffer, Position);
				Position += sizeof(short);

				data = Encoding.UTF8.GetString(Buffer, Position, len);
				Position += len;
			}

			return data;
		}

		public float PopFloat()
		{
			float data = 0;

			if(Buffer != null)
			{
				data = BitConverter.ToSingle(Buffer, Position);
				Position += sizeof(float);
			}

			return data;
		}

		public void SetProtocol(short protocolId)
		{
			ProtocolId = protocolId;
			Position = Defines.HEADER_SIZE;

			PushInt16(protocolId);
		}

		public void PushInt16(short data)
		{
			if(Buffer != null)
			{
				byte[] tmpBuffer = BitConverter.GetBytes(data);
				tmpBuffer.CopyTo(Buffer, Position);
				Position += tmpBuffer.Length;
			}
		}

		public void Push(byte data)
		{
			if(Buffer != null)
			{
				byte[] tmpBuffer = BitConverter.GetBytes((short)data);
				tmpBuffer.CopyTo(Buffer, Position);
				Position += sizeof(byte);
			}
		}

		public void Push(short data)
		{
			if(Buffer != null)
			{
				byte[] temp_buffer = BitConverter.GetBytes(data);
				temp_buffer.CopyTo(Buffer, Position);
				Position += temp_buffer.Length;
			}
		}

		public void Push(int data)
		{
			if(Buffer != null)
			{
				byte[] temp_buffer = BitConverter.GetBytes(data);
				temp_buffer.CopyTo(Buffer, Position);
				Position += temp_buffer.Length;
			}
		}

		public void Push(string data)
		{
			if(Buffer != null)
			{
				byte[] tmpBuffer = Encoding.UTF8.GetBytes(data);
				short len = (short)tmpBuffer.Length;
				byte[] lenBuffer = BitConverter.GetBytes(len);

				lenBuffer.CopyTo(Buffer, Position);
				Position += sizeof(short);

				tmpBuffer.CopyTo(Buffer, Position);
				Position += tmpBuffer.Length;
			}
		}

		public void Push(float data)
		{
			if(Buffer != null)
			{
				byte[] tmpBuffer = BitConverter.GetBytes(data);
				tmpBuffer.CopyTo(Buffer, Position);
				Position += tmpBuffer.Length;
			}
		}
	}
}
