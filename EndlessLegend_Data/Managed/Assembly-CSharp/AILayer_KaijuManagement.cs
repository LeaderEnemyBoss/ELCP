using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AILayer_KaijuManagement : AILayer
{
	private void CreateRelocationNeeds()
	{
		List<Region> list = new List<Region>();
		for (int i = 0; i < this.MajorEmpire.TamedKaijus.Count; i++)
		{
			Kaiju kaiju = this.MajorEmpire.TamedKaijus[i];
			Region mostProfitableRegionForKaiju = this.GetMostProfitableRegionForKaiju(kaiju, list);
			if (mostProfitableRegionForKaiju != null && kaiju.Region != mostProfitableRegionForKaiju)
			{
				WorldPosition validKaijuPosition = KaijuCouncil.GetValidKaijuPosition(mostProfitableRegionForKaiju, false);
				if (validKaijuPosition.IsValid)
				{
					list.Add(mostProfitableRegionForKaiju);
					KaijuRelocationMessage message = new KaijuRelocationMessage(kaiju.GUID, validKaijuPosition);
					base.AIEntity.AIPlayer.Blackboard.AddMessage(message);
				}
			}
		}
	}

	private void ExecuteRelocationNeeds()
	{
		List<KaijuRelocationMessage> list = new List<KaijuRelocationMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<KaijuRelocationMessage>(BlackboardLayerID.Empire, (KaijuRelocationMessage match) => match.State != BlackboardMessage.StateValue.Message_Canceled));
		for (int i = list.Count - 1; i >= 0; i--)
		{
			KaijuRelocationMessage message = list[i];
			if (!this.IsRelocationMessageValid(message))
			{
				base.AIEntity.AIPlayer.Blackboard.CancelMessage(message);
				list.RemoveAt(i);
			}
		}
		if (list.Count > 0)
		{
			this.validatedRelocationMessages = list;
			ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
			service.RegisterSynchronousJob(new SynchronousJob(this.SyncrhronousJob_Relocate));
		}
	}

	private Region GetMostProfitableRegionForKaiju(Kaiju kaiju, List<Region> excludedRegions = null)
	{
		List<Region> list = new List<Region>();
		for (int i = 0; i < this.worldPositionningService.World.Regions.Length; i++)
		{
			Region region2 = this.worldPositionningService.World.Regions[i];
			if (excludedRegions == null || !excludedRegions.Contains(region2))
			{
				if (region2 == kaiju.Region || KaijuCouncil.IsRegionValidForSettleKaiju(region2))
				{
					list.Add(region2);
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		IEnumerable<Region> source = from region in list
		orderby this.worldPositionningService.GetNeighbourRegionsWithCityOfEmpire(region, kaiju.MajorEmpire, false).Length descending, this.worldPositionningService.GetNeighbourRegionsWithCity(region, false).Length descending
		select region;
		return source.First<Region>();
	}

	private bool IsRelocationMessageValid(KaijuRelocationMessage message)
	{
		Kaiju item = null;
		if (!this.gameEntityRepositoryService.TryGetValue<Kaiju>(message.KaijuGUID, out item) || !this.MajorEmpire.TamedKaijus.Contains(item))
		{
			return false;
		}
		WorldPosition targetPosition = message.TargetPosition;
		if (!targetPosition.IsValid || !KaijuCouncil.IsPositionValidForSettleKaiju(targetPosition, null))
		{
			return false;
		}
		Region region = this.worldPositionningService.GetRegion(message.TargetPosition);
		if (region == null || !KaijuCouncil.IsRegionValidForSettleKaiju(region))
		{
			return false;
		}
		for (int i = 0; i < this.validatedRelocationMessages.Count; i++)
		{
			KaijuRelocationMessage kaijuRelocationMessage = this.validatedRelocationMessages[i];
			GameEntityGUID kaijuGUID = message.KaijuGUID;
			GameEntityGUID kaijuGUID2 = kaijuRelocationMessage.KaijuGUID;
			if (kaijuGUID == kaijuGUID2)
			{
				return false;
			}
			int regionIndex = (int)this.worldPositionningService.GetRegionIndex(message.TargetPosition);
			int regionIndex2 = (int)this.worldPositionningService.GetRegionIndex(kaijuRelocationMessage.TargetPosition);
			if (regionIndex == regionIndex2)
			{
				return false;
			}
		}
		return true;
	}

	private SynchronousJobState SyncrhronousJob_Relocate()
	{
		if (this.validatedRelocationMessages.Count > 0)
		{
			for (int i = this.validatedRelocationMessages.Count - 1; i >= 0; i--)
			{
				KaijuRelocationMessage kaijuRelocationMessage = this.validatedRelocationMessages[i];
				this.validatedRelocationMessages.RemoveAt(i);
				AILayer.Log("[AILayer_KaijuManagement] {0}: sending OrderRelocateKaiju | KaijuGUID: {1} | TargetPosition: {2}", new object[]
				{
					base.AIEntity.Empire.Name,
					kaijuRelocationMessage.KaijuGUID,
					kaijuRelocationMessage.TargetPosition
				});
				OrderRelocateKaiju order = new OrderRelocateKaiju(kaijuRelocationMessage.KaijuGUID, kaijuRelocationMessage.TargetPosition);
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
				base.AIEntity.AIPlayer.Blackboard.CancelMessage(kaijuRelocationMessage);
			}
			return SynchronousJobState.Success;
		}
		return SynchronousJobState.Failure;
	}

	private MajorEmpire MajorEmpire
	{
		get
		{
			if (base.AIEntity != null)
			{
				return base.AIEntity.Empire as MajorEmpire;
			}
			return null;
		}
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(this.gameService != null);
		this.gameEntityRepositoryService = this.gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.worldPositionningService = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_KaijuManagement_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_KaijuManagement_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_KaijuManagement_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		this.validatedRelocationMessages = new List<KaijuRelocationMessage>();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI && this.MajorEmpire != null && this.MajorEmpire.TamedKaijus.Count > 0;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		if (this.MajorEmpire == null)
		{
			return;
		}
		this.CreateRelocationNeeds();
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		this.ExecuteRelocationNeeds();
	}

	private List<KaijuRelocationMessage> validatedRelocationMessages;

	private float globalPriority = 0.5f;

	private float localPriority = 0.5f;

	private IGameService gameService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IWorldPositionningService worldPositionningService;
}
