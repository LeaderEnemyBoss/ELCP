using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AILayer_KaijuManagement : AILayerCommanderController
{
	public AILayer_KaijuManagement() : base("KaijuSupport")
	{
	}

	private void CreateRelocationNeeds()
	{
		float num = 0f;
		foreach (Kaiju kaiju in this.MajorEmpire.TamedKaijus)
		{
			if (kaiju.OnArmyMode())
			{
				num += kaiju.KaijuArmy.GetPropertyValue(SimulationProperties.LandMilitaryPower);
			}
			else
			{
				num += kaiju.KaijuGarrison.GetPropertyValue(SimulationProperties.LandMilitaryPower);
			}
		}
		this.KaijusToRise.Clear();
		this.KaijuSupport = (this.DiplomacyLayer.MilitaryPowerDif - num <= (base.AIEntity.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) - num) * 0.3f);
		List<Region> list = new List<Region>();
		List<StaticString> list2 = new List<StaticString>();
		for (int i = 0; i < this.MajorEmpire.TamedKaijus.Count; i++)
		{
			Kaiju kaiju2 = this.MajorEmpire.TamedKaijus[i];
			list2.Clear();
			if (!kaiju2.OnArmyMode() && this.garrisonAction_MigrateKaiju.CanExecute(kaiju2.KaijuGarrison, ref list2, new object[0]) && this.garrisonAction_MigrateKaiju.ComputeRemainingCooldownDuration(kaiju2.KaijuGarrison) < 1f)
			{
				if (!this.KaijuSupport || !this.IsKaijuValidForSupport(kaiju2))
				{
					Region mostProfitableRegionForKaiju = this.GetMostProfitableRegionForKaiju(kaiju2, list);
					if (mostProfitableRegionForKaiju != null && kaiju2.Region != mostProfitableRegionForKaiju)
					{
						WorldPosition bestDefensiveKaijuPosition = this.GetBestDefensiveKaijuPosition(mostProfitableRegionForKaiju);
						if (bestDefensiveKaijuPosition.IsValid)
						{
							list.Add(mostProfitableRegionForKaiju);
							KaijuRelocationMessage message = new KaijuRelocationMessage(kaiju2.GUID, bestDefensiveKaijuPosition);
							base.AIEntity.AIPlayer.Blackboard.AddMessage(message);
						}
					}
				}
				else
				{
					this.KaijusToRise.Add(kaiju2.GUID);
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
		if (list.Count > 0 || this.KaijusToRise.Count > 0)
		{
			this.validatedRelocationMessages = list;
			AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SyncrhronousJob_Relocate));
		}
	}

	public Region GetMostProfitableRegionForKaiju(Kaiju kaiju, List<Region> excludedRegions = null)
	{
		List<Region> list = new List<Region>();
		for (int i = 0; i < this.worldPositionningService.World.Regions.Length; i++)
		{
			Region region2 = this.worldPositionningService.World.Regions[i];
			if ((excludedRegions == null || !excludedRegions.Contains(region2)) && (region2 == kaiju.Region || KaijuCouncil.IsRegionValidForSettleKaiju(region2)))
			{
				list.Add(region2);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return (from region in list
		orderby this.worldPositionningService.GetNeighbourRegionsWithCityOfEmpire(region, kaiju.MajorEmpire, false).Length descending, this.worldPositionningService.GetNeighbourRegionsWithCity(region, false).Length descending, GuiSimulation.Instance.FilterRegionResources(region).Count descending
		select region).First<Region>();
	}

	private bool IsRelocationMessageValid(KaijuRelocationMessage message)
	{
		Kaiju kaiju = null;
		if (!this.gameEntityRepositoryService.TryGetValue<Kaiju>(message.KaijuGUID, out kaiju) || !this.MajorEmpire.TamedKaijus.Contains(kaiju))
		{
			return false;
		}
		if (kaiju.OnArmyMode())
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
				bool flag = false;
				KaijuRelocationMessage kaijuRelocationMessage = this.validatedRelocationMessages[i];
				Kaiju kaiju = null;
				List<StaticString> list = new List<StaticString>();
				if (!this.gameEntityRepositoryService.TryGetValue<Kaiju>(kaijuRelocationMessage.KaijuGUID, out kaiju) || !this.MajorEmpire.TamedKaijus.Contains(kaiju) || !this.garrisonAction_MigrateKaiju.CanExecute(kaiju.KaijuGarrison, ref list, new object[0]))
				{
				}
				if (kaiju != null && kaiju.KaijuGarrison != null && kaiju.KaijuGarrison.IsInEncounter)
				{
					flag = true;
				}
				if (!flag)
				{
					this.validatedRelocationMessages.RemoveAt(i);
					base.AIEntity.AIPlayer.Blackboard.CancelMessage(kaijuRelocationMessage);
				}
			}
			if (this.validatedRelocationMessages.Count == 0 && this.KaijusToRise.Count == 0)
			{
				return SynchronousJobState.Success;
			}
			return SynchronousJobState.Running;
		}
		else
		{
			if (this.KaijusToRise.Count <= 0)
			{
				return SynchronousJobState.Success;
			}
			int j = this.KaijusToRise.Count - 1;
			while (j >= 0)
			{
				bool flag2 = false;
				bool flag3 = false;
				Kaiju kaiju2 = null;
				List<StaticString> list2 = new List<StaticString>();
				if (!this.gameEntityRepositoryService.TryGetValue<Kaiju>(this.KaijusToRise[j], out kaiju2) || !this.MajorEmpire.TamedKaijus.Contains(kaiju2) || !this.garrisonAction_RiseKaiju.CanExecute(kaiju2.KaijuGarrison, ref list2, new object[0]))
				{
					flag2 = true;
				}
				if (kaiju2 != null && kaiju2.KaijuGarrison != null && kaiju2.KaijuGarrison.IsInEncounter)
				{
					flag3 = true;
				}
				if (!flag2)
				{
					List<StaticString> list3 = new List<StaticString>();
					if (this.garrisonAction_MigrateKaiju.CanExecute(kaiju2.KaijuGarrison, ref list3, new object[0]))
					{
						Army army = this.ChooseArmyToSupport();
						if (army != null && this.worldPositionningService.GetDistance(kaiju2.WorldPosition, army.WorldPosition) > 6)
						{
							Region bestSupportRegionForKaiju = this.GetBestSupportRegionForKaiju(kaiju2, army, null);
							if (bestSupportRegionForKaiju != null && kaiju2.Region != bestSupportRegionForKaiju)
							{
								WorldPosition validKaijuSupportPosition = this.GetValidKaijuSupportPosition(bestSupportRegionForKaiju, army);
								OrderRelocateKaiju order = new OrderRelocateKaiju(kaiju2.GUID, validKaijuSupportPosition, this.garrisonAction_MigrateKaiju.Name);
								base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
								return SynchronousJobState.Running;
							}
						}
					}
					OrderKaijuChangeMode order2 = new OrderKaijuChangeMode(kaiju2, false, true, true);
					Ticket ticket = null;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2, out ticket, new EventHandler<TicketRaisedEventArgs>(this.Order_KaijuRisen));
				}
				if (!flag3)
				{
					this.KaijusToRise.RemoveAt(j);
					if (this.KaijusToRise.Count == 0)
					{
						return SynchronousJobState.Success;
					}
					return SynchronousJobState.Running;
				}
				else
				{
					j--;
				}
			}
			if (this.KaijusToRise.Count == 0)
			{
				return SynchronousJobState.Success;
			}
			return SynchronousJobState.Running;
		}
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
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_KaijuManagement_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_KaijuManagement_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[]
		{
			"AILayer_Diplomacy_CreateLocalNeedsPass"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_KaijuManagement_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_KaijuManagement_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		this.validatedRelocationMessages = new List<KaijuRelocationMessage>();
		this.DiplomacyLayer = base.AIEntity.GetLayer<AILayer_Diplomacy>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.departmentOfDefense = this.MajorEmpire.GetAgency<DepartmentOfDefense>();
		this.departmentOfScience = this.MajorEmpire.GetAgency<DepartmentOfScience>();
		IDatabase<GarrisonAction> database = Databases.GetDatabase<GarrisonAction>(false);
		GarrisonAction garrisonAction = null;
		if (database == null || !database.TryGetValue("GarrisonActionMigrateKaiju", out garrisonAction))
		{
			Diagnostics.LogError("AILayer_KaijuManagement didnt find GarrisonActionMigrateKaiju");
		}
		else
		{
			this.garrisonAction_MigrateKaiju = (garrisonAction as GarrisonAction_MigrateKaiju);
		}
		GarrisonAction garrisonAction2 = null;
		if (database == null || !database.TryGetValue(GarrisonAction_RiseKaiju.ReadOnlyName, out garrisonAction2))
		{
			Diagnostics.LogError("AILayer_KaijuManagement didnt find " + GarrisonAction_RiseKaiju.ReadOnlyName.ToString());
		}
		else
		{
			this.garrisonAction_RiseKaiju = (garrisonAction2 as GarrisonAction_RiseKaiju);
		}
		this.MajorEmpire.TamedKaijusCollectionChanged += this.AILayer_KaijuManagement_TamedKaijusCollectionChanged;
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

	public override void Release()
	{
		base.Release();
		this.gameEntityRepositoryService = null;
		this.worldPositionningService = null;
		this.gameService = null;
		this.validatedRelocationMessages = null;
		this.DiplomacyLayer = null;
		this.aiDataRepositoryHelper = null;
		this.departmentOfDefense = null;
		this.departmentOfScience = null;
		this.garrisonAction_MigrateKaiju = null;
		this.garrisonAction_RiseKaiju = null;
		if (this.MajorEmpire != null)
		{
			this.MajorEmpire.TamedKaijusCollectionChanged -= this.AILayer_KaijuManagement_TamedKaijusCollectionChanged;
		}
	}

	private WorldPosition GetValidKaijuSupportPosition(Region targetRegion, Army ArmyToSupport)
	{
		List<WorldPosition> list = (from position in targetRegion.WorldPositions
		where KaijuCouncil.IsPositionValidForSettleKaiju(position, null)
		select position).ToList<WorldPosition>();
		if (list.Count == 0)
		{
			return WorldPosition.Invalid;
		}
		list.Sort((WorldPosition left, WorldPosition right) => this.worldPositionningService.GetDistance(left, ArmyToSupport.WorldPosition).CompareTo(this.worldPositionningService.GetDistance(right, ArmyToSupport.WorldPosition)));
		return list[0];
	}

	public Region GetBestSupportRegionForKaiju(Kaiju kaiju, Army ArmyToSupport, List<Region> excludedRegions = null)
	{
		List<Region> list = new List<Region>();
		int distance = this.worldPositionningService.GetDistance(kaiju.WorldPosition, ArmyToSupport.WorldPosition);
		for (int i = 0; i < this.worldPositionningService.World.Regions.Length; i++)
		{
			Region region = this.worldPositionningService.World.Regions[i];
			if ((excludedRegions == null || !excludedRegions.Contains(region)) && (region == kaiju.Region || (KaijuCouncil.IsRegionValidForSettleKaiju(region) && this.worldPositionningService.GetDistance(region.Barycenter, ArmyToSupport.WorldPosition) < distance)))
			{
				list.Add(region);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		list.Sort((Region left, Region right) => this.worldPositionningService.GetDistance(left.Barycenter, ArmyToSupport.WorldPosition).CompareTo(this.worldPositionningService.GetDistance(right.Barycenter, ArmyToSupport.WorldPosition)));
		return list[0];
	}

	public bool IsKaijuValidForSupport(Kaiju kaiju)
	{
		if (!this.MajorEmpire.TamedKaijus.Contains(kaiju))
		{
			return false;
		}
		foreach (Unit unit in kaiju.GetActiveTroops().Units)
		{
			if (unit.UnitDesign.Tags.Contains(Kaiju.MonsterUnitTag))
			{
				if (unit.GetPropertyValue(SimulationProperties.Health) > 200f * (float)(this.departmentOfScience.CurrentTechnologyEraNumber - 1) + (float)((this.departmentOfScience.CurrentTechnologyEraNumber + 1) * (this.departmentOfScience.CurrentTechnologyEraNumber + 1) * 10))
				{
					return true;
				}
				return false;
			}
		}
		return false;
	}

	private Army ChooseArmyToSupport()
	{
		float num = -1f;
		Army result = null;
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(this.departmentOfDefense.Armies[i].GUID);
			if (aidata != null && num < aidata.SupportScore)
			{
				num = aidata.SupportScore;
				result = this.departmentOfDefense.Armies[i];
			}
		}
		return result;
	}

	private void Order_KaijuRisen(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result == PostOrderResponse.Processed)
		{
			OrderKaijuChangeMode orderKaijuChangeMode = e.Order as OrderKaijuChangeMode;
			Kaiju kaiju = null;
			if (!this.gameEntityRepositoryService.TryGetValue<Kaiju>(orderKaijuChangeMode.KaijuGUID, out kaiju) || !this.MajorEmpire.TamedKaijus.Contains(kaiju))
			{
				return;
			}
			AICommander aicommander = this.aiCommanders.Find((AICommander match) => match.ForceArmyGUID == kaiju.KaijuArmy.GUID);
			if (aicommander == null)
			{
				this.AddCommander(new AICommander_KaijuSupport
				{
					ForceArmyGUID = kaiju.KaijuArmy.GUID,
					Empire = base.AIEntity.Empire,
					AIPlayer = base.AIEntity.AIPlayer
				});
				return;
			}
			aicommander.Initialize();
			aicommander.Load();
			aicommander.CreateMission();
		}
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		for (int i = 0; i < this.MajorEmpire.TamedKaijus.Count; i++)
		{
			Kaiju kaiju = this.MajorEmpire.TamedKaijus[i];
			if (kaiju.OnArmyMode())
			{
				AICommander aicommander = this.aiCommanders.Find((AICommander match) => match.ForceArmyGUID == kaiju.KaijuArmy.GUID);
				if (aicommander == null)
				{
					this.AddCommander(new AICommander_KaijuSupport
					{
						ForceArmyGUID = kaiju.KaijuArmy.GUID,
						Empire = base.AIEntity.Empire,
						AIPlayer = base.AIEntity.AIPlayer
					});
				}
				else
				{
					aicommander.Initialize();
					aicommander.Load();
					aicommander.CreateMission();
				}
			}
		}
	}

	private void AILayer_KaijuManagement_TamedKaijusCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		if (e.Action == CollectionChangeAction.Add && this.IsActive())
		{
			Kaiju kaiju = e.Element as Kaiju;
			if (kaiju != null && this.MajorEmpire.TamedKaijus.Contains(kaiju) && kaiju.OnArmyMode())
			{
				AICommander aicommander = this.aiCommanders.Find((AICommander match) => match.ForceArmyGUID == kaiju.KaijuArmy.GUID);
				if (aicommander == null)
				{
					this.AddCommander(new AICommander_KaijuSupport
					{
						ForceArmyGUID = kaiju.KaijuArmy.GUID,
						Empire = base.AIEntity.Empire,
						AIPlayer = base.AIEntity.AIPlayer
					});
					return;
				}
				aicommander.Initialize();
				aicommander.Load();
				aicommander.CreateMission();
			}
		}
	}

	private WorldPosition GetBestDefensiveKaijuPosition(Region targetRegion)
	{
		Region[] neighbourRegionsWithCityOfEmpire = this.worldPositionningService.GetNeighbourRegionsWithCityOfEmpire(targetRegion, this.MajorEmpire, false);
		if (neighbourRegionsWithCityOfEmpire.Length == 0)
		{
			return KaijuCouncil.GetValidKaijuPosition(targetRegion, false);
		}
		List<WorldPosition> list = (from position in targetRegion.WorldPositions
		where KaijuCouncil.IsPositionValidForSettleKaiju(position, null)
		select position).ToList<WorldPosition>();
		if (list.Count == 0)
		{
			return WorldPosition.Invalid;
		}
		int num = -1;
		int index = -1;
		for (int i = 0; i < list.Count; i++)
		{
			int num2 = 0;
			foreach (Region region in neighbourRegionsWithCityOfEmpire)
			{
				int distance = this.worldPositionningService.GetDistance(list[i], region.City.WorldPosition);
				num2 += ((distance > 5) ? Mathf.Max(15 - distance, 0) : ((15 - distance) * 3));
			}
			if (num2 > num)
			{
				num = num2;
				index = i;
			}
		}
		return list[index];
	}

	private List<KaijuRelocationMessage> validatedRelocationMessages;

	private float globalPriority = 0.5f;

	private float localPriority = 0.5f;

	private IGameService gameService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IWorldPositionningService worldPositionningService;

	private AILayer_Diplomacy DiplomacyLayer;

	private bool KaijuSupport;

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfScience departmentOfScience;

	private GarrisonAction_MigrateKaiju garrisonAction_MigrateKaiju;

	private List<GameEntityGUID> KaijusToRise = new List<GameEntityGUID>();

	private GarrisonAction_RiseKaiju garrisonAction_RiseKaiju;
}
