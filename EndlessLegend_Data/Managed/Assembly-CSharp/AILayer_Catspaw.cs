using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Catspaw", new object[]
{

})]
public class AILayer_Catspaw : AILayer
{
	public AILayer_Catspaw()
	{
		this.objectiveMessages = new List<GlobalObjectiveMessage>();
		base..ctor();
		this.GlobalPriority = new HeuristicValue(0f);
	}

	private void GenerateDenyCitySiege()
	{
		AILayer_Catspaw.<GenerateDenyCitySiege>c__AnonStorey7E9 <GenerateDenyCitySiege>c__AnonStorey7E = new AILayer_Catspaw.<GenerateDenyCitySiege>c__AnonStorey7E9();
		<GenerateDenyCitySiege>c__AnonStorey7E.interior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		int index;
		for (index = 0; index < <GenerateDenyCitySiege>c__AnonStorey7E.interior.Cities.Count; index++)
		{
			if (<GenerateDenyCitySiege>c__AnonStorey7E.interior.Cities[index].BesiegingEmpire != null)
			{
				if (<GenerateDenyCitySiege>c__AnonStorey7E.interior.Cities[index].BesiegingEmpire is MinorEmpire)
				{
					if (this.FindTask<CatspawTask_DenyCitySiege>((CatspawTask_DenyCitySiege match) => match.TargetGuid == <GenerateDenyCitySiege>c__AnonStorey7E.interior.Cities[index].GUID) == null)
					{
						CatspawTask_DenyCitySiege catspawTask_DenyCitySiege = new CatspawTask_DenyCitySiege();
						catspawTask_DenyCitySiege.Owner = base.AIEntity.Empire;
						catspawTask_DenyCitySiege.MyCity = <GenerateDenyCitySiege>c__AnonStorey7E.interior.Cities[index];
						this.catspawTasks.Add(catspawTask_DenyCitySiege);
					}
				}
			}
		}
	}

	private void GenerateDenySettlerAttack()
	{
		AILayer_Catspaw.<GenerateDenySettlerAttack>c__AnonStorey7EB <GenerateDenySettlerAttack>c__AnonStorey7EB = new AILayer_Catspaw.<GenerateDenySettlerAttack>c__AnonStorey7EB();
		<GenerateDenySettlerAttack>c__AnonStorey7EB.defense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		int index;
		for (index = 0; index < <GenerateDenySettlerAttack>c__AnonStorey7EB.defense.Armies.Count; index++)
		{
			if (<GenerateDenySettlerAttack>c__AnonStorey7EB.defense.Armies[index].IsSettler && this.FindTask<CatspawTask_DenySettlerAttack>((CatspawTask_DenySettlerAttack match) => match.TargetGuid == <GenerateDenySettlerAttack>c__AnonStorey7EB.defense.Armies[index].GUID) == null)
			{
				CatspawTask_DenySettlerAttack catspawTask_DenySettlerAttack = new CatspawTask_DenySettlerAttack();
				catspawTask_DenySettlerAttack.Owner = base.AIEntity.Empire;
				catspawTask_DenySettlerAttack.SettlerArmy = <GenerateDenySettlerAttack>c__AnonStorey7EB.defense.Armies[index];
				this.catspawTasks.Add(catspawTask_DenySettlerAttack);
			}
		}
	}

