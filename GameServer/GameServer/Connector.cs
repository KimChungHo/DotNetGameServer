using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public class Connector
	{
		public delegate void ConnectedHandler(UserToken token);
		public ConnectedHandler? ConnectedCallback { get; set; }

		private Socket _client;
		private NetworkService _networkService;

		public Connector(NetworkService networkService)
		{
			_networkService = networkService;
			ConnectedCallback = null;
		}

		public void Connect(IPEndPoint remoteEndpoint)
		{
			_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_client.NoDelay = true;

			SocketAsyncEventArgs args = new SocketAsyncEventArgs();
			args.Completed += OnConnectCompleted;
			args.RemoteEndPoint = remoteEndpoint;

			bool pending = _client.ConnectAsync(args);

			if(pending == false)
			{
				OnConnectCompleted(null, args);
			}
		}

		private void OnConnectCompleted(object? sender, SocketAsyncEventArgs args)
		{
			if(args.SocketError == SocketError.Success)
			{
				UserToken token = new UserToken(_networkService.LogicEntry);

				if(ConnectedCallback != null)
				{
					ConnectedCallback(token);
				}

				_networkService.OnConnectCompleted(_client, token);
			}
			else
			{
				Console.WriteLine($"Failed To connect. {args.SocketError}");
			}
		}
	}
}
