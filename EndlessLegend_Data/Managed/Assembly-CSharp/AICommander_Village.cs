using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommander_Village : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_Village(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.Pacification, globalObjectiveID, regionIndex)
	{
	}

	public AICommander_Village() : base(AICommanderMissionDefinition.AICommanderCategory.Pacification, 0UL, 0)
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.RegionTarget = null;
		if (attribute != -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			World world = (service.Game as global::Game).World;
			this.RegionTarget = world.Regions[attribute];
			Diagnostics.Assert(this.RegionTarget != null);
		}
		base.SubObjectiveGuid = reader.GetAttribute<ulong>("VillageGUID");
		this.villageBribeActionMessageId = reader.GetAttribute<ulong>("VillageBribeActionMessageId");
		this.villageConvertActionMessageId = reader.GetAttribute<ulong>("VillageConvertActionMessageId");
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionTarget != null) ? this.RegionTarget.Index : -1);
		writer.WriteAttributeString<ulong>("VillageBribeActionMessageId", this.villageBribeActionMessageId);
		writer.WriteAttributeString<ulong>("VillageConvertActionMessageId", this.villageConvertActionMessageId);
		base.WriteXml(writer);
	}

	public Region RegionTarget
	{
		get
		{
			return this.region;
		}
		set
		{
			this.region = value;
			if (value != null)
			{
				base.RegionIndex = value.Index;
			}
		}
	}

	public override bool IsMissionFinished(bool forceStep)
	{
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(base.SubObjectiveGuid, out gameEntity) || !(gameEntity is Village))
		{
			return true;
		}
		Village village = gameEntity as Village;
		if (base.Empire.SimulationObject.Tags.Contains(AILayer_Village.TagConversionTrait))
		{
			if (village.HasBeenConverted && village.Converter == base.Empire)
			{
				return true;
			}
		}
		else if (village.HasBeenPacified || village.HasBeenConverted)
		{
			return true;
		}
		if (this.villageConvertActionMessageId != 0UL)
		{
			EvaluableMessage_VillageAction evaluableMessage_VillageAction = base.AIPlayer.Blackboard.GetMessage(this.villageConvertActionMessageId) as EvaluableMessage_VillageAction;
			if (evaluableMessage_VillageAction != null && (evaluableMessage_VillageAction.State == BlackboardMessage.StateValue.Message_Canceled || evaluableMessage_VillageAction.State == BlackboardMessage.StateValue.Message_Failed))
			{
				return true;
			}
		}
		GlobalObjectiveMessage globalObjectiveMessage;
		return base.GlobalObjectiveID == 0UL || base.AIPlayer == null || !base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage) || globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Canceled || globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Failed;
	}

	public override void Load()
	{
		base.Load();
		IGameService service = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.departmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
	}

	public override void PopulateMission()
	{
		Tags tags = new Tags();
		tags.AddTag(base.Category.ToString());
		tags.AddTag("Village");
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService.TryGetValue(base.SubObjectiveGuid, out gameEntity) && gameEntity is Village)
		{
			Village village = gameEntity as Village;
			bool flag = base.Empire.SimulationObject.Tags.Contains(AILayer_Village.TagConversionTrait);
			bool flag2 = this.departmentOfScience.CanBribe() && !village.HasBeenConverted && !village.HasBeenPacified;
			if (flag)
			{
				this.SendConvertVillageAction(village);
			}
			if (flag2)
			{
				this.SendBribeVillageAction(village);
			}
			bool flag3 = false;
			bool flag4 = false;
			if (this.villageBribeActionMessageId > 0UL)
			{
				EvaluableMessage_VillageAction evaluableMessage_VillageAction = base.AIPlayer.Blackboard.GetMessage(this.villageBribeActionMessageId) as EvaluableMessage_VillageAction;
				if (evaluableMessage_VillageAction != null && evaluableMessage_VillageAction.ChosenBuyEvaluation != null)
				{
					flag4 = true;
				}
			}
			if (village.HasBeenPacified && flag2 && !flag4)
			{
				flag4 = true;
			}
			if (this.villageConvertActionMessageId > 0UL)
			{
				EvaluableMessage_VillageAction evaluableMessage_VillageAction2 = base.AIPlayer.Blackboard.GetMessage(this.villageConvertActionMessageId) as EvaluableMessage_VillageAction;
				if (evaluableMessage_VillageAction2 != null && evaluableMessage_VillageAction2.ChosenBuyEvaluation != null)
				{
					flag3 = true;
				}
			}
			if (flag && !flag3)
			{
				float num;
				base.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, false);
				if (AILayer_Village.GetVillageConversionCost(base.Empire as MajorEmpire, village) * 2f < num)
				{
					flag3 = true;
				}
			}
			if (flag)
			{
				tags.AddTag("Convert");
				if (village.HasBeenConverted)
				{
					tags.AddTag("Hardway");
				}
				else if (village.HasBeenPacified)
				{
					if (!flag3)
					{
						return;
					}
				}
				else if (flag2)
				{
					if (!flag3 || !flag4)
					{
						return;
					}
					tags.AddTag("Bribe");
				}
				else
				{
					tags.AddTag("Hardway");
				}
			}
			for (int i = base.Missions.Count - 1; i >= 0; i--)
			{
				if (base.Missions[i].MissionDefinition.Category.Contains(tags))
				{
					return;
				}
				this.CancelMission(base.Missions[i]);
			}
			base.PopulationFirstMissionFromCategory(tags, new object[]
			{
				this.RegionTarget,
				base.SubObjectiveGuid
			});
		}
	}

	public override void RefreshMission()
	{
		base.RefreshMission();
		this.PopulateMission();
		base.PromoteMission();
	}

	public override void Release()
	{
		if (this.villageBribeActionMessageId != 0UL)
		{
			EvaluableMessage_VillageAction evaluableMessage_VillageAction = base.AIPlayer.Blackboard.GetMessage(this.villageBribeActionMessageId) as EvaluableMessage_VillageAction;
			if (evaluableMessage_VillageAction != null)
			{
				if (evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate || evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining || evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending)
				{
					evaluableMessage_VillageAction.Cancel();
				}
				base.AIPlayer.Blackboard.CancelMessage(this.villageBribeActionMessageId);
			}
		}
		if (this.villageConvertActionMessageId != 0UL)
		{
			EvaluableMessage_VillageAction evaluableMessage_VillageAction2 = base.AIPlayer.Blackboard.GetMessage(this.villageConvertActionMessageId) as EvaluableMessage_VillageAction;
			if (evaluableMessage_VillageAction2 != null)
			{
				if (evaluableMessage_VillageAction2.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate || evaluableMessage_VillageAction2.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining || evaluableMessage_VillageAction2.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending)
				{
					evaluableMessage_VillageAction2.Cancel();
				}
				base.AIPlayer.Blackboard.CancelMessage(this.villageConvertActionMessageId);
			}
		}
		base.Release();
		this.departmentOfScience = null;
		this.gameEntityRepositoryService = null;
	}

	private void SendBribeVillageAction(Village village)
	{
		EvaluableMessage_VillageAction evaluableMessage_VillageAction = base.AIPlayer.Blackboard.FindFirst<EvaluableMessage_VillageAction>(BlackboardLayerID.Empire, (EvaluableMessage_VillageAction match) => match.RegionIndex == base.RegionIndex && match.VillageGUID == base.SubObjectiveGuid && match.AccountTag == AILayer_AccountManager.MilitaryAccountName);
		if (evaluableMessage_VillageAction == null || evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Cancel)
		{
			evaluableMessage_VillageAction = new EvaluableMessage_VillageAction(base.RegionIndex, base.SubObjectiveGuid, AILayer_AccountManager.MilitaryAccountName);
			this.villageBribeActionMessageId = base.AIPlayer.Blackboard.AddMessage(evaluableMessage_VillageAction);
		}
		else
		{
			this.villageBribeActionMessageId = evaluableMessage_VillageAction.ID;
		}
		evaluableMessage_VillageAction.TimeOut = 1;
		evaluableMessage_VillageAction.SetInterest(base.GlobalPriority, base.LocalPriority);
		evaluableMessage_VillageAction.UpdateBuyEvaluation("BribeVillage", 0UL, AILayer_Village.GetVillageBribeCost(base.Empire as MajorEmpire, village), (int)BuyEvaluation.MaxTurnGain, 0f, 0UL);
	}

	private void SendConvertVillageAction(Village village)
	{
		EvaluableMessage_VillageAction evaluableMessage_VillageAction = base.AIPlayer.Blackboard.FindFirst<EvaluableMessage_VillageAction>(BlackboardLayerID.Empire, (EvaluableMessage_VillageAction match) => match.RegionIndex == base.RegionIndex && match.VillageGUID == base.SubObjectiveGuid && match.AccountTag == AILayer_AccountManager.ConversionAccountName);
		if (evaluableMessage_VillageAction == null || evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Cancel)
		{
			evaluableMessage_VillageAction = new EvaluableMessage_VillageAction(base.RegionIndex, base.SubObjectiveGuid, AILayer_AccountManager.ConversionAccountName);
			this.villageConvertActionMessageId = base.AIPlayer.Blackboard.AddMessage(evaluableMessage_VillageAction);
		}
		else
		{
			this.villageConvertActionMessageId = evaluableMessage_VillageAction.ID;
		}
		evaluableMessage_VillageAction.TimeOut = 1;
		evaluableMessage_VillageAction.SetInterest(base.GlobalPriority, base.LocalPriority);
		evaluableMessage_VillageAction.UpdateBuyEvaluation("ConvertVillage", 0UL, AILayer_Village.GetVillageConversionCost(base.Empire as MajorEmpire, village), (int)BuyEvaluation.MaxTurnGain, 0f, 0UL);
	}

	private DepartmentOfScience departmentOfScience;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private Region region;

	private ulong villageBribeActionMessageId;

	private ulong villageConvertActionMessageId;
}
