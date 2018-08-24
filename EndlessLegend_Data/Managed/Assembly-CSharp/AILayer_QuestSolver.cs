using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AILayer_QuestSolver : AILayer
{
	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.departmentOfInternalAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfInternalAffairs>();
		this.synchronousJobRepository = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.randomNumberGeneratorAIHelper = AIScheduler.Services.GetService<IRandomNumberGeneratorAIHelper>();
		this.questSolverDatabase = Databases.GetDatabase<AIQuestSolverDefinition>(false);
		this.endTurnService = Services.GetService<IEndTurnService>();
		IGameService gameService = Services.GetService<IGameService>();
		this.questManagementService = gameService.Game.Services.GetService<IQuestManagementService>();
		this.questRepositoryService = gameService.Game.Services.GetService<IQuestRepositoryService>();
		this.defaultSolverDefinition = this.questSolverDatabase.GetValue(AILayer_QuestSolver.DefaultQuestSolverDefinition);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_QuestSolver_CreateLocalNeeds", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		base.Release();
		this.departmentOfInternalAffairs = null;
		this.synchronousJobRepository = null;
		this.personalityAIHelper = null;
		this.randomNumberGeneratorAIHelper = null;
		this.questSolverDatabase = null;
		this.endTurnService = null;
		this.questManagementService = null;
		this.questRepositoryService = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		this.toBeRemoved.Clear();
		foreach (KeyValuePair<GameEntityGUID, AIQuestSolver> keyValuePair in this.solverByQuest)
		{
			Quest quest;
			if (!this.questRepositoryService.TryGetValue(keyValuePair.Key, out quest) || !this.questManagementService.IsQuestRunningForEmpire(quest.QuestDefinition.Name, base.AIEntity.Empire))
			{
				this.toBeRemoved.Add(keyValuePair.Key);
			}
		}
		for (int i = 0; i < this.toBeRemoved.Count; i++)
		{
			this.solverByQuest.Remove(this.toBeRemoved[i]);
		}
		bool flag = false;
		foreach (Quest quest2 in this.departmentOfInternalAffairs.QuestJournal.Read(QuestState.InProgress))
		{
			if (quest2 != null && quest2.QuestDefinition != null)
			{
				if (!quest2.QuestDefinition.Tags.Contains(QuestDefinition.TagHidden))
				{
					AIQuestSolver aiquestSolver;
					if (!this.solverByQuest.TryGetValue(quest2.GUID, out aiquestSolver))
					{
						if (quest2.QuestDefinition.IsGlobal)
						{
							continue;
						}
						AIQuestSolverDefinition aiquestSolverDefinition;
						if (!this.questSolverDatabase.TryGetValue(quest2.QuestDefinition.Name, out aiquestSolverDefinition))
						{
							aiquestSolverDefinition = this.defaultSolverDefinition;
						}
						if (aiquestSolverDefinition.ChanceOfSuccess <= 0f)
						{
							continue;
						}
						aiquestSolver = new AIQuestSolver(aiquestSolverDefinition, this.ComputeDuration(aiquestSolverDefinition));
						this.solverByQuest.Add(quest2.GUID, aiquestSolver);
					}
					if (aiquestSolver != null)
					{
						int num = this.endTurnService.Turn - quest2.TurnWhenStarted;
						if (num >= aiquestSolver.ChosenDuration)
						{
							flag = true;
						}
					}
				}
			}
		}
		if (flag)
		{
			this.synchronousJobRepository.RegisterSynchronousJob(new SynchronousJob(this.ParseEndedSolvers));
		}
	}

	private bool CheckAgainstSuccesRate(AIQuestSolver solver)
	{
		float num = this.randomNumberGeneratorAIHelper.Range(0f, 1f);
		float value = this.personalityAIHelper.GetValue<float>(base.AIEntity.Empire, "AIQuestSolverDefinition/ChanceOfSuccess", solver.QuestSolverDefinition.ChanceOfSuccess);
		return num <= value;
	}

	private int ComputeDuration(AIQuestSolverDefinition definition)
	{
		int value = this.personalityAIHelper.GetValue<int>(base.AIEntity.Empire, "AIQuestSolverDefinition/MinimalTurnDuration", definition.MinimalTurnDuration);
		int value2 = this.personalityAIHelper.GetValue<int>(base.AIEntity.Empire, "AIQuestSolverDefinition/MaximalTurnDuration", definition.MaximalTurnDuration);
		return this.randomNumberGeneratorAIHelper.Range(value, value2);
	}

	private SynchronousJobState ParseEndedSolvers()
	{
		foreach (KeyValuePair<GameEntityGUID, AIQuestSolver> keyValuePair in this.solverByQuest)
		{
			Quest quest;
			if (this.questRepositoryService.TryGetValue(keyValuePair.Key, out quest))
			{
				if (quest.QuestState == QuestState.InProgress)
				{
					if (this.questManagementService.IsQuestRunningForEmpire(quest.QuestDefinition.Name, base.AIEntity.Empire))
					{
						AIQuestSolver value = keyValuePair.Value;
						int num = this.endTurnService.Turn - quest.TurnWhenStarted;
						if (num >= value.ChosenDuration)
						{
							if (this.CheckAgainstSuccesRate(value))
							{
								this.questManagementService.ForceQuestCompletion(keyValuePair.Key, QuestState.Completed);
							}
							else
							{
								this.questManagementService.ForceQuestCompletion(keyValuePair.Key, QuestState.Failed);
							}
						}
					}
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private bool TryGetQuestSolverDefinition(Quest quest, out AIQuestSolverDefinition solverDefinition)
	{
		if (this.questSolverDatabase.TryGetValue(quest.QuestDefinition.Name, out solverDefinition))
		{
			return true;
		}
		string x = string.Format("{0}-{1}-{2}", AILayer_QuestSolver.DefaultQuestSolverDefinition, quest.QuestDefinition.Category, quest.QuestDefinition.SubCategory);
		if (this.questSolverDatabase.TryGetValue(x, out solverDefinition))
		{
			return true;
		}
		x = string.Format("{0}-{1}", AILayer_QuestSolver.DefaultQuestSolverDefinition, quest.QuestDefinition.Category);
		if (this.questSolverDatabase.TryGetValue(x, out solverDefinition))
		{
			return true;
		}
		if (this.defaultSolverDefinition != null)
		{
			solverDefinition = this.defaultSolverDefinition;
			return true;
		}
		return false;
	}

	public static string DefaultQuestSolverDefinition = "DefaultQuestSolverDefinition";

	private AIQuestSolverDefinition defaultSolverDefinition;

	private DepartmentOfInternalAffairs departmentOfInternalAffairs;

	private IEndTurnService endTurnService;

	private IPersonalityAIHelper personalityAIHelper;

	private IQuestManagementService questManagementService;

	private IQuestRepositoryService questRepositoryService;

	private IDatabase<AIQuestSolverDefinition> questSolverDatabase;

	private IRandomNumberGeneratorAIHelper randomNumberGeneratorAIHelper;

	private Dictionary<GameEntityGUID, AIQuestSolver> solverByQuest = new Dictionary<GameEntityGUID, AIQuestSolver>();

	private ISynchronousJobRepositoryAIHelper synchronousJobRepository;

	private List<GameEntityGUID> toBeRemoved = new List<GameEntityGUID>();
}
