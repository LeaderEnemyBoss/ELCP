using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Catspaw", new object[]
{

})]
public class AILayer_Dissent : AILayer
{
	public HeuristicValue GlobalPriority { get; set; }

	public List<DissentTask> DissentTasks
	{
		get
		{
			return this.dissentTasks;
		}
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.warLayer = base.AIEntity.GetLayer<AILayer_War>();
		this.tickableRepository = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.game = (gameService.Game as global::Game);
		this.GlobalPriority = new HeuristicValue(0f);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Dissent_CreateLocalNeeds", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI && this.CanUseDissent();
	}

	public override void Release()
	{
		base.Release();
		for (int i = 0; i < this.dissentTasks.Count; i++)
		{
			this.tickableRepository.Unregister(this.dissentTasks[i]);
		}
		this.dissentTasks.Clear();
		this.dissentTasks.Clear();
		this.whiteList.Clear();
		this.GlobalPriority = null;
		this.game = null;
		this.tickableRepository = null;
		this.departmentOfForeignAffairs = null;
		this.warLayer = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		if (this.CanUseDissent())
		{
			this.UpdateGlobalPriority();
			this.CreateDissentTask_EmpireBased();
			for (int i = 0; i < this.dissentTasks.Count; i++)
			{
				this.dissentTasks[i].NewTurn(this);
			}
		}
		else
		{
			for (int j = 0; j < this.dissentTasks.Count; j++)
			{
				if (this.dissentTasks[j].AssociateRequest != null)
				{
					this.dissentTasks[j].AssociateRequest.Cancel();
				}
				this.tickableRepository.Unregister(this.dissentTasks[j]);
			}
			this.dissentTasks.Clear();
		}
	}

	private bool CanUseDissent()
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		return service != null && service.IsShared(DownloadableContent16.ReadOnlyName) && base.AIEntity.Empire.SimulationObject.Tags.Contains(DownloadableContent16.FactionTraitDissent);
	}

	private void UpdateGlobalPriority()
	{
		this.GlobalPriority.Reset();
		this.GlobalPriority.Add(0.5f, "constant", new object[0]);
	}

	private void CreateDissentTask_EmpireBased()
	{
		this.whiteList.Clear();
		int index;
		for (index = 0; index < this.game.Empires.Length; index++)
		{
			if (base.AIEntity.Empire.Index != index)
			{
				if (this.game.Empires[index] is MajorEmpire)
				{
					if (!this.departmentOfForeignAffairs.IsFriend(this.game.Empires[index]))
					{
						if (this.warLayer.GetWarStatusWithEmpire(index) != AILayer_War.WarStatusType.None)
						{
							this.whiteList.Add(this.game.Empires[index]);
							if (this.FindTask<DissentTask_Empire>((DissentTask_Empire match) => match.OtherEmpire == this.game.Empires[index]) == null)
							{
								DissentTask_Empire dissentTask_Empire = new DissentTask_Empire(base.AIEntity.Empire, this.game.Empires[index]);
								this.dissentTasks.Add(dissentTask_Empire);
								this.tickableRepository.Register(dissentTask_Empire);
							}
						}
					}
				}
			}
		}
		for (int i = this.dissentTasks.Count - 1; i >= 0; i--)
		{
			DissentTask_Empire dissentTask_Empire2 = this.dissentTasks[i] as DissentTask_Empire;
			if (dissentTask_Empire2 != null)
			{
				if (!this.whiteList.Contains(dissentTask_Empire2.OtherEmpire))
				{
					this.tickableRepository.Unregister(dissentTask_Empire2);
					this.dissentTasks.RemoveAt(i);
				}
			}
		}
	}

	private T FindTask<T>(Func<T, bool> match) where T : DissentTask
	{
		for (int i = 0; i < this.dissentTasks.Count; i++)
		{
			T t = this.dissentTasks[i] as T;
			if (t != null)
			{
				if (match == null || match(t))
				{
					return t;
				}
			}
		}
		return (T)((object)null);
	}

	private List<DissentTask> dissentTasks = new List<DissentTask>();

	private global::Game game;

	private List<global::Empire> whiteList = new List<global::Empire>();

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private AILayer_War warLayer;

	private ITickableRepositoryAIHelper tickableRepository;
}
