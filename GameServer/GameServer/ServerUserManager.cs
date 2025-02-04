using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public class ServerUserManager
	{
		private object _csUser;
		private List<UserToken> _users;
		private Timer _timerHeartbeat;
		private long _heartbeatDuration;

		public ServerUserManager()
		{
			_csUser = new object();
			_users = new List<UserToken>();
		}

		public void StartHeartbeatChecking(uint checkIntervalSec, uint allowDurationSec)
		{
			_heartbeatDuration = allowDurationSec * 10000000;
			_timerHeartbeat = new Timer(CheckHeartbeat, null, 1000 * checkIntervalSec, 1000 * checkIntervalSec);
		}

		public void StopHeartbeatChecking()
		{
			_timerHeartbeat.Dispose();
		}

		public void Add(UserToken user)
		{
			lock(_csUser)
			{
				_users.Add(user);
			}
		}

		public void Remove(UserToken user)
		{
			lock(_csUser)
			{
				_users.Remove(user);
			}
		}

		public bool IsExist(UserToken user)
		{
			lock(_csUser)
			{
				return _users.Exists(obj => obj == user);
			}
		}

		public int GetTotalCount()
		{
			return _users.Count;
		}

		private void CheckHeartbeat(object state)
		{
			long allowedTime = DateTime.Now.Ticks - _heartbeatDuration;

			lock(_csUser)
			{
				for(int i = 0; i < _users.Count; i++)
				{
					long heartbeatTime = _users[i].LastestHeartbeatTime;

					if(heartbeatTime >= allowedTime)
					{
						continue;
					}

					_users[i].Disconnect();
				}
			}
		}
	}
}
