using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public class UserToken
	{
		enum State
		{
			Idle,
			Connected,
			ReserveClosing,
			Closed,
		}

		private State _currentState;
		public Socket? socket;

		const short SYS_CLOSE_REQ = 0;
		const short SYS_CLOSE_ACK = -1;
		public const short SYS_START_HEARTBEAT = -2;
		public const short SYS_UPDATE_HEARTBEAT = -3;

		private int _isClosed;

		public SocketAsyncEventArgs ReceiveEventArgs { get; private set; }
		public SocketAsyncEventArgs SendEventArgs { get; private set; }

		private List<ArraySegment<byte>> _sendingList;
		private object _sendingQueue;
		private IMessageDispatcher _messageDispatcher;

		public long LastestHeartbeatTime { get; private set; }
		private HeartbeatSender _heartbeatSender;
		private bool _autoHeartbeat;
		
		private MessageResolver _messageResolver;
		private IPeer? _peer;

		public delegate void CloseDelegate(UserToken token);
		public CloseDelegate OnSessionClosed;

		public UserToken(IMessageDispatcher dispatcher)
		{
			_messageDispatcher = dispatcher;
			_sendingQueue = new object();

			_messageResolver = new MessageResolver();
			_peer = null;
			_sendingList = new List<ArraySegment<byte>>();
			LastestHeartbeatTime = DateTime.Now.Ticks;

			_currentState = State.Idle;
		}

		public void OnConnected()
		{
			_currentState = State.Connected;
			_isClosed = 0;
			_autoHeartbeat = true;
		}

		public void SetPeer(IPeer peer)
		{
			_peer = peer;
		}

		public void SetEventArgs(SocketAsyncEventArgs receiveEventArgs, SocketAsyncEventArgs sendEventArgs)
		{
			ReceiveEventArgs = receiveEventArgs;
			SendEventArgs = sendEventArgs;
		}

		public void OnReceive(byte[] buffer, int offset, int transfered)
		{
			_messageResolver.OnReceive(buffer, offset, transfered, OnMessageCompleted);
		}

		private void OnMessageCompleted(ArraySegment<byte> buffer)
		{
			if(_peer == null)
			{
				return;
			}

			if(_messageDispatcher != null)
			{
				_messageDispatcher.OnMessage(this, buffer);
			}
			else
			{
				Packet packet = new Packet(buffer, this);
				OnMessage(packet);
			}
		}

		public void OnMessage(Packet msg)
		{
			switch(msg.ProtocolId)
			{
				case SYS_CLOSE_REQ:
					Disconnect();

					return;

				case SYS_START_HEARTBEAT:
					msg.PopProtocolId();
					byte interval = msg.PopByte();
					_heartbeatSender = new HeartbeatSender(this, interval);

					if(_autoHeartbeat)
					{
						StartHeartbeat();
					}

					return;

				case SYS_UPDATE_HEARTBEAT:
					LastestHeartbeatTime = DateTime.Now.Ticks;

					return;
			}

			if(_peer != null)
			{
				try
				{
					switch(msg.ProtocolId)
					{
						case SYS_CLOSE_ACK:
							_peer.OnRemoved();

							break;

						default:
							_peer.OnMessage(msg);

							break;
					}
				}
				catch(Exception)
				{
					Close();
				}
			}

			if(msg.ProtocolId == SYS_CLOSE_ACK)
			{
				OnSessionClosed?.Invoke(this);
			}
		}

		public void Close()
		{
			if(Interlocked.CompareExchange(ref _isClosed, 1, 0) == 1)
			{
				return;
			}

			if(_currentState == State.Closed)
			{
				return;
			}

			_currentState = State.Closed;
			socket?.Close();
			socket = null;

			SendEventArgs.UserToken = null;
			ReceiveEventArgs.UserToken = null;

			_sendingList.Clear();
			_messageResolver.ClearBuffer();

			if(_peer != null)
			{
				Packet msg = Packet.Create(-1);

				if(_messageDispatcher != null)
				{
					if(msg.Buffer != null)
					{
						_messageDispatcher.OnMessage(this, new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
					}
				}
				else
				{
					OnMessage(msg);
				}
			}
		}

		public void Send(Packet msg)
		{
			msg.RecoredSize();

			if(msg.Buffer != null)
			{
				Send(new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
			}

		}

		public void Send(ArraySegment<byte> data)
		{
			lock(_sendingQueue)
			{
				_sendingList.Add(data);

				if(_sendingList.Count > 1)
				{
					return;
				}
			}

			StartSend();
		}

		public void StartSend()
		{
			try
			{
				SendEventArgs.BufferList = _sendingList;

				if(socket != null)
				{
					bool pending = socket.SendAsync(SendEventArgs);

					if(pending == false)
					{
						ProcessSend(SendEventArgs);
					}
				}
			}
			catch(Exception e)
			{
				if(socket == null)
				{
					Close();

					return;
				}

				Console.WriteLine($"Send Error. Close socket. {e.Message}");
				throw new Exception(e.Message, e);
			}
		}

		public void ProcessSend(SocketAsyncEventArgs args)
		{
			if(args.BytesTransferred <= 0 || args.SocketError != SocketError.Success)
			{
				return;
			}

			lock(_sendingQueue)
			{
				int size = _sendingList.Sum(obj => obj.Count);

				if(args.BytesTransferred != size)
				{
					if(args.BytesTransferred < _sendingList[0].Count)
					{
						string error = $"Need to send more! transferred {args.BytesTransferred},  packet size {size}";
						Console.WriteLine(error);
						Close();

						return;
					}

					int sentIndex = 0;
					int sum = 0;

					for(int i = 0; i < _sendingList.Count; i++)
					{
						sum += _sendingList[i].Count;

						if(sum <= args.BytesTransferred)
						{
							sentIndex = i;

							continue;
						}

						break;
					}

					_sendingList.RemoveRange(0, sentIndex + 1);
					StartSend();

					return;
				}

				_sendingList.Clear();

				if(_currentState == State.ReserveClosing)
				{
					socket?.Shutdown(SocketShutdown.Send);
				}
			}
		}

		public void Disconnect()
		{
			try
			{
				if(_sendingList.Count <= 0)
				{
					socket?.Shutdown(SocketShutdown.Send);

					return;
				}

				_currentState = State.ReserveClosing;
			}
			catch(Exception)
			{
				Close();
			}
		}

		public void Ban()
		{
			try
			{
				Kick();
			}
			catch (Exception)
			{
				Close();
			}
		}

		public void Kick()
		{
			Packet packet = Packet.Create(SYS_CLOSE_REQ);

			Send(packet);
		}

		public bool IsConnected()
		{
			bool result = _currentState == State.Connected;

			return result;
		}

		public void StartHeartbeat()
		{
			_heartbeatSender?.Play();
		}

		public void StopHeartbeat()
		{
			_heartbeatSender?.Stop();
		}

		public void DisableAutoHeartbeat()
		{
			StopHeartbeat();

			_autoHeartbeat = false;
		}

		public void UpdateHeartbeatManually(float time)
		{
			_heartbeatSender?.Update(time);
		}
	}
}
