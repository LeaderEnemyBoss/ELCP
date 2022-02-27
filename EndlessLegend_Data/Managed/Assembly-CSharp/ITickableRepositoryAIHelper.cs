using System;
using Amplitude.Unity.Framework;

public interface ITickableRepositoryAIHelper : IService
{
	void Register(ITickable tickable);

	void Unregister(ITickable tickable);

	void RegisterUpdate(IUpdatable updatable);

	void UnregisterUpdate(IUpdatable updatable);
}
