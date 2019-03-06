using System;
using System.Collections;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Steam;

namespace Amplitude.Unity.Session
{
	[Diagnostics.TagAttribute("Session")]
	public class SessionManager : Manager, IService, ISessionService
	{
		public SessionManager()
		{
			this.SessionClass = typeof(Session);
		}

		public event EventHandler<SessionChangeEventArgs> SessionChange;

		public Type SessionClass { get; set; }

		public Session Session { get; protected set; }

		public override IEnumerator BindServices()
		{
			yield return base.BindServices();
			base.SetLastError(0, "Waiting for service dependencies...");
			yield return base.BindService<ISteamService>();
			Services.AddService<ISessionService>(this);
			yield break;
		}

		public void CreateSession()
		{
			if (this.Session != null)
			{
				throw new InvalidOperationException("A session has already been created. Release the current session before attempting to create a new one.");
			}
			Diagnostics.Assert(this.SessionClass != null);
			this.Session = (Session)Activator.CreateInstance(this.SessionClass);
			this.Session.SessionChange += this.Session_SessionChange;
			this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Created, this.Session));
		}

		public void ReleaseSession()
		{
			if (this.Session != null)
			{
				this.Session.SessionChange -= this.Session_SessionChange;
				this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Releasing, this.Session));
				this.Session.Dispose();
				this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Released, this.Session));
				this.Session = null;
			}
		}

		protected virtual void OnDestroy()
		{
			if (this.Session != null)
			{
				this.Session = null;
			}
		}

		protected virtual void OnSessionChange(SessionChangeEventArgs e)
		{
			if (this.SessionChange != null)
			{
				this.SessionChange(this, e);
			}
		}

		protected virtual void LateUpdate()
		{
			if (this.Session != null)
			{
				this.Session.Update();
			}
		}

		[Obsolete]
		private IEnumerator ReleaseSessionAsync(Session session)
		{
			this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Releasing, session));
			yield return Coroutine.WaitForSeconds(1f);
			session.Dispose();
			this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Released, session));
			yield break;
		}

		private void Session_SessionChange(object sender, SessionChangeEventArgs e)
		{
			this.OnSessionChange(e);
		}

		private void ReleaseSessionAsync_CoroutineExceptionCallback(object sender, CoroutineExceptionEventArgs e)
		{
			Diagnostics.LogError("An exception has been raised while releasing the session.\n{0}", new object[]
			{
				e.Exception.ToString()
			});
		}
	}
}
