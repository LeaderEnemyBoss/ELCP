using System;
using Amplitude.Unity.Framework;

public interface ITickableRepositoryAIHelper : IService
{
	void Register(ITickable tickable);

	void Unregister(ITickable tickable);
}
