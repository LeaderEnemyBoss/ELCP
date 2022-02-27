using System;
using System.Collections;
using Amplitude.Unity.Framework;
using UnityEngine;

namespace Amplitude.Unity.Runtime
{
	[Diagnostics.TagAttribute("Runtime")]
	public class RuntimeManager : Manager, IService, IRuntimeService
	{
		public RuntimeManager()
		{
			this.RuntimeClass = typeof(Runtime);
		}

		public event EventHandler<RuntimeChangeEventArgs> RuntimeChange;

		public event EventHandler<RuntimeExceptionEventArgs> RuntimeException;

		public event EventHandler<RuntimeModuleDatabaseUpdateEventArgs> RuntimeModuleDatabaseUpdate;

		public Runtime Runtime { get; private set; }

		public string VanillaModuleName
		{
			get
			{
				return this.DefaultModuleName;
			}
		}

		protected Type RuntimeClass { get; set; }

		public override IEnumerator BindServices()
		{
			yield return base.BindServices();
			yield return this.LoadDatabases();
			Services.AddService<IRuntimeService>(this);
			yield break;
		}

		public void LoadRuntime(params RuntimeModuleConfiguration[] configuration)
		{
			this.ReloadRuntime(configuration);
		}

		public void UnloadRuntime(bool instant = false)
		{
			if (this.unloading == null)
			{
				this.unloading = UnityCoroutine.StartCoroutine(this, this.UnloadRuntimeAsync(), new EventHandler<CoroutineExceptionEventArgs>(this.UnloadRuntime_CoroutineExceptionCallback));
				if (this.unloading.IsFinished)
				{
					this.unloading = null;
				}
				else if (instant)
				{
					this.unloading.RunUntilIsFinished();
					this.unloading = null;
				}
			}
		}

		public void ReloadRuntime(params RuntimeModuleConfiguration[] configuration)
		{
			if (this.reloading == null)
			{
				this.reloading = UnityCoroutine.StartCoroutine(this, this.ReloadRuntimeAsync(configuration), new EventHandler<CoroutineExceptionEventArgs>(this.ReloadRuntime_CoroutineExceptionCallback));
				if (this.reloading.IsFinished)
				{
					this.reloading = null;
				}
			}
		}

		protected virtual IEnumerator LoadDatabases()
		{
			base.SetLastError(0, "Loading the default module databases...");
			if (this.DefaultModuleFiles != null)
			{
				for (int index = 0; index < this.DefaultModuleFiles.Length; index++)
				{
					TextAsset textAsset = this.DefaultModuleFiles[index];
					if (textAsset != null && !Databases.LoadDatabase<RuntimeModule>(textAsset, null, null))
					{
						base.SetLastError(1, string.Format("Error while loading the module asset file '{0}'.", textAsset.name));
					}
					yield return null;
				}
			}
			yield break;
		}

		protected virtual void OnRuntimeException(RuntimeExceptionEventArgs e)
		{
			if (this.RuntimeException != null)
			{
				this.RuntimeException(this, e);
			}
		}

		protected virtual void OnRuntimeModuleDatabaseUpdate(RuntimeModuleDatabaseUpdateEventArgs e)
		{
			if (this.RuntimeModuleDatabaseUpdate != null)
			{
				this.RuntimeModuleDatabaseUpdate(this, e);
			}
		}

		protected virtual void Update()
		{
			if (this.Runtime != null && this.Runtime.HasBeenLoaded)
			{
				this.Runtime.FiniteStateMachine.Update();
			}
		}

		private void OnRuntimeChange(RuntimeChangeEventArgs e)
		{
			if (this.RuntimeChange != null)
			{
				this.RuntimeChange(this, e);
			}
			base.SetLastError(0, "The runtime status has changed to '{0}'.", new object[]
			{
				e.Action.ToString()
			});
		}

		private IEnumerator ReloadRuntimeAsync(params RuntimeModuleConfiguration[] configuration)
		{
			yield return this.UnloadRuntimeAsync();
			if ((configuration == null || configuration.Length == 0) && !string.IsNullOrEmpty(this.DefaultModuleName))
			{
				Diagnostics.Log("Runtime configuration is null or empty; using the default module '{0}' as new configuration.", new object[]
				{
					this.DefaultModuleName
				});
				configuration = new RuntimeModuleConfiguration[]
				{
					new RuntimeModuleConfiguration(this.DefaultModuleName)
				};
			}
			Diagnostics.Assert(this.RuntimeClass != null);
			Runtime runtime = (Runtime)Activator.CreateInstance(this.RuntimeClass);
			this.OnRuntimeChange(new RuntimeChangeEventArgs(RuntimeChangeAction.Loading, runtime));
			yield return runtime.Load(configuration);
			if (runtime.HasBeenLoaded)
			{
				this.Runtime = runtime;
				this.OnRuntimeChange(new RuntimeChangeEventArgs(RuntimeChangeAction.Loaded, this.Runtime));
				if (this.Runtime.FiniteStateMachine.InitialStateType != null)
				{
					this.Runtime.FiniteStateMachine.PostStateChange(this.Runtime.FiniteStateMachine.InitialStateType, new object[0]);
				}
			}
			this.reloading = null;
			yield break;
		}

		private void UnloadRuntime_CoroutineExceptionCallback(object sender, CoroutineExceptionEventArgs e)
		{
			base.SetLastError(-1, e.Exception.Message);
			this.OnRuntimeException(new RuntimeExceptionEventArgs(e.Exception));
		}

		private void ReloadRuntime_CoroutineExceptionCallback(object sender, CoroutineExceptionEventArgs e)
		{
			base.SetLastError(-1, e.Exception.Message);
			this.OnRuntimeException(new RuntimeExceptionEventArgs(e.Exception));
		}

		private IEnumerator UnloadRuntimeAsync()
		{
			if (this.Runtime != null)
			{
				this.OnRuntimeChange(new RuntimeChangeEventArgs(RuntimeChangeAction.Unloading, this.Runtime));
				yield return this.Runtime.Unload();
				this.Runtime = null;
				this.OnRuntimeChange(new RuntimeChangeEventArgs(RuntimeChangeAction.Unloaded, null));
			}
			this.unloading = null;
			yield break;
		}

		public string DefaultModuleName;

		public TextAsset[] DefaultModuleFiles;

		private Coroutine reloading;

		private Coroutine unloading;
	}
}
