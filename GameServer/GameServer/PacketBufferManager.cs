using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public class PacketBufferManager
	{
		//private static object CsBuffer = new object();
		//private static Stack<Packet> Pool;
		//private static int PoolCapacity;

		//public static void Init(int capacity)
		//{
		//	Pool = new Stack<Packet>();

		//	PoolCapacity = capacity;
		//	Allocate();
		//}

		//private static void Allocate()
		//{
		//	for(int i = 0; i < PoolCapacity; i++)
		//	{
		//		Pool.Push(new Packet());
		//	}
		//}

		//public static Packet Pop()
		//{
		//	lock(CsBuffer)
		//	{
		//		if(Pool.Count <= 0)
		//		{
		//			Console.WriteLine("Reallocate.");
		//			Allocate();
		//		}

		//		return Pool.Pop();
		//	}
		//}

		//public static void Push(Packet packet)
		//{
		//	lock(CsBuffer)
		//	{
		//		Pool.Push(packet);
		//	}
		//}
	}
}
