using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	internal class Listener
	{
		private SocketAsyncEventArgs _acceptArgs;
		private Socket _listenSocket;
		private AutoResetEvent _flowControlEvent;

		public delegate void NewClientHandler(Socket? clientSocket, object? token);
		public NewClientHandler? callbackOnNewClient;

		public Listener()
		{
			callbackOnNewClient = null;
		}

		public void Start(string host, int port, int backlog)
		{
			_listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			IPAddress address;

			if(host == "0.0.0.0")
			{
				address = IPAddress.Any;
			}
			else
			{
				 address = IPAddress.Parse(host);
			}

			IPEndPoint endPoint = new IPEndPoint(address, port);

			try
			{
				_listenSocket.Bind(endPoint);
				_listenSocket.Listen(backlog);

				_acceptArgs = new SocketAsyncEventArgs();
				_acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

				Thread listenThread = new Thread(DoListen);
				listenThread.Start();
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private void DoListen()
		{
			_flowControlEvent = new AutoResetEvent(false);

			while(true)
			{
				_acceptArgs.AcceptSocket = null;

				bool pending = true;

				try
				{
					pending = _listenSocket.AcceptAsync(_acceptArgs);
				}
				catch(Exception e)
				{
					Console.WriteLine(e.Message);

					continue;
				}

				if(pending == false)
				{
					OnAcceptCompleted(null, _acceptArgs);
				}

				_flowControlEvent.WaitOne();
			}
		}

		private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs args)
		{
			if(args.SocketError == SocketError.Success)
			{
				Socket? clientSocket = args.AcceptSocket;

				if(clientSocket != null)
				{
					clientSocket.NoDelay = true;
				}

				callbackOnNewClient?.Invoke(clientSocket, args.UserToken);

				_flowControlEvent.Set();

				return;
			}
			else
			{
				Console.WriteLine($"Failed to accept client.\n{args.SocketError}");
			}

			_flowControlEvent.Set();
		}
	}
}
