using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public class NetworkService
	{
		private Listener _clientListener;

		private SocketAsyncEventArgsPool _receiveEventArgsPool;
		private SocketAsyncEventArgsPool _sendEventArgsPool;

		public delegate void SessionHandler(UserToken token);
		public SessionHandler? SessionCreateCallback { get; set; }

		public LogicMessageEntry LogicEntry { get; private set; }
		public ServerUserManager ServerUserManager { get; private set; }

		public NetworkService(bool useLogicThread = false)
		{
			SessionCreateCallback = null;
			ServerUserManager = new ServerUserManager();

			if(useLogicThread == true)
			{
				LogicEntry = new LogicMessageEntry(this);
				LogicEntry.Start();
			}
		}

		public void Initialize()
		{
			Initialize(10000, 1024);
		}

		public void Initialize(int maxConnection, int bufferSize)
		{
			int preAllocCount = 1;

			BufferManager bufferManager = new BufferManager(maxConnection * bufferSize * preAllocCount, bufferSize);
			_receiveEventArgsPool = new SocketAsyncEventArgsPool(maxConnection);
			_sendEventArgsPool = new SocketAsyncEventArgsPool(maxConnection);

			bufferManager.InitBuffer();

			SocketAsyncEventArgs args;

			for(int i = 0; i < maxConnection; i++)
			{
				// receive pool
				args = new SocketAsyncEventArgs();
				args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
				args.UserToken = null;
				bufferManager.SetBuffer(args);
				_receiveEventArgsPool.Push(args);

				// send pool
				args = new SocketAsyncEventArgs();
				args.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);
				args.UserToken = null;
				args.SetBuffer(null, 0, 0);
				_sendEventArgsPool.Push(args);
			}
		}

		public void Listen(string host, int port, int backlog)
		{
			Listener clientListener = new Listener();
			clientListener.callbackOnNewClient += OnNewClient;
			clientListener.Start(host, port, backlog);

			ServerUserManager.StartHeartbeatChecking(10, 10);
		}

		public void DisableHeartbeat()
		{
			ServerUserManager.StopHeartbeatChecking();
		}

		public void OnConnectCompleted(Socket socket, UserToken token)
		{
			token.OnSessionClosed += OnSessionClosed;
			ServerUserManager.Add(token);

			SocketAsyncEventArgs receiveEventArgs = new SocketAsyncEventArgs();
			receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
			receiveEventArgs.UserToken = token;
			receiveEventArgs.SetBuffer(new byte[1024], 0, 1024);

			SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();
			sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);
			sendEventArgs.UserToken = token;
			sendEventArgs.SetBuffer(null, 0, 0);

			BeginReceive(socket, receiveEventArgs, sendEventArgs);
		}

		private void OnNewClient(Socket? socket, object? token)
		{
			SocketAsyncEventArgs receiveArgs = _receiveEventArgsPool.Pop();
			SocketAsyncEventArgs sendArgs = _sendEventArgsPool.Pop();

			UserToken userToken = new UserToken(LogicEntry);
			userToken.OnSessionClosed += OnSessionClosed;
			receiveArgs.UserToken = userToken;
			sendArgs.UserToken = userToken;

			ServerUserManager.Add(userToken);
			userToken.OnConnected();
			SessionCreateCallback?.Invoke(userToken);
			BeginReceive(socket, receiveArgs, sendArgs);

			Packet packet = Packet.Create(UserToken.SYS_START_HEARTBEAT);
			packet.Push(5);
			userToken.Send(packet);
		}

		private void BeginReceive(Socket? socket, SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
		{
			UserToken? token = receiveArgs.UserToken as UserToken;

			if(token != null)
			{
				token.SetEventArgs(receiveArgs, sendArgs);
				token.socket = socket;
			}

			if(socket != null)
			{
				bool pending = socket.ReceiveAsync(receiveArgs);

				if(pending == false)
				{
					ProcessReceive(receiveArgs);
				}
			}
		}

		private void ReceiveCompleted(object? sender, SocketAsyncEventArgs args)
		{
			if(args.LastOperation == SocketAsyncOperation.Receive)
			{
				ProcessReceive(args);

				return;
			}
			else
			{
				throw new ArgumentException("The last operation completed on the socket was not a receive.");
			}
		}

		private void SendCompleted(object? sender, SocketAsyncEventArgs args)
		{
			try
			{
				UserToken? token = args.UserToken as UserToken;

				token?.ProcessSend(args);
			}
			catch(Exception)
			{
			}
		}

		private void ProcessReceive(SocketAsyncEventArgs args)
		{
			UserToken? token = args.UserToken as UserToken;

			if(token != null)
			{
				if(args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
					if(args.Buffer != null)
					{
						token.OnReceive(args.Buffer, args.Offset, args.BytesTransferred);
					}

					if(token.socket != null)
					{
						bool pending = token.socket.ReceiveAsync(args);

						if(pending == false)
						{
							ProcessReceive(args);
						}
					}
				}
				else
				{
					try
					{
						token.Close();
					}
					catch(Exception)
					{
						Console.WriteLine("Already closed this socket.");
					}
				}
			}
		}

		private void OnSessionClosed(UserToken token)
		{
			ServerUserManager.Remove(token);
			_receiveEventArgsPool?.Push(token.ReceiveEventArgs);
			_sendEventArgsPool?.Push(token.SendEventArgs);

			token.SetEventArgs(null, null);
		}
	}
}