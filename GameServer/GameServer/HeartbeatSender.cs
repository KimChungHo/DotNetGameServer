using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	internal class HeartbeatSender
	{
		private UserToken _server;
		private Timer _timerHeartbeat;
		private uint _interval;

		private float _elapsedTime;

		public HeartbeatSender(UserToken server, long interval)
		{
			_server = server;
			_interval = interval;
			_timerHeartbeat = new Timer(OnTimer, null, Timeout.Infinite, _interval * 1000);
		}

		private void OnTimer(object? state)
		{
			Send();
		}

		private void Send()
		{
			Packet packet = Packet.Create(UserToken.SYS_UPDATE_HEARTBEAT);
			_server.Send(packet);
		}

		public void Update(float time)
		{
			_elapsedTime += time;

			if(_elapsedTime < _interval)
			{
				return;
			}

			_elapsedTime = 0;
			Send();
		}

		public void Stop()
		{
			_elapsedTime = 0;
			_timerHeartbeat.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Play()
		{
			_elapsedTime = 0;
			_timerHeartbeat.Change(0, _interval * 1000);
		}
	}
}