	private void GenerateHelpVillage()
	{
		bool flag = base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitCultists14");
		if (flag)
		{
			return;
		}
		this.objectiveMessages.Clear();
		base.AIEntity.AIPlayer.Blackboard.FillMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == "Village" && match.SubObjectifGUID != GameEntityGUID.Zero && match.State != BlackboardMessage.StateValue.Message_Canceled, ref this.objectiveMessages);
		AILayer_ArmyManagement layer = base.AIEntity.GetLayer<AILayer_ArmyManagement>();
		int index;
		for (index = 0; index < this.objectiveMessages.Count; index++)
		{
			CatspawTask_HelpVillage catspawTask_HelpVillage = this.FindTask<CatspawTask_HelpVillage>((CatspawTask_HelpVillage match) => match.TargetGuid == this.objectiveMessages[index].SubObjectifGUID);
			AICommander aicommander = layer.FindCommander(this.objectiveMessages[index]);
			if (aicommander == null || aicommander.HasMissionRunning())
			{
				if (catspawTask_HelpVillage != null)
				{
					if (catspawTask_HelpVillage.AssignedArmy != null)
					{
						catspawTask_HelpVillage.AssignedArmy.Unassign();
					}
					this.catspawTasks.Remove(catspawTask_HelpVillage);
				}
			}
			else if (catspawTask_HelpVillage == null)
			{
				catspawTask_HelpVillage = new CatspawTask_HelpVillage();
				catspawTask_HelpVillage.Owner = base.AIEntity.Empire;
				catspawTask_HelpVillage.TargetGuid = this.objectiveMessages[index].SubObjectifGUID;
				this.catspawTasks.Add(catspawTask_HelpVillage);
			}
		}
	}

	private void GenerateHelpFortress()
	{
		AILayer_Navy layer = base.AIEntity.GetLayer<AILayer_Navy>();
		if (layer == null || layer.NavyImportance < 0.5f)
		{
			return;
		}
		for (int i = 0; i < layer.NavyTasks.Count; i++)
		{
			NavyTask_Takeover takeOver = layer.NavyTasks[i] as NavyTask_Takeover;
			if (takeOver != null)
			{
				CatspawTask_HelpFortress catspawTask_HelpFortress = this.FindTask<CatspawTask_HelpFortress>((CatspawTask_HelpFortress match) => match.TargetGuid == takeOver.TargetGuid);
				if (takeOver.AssignedArmy != null)
				{
					if (catspawTask_HelpFortress != null)
					{
						if (catspawTask_HelpFortress.AssignedArmy != null)
						{
							catspawTask_HelpFortress.AssignedArmy.Unassign();
						}
						this.catspawTasks.Remove(catspawTask_HelpFortress);
					}
				}
				else if (catspawTask_HelpFortress == null)
				{
					catspawTask_HelpFortress = new CatspawTask_HelpFortress();
					catspawTask_HelpFortress.Owner = base.AIEntity.Empire;
					catspawTask_HelpFortress.TargetGuid = takeOver.TargetGuid;
					this.catspawTasks.Add(catspawTask_HelpFortress);
				}
			}
		}
	}

	private T FindTask<T>(Func<T, bool> predicate) where T : ArmyTask
	{
		for (int i = 0; i < this.catspawTasks.Count; i++)
		{
			T t = this.catspawTasks[i] as T;
			if (t != null && (predicate == null || predicate(t)))
			{
				return t;
			}
		}
		return (T)((object)null);
	}

	public HeuristicValue GlobalPriority { get; set; }

	public List<CatspawArmy> CatspawArmies
	{
		get
		{
			return this.catspawArmies;
		}
	}

	public List<CatspawTask> CatspawTasks
	{
		get
		{
			return this.catspawTasks;
		}
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		base.AIEntity.AIPlayer.AIPlayerStateChange += this.AIPlayer_AIPlayerStateChange;
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfDefense.ArmiesCollectionChange += this.DepartmentOfDefense_ArmiesCollectionChange;
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.game = (gameService.Game as global::Game);
		this.visibilityService = gameService.Game.Services.GetService<IVisibilityService>();
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Catspaw_CreateLocalNeeds", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_Catspaw_ExecuteNeeds", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI && this.CanUseCatspaw();
	}

	public override void Release()
	{
		base.AIEntity.AIPlayer.AIPlayerStateChange -= this.AIPlayer_AIPlayerStateChange;
		if (this.departmentOfDefense != null)
		{
			this.departmentOfDefense.ArmiesCollectionChange -= this.DepartmentOfDefense_ArmiesCollectionChange;
			this.departmentOfDefense = null;
		}
		this.departmentOfTheTreasury = null;
		this.game = null;
		this.visibilityService = null;
		this.worldPositionningService = null;
		for (int i = 0; i < this.catspawArmies.Count; i++)
		{
			this.catspawArmies[i].Release();
		}
		this.catspawArmies.Clear();
		this.CatspawTasks.Clear();
		this.catspawRequests.Clear();
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		this.GlobalPriority.Reset();
		this.GlobalPriority.Add(0.8f, "(constant)", new object[0]);
		this.UpdateArmyWithCatspaw();
		this.UpdateCatspawTasksValidity();
		this.GenerateTasks();
		this.CheckArmyAgainstCatspawRelease();
		this.CheckArmyAgainstCatspawStart();
		this.UpdateCatspawArmyPolicyRelease();
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_StartValidatedCatspawRequest));
	}

	private bool CanUseCatspaw()
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		return service != null && service.IsShared(DownloadableContent16.ReadOnlyName) && base.AIEntity.Empire.SimulationObject.Tags.Contains(DownloadableContent16.FactionTraitCatspaw);
	}

	private void GenerateTasks()
	{
		this.GenerateDenyCitySiege();
		this.GenerateDenySettlerAttack();
		this.GenerateHelpVillage();
		this.GenerateHelpFortress();
	}

	private void CheckArmyAgainstCatspawStart()
	{
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			if (!(this.game.Empires[i] is MajorEmpire))
			{
				if (!(this.game.Empires[i] is LesserEmpire))
				{
					DepartmentOfDefense agency = this.game.Empires[i].GetAgency<DepartmentOfDefense>();
					for (int j = 0; j < agency.Armies.Count; j++)
					{
						Army minorArmy = agency.Armies[j];
						if (this.visibilityService.IsWorldPositionVisibleFor(minorArmy.WorldPosition, base.AIEntity.Empire))
						{
							float allowedTurn = 1f;
							CatspawTask catspawTask = this.FindBestTaskFor(minorArmy, allowedTurn);
							if (catspawTask != null)
							{
								CatspawRequest catspawRequest = this.catspawRequests.Find((CatspawRequest match) => match.ArmyGuid == minorArmy.GUID);
								if (catspawRequest == null)
								{
									catspawRequest = new CatspawRequest();
									catspawRequest.ArmyGuid = minorArmy.GUID;
									base.AIEntity.AIPlayer.Blackboard.AddMessage(catspawRequest);
									this.catspawRequests.Add(catspawRequest);
								}
								HeuristicValue heuristicValue = new HeuristicValue(0f);
								heuristicValue.Add(catspawTask.GetLocalPriority(), "Task local priority", new object[0]);
								catspawRequest.SetInterest(this.GlobalPriority, heuristicValue);
								catspawRequest.TimeOut = 1;
								float catspawCost = this.GetCatspawCost(minorArmy);
								catspawRequest.UpdateBuyEvaluation("CatspawArmy", 0UL, catspawCost, (int)BuyEvaluation.MaxTurnGain, 0f, 0UL);
							}
						}
					}
				}
			}
		}
	}

	private int HowManyArmiesMayIKeep()
	{
		float num;
		this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, true);
		float num2 = 0f;
		this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, out num2, true);
		float num3 = 50f;
		int num4 = 0;
		float num5 = 2f;
		if (num > num3 * num5)
		{
			num4 += Mathf.FloorToInt(num / (num3 * num5));
		}
		float num6 = 1f;
		if (num2 > num3 * num6)
		{
			num4 += Mathf.FloorToInt(num / (num3 * num6));
		}
		return num4;
	}

	private void CheckArmyAgainstCatspawRelease()
	{
		for (int i = 0; i < this.catspawArmies.Count; i++)
		{
			if (this.catspawArmies[i].CurrentMainTask == null || !this.catspawArmies[i].CurrentMainTask.CheckValidity() || this.catspawArmies[i].BehaviorState == ArmyWithTask.ArmyBehaviorState.Succeed)
			{
				this.catspawArmies[i].ValidateMainTask();
				if (this.catspawArmies[i].CurrentMainTask == null)
				{
					this.catspawArmies[i].ReleasePolicy = CatspawArmy.ReleasePolicyType.OnEndTurn;
				}
				this.catspawArmies[i].State = TickableState.NeedTick;
			}
		}
	}

	private float GetCatspawCost(Army minorArmy)
	{
		ConstructionCost[] catspawCost = DepartmentOfDefense.GetCatspawCost(base.AIEntity.Empire, minorArmy);
		if (catspawCost.Length >= 1)
		{
			return catspawCost[0].GetValue(base.AIEntity.Empire);
		}
		return 0f;
	}

	private void DepartmentOfDefense_ArmiesCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		Army army = e.Element as Army;
		if (army == null)
		{
			return;
		}
		if (e.Action == CollectionChangeAction.Add)
		{
			if (!army.HasCatspaw)
			{
				return;
			}
			this.CreateCatspawArmy(army);
			this.UpdateCatspawArmyPolicyRelease();
		}
		else if (e.Action == CollectionChangeAction.Remove)
		{
			for (int i = 0; i < this.catspawArmies.Count; i++)
			{
				if (this.catspawArmies[i].Garrison == army)
				{
					this.catspawArmies[i].Release();
					this.catspawArmies.RemoveAt(i);
					break;
				}
			}
		}
	}

	private void SetArmyReleasePolicy(CatspawArmy army)
	{
		if (army.ReleasePolicy == CatspawArmy.ReleasePolicyType.OnEndTurn || army.ReleasePolicy == CatspawArmy.ReleasePolicyType.Force)
		{
			return;
		}
		if (this.numberOfArmies <= 0)
		{
			army.ReleasePolicy = CatspawArmy.ReleasePolicyType.OnEndTurn;
		}
		else
		{
			army.ReleasePolicy = CatspawArmy.ReleasePolicyType.None;
			this.numberOfArmies--;
		}
	}

	private void UpdateArmyWithCatspaw()
	{
		for (int i = this.catspawArmies.Count - 1; i >= 0; i--)
		{
			if (this.catspawArmies[i].Garrison == null)
			{
				this.catspawArmies[i].Release();
				this.catspawArmies.RemoveAt(i);
			}
		}
		int index;
		for (index = 0; index < this.departmentOfDefense.Armies.Count; index++)
		{
			if (this.departmentOfDefense.Armies[index].HasCatspaw)
			{
				if (!this.catspawArmies.Exists((CatspawArmy match) => match.Garrison == this.departmentOfDefense.Armies[index]))
				{
					this.CreateCatspawArmy(this.departmentOfDefense.Armies[index]);
				}
			}
		}
	}

	private void UpdateCatspawArmyPolicyRelease()
	{
		this.numberOfArmies = this.HowManyArmiesMayIKeep();
		for (int i = 0; i < this.catspawArmies.Count; i++)
		{
			if (this.catspawArmies[i].ReleasePolicy != CatspawArmy.ReleasePolicyType.Force)
			{
				this.SetArmyReleasePolicy(this.catspawArmies[i]);
			}
		}
	}

	private void UpdateCatspawTasksValidity()
	{
		for (int i = this.catspawTasks.Count - 1; i >= 0; i--)
		{
			if (!this.catspawTasks[i].CheckValidity())
			{
				if (this.catspawTasks[i].AssignedArmy != null)
				{
					this.catspawTasks[i].AssignedArmy.Unassign();
				}
				this.catspawTasks.RemoveAt(i);
			}
		}
	}

	private void CreateCatspawArmy(Army army)
	{
		CatspawArmy catspawArmy = new CatspawArmy(army, this);
		catspawArmy.IsActive = this.IsActive();
		catspawArmy.Initialize();
		catspawArmy.BehaviorState = ArmyWithTask.ArmyBehaviorState.NeedRun;
		catspawArmy.State = TickableState.NeedTick;
		this.catspawArmies.Add(catspawArmy);
		this.UpdateCatspawArmyPolicyRelease();
	}

	private CatspawTask FindBestTaskFor(Army minorArmy, float allowedTurn)
	{
		CatspawTask result = null;
		float num = float.MaxValue;
		for (int i = 0; i < this.CatspawTasks.Count; i++)
		{
			if (this.CatspawTasks[i].AssignedArmy == null)
			{
				if (this.CatspawTasks[i].CheckValidity())
				{
					if (this.CatspawTasks[i].IsMinorArmyValid(minorArmy, allowedTurn))
					{
						float num2 = (float)this.worldPositionningService.GetDistance(minorArmy.WorldPosition, this.CatspawTasks[i].GetTargetPosition());
						if (num > num2)
						{
							result = this.CatspawTasks[i];
							num = num2;
						}
					}
				}
			}
		}
		return result;
	}

	private SynchronousJobState SynchronousJob_StartValidatedCatspawRequest()
	{
		for (int i = this.catspawRequests.Count - 1; i >= 0; i--)
		{
			if (this.catspawRequests[i].ChosenBuyEvaluation != null && this.catspawRequests[i].EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && this.catspawRequests[i].ChosenBuyEvaluation.State == BuyEvaluation.EvaluationState.Purchased)
			{
				OrderToggleCatspaw order = new OrderToggleCatspaw(base.AIEntity.Empire.Index, this.catspawRequests[i].ArmyGuid, true);
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
				this.catspawRequests[i].SetObtained();
				this.catspawRequests.RemoveAt(i);
			}
		}
		return SynchronousJobState.Success;
	}

	private void AIPlayer_AIPlayerStateChange(object sender, EventArgs e)
	{
		for (int i = 0; i < this.catspawArmies.Count; i++)
		{
			this.catspawArmies[i].IsActive = this.IsActive();
		}
	}

	public const string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Catspaw";

	private List<GlobalObjectiveMessage> objectiveMessages;

	private List<CatspawArmy> catspawArmies = new List<CatspawArmy>();

	private List<CatspawTask> catspawTasks = new List<CatspawTask>();

	private List<CatspawRequest> catspawRequests = new List<CatspawRequest>();

	private IVisibilityService visibilityService;

	private IWorldPositionningService worldPositionningService;

	private global::Game game;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private int numberOfArmies;
}
