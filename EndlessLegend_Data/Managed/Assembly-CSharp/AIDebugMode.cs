using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.AI.Decision.Diagnostics;
using Amplitude.Unity.AI.Evaluation.Diagnostics;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.UI;
using Amplitude.Unity.View;
using UnityEngine;

public class AIDebugMode : Amplitude.Unity.Framework.Behaviour, IAIEvaluationHelper<AIDebugMode.Element, AIDebugMode.Context>
{
	public AIDebugMode()
	{
		this.altarDecisionMakerDebugger = new ElementEvaluationGUILayoutDebugger<ConstructibleElement, InterpreterContext>("Orb unlock evaluations", true);
		this.seasonEffectDecisionMakerDebugger = new ElementEvaluationGUILayoutDebugger<SeasonEffect, InterpreterContext>("Season effect evaluations", true);
	}

	private void DiplayAltarDebugger()
	{
		this.DisplayAltarSummary();
		UnityEngine.GUILayout.Space(10f);
		this.DisplayEmpireState(new string[]
		{
			"Research"
		});
		UnityEngine.GUILayout.Space(10f);
		this.DisplayAltarEvaluableMessages();
		UnityEngine.GUILayout.Space(10f);
		if (this.aiLayerResearch != null && Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Assert(this.altarDecisionMakerDebugger != null);
			this.altarDecisionMakerDebugger.OnGUI(this.aiLayerResearch.OrbUnlockDecisionMakerEvaluationDataHistoric, true);
		}
		UnityEngine.GUILayout.Space(10f);
		if (this.aiLayerAltar != null && Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Assert(this.seasonEffectDecisionMakerDebugger != null);
			this.seasonEffectDecisionMakerDebugger.OnGUI(this.aiLayerAltar.SeasonEffectsEvaluationDataHistoric, true);
		}
	}

	private void DisplayAltarEvaluableMessages()
	{
		UnityEngine.GUILayout.Label("<b>Evaluable messages</b>", new GUILayoutOption[0]);
		this.evaluableMessages.Clear();
		this.evaluableMessages.AddRange(this.empireBlackboard.GetMessages<EvaluableMessage>(BlackboardLayerID.Empire));
		this.evaluableMessages.Sort((EvaluableMessage left, EvaluableMessage right) => -1 * left.Interest.CompareTo(right.Interest));
		this.empirePointEvaluableMessages.Clear();
		this.moneyEvaluableMessages.Clear();
		this.orbEvaluableMessages.Clear();
		for (int i = 0; i < this.evaluableMessages.Count; i++)
		{
			StaticString accountTagResource = this.aiLayerAccountManager.GetAccountTagResource(this.evaluableMessages[i].AccountTag);
			if (accountTagResource == DepartmentOfTheTreasury.Resources.EmpirePoint)
			{
				this.empirePointEvaluableMessages.Add(this.evaluableMessages[i]);
			}
			else if (accountTagResource == DepartmentOfTheTreasury.Resources.EmpireMoney)
			{
				this.moneyEvaluableMessages.Add(this.evaluableMessages[i]);
			}
			else if (accountTagResource == DepartmentOfTheTreasury.Resources.Orb)
			{
				this.orbEvaluableMessages.Add(this.evaluableMessages[i]);
			}
		}
		UnityEngine.GUILayout.Label("<b>Orbs</b>", new GUILayoutOption[0]);
		this.orbEvaluableMessageTable.OnGUI(this.orbEvaluableMessages, float.PositiveInfinity);
	}

	private void DisplayAltarSummary()
	{
		if (this.resourceAmas != null)
		{
			UnityEngine.GUILayout.Label("<b>Summary</b>", new GUILayoutOption[0]);
			UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			UnityEngine.GUILayout.Space(80f);
			UnityEngine.GUILayout.Label("<i>Food</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Industry</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Science</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Dust</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Empire Point</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(80f)
			});
			UnityEngine.GUILayout.EndHorizontal();
			UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label("<i>AMAS</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(80f)
			});
			UnityEngine.GUILayout.Label("-", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("-", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.TechnologyReferenceTurnCount), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.NetEmpireMoney), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.NetEmpirePrestige), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.EndHorizontal();
			AmasEmpireDataMessage amasEmpireDataMessage = this.altarBlackboard.GetMessage(this.aiLayerAmasEmpire.AmasEmpireDataMessageID) as AmasEmpireDataMessage;
			if (amasEmpireDataMessage != null)
			{
				UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				UnityEngine.GUILayout.Label("<i>Weight</i>", new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(80f)
				});
				if (amasEmpireDataMessage.ResearchWeights != null)
				{
					for (int i = 0; i < amasEmpireDataMessage.ResearchWeights.Length; i++)
					{
						this.WeightLabel(amasEmpireDataMessage.ResearchWeights[i], new GUILayoutOption[]
						{
							UnityEngine.GUILayout.Width(60f)
						});
					}
				}
				UnityEngine.GUILayout.EndHorizontal();
			}
		}
	}

	private void InitializeAltarDebugger()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			Diagnostics.LogError("Failed to retrieve ai entity empire.");
			this.ReleaseAltarDebugger();
		}
		this.aiEntityContext = entity.Context;
		this.altarBlackboard = entity.Blackboard;
		Diagnostics.Assert(this.altarBlackboard != null);
		this.aiLayerResearch = entity.GetLayer<AILayer_Research>();
		Diagnostics.Assert(this.aiLayerResearch != null);
		this.aiLayerAltar = entity.GetLayer<AILayer_Altar>();
		Diagnostics.Assert(this.aiLayerAltar != null);
		this.aiLayerAmasEmpire = entity.GetLayer<AILayer_AmasEmpire>();
		Diagnostics.Assert(this.aiLayerAmasEmpire != null);
		this.aiLayerAccountManager = entity.GetLayer<AILayer_AccountManager>();
		Diagnostics.Assert(this.aiLayerAccountManager != null);
		AIEntity_Amas entity2 = aiplayer_MajorEmpire.GetEntity<AIEntity_Amas>();
		Diagnostics.Assert(entity2 != null);
		AILayer_ResourceAmas layer = entity2.GetLayer<AILayer_ResourceAmas>();
		Diagnostics.Assert(layer != null);
		this.resourceAmas = layer.Amas;
		this.orbEvaluableMessageTable = this.InitializeTable();
		this.currentDebugMode = AIDebugMode.DebugMode.Altar;
	}

	private void ReleaseAltarDebugger()
	{
		this.aiEntityContext = null;
		this.altarBlackboard = null;
		this.aiLayerResearch = null;
		this.aiLayerAltar = null;
		this.aiLayerAmasEmpire = null;
		this.aiLayerAccountManager = null;
		this.resourceAmas = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private void DiplayArmyDebugger()
	{
		if (this.selectedArmy == null || this.selectedArmy.SimulationObject == null)
		{
			return;
		}
		if ((this.selectedArmy.Empire is LesserEmpire && this.aiLayerQuestManager == null) || (!(this.selectedArmy.Empire is LesserEmpire) && this.aiLayerArmyManagement == null))
		{
			return;
		}
		UnityEngine.GUILayout.Label(string.Format("Military power: {0}", this.selectedArmy.GetPropertyValue(SimulationProperties.MilitaryPower).ToString("0.0")), new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label(string.Format("<b>Army {0} commanders</b>", this.selectedArmy.GUID), new GUILayoutOption[0]);
		if (this.selectedArmy.Empire is LesserEmpire)
		{
			this.DisplayCommanders(this.aiLayerQuestManager.AICommanders, (AICommander commander) => commander.Missions.Any((AICommanderMission mission) => mission.AIDataArmyGUID == this.selectedArmy.GUID));
		}
		else
		{
			this.DisplayCommanders(this.aiLayerArmyManagement.AICommanders, (AICommander commander) => commander.Missions.Any((AICommanderMission mission) => mission.AIDataArmyGUID == this.selectedArmy.GUID));
			this.DisplayCommanders(this.aiLayerArmyRecruitment.AICommanders, (AICommander commander) => commander.Missions.Any((AICommanderMission mission) => mission.AIDataArmyGUID == this.selectedArmy.GUID));
			this.DisplayCommanders(this.aiLayerManta.AICommanders, (AICommander commander) => commander.Missions.Any((AICommanderMission mission) => mission.AIDataArmyGUID == this.selectedArmy.GUID));
			this.DisplayCommanders(this.aiLayerColossus.AICommanders, (AICommander commander) => commander.Missions.Any((AICommanderMission mission) => mission.AIDataArmyGUID == this.selectedArmy.GUID));
			this.DisplayCommanders(this.aiLayerKaiju.AICommanders, (AICommander commander) => commander.Missions.Any((AICommanderMission mission) => mission.AIDataArmyGUID == this.selectedArmy.GUID));
		}
		UnityEngine.GUILayout.Space(10f);
	}

	private void InitializeArmyDebugger(Army army)
	{
		this.selectedArmy = army;
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		global::Empire empire = army.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		if (empire is LesserEmpire)
		{
			AIPlayer_LesserEmpire aiplayer_LesserEmpire = this.aiScheduler.AIPlayer_LesserEmpire;
			if (aiplayer_LesserEmpire == null)
			{
				Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
				{
					empire.Index
				});
			}
			AIEntity_LesserEmpire entity = aiplayer_LesserEmpire.GetEntity<AIEntity_LesserEmpire>();
			if (entity == null)
			{
				Diagnostics.LogError("Failed to retrieve ai entity empire.");
				this.ReleaseArmyDebugger();
			}
			this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
			this.aiLayerQuestManager = entity.GetLayer<AILayer_QuestManager>();
			this.currentDebugMode = AIDebugMode.DebugMode.Army;
			return;
		}
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
		}
		AIEntity_Empire entity2 = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity2 == null)
		{
			Diagnostics.LogError("Failed to retrieve ai entity empire.");
			this.ReleaseArmyDebugger();
		}
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.aiLayerArmyManagement = entity2.GetLayer<AILayer_ArmyManagement>();
		Diagnostics.Assert(this.aiLayerArmyManagement != null);
		this.aiLayerArmyRecruitment = entity2.GetLayer<AILayer_ArmyRecruitment>();
		Diagnostics.Assert(this.aiLayerArmyRecruitment != null);
		this.aiLayerManta = entity2.GetLayer<AILayer_Manta>();
		Diagnostics.Assert(this.aiLayerManta != null);
		this.aiLayerColossus = entity2.GetLayer<AILayer_Colossus>();
		this.aiLayerKaiju = entity2.GetLayer<AILayer_KaijuManagement>();
		this.currentDebugMode = AIDebugMode.DebugMode.Army;
	}

	private void ReleaseArmyDebugger()
	{
		this.selectedArmy = null;
		this.aiLayerArmyManagement = null;
		this.aiLayerArmyRecruitment = null;
		this.aiLayerManta = null;
		this.aiLayerColossus = null;
		this.aiLayerQuestManager = null;
		this.aiLayerKaiju = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private void DiplayCityListDebugger()
	{
		UnityEngine.GUILayout.Label("<b>Evaluable messages</b>", new GUILayoutOption[0]);
		this.evaluableMessages.Clear();
		this.evaluableMessages.AddRange(this.empireBlackboard.GetMessages<EvaluableMessage>(BlackboardLayerID.City));
		this.evaluableMessages.Sort((EvaluableMessage left, EvaluableMessage right) => -1 * left.Interest.CompareTo(right.Interest));
		this.cityEvaluableMessageTable.OnGUI(this.evaluableMessages, float.PositiveInfinity);
	}

	private void InitializeCityListDebugger()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			Diagnostics.LogError("Failed to retrieve ai entity empire.");
			this.ReleaseDefaultDebugger();
		}
		this.empireBlackboard = entity.Blackboard;
		Diagnostics.Assert(this.empireBlackboard != null);
		this.aiLayerEconomy = entity.GetLayer<AILayer_Economy>();
		Diagnostics.Assert(this.aiLayerEconomy != null);
		this.aiLayerAccountManager = entity.GetLayer<AILayer_AccountManager>();
		Diagnostics.Assert(this.aiLayerAccountManager != null);
		this.currentDebugMode = AIDebugMode.DebugMode.CityList;
		this.cityEvaluableMessageTable = new Amplitude.Unity.UI.GUILayout.Table<EvaluableMessage>(new Action<ITableContentManager, EvaluableMessage, bool>(this.EvaluableMessageDisplayer), false, true);
		this.cityEvaluableMessageTable.AddColumn("<b>ID</b>", 30f);
		this.cityEvaluableMessageTable.AddColumn("<b>TYPE</b>", 140f);
		this.cityEvaluableMessageTable.AddColumn("<b>STATE</b>", 105f);
		this.cityEvaluableMessageTable.AddColumn("<b>ACCOUNT</b>", 80f);
		this.cityEvaluableMessageTable.AddColumn("<b>INTEREST</b>", 75f);
		this.cityEvaluableMessageTable.AddColumn("<b>SUMMARY</b>", float.PositiveInfinity);
	}

	private void ReleaseCityListDebugger()
	{
		this.empireBlackboard = null;
		this.aiLayerAccountManager = null;
		this.aiLayerEconomy = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private void DiplayCityDebugger()
	{
		this.DisplayCitySummary();
		UnityEngine.GUILayout.Space(10f);
		this.DisplayCityState();
		UnityEngine.GUILayout.Space(10f);
		this.DisplayBuildingProductionEvaluations();
		UnityEngine.GUILayout.Space(10f);
		this.DisplayUnitProductionEvaluations();
		UnityEngine.GUILayout.Space(10f);
		this.DisplayBoosterProductionEvaluations();
		UnityEngine.GUILayout.Space(10f);
		this.DisplayWonderProductionEvaluations();
		UnityEngine.GUILayout.Space(10f);
		if (this.aiLayerProduction == null)
		{
			return;
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Assert(this.productionDecisionMakerDebugger != null);
			this.productionDecisionMakerDebugger.OnGUI(this.aiLayerProduction.DecisionMakerEvaluationDataHistoric, true);
		}
		UnityEngine.GUILayout.Space(10f);
		this.DisplayEspionageInfos();
		UnityEngine.GUILayout.Space(10f);
	}

	private void DisplayBoosterProductionEvaluations()
	{
		UnityEngine.GUILayout.Label("<b>Booster production evaluations</b>", new GUILayoutOption[0]);
		this.boosterProductionMessages.Clear();
		GameEntityGUID cityGUID = this.aiEntityCity.City.GUID;
		this.boosterProductionMessages.AddRange(this.cityBlackboard.GetMessages<EvaluableMessage_CityBooster>(BlackboardLayerID.Empire, (EvaluableMessage_CityBooster match) => match.ChosenProductionEvaluation != null && match.ChosenProductionEvaluation.CityGuid == cityGUID));
		this.boosterProductionMessages.Sort(this.boosterProductionMessageComparison);
		Predicate<ProductionEvaluation> <>9__1;
		foreach (EvaluableMessage_CityBooster evaluableMessage_CityBooster in this.boosterProductionMessages)
		{
			string text = string.Format("{1} for city {2} <i>[{3}]</i>", new object[]
			{
				evaluableMessage_CityBooster.Interest.Value.ToString("F"),
				evaluableMessage_CityBooster.BoosterDefinitionName,
				evaluableMessage_CityBooster.CityGuid,
				evaluableMessage_CityBooster.State.ToString().Replace("Message_", string.Empty)
			});
			List<ProductionEvaluation> productionEvaluations = evaluableMessage_CityBooster.ProductionEvaluations;
			Predicate<ProductionEvaluation> match2;
			if ((match2 = <>9__1) == null)
			{
				match2 = (<>9__1 = ((ProductionEvaluation match) => match.CityGuid == cityGUID));
			}
			foreach (ProductionEvaluation productionEvaluation in productionEvaluations.FindAll(match2))
			{
				text += string.Format("\n        <size=11><b>{0}</b> by {1} (base interest was {2})</size>", productionEvaluation.ProductionFinalScore.ToString("F"), productionEvaluation.LayerTag, evaluableMessage_CityBooster.Interest.Value.ToString("F"));
			}
			Color color = (evaluableMessage_CityBooster.State != BlackboardMessage.StateValue.Message_InProgress) ? ((evaluableMessage_CityBooster.State != BlackboardMessage.StateValue.Message_Success) ? Color.gray : Color.green) : GUI.color;
			Amplitude.Unity.UI.GUILayout.Label(text, color, new GUILayoutOption[0]);
		}
	}

	private void DisplayBuildingProductionEvaluations()
	{
		UnityEngine.GUILayout.Label("<b>Building production evaluations</b>", new GUILayoutOption[0]);
		this.buildingProductionMessages.Clear();
		GameEntityGUID cityGUID = this.aiEntityCity.City.GUID;
		this.buildingProductionMessages.AddRange(this.cityBlackboard.GetMessages<EvaluableMessage_BuildingProduction>(BlackboardLayerID.City, (EvaluableMessage_BuildingProduction match) => match.CityGuid == cityGUID));
		this.buildingProductionMessages.Sort(this.buildingProductionMessageComparison);
		Predicate<ProductionEvaluation> <>9__1;
		foreach (EvaluableMessage_BuildingProduction evaluableMessage_BuildingProduction in this.buildingProductionMessages)
		{
			string text = string.Format("{1}", evaluableMessage_BuildingProduction.Interest.Value.ToString("F"), evaluableMessage_BuildingProduction.ConstructibleElementName);
			if (evaluableMessage_BuildingProduction.BuildingPosition.IsValid)
			{
				text += string.Format(" <size=11>at position {0}</size>", evaluableMessage_BuildingProduction.BuildingPosition);
			}
			text += string.Format(" <i>[{0}]</i>", evaluableMessage_BuildingProduction.State.ToString().Replace("Message_", string.Empty));
			List<ProductionEvaluation> productionEvaluations = evaluableMessage_BuildingProduction.ProductionEvaluations;
			Predicate<ProductionEvaluation> match2;
			if ((match2 = <>9__1) == null)
			{
				match2 = (<>9__1 = ((ProductionEvaluation match) => match.CityGuid == cityGUID));
			}
			foreach (ProductionEvaluation productionEvaluation in productionEvaluations.FindAll(match2))
			{
				text += string.Format("\n        <size=11><b>{0}</b> by {1} (base interest was {2})</size>", productionEvaluation.ProductionFinalScore.ToString("F"), productionEvaluation.LayerTag, evaluableMessage_BuildingProduction.Interest.Value.ToString("F"));
			}
			Color color = (evaluableMessage_BuildingProduction.State != BlackboardMessage.StateValue.Message_InProgress) ? ((evaluableMessage_BuildingProduction.State != BlackboardMessage.StateValue.Message_Success) ? Color.gray : Color.green) : GUI.color;
			Amplitude.Unity.UI.GUILayout.Label(text, color, new GUILayoutOption[0]);
		}
	}

	private void DisplayWonderProductionEvaluations()
	{
		UnityEngine.GUILayout.Label("<b>Wonder production evaluations</b>", new GUILayoutOption[0]);
		this.wonderProductionMessages.Clear();
		GameEntityGUID cityGUID = this.aiEntityCity.City.GUID;
		this.wonderProductionMessages.AddRange(this.cityBlackboard.GetMessages<EvaluableMessage_Wonder>(BlackboardLayerID.Empire, (EvaluableMessage_Wonder match) => match.ChosenProductionEvaluation == null || match.ChosenProductionEvaluation.CityGuid == cityGUID));
		this.wonderProductionMessages.Sort(this.wonderProductionMessageComparison);
		Predicate<ProductionEvaluation> <>9__1;
		foreach (EvaluableMessage_Wonder evaluableMessage_Wonder in this.wonderProductionMessages)
		{
			string text = string.Format("{1}", evaluableMessage_Wonder.Interest.Value.ToString("F"), evaluableMessage_Wonder.ConstructibleElementName);
			text += string.Format(" <i>[{0}]</i>", evaluableMessage_Wonder.State.ToString().Replace("Message_", string.Empty));
			List<ProductionEvaluation> productionEvaluations = evaluableMessage_Wonder.ProductionEvaluations;
			Predicate<ProductionEvaluation> match2;
			if ((match2 = <>9__1) == null)
			{
				match2 = (<>9__1 = ((ProductionEvaluation match) => match.CityGuid == cityGUID));
			}
			foreach (ProductionEvaluation productionEvaluation in productionEvaluations.FindAll(match2))
			{
				text += string.Format("\n        <size=11><b>{0}</b> by {1} (base interest was {2})</size>", productionEvaluation.ProductionFinalScore.ToString("F"), productionEvaluation.LayerTag, evaluableMessage_Wonder.Interest.Value.ToString("F"));
			}
			Color color = (evaluableMessage_Wonder.State != BlackboardMessage.StateValue.Message_InProgress) ? ((evaluableMessage_Wonder.State != BlackboardMessage.StateValue.Message_Success) ? Color.gray : Color.green) : GUI.color;
			Amplitude.Unity.UI.GUILayout.Label(text, color, new GUILayoutOption[0]);
		}
	}

	private void DisplayCityState()
	{
		if (this.aiEntityCity.AICityState == null)
		{
			UnityEngine.GUILayout.Label("<color=red>No city state</color>", new GUILayoutOption[0]);
			return;
		}
		UnityEngine.GUILayout.Label(string.Format("<b>City State : {0}</b>", this.aiEntityCity.AICityState.Name), new GUILayoutOption[0]);
		if (this.aiEntityCity.AICityState.ParameterModifierGroups == null)
		{
			return;
		}
		if (this.cityStateModifierGroups == null || this.cityStateModifierGroups.Length < this.aiEntityCity.AICityState.ParameterModifierGroups.Length)
		{
			this.cityStateModifierGroups = new bool[this.aiEntityCity.AICityState.ParameterModifierGroups.Length];
		}
		for (int i = 0; i < this.aiEntityCity.AICityState.ParameterModifierGroups.Length; i++)
		{
			ParameterModifierGroupDefinition parameterModifierGroupDefinition = this.aiEntityCity.AICityState.ParameterModifierGroups[i];
			this.cityStateModifierGroups[i] = UnityEngine.GUILayout.Toggle(this.cityStateModifierGroups[i], parameterModifierGroupDefinition.Name, new GUILayoutOption[0]);
			if (this.cityStateModifierGroups[i])
			{
				if (parameterModifierGroupDefinition.ParameterModifiers != null)
				{
					for (int j = 0; j < parameterModifierGroupDefinition.ParameterModifiers.Length; j++)
					{
						UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
						global::AIParameter.AIModifier aimodifier = parameterModifierGroupDefinition.ParameterModifiers[j];
						UnityEngine.GUILayout.Label(string.Format("<color=orange>{0}</color>", aimodifier.Name), new GUILayoutOption[]
						{
							UnityEngine.GUILayout.Width(160f)
						});
						UnityEngine.GUILayout.Label(aimodifier.Value.ToString("0.00"), new GUILayoutOption[]
						{
							UnityEngine.GUILayout.Width(50f)
						});
						UnityEngine.GUILayout.Label(this.aiEntityCity.Context.GetModifierValueUnnormalized(parameterModifierGroupDefinition.Name, aimodifier.Name).ToString("0.00"), new GUILayoutOption[]
						{
							UnityEngine.GUILayout.Width(50f)
						});
						UnityEngine.GUILayout.EndHorizontal();
					}
				}
				else
				{
					UnityEngine.GUILayout.Label("<color=red>No modifiers</color>", new GUILayoutOption[0]);
				}
			}
		}
	}

	private void DisplayCitySummary()
	{
		if (this.cityAmasAgentGroup != null)
		{
			UnityEngine.GUILayout.Label("<b>Summary</b>", new GUILayoutOption[0]);
			float weight = 0f;
			float weight2 = 0f;
			UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label(string.Format("<i>Economical Stress: </i>", new object[0]), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(160f)
			});
			this.WeightLabel(weight, new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(40f)
			});
			UnityEngine.GUILayout.Label(string.Format("<i>Military Stress: </i>", new object[0]), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(160f)
			});
			this.WeightLabel(weight2, new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(40f)
			});
			UnityEngine.GUILayout.EndHorizontal();
			if (this.aiCityData != null)
			{
				UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				UnityEngine.GUILayout.Label(string.Format("<i>Maturity count: </i>", new object[0]), new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(160f)
				});
				this.WeightLabel((float)this.aiCityData.NumberOfMaturity, new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(40f)
				});
				UnityEngine.GUILayout.Label(string.Format("<i>Maturity Average: </i>", new object[0]), new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(160f)
				});
				this.WeightLabel(this.aiCityData.MaturityAIData.GetAverageValue("Maturity"), new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(40f)
				});
				UnityEngine.GUILayout.EndHorizontal();
				for (int i = 0; i < this.aiCityData.MaturityDebugger.Count; i++)
				{
					UnityEngine.GUILayout.Label(string.Format("<b>     {0}</b>", this.aiCityData.MaturityDebugger[i]), new GUILayoutOption[0]);
				}
				UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				UnityEngine.GUILayout.Label(string.Format("<i>Expand orientation: </i>", new object[0]), new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(160f)
				});
				UnityEngine.GUILayout.Label(this.aiCityData.CurrentExtensionOrientation.ToString(), new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(40f)
				});
				UnityEngine.GUILayout.EndHorizontal();
			}
			UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			UnityEngine.GUILayout.Space(80f);
			UnityEngine.GUILayout.Label("<i>Food</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Industry</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Science</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Dust</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Empire Point</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(80f)
			});
			UnityEngine.GUILayout.EndHorizontal();
			UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label("<i>AMAS</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(80f)
			});
			this.AgentCriticityLabel(this.cityAmasAgentGroup.GetAgent(AILayer_ResourceAmas.AgentNames.PopulationReferenceTurnCount), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.cityAmasAgentGroup.GetAgent(AILayer_ResourceAmas.AgentNames.IndustryReferenceTurnCount), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.cityAmasAgentGroup.GetAgent(AILayer_ResourceAmas.AgentNames.NetCityResearch), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.cityAmasAgentGroup.GetAgent(AILayer_ResourceAmas.AgentNames.NetCityMoney), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.cityAmasAgentGroup.GetAgent(AILayer_ResourceAmas.AgentNames.NetCityEmpirePoint), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.EndHorizontal();
			AmasCityDataMessage amasCityDataMessage = this.cityBlackboard.GetMessage(this.aiLayerAmasCity.AmasCityDataMessageID) as AmasCityDataMessage;
			if (amasCityDataMessage != null)
			{
				UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				UnityEngine.GUILayout.Label("<i>Prod. Weight</i>", new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(80f)
				});
				if (amasCityDataMessage.ProductionWeights != null)
				{
					for (int j = 0; j < amasCityDataMessage.ProductionWeights.Length; j++)
					{
						this.WeightLabel(amasCityDataMessage.ProductionWeights[j], new GUILayoutOption[]
						{
							UnityEngine.GUILayout.Width(60f)
						});
					}
				}
				UnityEngine.GUILayout.EndHorizontal();
			}
		}
	}

	private void DisplayEspionageInfos()
	{
		UnityEngine.GUILayout.Label("<b>Anti spy</b>", new GUILayoutOption[0]);
		CityAntiSpyMessage cityAntiSpyMessage = (this.aiLayerCityAntiSpy.AntiSpyBlackboardMessageID == 0UL) ? null : (this.cityBlackboard.GetMessage(this.aiLayerCityAntiSpy.AntiSpyBlackboardMessageID) as CityAntiSpyMessage);
		if (cityAntiSpyMessage != null)
		{
			UnityEngine.GUILayout.Label(Amplitude.Unity.UI.GUILayout.Format("Has city been seen by another empire: {0}", new object[]
			{
				cityAntiSpyMessage.HasCityBeenSeenByAnotherEmpire
			}), new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label(Amplitude.Unity.UI.GUILayout.Format(false, Amplitude.Unity.UI.GUILayout.FloatFormat.Percent, "Spy presence probability: {0}", new object[]
			{
				cityAntiSpyMessage.SpyPresenceProbability
			}), new GUILayoutOption[0]);
		}
	}

	private void DisplayUnitProductionEvaluations()
	{
		UnityEngine.GUILayout.Label("<b>Unit production evaluations</b>", new GUILayoutOption[0]);
		this.unitProductionMessages.Clear();
		GameEntityGUID cityGUID = this.aiEntityCity.City.GUID;
		this.unitProductionMessages.AddRange(this.cityBlackboard.GetMessages<EvaluableMessageWithUnitDesign>(BlackboardLayerID.Empire, (EvaluableMessageWithUnitDesign match) => match.ChosenProductionEvaluation != null && match.ChosenProductionEvaluation.CityGuid == cityGUID));
		this.unitProductionMessages.Sort(this.unitProductionMessageComparison);
		Predicate<ProductionEvaluation> <>9__1;
		foreach (EvaluableMessageWithUnitDesign evaluableMessageWithUnitDesign in this.unitProductionMessages)
		{
			string text = string.Format("{1} <i>[{2}]</i>", evaluableMessageWithUnitDesign.Interest.Value.ToString("F"), evaluableMessageWithUnitDesign.UnitDesign, evaluableMessageWithUnitDesign.State.ToString().Replace("Message_", string.Empty));
			List<ProductionEvaluation> productionEvaluations = evaluableMessageWithUnitDesign.ProductionEvaluations;
			Predicate<ProductionEvaluation> match2;
			if ((match2 = <>9__1) == null)
			{
				match2 = (<>9__1 = ((ProductionEvaluation match) => match.CityGuid == cityGUID));
			}
			foreach (ProductionEvaluation productionEvaluation in productionEvaluations.FindAll(match2))
			{
				text += string.Format("\n        <size=11><b>{0}</b> by {1} (base interest was {2})</size>", productionEvaluation.ProductionFinalScore.ToString("F"), productionEvaluation.LayerTag, evaluableMessageWithUnitDesign.Interest.Value.ToString("F"));
			}
			Color color = (evaluableMessageWithUnitDesign.State != BlackboardMessage.StateValue.Message_InProgress) ? ((evaluableMessageWithUnitDesign.State != BlackboardMessage.StateValue.Message_Success) ? Color.gray : Color.green) : GUI.color;
			Amplitude.Unity.UI.GUILayout.Label(text, color, new GUILayoutOption[0]);
		}
	}

	private void InitializeCityDebugger(City city)
	{
		this.ReleaseCityDebugger();
		if (city == null)
		{
			return;
		}
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(city.Empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			return;
		}
		this.aiEntityCity = aiplayer_MajorEmpire.GetEntity<AIEntity_City>((AIEntity_City match) => match.City.GUID == city.GUID);
		if (this.aiEntityCity == null)
		{
			this.ReleaseCityDebugger();
			return;
		}
		if (!this.aiEntityCity.HasBeenInitialized)
		{
			this.ReleaseCityDebugger();
			return;
		}
		this.cityBlackboard = this.aiEntityCity.Blackboard;
		Diagnostics.Assert(this.cityBlackboard != null);
		this.aiLayerProduction = this.aiEntityCity.GetLayer<AILayer_Production>();
		Diagnostics.Assert(this.aiLayerProduction != null);
		this.aiLayerCityAntiSpy = this.aiEntityCity.GetLayer<AILayer_CityAntiSpy>();
		Diagnostics.Assert(this.aiLayerCityAntiSpy != null);
		this.aiLayerAmasCity = this.aiEntityCity.GetLayer<AILayer_AmasCity>();
		Diagnostics.Assert(this.aiLayerAmasCity != null);
		AIEntity_Amas entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Amas>();
		Diagnostics.Assert(entity != null);
		AILayer_ResourceAmas layer = entity.GetLayer<AILayer_ResourceAmas>();
		Diagnostics.Assert(layer != null);
		this.cityAmasAgentGroup = layer.GetCityAgentGroup(city);
		this.aiCityData = this.aiDataRepository.GetAIData<AIData_City>(city.GUID);
		this.currentDebugMode = AIDebugMode.DebugMode.City;
	}

	private void ReleaseCityDebugger()
	{
		this.aiEntityCity = null;
		this.aiLayerProduction = null;
		this.aiLayerAmasCity = null;
		this.cityBlackboard = null;
		this.aiLayerCityAntiSpy = null;
		this.cityAmasAgentGroup = null;
		this.buildingProductionMessages.Clear();
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	public IEnumerable<IAIParameterConverter<AIDebugMode.Context>> GetAIParameterConverters(StaticString aiParameterName)
	{
		if (aiParameterName == "Secondary")
		{
			yield return new AIDebugMode.Converter("Main", 0.5f);
		}
		yield break;
	}

	public IEnumerable<IAIParameter<AIDebugMode.Context>> GetAIParameters(AIDebugMode.Element element)
	{
		yield return new AIDebugMode.AIParameter("Main", element.Score);
		yield return new AIDebugMode.AIParameter("Secondary", 5f);
		yield break;
	}

	public IEnumerable<IAIPrerequisite<AIDebugMode.Context>> GetAIPrerequisites(AIDebugMode.Element element)
	{
		yield return new AIDebugMode.RandomPrerequisite();
		yield break;
	}

	private void DiplayDecisionMakerDebugger()
	{
		if (this.guiLayoutDebugger == null)
		{
			return;
		}
		this.guiLayoutDebugger.OnGUI(this.evaluationDataHistoric, true);
		if (UnityEngine.GUILayout.Button("Pass turn", new GUILayoutOption[0]))
		{
			this.score.EndTurnUpdate();
		}
		DiplomaticRelationScoreDebugger.DiplomaticRelationScoreViewer(this.score, ref this.ignoreDebugNulModifiers, true);
	}

	private void InitializeDecisionMakerDebugger()
	{
		DecisionMaker<AIDebugMode.Element, AIDebugMode.Context> decisionMaker = new DecisionMaker<AIDebugMode.Element, AIDebugMode.Context>(this, new AIDebugMode.Context());
		decisionMaker.ParameterContextModifierDelegate = ((AIDebugMode.Element A_0, StaticString A_1) => UnityEngine.Random.value);
		List<AIDebugMode.Element> list = new List<AIDebugMode.Element>();
		list.Add(new AIDebugMode.Element("My SUPER Element", 10f));
		list.Add(new AIDebugMode.Element("Element1", 5f));
		list.Add(new AIDebugMode.Element("Element2", 2f));
		list.Add(new AIDebugMode.Element("Element3", 7f));
		list.Add(new AIDebugMode.Element("Element548", 0f));
		List<DecisionResult> list2 = new List<DecisionResult>();
		decisionMaker.RegisterOutput("Main");
		DecisionMakerEvaluationData<AIDebugMode.Element, AIDebugMode.Context> item;
		decisionMaker.EvaluateDecisions(list, ref list2, out item);
		this.evaluationDataHistoric.Add(item);
		this.evaluationDataHistoric.Add(item);
		this.evaluationDataHistoric.Add(item);
		this.evaluationDataHistoric.Add(item);
		this.evaluationDataHistoric.Add(item);
		this.guiLayoutDebugger = new DecisionMakerGUILayoutDebugger<AIDebugMode.Element, AIDebugMode.Context>("Test data", true);
		this.guiLayoutDebugger.TitleStyle = this.uiTitleStyle;
		this.score = new DiplomaticRelationScore(1f, -100f, 100f);
		this.score.AddModifier(new DiplomaticRelationScoreModifierDefinition("Test", "Category1", new UnityEngine.AnimationCurve(new Keyframe[]
		{
			new Keyframe(1f, 1f),
			new Keyframe(0f, 0f)
		}), 100f, 0f, -2f, 2f, 10, 0f, 10f), 1f, 1f, null);
		this.score.AddModifier(new DiplomaticRelationScoreModifierDefinition("Test", "Category1", new UnityEngine.AnimationCurve(new Keyframe[]
		{
			new Keyframe(1f, 1f),
			new Keyframe(0f, 0f)
		}), 100f, 0f, -2f, 2f, 10, 0f, 10f), 1f, 1f, null);
		this.score.AddModifier(new DiplomaticRelationScoreModifierDefinition("Test2", "Category1", new UnityEngine.AnimationCurve(new Keyframe[]
		{
			new Keyframe(0f, 1f),
			new Keyframe(1f, 0f)
		}), 100f, 0f, -2f, 2f, 10, 0f, 10f), 2f, 1f, null);
		this.score.AddModifier(new DiplomaticRelationScoreModifierDefinition("Test3", "Category2", new UnityEngine.AnimationCurve(new Keyframe[]
		{
			new Keyframe(0f, 1f),
			new Keyframe(1f, 0f)
		}), 100f, 0f, -2f, 2f, 10, 0f, 10f), 1f, 1f, null);
	}

	private void DiplayDefaultDebugger()
	{
		this.DisplayEmpireState(new string[0]);
		UnityEngine.GUILayout.Space(10f);
		UnityEngine.GUILayout.Label(string.Format("<b>Commanders</b>", new object[0]), new GUILayoutOption[0]);
		this.displayAllCommanders = UnityEngine.GUILayout.Toggle(this.displayAllCommanders, "Display all commanders", new GUILayoutOption[0]);
		this.DisplayCommanders(this.aiLayerArmyManagement.AICommanders, (!this.displayAllCommanders) ? this.commanderWithActiveMissionsPredicate : null);
		this.DisplayCommanders(this.aiLayerArmyRecruitment.AICommanders, (!this.displayAllCommanders) ? this.commanderWithActiveMissionsPredicate : null);
		this.DisplayGlobalObjectiveMessages();
	}

	private void DisplayGlobalObjectiveMessages()
	{
		UnityEngine.GUILayout.Label("<b>Global objective messages</b>", new GUILayoutOption[0]);
		this.globalObjectiveMessages.Clear();
		this.globalObjectiveMessages.AddRange(this.empireBlackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire));
		this.globalObjectiveMessages.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
		this.globalObjectiveMessageTable.OnGUI(this.globalObjectiveMessages, float.PositiveInfinity);
	}

	private void DisplayCommanders(IList<AICommander> commanders, Predicate<AICommander> filter = null)
	{
		for (int i = 0; i < commanders.Count; i++)
		{
			AICommander aicommander = commanders[i];
			if (filter == null || filter(aicommander))
			{
				string text = string.Format("{0} <size=11>({1})</size>", aicommander.GetType().ToString().Replace("AICommander_", string.Empty), aicommander.InternalGUID);
				if (aicommander is AICommanderWithObjective)
				{
					AICommanderWithObjective aicommanderWithObjective = aicommander as AICommanderWithObjective;
					text += string.Format(",Region={0}({1})", this.GetRegionName(aicommanderWithObjective.RegionIndex), aicommanderWithObjective.RegionIndex);
				}
				if (aicommander is AICommander_Manta)
				{
					AICommander_Manta aicommander_Manta = aicommander as AICommander_Manta;
					text += string.Format(",MissionType={0}", aicommander_Manta.MantaBehavior.ToString());
					if (aicommander_Manta.MantaZone != null)
					{
						text += string.Format(",ZoneScore={0}", aicommander_Manta.MantaZone.MantaZoneScore);
						text += ",RegionIndexes=";
						aicommander_Manta.MantaZone.PrintRegions(ref text);
					}
					else
					{
						text += ",InvalidZone";
					}
				}
				Diagnostics.Assert(aicommander.Missions != null);
				int j = 0;
				while (j < aicommander.Missions.Count)
				{
					AICommanderMission aicommanderMission = aicommander.Missions[j];
					AIData_Army aidata_Army = (this.aiDataRepository == null) ? null : this.aiDataRepository.GetAIData<AIData_Army>(aicommanderMission.AIDataArmyGUID);
					string text2 = "white";
					switch (aicommanderMission.Completion)
					{
					case AICommanderMission.AICommanderMissionCompletion.Success:
						text2 = "green";
						break;
					case AICommanderMission.AICommanderMissionCompletion.Fail:
						text2 = "red";
						break;
					case AICommanderMission.AICommanderMissionCompletion.Interrupted:
						goto IL_2F9;
					case AICommanderMission.AICommanderMissionCompletion.Cancelled:
						text2 = "grey";
						break;
					default:
						goto IL_2F9;
					}
					IL_19E:
					string text3 = string.Empty;
					if (aidata_Army != null)
					{
						text3 = string.Format(" Army={0}({1})", aidata_Army.Army.LocalizedName, aidata_Army.Army.GUID);
					}
					string text4 = (aidata_Army == null || aidata_Army.ArmyMission == null) ? string.Empty : (" Error=" + aidata_Army.ArmyMission.ErrorCode.ToString());
					string text5 = (aidata_Army == null || aidata_Army.ArmyMission == null) ? string.Empty : (" Debug=" + aidata_Army.ArmyMission.LastDebugString + " Node=" + aidata_Army.ArmyMission.LastNodeName);
					text += string.Format("\n        <color={7}><size=11><b>{0}</b> ({1}) Priority={2} - {3} - {6} {4}{5}{8} - {9}</size></color>", new object[]
					{
						aicommanderMission.GetType().ToString().Replace("AICommanderMission_", string.Empty),
						aicommanderMission.InternalGUID,
						aicommander.GetPriority(aicommanderMission).ToString("0.000"),
						aicommanderMission.Completion,
						text3,
						(!aicommanderMission.IsActive) ? " </color=red>Not active</color>" : string.Empty,
						aicommanderMission.State,
						text2,
						text4,
						text5
					});
					j++;
					continue;
					IL_2F9:
					if (aicommanderMission.State == TickableState.NeedTick)
					{
						text2 = "yellow";
						goto IL_19E;
					}
					goto IL_19E;
				}
				UnityEngine.GUILayout.Label(text, new GUILayoutOption[0]);
			}
		}
	}

	private void DisplayEmpireState(params string[] filterGroups)
	{
		if (this.aiLayerStrategy == null)
		{
			return;
		}
		AIStrategicPlanDefinition currentStrategicPlan = this.aiLayerStrategy.GetCurrentStrategicPlan();
		if (currentStrategicPlan == null)
		{
			UnityEngine.GUILayout.Label("<color=red>No empire state</color>", new GUILayoutOption[0]);
			return;
		}
		UnityEngine.GUILayout.Label(string.Format("<b>Empire State : {0}</b>", currentStrategicPlan.Name), new GUILayoutOption[0]);
		if (currentStrategicPlan.ParameterModifierGroups == null)
		{
			return;
		}
		if (this.empireStateModifierGroups == null || this.empireStateModifierGroups.Length < currentStrategicPlan.ParameterModifierGroups.Length)
		{
			this.empireStateModifierGroups = new bool[currentStrategicPlan.ParameterModifierGroups.Length];
		}
		for (int i = 0; i < currentStrategicPlan.ParameterModifierGroups.Length; i++)
		{
			ParameterModifierGroupDefinition parameterModifierGroupDefinition = currentStrategicPlan.ParameterModifierGroups[i];
			Diagnostics.Assert(parameterModifierGroupDefinition != null);
			if (filterGroups == null || filterGroups.Length == 0 || Array.Exists<string>(filterGroups, (string match) => match == parameterModifierGroupDefinition.Name))
			{
				this.empireStateModifierGroups[i] = UnityEngine.GUILayout.Toggle(this.empireStateModifierGroups[i], parameterModifierGroupDefinition.Name, new GUILayoutOption[0]);
				if (this.empireStateModifierGroups[i])
				{
					if (parameterModifierGroupDefinition.ParameterModifiers != null)
					{
						for (int j = 0; j < parameterModifierGroupDefinition.ParameterModifiers.Length; j++)
						{
							global::AIParameter.AIModifier aimodifier = parameterModifierGroupDefinition.ParameterModifiers[j];
							AIContextModifier modifier = this.aiEntityContext.GetModifier(parameterModifierGroupDefinition.Name, aimodifier.Name);
							float value = aimodifier.Value;
							float value2 = modifier.Value;
							string text = string.Format("<color=orange>{0}</color>", aimodifier.Name);
							UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
							modifier.DebuggerFoldout = UnityEngine.GUILayout.Toggle(modifier.DebuggerFoldout, text, new GUILayoutOption[]
							{
								UnityEngine.GUILayout.Width(200f)
							});
							UnityEngine.GUILayout.Label(value.ToString("#0.##"), new GUILayoutOption[]
							{
								UnityEngine.GUILayout.Width(50f)
							});
							UnityEngine.GUILayout.Label(value2.ToString("#0.##"), new GUILayoutOption[]
							{
								UnityEngine.GUILayout.Width(50f)
							});
							UnityEngine.GUILayout.EndHorizontal();
							if (modifier.DebuggerFoldout)
							{
								for (int k = 0; k < modifier.Boosts.Count; k++)
								{
									UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
									UnityEngine.GUILayout.Label(string.Format("     {0}", modifier.Boosts[k].LayerName), new GUILayoutOption[]
									{
										UnityEngine.GUILayout.Width(200f)
									});
									UnityEngine.GUILayout.Label(modifier.Boosts[k].Value.ToString("#0.##"), new GUILayoutOption[]
									{
										UnityEngine.GUILayout.Width(50f)
									});
									UnityEngine.GUILayout.Label(string.Format("timeout({0})", modifier.Boosts[k].Timeout.ToString("#0.##")), new GUILayoutOption[0]);
									UnityEngine.GUILayout.EndHorizontal();
								}
							}
						}
					}
					else
					{
						UnityEngine.GUILayout.Label("<color=red>No modifiers</color>", new GUILayoutOption[0]);
					}
				}
			}
		}
	}

	private void InitializeDefaultDebugger()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogWarning("[AIDebugMode] Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
			this.ReleaseDefaultDebugger();
			return;
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			Diagnostics.LogError("[AIDebugMode] Failed to retrieve ai entity empire.");
			this.ReleaseDefaultDebugger();
			return;
		}
		this.aiEntityContext = entity.Context;
		this.empireBlackboard = entity.Blackboard;
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.aiLayerAmasEmpire = entity.GetLayer<AILayer_AmasEmpire>();
		Diagnostics.Assert(this.aiLayerAmasEmpire != null);
		AIEntity_Amas entity2 = aiplayer_MajorEmpire.GetEntity<AIEntity_Amas>();
		Diagnostics.Assert(entity2 != null);
		AILayer_ResourceAmas layer = entity2.GetLayer<AILayer_ResourceAmas>();
		Diagnostics.Assert(layer != null);
		this.resourceAmas = layer.Amas;
		this.aiLayerStrategy = entity.GetLayer<AILayer_Strategy>();
		Diagnostics.Assert(this.aiLayerStrategy != null);
		this.aiLayerArmyManagement = entity.GetLayer<AILayer_ArmyManagement>();
		Diagnostics.Assert(this.aiLayerArmyManagement != null);
		this.aiLayerArmyRecruitment = entity.GetLayer<AILayer_ArmyRecruitment>();
		Diagnostics.Assert(this.aiLayerArmyRecruitment != null);
		this.globalObjectiveMessageTable = new Amplitude.Unity.UI.GUILayout.Table<GlobalObjectiveMessage>(new Action<ITableContentManager, GlobalObjectiveMessage, bool>(this.GlobalObjectiveMessageDisplayer), false, true);
		this.globalObjectiveMessageTable.AddColumn("<b>ID</b>", 30f);
		this.globalObjectiveMessageTable.AddColumn("<b>TYPE</b>", 120f);
		this.globalObjectiveMessageTable.AddColumn("<b>STATE</b>", 90f);
		this.globalObjectiveMessageTable.AddColumn("<b>REGION</b>", 90f);
		this.globalObjectiveMessageTable.AddColumn("<b>INTEREST</b>", 75f);
		this.globalObjectiveMessageTable.AddColumn("<b>OTHER DATA</b>", 400f);
		this.currentDebugMode = AIDebugMode.DebugMode.Default;
	}

	private void GlobalObjectiveMessageDisplayer(ITableContentManager tableContentManager, GlobalObjectiveMessage message, bool isSelected)
	{
		string text = message.ObjectiveType;
		string text2 = message.State.ToString().Replace("Message_", string.Empty);
		string regionName = this.GetRegionName(message.RegionIndex);
		tableContentManager.AddRow(new string[]
		{
			message.ID.ToString(),
			text,
			text2,
			regionName,
			message.Interest.ToString("0.000"),
			string.Format("LocalScore: {0} - GlobalScore: {1} {2}", message.LocalPriority.Value.ToString("0.00"), message.GlobalPriority.Value.ToString("0.00"), (!string.IsNullOrEmpty(message.ObjectiveState)) ? ("- ObjectiveState: " + message.ObjectiveState) : string.Empty)
		});
	}

	private void ReleaseDefaultDebugger()
	{
		this.empireBlackboard = null;
		this.resourceAmas = null;
		this.aiLayerAmasEmpire = null;
		this.aiLayerStrategy = null;
		this.aiLayerArmyManagement = null;
		this.aiLayerArmyRecruitment = null;
		this.aiEntityContext = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private void DiplayDiplomacyDebugger()
	{
		if (this.aiLayerDiplomacy == null)
		{
			return;
		}
		this.DisplayWantedDiplomaticState();
		UnityEngine.GUILayout.Space(10f);
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Assert(this.diplomacyDecisionMakerDebugger != null);
			this.diplomacyDecisionMakerDebugger.OnGUI(this.aiLayerDiplomacy.DebugEvaluationsHistoric, true);
		}
	}

	private void DisplayWantedDiplomaticState()
	{
		UnityEngine.GUILayout.Label("<b>Summary</b>", new GUILayoutOption[0]);
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Empire</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(100f)
		});
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = this.game.Empires[i] as MajorEmpire;
			if (majorEmpire != null)
			{
				UnityEngine.GUILayout.Label(majorEmpire.Index.ToString(), new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(50f)
				});
			}
		}
		UnityEngine.GUILayout.EndHorizontal();
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Wanted state</i>\n<i>War status</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(80f)
		});
		for (int j = 0; j < this.game.Empires.Length; j++)
		{
			MajorEmpire empire = this.game.Empires[j] as MajorEmpire;
			if (empire != null)
			{
				WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage = this.diplomacyBlackboard.FindFirst<WantedDiplomaticRelationStateMessage>(BlackboardLayerID.Empire, (WantedDiplomaticRelationStateMessage match) => match.OpponentEmpireIndex == empire.Index);
				if (wantedDiplomaticRelationStateMessage == null)
				{
					UnityEngine.GUILayout.Label("-", new GUILayoutOption[]
					{
						UnityEngine.GUILayout.Width(50f)
					});
				}
				else
				{
					UnityEngine.GUILayout.Label(string.Format("{0}\n{1}", wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName.ToString().Replace("DiplomaticRelationState", string.Empty), wantedDiplomaticRelationStateMessage.CurrentWarStatusType), new GUILayoutOption[]
					{
						UnityEngine.GUILayout.Width(50f)
					});
				}
			}
		}
		UnityEngine.GUILayout.EndHorizontal();
	}

	private void InitializeDiplomacyDebugger()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			Diagnostics.LogError("Failed to retrieve ai entity empire.");
			this.ReleaseDiplomacyDebugger();
		}
		this.diplomacyBlackboard = entity.Blackboard;
		Diagnostics.Assert(this.diplomacyBlackboard != null);
		this.aiLayerDiplomacy = entity.GetLayer<AILayer_Diplomacy>();
		Diagnostics.Assert(this.aiLayerDiplomacy != null);
		this.currentDebugMode = AIDebugMode.DebugMode.Diplomacy;
	}

	private void ReleaseDiplomacyDebugger()
	{
		this.diplomacyBlackboard = null;
		this.aiLayerDiplomacy = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private void DiplayEmpireDebugger()
	{
		this.DisplayEmpireSummary();
		UnityEngine.GUILayout.Space(10f);
		if (this.isEmpirePlanPanelVisible)
		{
			this.DisplayEmpireState(new string[]
			{
				"EmpirePlan"
			});
			UnityEngine.GUILayout.Space(10f);
			this.DisplayWantedEmpirePlan();
			UnityEngine.GUILayout.Space(10f);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Assert(this.empirePlanDecisionMakerDebugger != null);
				this.empirePlanDecisionMakerDebugger.OnGUI(this.aiLayerEmpirePlan.DecisionMakerEvaluationDataHistoric, true);
				return;
			}
		}
		else
		{
			this.DisplayEmpireEvaluableMessages();
		}
	}

	private void DisplayEmpireEvaluableMessages()
	{
		UnityEngine.GUILayout.Label("<b>Evaluable messages</b>", new GUILayoutOption[0]);
		this.evaluableMessages.Clear();
		this.evaluableMessages.AddRange(this.empireBlackboard.GetMessages<EvaluableMessage>(BlackboardLayerID.Empire));
		this.evaluableMessages.Sort((EvaluableMessage left, EvaluableMessage right) => -1 * left.Interest.CompareTo(right.Interest));
		this.empirePointEvaluableMessages.Clear();
		this.moneyEvaluableMessages.Clear();
		this.orbEvaluableMessages.Clear();
		for (int i = 0; i < this.evaluableMessages.Count; i++)
		{
			StaticString accountTagResource = this.aiLayerAccountManager.GetAccountTagResource(this.evaluableMessages[i].AccountTag);
			if (accountTagResource == DepartmentOfTheTreasury.Resources.EmpirePoint)
			{
				this.empirePointEvaluableMessages.Add(this.evaluableMessages[i]);
			}
			else if (accountTagResource == DepartmentOfTheTreasury.Resources.EmpireMoney)
			{
				this.moneyEvaluableMessages.Add(this.evaluableMessages[i]);
			}
			else if (accountTagResource == DepartmentOfTheTreasury.Resources.Orb)
			{
				this.orbEvaluableMessages.Add(this.evaluableMessages[i]);
			}
		}
		UnityEngine.GUILayout.Label("<b>Empire Point</b>", new GUILayoutOption[0]);
		this.empirePointEvaluableMessageTable.OnGUI(this.empirePointEvaluableMessages, float.PositiveInfinity);
		UnityEngine.GUILayout.Label("<b>Money</b>", new GUILayoutOption[0]);
		this.moneyEvaluableMessageTable.OnGUI(this.moneyEvaluableMessages, float.PositiveInfinity);
		UnityEngine.GUILayout.Label("<b>Orbs</b>", new GUILayoutOption[0]);
		this.orbEvaluableMessageTable.OnGUI(this.orbEvaluableMessages, float.PositiveInfinity);
	}

	private void DisplayEmpireSummary()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		if (this.aiLayerAccountManager == null || empire == null)
		{
			return;
		}
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Account tag</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(130f)
		});
		UnityEngine.GUILayout.Label("<i>Balance</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(60f)
		});
		UnityEngine.GUILayout.Label("<i>Net</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(60f)
		});
		UnityEngine.GUILayout.Label("<i>Percent</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(60f)
		});
		UnityEngine.GUILayout.Label("<i>Max</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(60f)
		});
		UnityEngine.GUILayout.Label("<i>Promised</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(60f)
		});
		UnityEngine.GUILayout.Label("<i>Wanted</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(60f)
		});
		UnityEngine.GUILayout.Label("<i>Deficit prevention</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(150f)
		});
		UnityEngine.GUILayout.EndHorizontal();
		foreach (Account account in this.aiLayerAccountManager.Debug_Accounts)
		{
			UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label(account.AccountTag, new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(130f)
			});
			UnityEngine.GUILayout.Label(string.Format("<b>{0}</b>", account.EstimatedBalance.ToString("0.0")), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label(string.Format("{0}", (account.EstimatedNetOutcome * account.CurrentProfitPercent).ToString("0.0")), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label(string.Format("{0}%", (account.CurrentProfitPercent * 100f).ToString("0")), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label((account.MaxAccount >= 0f) ? account.MaxAccount.ToString("0.0") : string.Empty, new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label(account.PromisedAmount.ToString("0.0"), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label(account.WantedAmount.ToString("0.0"), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label(account.GetBailiffPreventionSaving().ToString("0.0"), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(150f)
			});
			UnityEngine.GUILayout.EndHorizontal();
		}
		UnityEngine.GUILayout.Space(10f);
		float num = 0f;
		float num2 = 0f;
		foreach (City city in empire.GetAgency<DepartmentOfTheInterior>().Cities)
		{
			num += city.GetPropertyValue(SimulationProperties.DustPopulation);
			num2 += city.GetPropertyValue(SimulationProperties.CityPointPopulation);
		}
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Money</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(100f)
		});
		UnityEngine.GUILayout.Label("Global stress", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(100f)
		});
		this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.NetEmpireMoney), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.Label("Wanted stress", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(120f)
		});
		this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.EconomyAccountNeedMoneyRatio), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.Label("Reference stress", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(120f)
		});
		this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.MoneyReferenceRatio), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.Label("Population assigned", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(120f)
		});
		UnityEngine.GUILayout.Label(num.ToString("0"), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.EndHorizontal();
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Empire Point</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(100f)
		});
		UnityEngine.GUILayout.Label("Global stress", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(100f)
		});
		this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.NetEmpirePrestige), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.Label("Empire plan stress", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(120f)
		});
		this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.EmpirePlanNeedPrestigeRatio), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.Label("Diplomacy stress", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(120f)
		});
		this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.DiplomacyNeedPrestigeRatio), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.Label("Population assigned", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(120f)
		});
		UnityEngine.GUILayout.Label(num2.ToString("0"), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.EndHorizontal();
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label(string.Format("Global economic stress: {0}", this.aiLayerEconomy.GlobalEconomicStressPondered.ToString("0.000")), new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label(string.Format("Global military stress: {0}", this.aiLayerEconomy.GlobalMilitaryStressPondered.ToString("0.000")), new GUILayoutOption[0]);
		UnityEngine.GUILayout.EndHorizontal();
	}

	private void DisplayWantedEmpirePlan()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		if (empire == null)
		{
			return;
		}
		DepartmentOfPlanificationAndDevelopment agency = empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		float registryValue = AIScheduler.Services.GetService<IPersonalityAIHelper>().GetRegistryValue<float>(empire as global::Empire, "AI/MajorEmpire/AIEntity_Empire/AILayer_EmpirePlan/MaximumPopulationPercentToReachObjective", 0f);
		UnityEngine.GUILayout.Label(string.Format("<b>Wanted empire plan</b> <size=11>(Maximum population percent to reach objective: {0}%)</size>", registryValue * 100f), new GUILayoutOption[0]);
		Diagnostics.Assert(this.empirePlanMessages != null);
		this.empirePlanMessages.Clear();
		this.empirePlanMessages.AddRange(this.empireBlackboard.GetMessages<EvaluableMessage_EmpirePlan>(BlackboardLayerID.Empire, (EvaluableMessage_EmpirePlan match) => match.State == BlackboardMessage.StateValue.Message_InProgress));
		this.empirePlanMessages.Sort((EvaluableMessage_EmpirePlan left, EvaluableMessage_EmpirePlan right) => -1 * left.Interest.CompareTo(right.Interest));
		float num = 0f;
		for (int i = 0; i < this.empirePlanMessages.Count; i++)
		{
			EvaluableMessage_EmpirePlan evaluableMessage_EmpirePlan = this.empirePlanMessages[i];
			UnityEngine.GUILayout.Label(string.Format("<b>{0}</b> {1} Level {2}", evaluableMessage_EmpirePlan.Interest.Value.ToString("0.00"), evaluableMessage_EmpirePlan.EmpirePlanClass, evaluableMessage_EmpirePlan.EmpirePlanLevel), new GUILayoutOption[0]);
			EmpirePlanDefinition empirePlanDefinition = agency.GetEmpirePlanDefinition(evaluableMessage_EmpirePlan.EmpirePlanClass, evaluableMessage_EmpirePlan.EmpirePlanLevel);
			num += DepartmentOfTheTreasury.GetProductionCostWithBonus(empire.SimulationObject, empirePlanDefinition, DepartmentOfTheTreasury.Resources.EmpirePoint);
		}
		UnityEngine.GUILayout.Space(5f);
		UnityEngine.GUILayout.Label(string.Format("<i>Empire point cost:</i> <b>{0}</b>", num.ToString("0.0")), new GUILayoutOption[0]);
	}

	private void DisplayAIStrategicPlan()
	{
	}

	private void EvaluableMessageDisplayer(ITableContentManager tableContentManager, EvaluableMessage message, bool isMessageSelected)
	{
		string text = message.GetType().ToString();
		text = text.Substring(17, Mathf.Min(20, text.Length - 17));
		string text2 = (message.EvaluationState != EvaluableMessage.EvaluableMessageState.Pending_MissingResource) ? message.EvaluationState.ToString() : "MissingResource";
		string text3 = message.AccountTag.ToString().Replace("Account", string.Empty);
		tableContentManager.AddRow(new string[]
		{
			message.ID.ToString(),
			text,
			text2,
			text3,
			message.Interest.Value.ToString("0.000"),
			message.Summary(this.empire, this.empireBlackboard)
		});
		if (!isMessageSelected)
		{
			if (message.ChosenProductionEvaluation != null)
			{
				string text4 = "yellow";
				tableContentManager.AddRow(new string[]
				{
					string.Empty,
					string.Format("<color={0}>Production eval</color>", text4),
					string.Format("<color={1}>{0}</color>", message.ChosenProductionEvaluation.State, text4),
					string.Format("<color={1}>{0}</color>", message.ChosenProductionEvaluation.LayerTag, text4),
					string.Format("<color={1}>{0}</color>", message.ChosenProductionEvaluation.ProductionFinalScore, text4),
					string.Format("<color={4}>Cost: {0} ({1} turns) City: {2} Eco stress: {3}</color>", new object[]
					{
						message.ChosenProductionEvaluation.ProductionCost,
						message.ChosenProductionEvaluation.ProductionDurationInTurn,
						message.ChosenProductionEvaluation.CityGuid,
						message.ChosenProductionEvaluation.CityEconomicalStress,
						text4
					})
				});
			}
			if (message.ChosenBuyEvaluation != null)
			{
				string text5 = "yellow";
				tableContentManager.AddRow(new string[]
				{
					string.Empty,
					string.Format("<color={0}>Buyout eval</color>", text5),
					string.Format("<color={1}>{0}</color>", message.ChosenBuyEvaluation.State, text5),
					string.Format("<color={1}>{0}</color>", message.ChosenBuyEvaluation.LayerTag, text5),
					string.Format("<color={1}>{0}</color>", message.ChosenBuyEvaluation.BuyoutFinalScore, text5),
					string.Format("<color={3}>Cost: {0} Turn gain: {1} City: {2}</color>", new object[]
					{
						message.ChosenBuyEvaluation.DustCost,
						message.ChosenBuyEvaluation.TurnGain,
						message.ChosenBuyEvaluation.CityGuid,
						text5
					})
				});
			}
			return;
		}
		if (message.ProductionEvaluations != null && message.ProductionEvaluations.Count > 0)
		{
			for (int i = 0; i < message.ProductionEvaluations.Count; i++)
			{
				ProductionEvaluation productionEvaluation = message.ProductionEvaluations[i];
				string text6 = (productionEvaluation != message.ChosenProductionEvaluation) ? "silver" : "yellow";
				tableContentManager.AddRow(new string[]
				{
					string.Empty,
					string.Format("<color={0}>Production eval</color>", text6),
					string.Format("<color={1}>{0}</color>", productionEvaluation.State, text6),
					string.Format("<color={1}>{0}</color>", productionEvaluation.LayerTag, text6),
					string.Format("<color={1}>{0}</color>", productionEvaluation.ProductionFinalScore, text6),
					string.Format("<color={4}>Cost: {0} ({1} turns) City: {2} Eco stress: {3}</color>", new object[]
					{
						productionEvaluation.ProductionCost,
						productionEvaluation.ProductionDurationInTurn,
						productionEvaluation.CityGuid,
						productionEvaluation.CityEconomicalStress,
						text6
					})
				});
			}
		}
		else
		{
			tableContentManager.AddRow(new string[]
			{
				string.Empty,
				"<color=silver>No prod. eval</color>"
			});
		}
		if (message.BuyEvaluations != null && message.BuyEvaluations.Count > 0)
		{
			for (int j = 0; j < message.BuyEvaluations.Count; j++)
			{
				BuyEvaluation buyEvaluation = message.BuyEvaluations[j];
				string text7 = (buyEvaluation != message.ChosenBuyEvaluation) ? "silver" : "yellow";
				tableContentManager.AddRow(new string[]
				{
					string.Empty,
					string.Format("<color={0}>Buyout eval</color>", text7),
					string.Format("<color={1}>{0}</color>", buyEvaluation.State, text7),
					string.Format("<color={1}>{0}</color>", buyEvaluation.LayerTag, text7),
					string.Format("<color={1}>{0}</color>", buyEvaluation.BuyoutFinalScore, text7),
					string.Format("<color={3}>Cost: {0} Turn gain: {1} City: {2}</color>", new object[]
					{
						buyEvaluation.DustCost,
						buyEvaluation.TurnGain,
						buyEvaluation.CityGuid,
						text7
					})
				});
			}
			return;
		}
		tableContentManager.AddRow(new string[]
		{
			string.Empty,
			"<color=silver>No buyout eval</color>"
		});
	}

	private void InitializeEmpireDebugger()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			Diagnostics.LogError("Failed to retrieve ai entity empire.");
			this.ReleaseDefaultDebugger();
		}
		this.empire = entity.Empire;
		this.empireBlackboard = entity.Blackboard;
		Diagnostics.Assert(this.empireBlackboard != null);
		this.aiLayerEmpirePlan = entity.GetLayer<AILayer_EmpirePlan>();
		Diagnostics.Assert(this.aiLayerEmpirePlan != null);
		this.aiLayerEconomy = entity.GetLayer<AILayer_Economy>();
		Diagnostics.Assert(this.aiLayerEconomy != null);
		this.aiLayerAccountManager = entity.GetLayer<AILayer_AccountManager>();
		Diagnostics.Assert(this.aiLayerAccountManager != null);
		AIEntity_Amas entity2 = aiplayer_MajorEmpire.GetEntity<AIEntity_Amas>();
		Diagnostics.Assert(entity2 != null);
		AILayer_ResourceAmas layer = entity2.GetLayer<AILayer_ResourceAmas>();
		Diagnostics.Assert(layer != null);
		this.resourceAmas = layer.Amas;
		this.currentDebugMode = AIDebugMode.DebugMode.Empire;
		this.moneyEvaluableMessageTable = this.InitializeTable();
		this.empirePointEvaluableMessageTable = this.InitializeTable();
		this.orbEvaluableMessageTable = this.InitializeTable();
	}

	private void ReleaseEmpireDebugger()
	{
		this.empireBlackboard = null;
		this.resourceAmas = null;
		this.aiLayerEmpirePlan = null;
		this.aiLayerEconomy = null;
		this.aiLayerAccountManager = null;
		this.empire = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private Amplitude.Unity.UI.GUILayout.Table<EvaluableMessage> InitializeTable()
	{
		Amplitude.Unity.UI.GUILayout.Table<EvaluableMessage> table = new Amplitude.Unity.UI.GUILayout.Table<EvaluableMessage>(new Action<ITableContentManager, EvaluableMessage, bool>(this.EvaluableMessageDisplayer), false, true);
		table.AddColumn("<b>ID</b>", 30f);
		table.AddColumn("<b>TYPE</b>", 140f);
		table.AddColumn("<b>STATE</b>", 105f);
		table.AddColumn("<b>ACCOUNT</b>", 80f);
		table.AddColumn("<b>INTEREST</b>", 75f);
		table.AddColumn("<b>SUMMARY</b>", float.PositiveInfinity);
		return table;
	}

	private void DiplayEncounterDebugger()
	{
		if (this.activePlayerAILayerEncounter != null)
		{
			UnityEngine.GUILayout.Label(string.Format("AILayer Encounter {0}", (!this.activePlayerAILayerEncounter.IsActive()) ? "Off" : "On"), new GUILayoutOption[0]);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Assert(this.encounterStrategyDecisionMakerDebugger != null);
				this.encounterStrategyDecisionMakerDebugger.OnGUI(this.activePlayerAILayerEncounter.DecisionMakerEvaluationDataHistoric, true);
			}
		}
		UnityEngine.GUILayout.Space(10f);
		Diagnostics.Assert(this.selectedWorldBattleUnitReference != null);
		if (this.selectedWorldBattleUnitReference.Target != null)
		{
			UnityEngine.GUILayout.Label(string.Format("<b>Selected unit</b>", new object[0]), new GUILayoutOption[0]);
			this.DisplayWorldBattleUnit(this.selectedWorldBattleUnitReference.Target as WorldBattleUnit);
		}
		Diagnostics.Assert(this.highlightedWorldBattleUnitReference != null);
		if (this.highlightedWorldBattleUnitReference.Target != null)
		{
			UnityEngine.GUILayout.Label(string.Format("<b>Highlighted unit</b>", new object[0]), new GUILayoutOption[0]);
			this.DisplayWorldBattleUnit(this.highlightedWorldBattleUnitReference.Target as WorldBattleUnit);
		}
	}

	private void DisplayWorldBattleUnit(WorldBattleUnit target)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		UnityEngine.GUILayout.Label(string.Format("<b>Unit {0}</b> - {1}", target.Unit.GUID, target.Unit.UnitDesign.ToString()), new GUILayoutOption[0]);
		EncounterUnit encounterUnit = target.EncounterUnit;
		Diagnostics.Assert(encounterUnit != null);
		Diagnostics.Assert(this.aiLayerEncounterByContenderGuid != null);
		AILayer_Encounter ailayer_Encounter = (!this.aiLayerEncounterByContenderGuid.ContainsKey(encounterUnit.Contender.GUID)) ? null : this.aiLayerEncounterByContenderGuid[encounterUnit.Contender.GUID];
		if (ailayer_Encounter == null)
		{
			UnityEngine.GUILayout.Label(string.Format("No analysis data found for contender {0}.", encounterUnit.Contender.GUID), new GUILayoutOption[0]);
			return;
		}
		if (!Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			return;
		}
		AILayer_Encounter.AIEncounterBattleGroundAnalysis aiencounterBattleGroundAnalysis = ailayer_Encounter.Debug_LastEncounterAnalysis.FirstOrDefault((AILayer_Encounter.AIEncounterBattleGroundAnalysis match) => match.ContenderGUID == encounterUnit.Contender.GUID);
		if (aiencounterBattleGroundAnalysis == null)
		{
			UnityEngine.GUILayout.Label(string.Format("No analysis data found for contender {0}.", encounterUnit.Contender.GUID), new GUILayoutOption[0]);
			return;
		}
		AILayer_Encounter.AIEncounterUnitAnalysis aiencounterUnitAnalysis = aiencounterBattleGroundAnalysis.AllyUnitPlayingNextBattleRound.FirstOrDefault((AILayer_Encounter.AIEncounterUnitAnalysis match) => match.UnitGUID == target.Unit.GUID);
		if (aiencounterUnitAnalysis == null)
		{
			UnityEngine.GUILayout.Label(string.Format("No analysis data found for unit {0}.", target.Unit.GUID), new GUILayoutOption[0]);
			return;
		}
		UnityEngine.GUILayout.Label(string.Format("Unit pattern: <i>{0}</i> <size=11>from category {1}</size>", aiencounterUnitAnalysis.UnitPattern, aiencounterUnitAnalysis.UnitPatternCategory), new GUILayoutOption[0]);
	}

	private AILayer_Encounter GetAILayerEncounter(MajorEmpire empire)
	{
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire, out aiplayer_MajorEmpire))
		{
			return null;
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			return null;
		}
		return entity.GetLayer<AILayer_Encounter>();
	}

	private void InitializeEncounterDebugger(Encounter encounter)
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			Diagnostics.LogError("Failed to retrieve ai entity empire.");
			this.ReleaseDefaultDebugger();
		}
		this.activePlayerAILayerEncounter = entity.GetLayer<AILayer_Encounter>();
		Diagnostics.Assert(this.activePlayerAILayerEncounter != null);
		Diagnostics.Assert(this.aiLayerEncounterByContenderGuid != null);
		this.aiLayerEncounterByContenderGuid.Clear();
		for (int i = 0; i < encounter.Contenders.Count; i++)
		{
			Contender contender = encounter.Contenders[i];
			MajorEmpire majorEmpire = contender.Empire as MajorEmpire;
			if (majorEmpire != null)
			{
				AILayer_Encounter ailayerEncounter = this.GetAILayerEncounter(majorEmpire);
				if (ailayerEncounter != null)
				{
					this.aiLayerEncounterByContenderGuid.Add(contender.GUID, ailayerEncounter);
				}
			}
		}
		this.currentDebugMode = AIDebugMode.DebugMode.Encounter;
	}

	private void ReleaseEncounterDebugger()
	{
		this.activePlayerAILayerEncounter = null;
		this.aiLayerEncounterByContenderGuid.Clear();
		this.selectedWorldBattleUnitReference.Target = null;
		this.highlightedWorldBattleUnitReference.Target = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private void SetHighlightedWorldBattleUnit(WorldBattleUnit selectedWorldBattleUnit, WorldBattleUnit highlightedWorldBattleUnit)
	{
		Diagnostics.Assert(this.selectedWorldBattleUnitReference != null);
		this.selectedWorldBattleUnitReference.Target = selectedWorldBattleUnit;
		Diagnostics.Assert(this.highlightedWorldBattleUnitReference != null);
		this.highlightedWorldBattleUnitReference.Target = highlightedWorldBattleUnit;
	}

	private void UpdateEncounterDebugger()
	{
		WorldBattleUnit selectedWorldBattleUnit = null;
		WorldBattleUnit highlightedWorldBattleUnit = null;
		for (int i = 0; i < this.cursorTargetService.SelectedCursorTargets.Count; i++)
		{
			WorldBattleUnitCursorTarget worldBattleUnitCursorTarget = this.cursorTargetService.SelectedCursorTargets[i] as WorldBattleUnitCursorTarget;
			if (worldBattleUnitCursorTarget != null)
			{
				selectedWorldBattleUnit = worldBattleUnitCursorTarget.WorldBattleUnit;
				break;
			}
		}
		EncounterTargetingPhaseWorldCursor encounterTargetingPhaseWorldCursor = this.cursorService.CurrentCursor as EncounterTargetingPhaseWorldCursor;
		if (encounterTargetingPhaseWorldCursor != null && encounterTargetingPhaseWorldCursor.HighlightedBattleUnit != null)
		{
			highlightedWorldBattleUnit = encounterTargetingPhaseWorldCursor.HighlightedBattleUnit.WorldBattleUnit;
		}
		EncounterDeploymentWorldCursor encounterDeploymentWorldCursor = this.cursorService.CurrentCursor as EncounterDeploymentWorldCursor;
		if (encounterDeploymentWorldCursor != null && encounterDeploymentWorldCursor.HighlightedBattleUnit != null)
		{
			highlightedWorldBattleUnit = encounterDeploymentWorldCursor.HighlightedBattleUnit.WorldBattleUnit;
		}
		this.SetHighlightedWorldBattleUnit(selectedWorldBattleUnit, highlightedWorldBattleUnit);
	}

	private void DiplayEspionageDebugger()
	{
		UnityEngine.GUILayout.Label("<b>Summary</b>", new GUILayoutOption[0]);
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label(Amplitude.Unity.UI.GUILayout.Format("ThriftyFactor: {0}", new object[]
		{
			this.aiLayerEmpireAntiSpy.ThriftyFactor
		}), new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label(Amplitude.Unity.UI.GUILayout.Format("CarefulnessFactor: {0}", new object[]
		{
			this.aiLayerEmpireAntiSpy.CarefulnessFactor
		}), new GUILayoutOption[0]);
		UnityEngine.GUILayout.EndHorizontal();
		UnityEngine.GUILayout.Space(10f);
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Global round up desire:</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(150f)
		});
		this.WeightLabel(this.aiLayerEmpireAntiSpy.GlobalRoundUpDesire, new GUILayoutOption[0]);
		UnityEngine.GUILayout.Space(10f);
		if (this.aiLayerEmpireAntiSpy.IsAtLeastOneCityRoundingUp)
		{
			UnityEngine.GUILayout.Label(Amplitude.Unity.UI.GUILayout.Format("<i>Round up in progress at least in one city</i>", new object[0]), new GUILayoutOption[0]);
		}
		UnityEngine.GUILayout.EndHorizontal();
		if (this.aiLayerEmpireAntiSpy.WantedRoundUpOnCityGUID != GameEntityGUID.Zero)
		{
			UnityEngine.GUILayout.Label(Amplitude.Unity.UI.GUILayout.Format("Round up wanted <i>(city GUID: {0})</i>", new object[]
			{
				this.aiLayerEmpireAntiSpy.WantedRoundUpOnCityGUID
			}), new GUILayoutOption[0]);
		}
		else
		{
			UnityEngine.GUILayout.Label(Amplitude.Unity.UI.GUILayout.Format("No round up wanted.", new object[0]), new GUILayoutOption[0]);
		}
		UnityEngine.GUILayout.Space(10f);
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Research AntiSpy modifier:</i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(200f)
		});
		if (this.aILayerHeroAssignation.IsActive())
		{
			float modifierValueUnnormalized = this.aiEntityContext.GetModifierValueUnnormalized(this.researchContextGroupName, AILayer_EmpireAntiSpy.AntiSpyParameterModifier);
			this.WeightLabel(modifierValueUnnormalized, new GUILayoutOption[0]);
		}
		UnityEngine.GUILayout.EndHorizontal();
		GameEspionageScreen guiPanel = this.guiService.GetGuiPanel<GameEspionageScreen>();
		if (guiPanel.SelectedSpy != null)
		{
			List<InfiltrationActionData> debugInfiltrationInfo = this.aILayerHeroAssignation.GetDebugInfiltrationInfo(guiPanel.SelectedSpy.GUID);
			UnityEngine.GUILayout.Label(Amplitude.Unity.UI.GUILayout.Format("<b>Hero: {0}, Threshold: {1}</b>", new object[]
			{
				guiPanel.SelectedSpy.UnitDesign.LocalizedName,
				(debugInfiltrationInfo.Count > 0) ? debugInfiltrationInfo[0].UtilityThreshold : -1f
			}), new GUILayoutOption[0]);
			foreach (InfiltrationActionData infiltrationActionData in debugInfiltrationInfo)
			{
				UnityEngine.GUILayout.Label(Amplitude.Unity.UI.GUILayout.Format("<i>{0}: {1}</i>", new object[]
				{
					infiltrationActionData.ChosenActionName,
					infiltrationActionData.ChosenActionUtility
				}), new GUILayoutOption[0]);
			}
		}
	}

	private void InitializeEspionageDebugger()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			Diagnostics.LogError("Failed to retrieve ai entity empire.");
			this.ReleaseEspionageDebugger();
		}
		this.aiEntityContext = entity.Context;
		this.aiLayerEmpireAntiSpy = entity.GetLayer<AILayer_EmpireAntiSpy>();
		this.aILayerHeroAssignation = entity.GetLayer<AILayer_HeroAssignation>();
		Diagnostics.Assert(this.aiLayerEmpireAntiSpy != null);
		this.currentDebugMode = AIDebugMode.DebugMode.Espionage;
		AILayer_Research layer = entity.GetLayer<AILayer_Research>();
		this.researchContextGroupName = layer.GetResearchContextGroupName();
	}

	private void ReleaseEspionageDebugger()
	{
		this.currentDebugMode = AIDebugMode.DebugMode.None;
		this.aiLayerEmpireAntiSpy = null;
		this.aiEntityContext = null;
		this.aILayerHeroAssignation = null;
	}

	private string GetRegionName(int regionIndex)
	{
		if (this.worldPositionningService == null)
		{
			return regionIndex.ToString();
		}
		Region region = this.worldPositionningService.GetRegion(regionIndex);
		if (region == null)
		{
			return regionIndex.ToString();
		}
		return region.LocalizedName;
	}

	private void DiplayNavalArmyDebugger()
	{
		if (this.navyLayer == null || this.selectedNavalArmy == null)
		{
			return;
		}
		this.selectedNavalArmy.DisplayDebug();
	}

	private void InitializeNavalArmyDebugger(Army army)
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = army.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity == null)
			{
				Diagnostics.LogError("Failed to retrieve ai entity empire.");
				this.ReleaseNavalArmyDebugger();
				return;
			}
			this.navyLayer = entity.GetLayer<AILayer_Navy>();
		}
		else
		{
			AIPlayer_NavalEmpire aiplayer_NavalEmpire;
			if (!this.aiScheduler.TryGetNavalEmpireAIPlayer(out aiplayer_NavalEmpire))
			{
				Diagnostics.LogError("Failed to retrieve ai entity empire.");
				this.ReleaseNavalArmyDebugger();
				return;
			}
			AIEntity_NavalEmpire entity2 = aiplayer_MajorEmpire.GetEntity<AIEntity_NavalEmpire>();
			if (entity2 == null)
			{
				Diagnostics.LogError("Failed to retrieve ai entity empire.");
				this.ReleaseNavalArmyDebugger();
				return;
			}
			this.navyLayer = entity2.GetLayer<AILayer_Raiders>();
		}
		Diagnostics.Assert(this.navyLayer != null);
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.selectedNavalArmy = this.navyLayer.GetNavyArmy(army);
		this.currentDebugMode = AIDebugMode.DebugMode.NavalArmy;
	}

	private void ReleaseNavalArmyDebugger()
	{
		this.selectedNavalArmy = null;
		this.navyLayer = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private void DiplayNegotiationDebugger()
	{
		if (this.negotiationEngagedWithEmpire == null)
		{
			return;
		}
		UnityEngine.GUILayout.Label("<b>Summary</b>", new GUILayoutOption[0]);
		WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage = this.diplomacyBlackboard.FindFirst<WantedDiplomaticRelationStateMessage>(BlackboardLayerID.Empire, (WantedDiplomaticRelationStateMessage match) => match.OpponentEmpireIndex == this.negotiationEngagedWithEmpire.Index);
		if (wantedDiplomaticRelationStateMessage == null)
		{
			UnityEngine.GUILayout.Label("Wanted diplomatic relation state: <none>", new GUILayoutOption[0]);
		}
		else
		{
			UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label(string.Format("Wanted state: <b>{0}</b>", wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName.ToString().Replace("DiplomaticRelationState", string.Empty)), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(150f)
			});
			UnityEngine.GUILayout.Label(string.Format("War status: <b>{0}</b>", wantedDiplomaticRelationStateMessage.CurrentWarStatusType), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(120f)
			});
			UnityEngine.GUILayout.Label(string.Format("Desire: <b>{0}</b>", wantedDiplomaticRelationStateMessage.Criticity.ToString("F2")), new GUILayoutOption[0]);
			UnityEngine.GUILayout.EndHorizontal();
			StaticString mostWantedDiplomaticTermAgentName = this.aiLayerDiplomacy.GetMostWantedDiplomaticTermAgentName(this.negotiationEngagedWithEmpire);
			UnityEngine.GUILayout.Label(string.Format("Most wanted diplomatic term agent: <b>{0}</b>", mostWantedDiplomaticTermAgentName), new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label(string.Format("PeaceWish: <b>{0}</b>", this.aiLayerDiplomacy.GetPeaceWish(this.negotiationEngagedWithEmpire.Index)), new GUILayoutOption[0]);
			foreach (Agent agent in this.aiLayerDiplomacy.GetAgents(this.negotiationEngagedWithEmpire as MajorEmpire))
			{
				UnityEngine.GUILayout.Label(string.Format("<i>{0}</i>: {1}", agent.AgentDefinition.Name, agent.CriticityMax.Intensity), new GUILayoutOption[0]);
			}
		}
		UnityEngine.GUILayout.Space(10f);
		if (this.aiLayerAttitude != null)
		{
			AILayer_Attitude.Attitude attitude = this.aiLayerAttitude.GetAttitude(this.negotiationEngagedWithEmpire);
			if (attitude != null)
			{
				UnityEngine.GUILayout.Label(string.Format("<b>Attitude towards the Empire {0}</b>", this.negotiationEngagedWithEmpire.Index), new GUILayoutOption[0]);
				DiplomaticRelationScoreDebugger.DiplomaticRelationScoreViewer(attitude.Score, ref this.ignoreDebugNulModifiers, true);
			}
			else
			{
				UnityEngine.GUILayout.Label(string.Format("<color=red>No attitude found towards the Empire {0}</color>", this.negotiationEngagedWithEmpire.Index), new GUILayoutOption[0]);
			}
		}
		UnityEngine.GUILayout.Space(10f);
		if (this.aiLayerDiplomacyOtherEmpire != null && Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			UnityEngine.GUILayout.Label(string.Format("<b>Empire {0} diplomatic term evaluations</b>", this.negotiationEngagedWithEmpire.Index), new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label(string.Format("<color=orange>/!\\ All evaluations are NOT for the empire {0}</color>", this.playerControllerRepository.ActivePlayerController.Empire.Index), new GUILayoutOption[0]);
			Diagnostics.Assert(this.diplomacyDecisionMakerDebugger != null);
			this.diplomacyDecisionMakerDebugger.OnGUI(this.aiLayerDiplomacyOtherEmpire.DebugEvaluationsHistoric, false);
		}
	}

	private void InitializeNegotiationDebugger()
	{
		if (this.negotiationEngagedWithEmpire != null)
		{
			this.ReleaseNegotiationDebugger();
		}
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			Diagnostics.LogError("Failed to retrieve ai entity empire.");
			this.ReleaseNegotiationDebugger();
		}
		this.diplomacyBlackboard = entity.Blackboard;
		Diagnostics.Assert(this.diplomacyBlackboard != null);
		this.aiLayerDiplomacy = entity.GetLayer<AILayer_Diplomacy>();
		Diagnostics.Assert(this.aiLayerDiplomacy != null);
		this.aiLayerAttitude = entity.GetLayer<AILayer_Attitude>();
		Diagnostics.Assert(this.aiLayerAttitude != null);
		this.currentDebugMode = AIDebugMode.DebugMode.Negotiation;
	}

	private void ReleaseNegotiationDebugger()
	{
		this.aiLayerDiplomacy = null;
		this.aiLayerAttitude = null;
		this.diplomacyBlackboard = null;
		this.aiLayerDiplomacyOtherEmpire = null;
		this.negotiationEngagedWithEmpire = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private void UpdateNegotiationDebugger()
	{
		GameNegotiationScreen guiPanel = this.guiService.GetGuiPanel<GameNegotiationScreen>();
		if (guiPanel != null && guiPanel.IsVisible)
		{
			global::Empire selectedEmpire = guiPanel.SelectedEmpire;
			if (selectedEmpire != this.negotiationEngagedWithEmpire)
			{
				this.negotiationEngagedWithEmpire = selectedEmpire;
				if (this.negotiationEngagedWithEmpire != null)
				{
					Diagnostics.Assert(this.aiScheduler != null);
					AIPlayer_MajorEmpire aiplayer_MajorEmpire;
					if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(this.negotiationEngagedWithEmpire as MajorEmpire, out aiplayer_MajorEmpire))
					{
						Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
						{
							this.negotiationEngagedWithEmpire.Index
						});
					}
					AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
					if (entity == null)
					{
						Diagnostics.LogError("Failed to retrieve ai entity empire.");
					}
					this.aiLayerDiplomacyOtherEmpire = entity.GetLayer<AILayer_Diplomacy>();
					Diagnostics.Assert(this.aiLayerDiplomacy != null);
					return;
				}
				this.aiLayerDiplomacyOtherEmpire = null;
				return;
			}
		}
		else
		{
			this.negotiationEngagedWithEmpire = null;
			this.aiLayerDiplomacyOtherEmpire = null;
		}
	}

	private void DiplayResearchDebugger()
	{
		this.DisplayResearchSummary();
		UnityEngine.GUILayout.Space(10f);
		this.DisplayEmpireState(new string[]
		{
			"Research"
		});
		UnityEngine.GUILayout.Space(10f);
		if (this.aiLayerResearch == null)
		{
			return;
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Assert(this.researchDecisionMakerDebugger != null);
			this.researchDecisionMakerDebugger.OnGUI(this.aiLayerResearch.TechnologyDecisionMakerEvaluationDataHistoric, true);
		}
	}

	private void DisplayResearchSummary()
	{
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && this.aiLayerResearch != null)
		{
			if (this.aiLayerVictory != null)
			{
				UnityEngine.GUILayout.Label(string.Format("<b>Victory Focus: {0}</b>", this.aiLayerVictory.CurrentFocus), new GUILayoutOption[0]);
			}
			foreach (DecisionResult decisionResult in this.aiLayerResearch.GetTechnologyDecisions())
			{
				UnityEngine.GUILayout.Label(string.Format("<i>{0}</i>: {1}", (decisionResult.Element as DepartmentOfScience.ConstructibleElement).Name.ToString(), decisionResult.Score), new GUILayoutOption[0]);
			}
		}
		if (this.resourceAmas != null)
		{
			UnityEngine.GUILayout.Label("<b>Summary</b>", new GUILayoutOption[0]);
			UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			UnityEngine.GUILayout.Space(80f);
			UnityEngine.GUILayout.Label("<i>Food</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Industry</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Science</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Dust</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("<i>Empire Point</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(80f)
			});
			UnityEngine.GUILayout.EndHorizontal();
			UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label("<i>AMAS</i>", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(80f)
			});
			UnityEngine.GUILayout.Label("-", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.Label("-", new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.TechnologyReferenceTurnCount), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.NetEmpireMoney), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			this.AgentCriticityLabel(this.resourceAmas.GetAgent(AILayer_ResourceAmas.AgentNames.NetEmpirePrestige), new GUILayoutOption[]
			{
				UnityEngine.GUILayout.Width(60f)
			});
			UnityEngine.GUILayout.EndHorizontal();
			AmasEmpireDataMessage amasEmpireDataMessage = this.researchBlackboard.GetMessage(this.aiLayerAmasEmpire.AmasEmpireDataMessageID) as AmasEmpireDataMessage;
			if (amasEmpireDataMessage != null)
			{
				UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				UnityEngine.GUILayout.Label("<i>Weight</i>", new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(80f)
				});
				if (amasEmpireDataMessage.ResearchWeights != null)
				{
					for (int i = 0; i < amasEmpireDataMessage.ResearchWeights.Length; i++)
					{
						this.WeightLabel(amasEmpireDataMessage.ResearchWeights[i], new GUILayoutOption[]
						{
							UnityEngine.GUILayout.Width(60f)
						});
					}
				}
				UnityEngine.GUILayout.EndHorizontal();
			}
		}
	}

	private void InitializeResearchDebugger()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			this.ReleaseResearchDebugger();
		}
		this.aiEntityContext = entity.Context;
		this.researchBlackboard = entity.Blackboard;
		Diagnostics.Assert(this.researchBlackboard != null);
		this.aiLayerResearch = entity.GetLayer<AILayer_Research>();
		this.aiLayerVictory = entity.GetLayer<AILayer_Victory>();
		Diagnostics.Assert(this.aiLayerResearch != null);
		this.aiLayerAmasEmpire = entity.GetLayer<AILayer_AmasEmpire>();
		Diagnostics.Assert(this.aiLayerAmasEmpire != null);
		AIEntity_Amas entity2 = aiplayer_MajorEmpire.GetEntity<AIEntity_Amas>();
		Diagnostics.Assert(entity2 != null);
		AILayer_ResourceAmas layer = entity2.GetLayer<AILayer_ResourceAmas>();
		Diagnostics.Assert(layer != null);
		this.resourceAmas = layer.Amas;
		this.currentDebugMode = AIDebugMode.DebugMode.Research;
	}

	private void ReleaseResearchDebugger()
	{
		this.aiEntityContext = null;
		this.researchBlackboard = null;
		this.aiLayerResearch = null;
		this.aiLayerAmasEmpire = null;
		this.resourceAmas = null;
		this.aiLayerVictory = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private void DiplayUnitDesignDebugger()
	{
		if (!Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			return;
		}
		if (this.selectedUnitDesign != null)
		{
			this.DisplayUnitDesignSummary(this.selectedUnitDesign);
			UnityEngine.GUILayout.Space(10f);
			AIUnitDesignData aiunitDesignData = null;
			if (this.unitDesignDataRepository.TryGetUnitDesignData(this.playerControllerRepository.ActivePlayerController.Empire.Index, this.selectedUnitDesign.Model, out aiunitDesignData))
			{
				this.DisplayUnitDesignDataSummary(aiunitDesignData);
				UnityEngine.GUILayout.Space(10f);
				Diagnostics.Assert(this.unitDesignDecisionMakerDebugger != null);
				this.unitDesignDecisionMakerDebugger.OnGUI(aiunitDesignData.DecisionMakerEvaluationDataHistoric, true);
			}
			AfterBattleData afterBattleData = null;
			for (int i = 0; i < this.aiLayerUnitDesigner.AfterBattleDataByUnitBody.Count; i++)
			{
				if (this.aiLayerUnitDesigner.AfterBattleDataByUnitBody[i].UnitBodyName == this.selectedUnitDesign.UnitBodyDefinition.Name)
				{
					afterBattleData = this.aiLayerUnitDesigner.AfterBattleDataByUnitBody[i];
					break;
				}
			}
			if (afterBattleData != null)
			{
				this.DisplayAfterBattleData(afterBattleData);
			}
			if (this.selectedUnitDesign is UnitProfile && (this.selectedUnitDesign as UnitProfile).IsHero && this.aILayerHeroAssignation != null && Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				DepartmentOfEducation agency = this.playerControllerRepository.ActivePlayerController.Empire.GetAgency<DepartmentOfEducation>();
				Unit unit = null;
				foreach (Unit unit2 in agency.Heroes)
				{
					if (unit2.UnitDesign == this.selectedUnitDesign)
					{
						unit = unit2;
						break;
					}
				}
				AIData_Unit aidata_Unit;
				if (unit != null && this.aiDataRepository.TryGetAIData<AIData_Unit>(unit.GUID, out aidata_Unit))
				{
					string arg = string.Empty;
					if (aidata_Unit.HeroData.WantedHeroAssignation != null && aidata_Unit.HeroData.WantedHeroAssignation.Garrison != null)
					{
						arg = aidata_Unit.HeroData.WantedHeroAssignation.Garrison.LocalizedName;
					}
					UnityEngine.GUILayout.Label(string.Format("<b>Speciality: {0}, Wants to be in: {1}</b>", AILayer_HeroAssignation.HeroAssignationTypeNames[aidata_Unit.HeroData.ChosenSpecialty], arg), new GUILayoutOption[0]);
					foreach (string arg2 in this.aILayerHeroAssignation.GetHeroSkillDecisions(aidata_Unit))
					{
						UnityEngine.GUILayout.Label(string.Format("<i>{0}</i>", arg2), new GUILayoutOption[0]);
					}
				}
			}
		}
	}

	private void DisplayAfterBattleData(AfterBattleData afterBattleData)
	{
		UnityEngine.GUILayout.Label("<b>Encounter data</b>", new GUILayoutOption[0]);
		this.showModifierData = UnityEngine.GUILayout.Toggle(this.showModifierData, "Display battle modifiers", new GUILayoutOption[0]);
		if (this.showModifierData)
		{
			for (int i = 0; i < afterBattleData.Modifiers.Length; i++)
			{
				UnityEngine.GUILayout.Label(this.FormatLabelContent("    {0}: <b>{1}</b> (Base={2}, Boost={3})", new object[]
				{
					afterBattleData.Modifiers[i].Name,
					afterBattleData.Modifiers[i].Value,
					afterBattleData.Modifiers[i].BaseValue,
					afterBattleData.Modifiers[i].Boost
				}), new GUILayoutOption[0]);
			}
		}
		this.showEncounterData = UnityEngine.GUILayout.Toggle(this.showEncounterData, "Display EncounterData", new GUILayoutOption[0]);
		if (this.showEncounterData)
		{
			Diagnostics.Assert(afterBattleData.EncounterData != null);
			if (this.encounterDataFoldout == null || this.encounterDataFoldout.Length < afterBattleData.EncounterData.Length)
			{
				this.encounterDataFoldout = new bool[afterBattleData.EncounterData.Length];
			}
			for (int j = 0; j < afterBattleData.EncounterData.Length; j++)
			{
				string text = this.FormatLabelContent("    {0}: Turn {1}, Modifier count={2}", new object[]
				{
					j,
					afterBattleData.EncounterDataLastTurn[j],
					afterBattleData.EncounterData[j].Count
				});
				this.encounterDataFoldout[j] = UnityEngine.GUILayout.Toggle(this.encounterDataFoldout[j], text, new GUILayoutOption[0]);
				if (this.encounterDataFoldout[j])
				{
					for (int k = 0; k < afterBattleData.EncounterData[j].Count; k++)
					{
						UnityEngine.GUILayout.Label(this.FormatLabelContent("        {0}: <b>{1}</b>", new object[]
						{
							afterBattleData.EncounterData[j][k].Name,
							afterBattleData.EncounterData[j][k].Value
						}), new GUILayoutOption[0]);
					}
				}
			}
		}
	}

	private void DisplayUnitDesignDataSummary(AIUnitDesignData unitDesignData)
	{
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Production cost ratio: </i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(200f)
		});
		UnityEngine.GUILayout.Label(unitDesignData.EmpireCostRatio.ToString("0.000"), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(60f)
		});
		UnityEngine.GUILayout.Space(10f);
		UnityEngine.GUILayout.Label("<i>Usage: </i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(200f)
		});
		UnityEngine.GUILayout.Label((unitDesignData.EmpireWideRatio * 100f).ToString("0.0") + "%", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(60f)
		});
		UnityEngine.GUILayout.EndHorizontal();
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Score Modifier: </i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(200f)
		});
		UnityEngine.GUILayout.Label(unitDesignData.ScoreModifier.ToString("F"), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(80f)
		});
		UnityEngine.GUILayout.EndHorizontal();
		string text = string.Format("<i>Old UnitDesign Score:</i> {0}", unitDesignData.OldUnitDesignScoring.GlobalScore.ToString("F"));
		this.showOldUnitDesignScoreDetail = UnityEngine.GUILayout.Toggle(this.showOldUnitDesignScoreDetail, text, new GUILayoutOption[0]);
		if (this.showOldUnitDesignScoreDetail)
		{
			for (int i = 0; i < unitDesignData.OldUnitDesignScoring.ItemNamePerSlot.Length; i++)
			{
				UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				UnityEngine.GUILayout.Label("    ", new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(10f)
				});
				string text2 = string.Empty;
				if (!string.IsNullOrEmpty(unitDesignData.OldUnitDesignScoring.ItemNamePerSlot[i]))
				{
					text2 = unitDesignData.OldUnitDesignScoring.ItemNamePerSlot[i];
				}
				UnityEngine.GUILayout.Label(text2, new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(200f)
				});
				UnityEngine.GUILayout.Label(unitDesignData.OldUnitDesignScoring.ItemScorePerSlot[i].ToString("F"), new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(80f)
				});
				UnityEngine.GUILayout.EndHorizontal();
			}
		}
		text = string.Format("<i>New UnitDesign Score:</i> {0}", unitDesignData.NewUnitDesignScoring.GlobalScore.ToString("F"));
		this.showNewUnitDesignScoreDetail = UnityEngine.GUILayout.Toggle(this.showNewUnitDesignScoreDetail, text, new GUILayoutOption[0]);
		if (this.showNewUnitDesignScoreDetail)
		{
			for (int j = 0; j < unitDesignData.NewUnitDesignScoring.ItemNamePerSlot.Length; j++)
			{
				UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				UnityEngine.GUILayout.Label("    ", new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(10f)
				});
				string text3 = string.Empty;
				if (!string.IsNullOrEmpty(unitDesignData.NewUnitDesignScoring.ItemNamePerSlot[j]))
				{
					text3 = unitDesignData.NewUnitDesignScoring.ItemNamePerSlot[j];
				}
				UnityEngine.GUILayout.Label(text3, new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(200f)
				});
				UnityEngine.GUILayout.Label(unitDesignData.NewUnitDesignScoring.ItemScorePerSlot[j].ToString("F"), new GUILayoutOption[]
				{
					UnityEngine.GUILayout.Width(80f)
				});
				UnityEngine.GUILayout.EndHorizontal();
			}
		}
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Current unit pattern: </i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(200f)
		});
		UnityEngine.GUILayout.Label(string.Format("{0} (Score: {1})", unitDesignData.CurrentUnitPattern, unitDesignData.CurrentUnitPatternAffinity.ToString("F")), new GUILayoutOption[0]);
		UnityEngine.GUILayout.EndHorizontal();
		this.showUnitPatternAffinities = UnityEngine.GUILayout.Toggle(this.showUnitPatternAffinities, "Show all unit pattern scores", new GUILayoutOption[0]);
		if (this.showUnitPatternAffinities && unitDesignData.UnitPatternAffinities != null)
		{
			for (int k = 0; k < unitDesignData.UnitPatternAffinities.Length; k++)
			{
				AIUnitPatternDefinition aiunitPatternDefinition = unitDesignData.UnitPatternAffinities[k].Element as AIUnitPatternDefinition;
				if (aiunitPatternDefinition != null)
				{
					UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					UnityEngine.GUILayout.Label(aiunitPatternDefinition.Name, new GUILayoutOption[]
					{
						UnityEngine.GUILayout.Width(200f)
					});
					UnityEngine.GUILayout.Space(10f);
					if (unitDesignData.UnitPatternAffinities[k].Score == 3.40282347E+38f || unitDesignData.UnitPatternAffinities[k].Score == -3.40282347E+38f)
					{
						UnityEngine.GUILayout.Label("Inf", new GUILayoutOption[]
						{
							UnityEngine.GUILayout.Width(60f)
						});
					}
					else
					{
						UnityEngine.GUILayout.Label(unitDesignData.UnitPatternAffinities[k].Score.ToString("F"), new GUILayoutOption[]
						{
							UnityEngine.GUILayout.Width(60f)
						});
					}
					UnityEngine.GUILayout.EndHorizontal();
				}
			}
		}
	}

	private void DisplayUnitDesignSummary(UnitDesign unitDesign)
	{
		UnityEngine.GUILayout.Label("<b>Summary</b>", new GUILayoutOption[0]);
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		UnityEngine.GUILayout.Label("<i>Name: </i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.Label(unitDesign.LocalizedName, new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(80f)
		});
		UnityEngine.GUILayout.Label("<i>Model: </i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(40f)
		});
		UnityEngine.GUILayout.Label(string.Format("{0}.{1}", unitDesign.Model, unitDesign.ModelRevision), new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(80f)
		});
		UnityEngine.GUILayout.Label("<i>Tags: </i>", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(60f)
		});
		UnityEngine.GUILayout.Label(unitDesign.Tags.ToString(), new GUILayoutOption[0]);
		UnityEngine.GUILayout.EndHorizontal();
	}

	private string FormatLabelContent(string content, params object[] parameters)
	{
		return Amplitude.Unity.UI.GUILayout.Format(true, Amplitude.Unity.UI.GUILayout.FloatFormat.Default, content, parameters);
	}

	private void InitializeUnitDesignDebugger()
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = this.playerControllerRepository.ActivePlayerController.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Failed to retrieve ai player of empire {0}.", new object[]
			{
				empire.Index
			});
			this.ReleaseUnitDesignDebugger();
			return;
		}
		AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (entity == null)
		{
			Diagnostics.LogError("Failed to retrieve ai entity empire.");
			this.ReleaseUnitDesignDebugger();
			return;
		}
		this.aiLayerUnitDesigner = entity.GetLayer<AILayer_UnitDesigner>();
		this.aILayerHeroAssignation = entity.GetLayer<AILayer_HeroAssignation>();
		this.currentDebugMode = AIDebugMode.DebugMode.UnitDesign;
		this.unitDesignDataRepository = AIScheduler.Services.GetService<IAIUnitDesignDataRepository>();
	}

	private void ReleaseUnitDesignDebugger()
	{
		this.researchBlackboard = null;
		this.aiLayerResearch = null;
		this.aiLayerAmasEmpire = null;
		this.aiLayerStrategy = null;
		this.resourceAmas = null;
		this.selectedUnitDesign = null;
		this.aILayerHeroAssignation = null;
		this.currentDebugMode = AIDebugMode.DebugMode.None;
	}

	private bool IsDockedToLeft
	{
		get
		{
			return this.isDockedToLeft;
		}
		set
		{
			this.isDockedToLeft = value;
			this.UpdateUIScreenRect();
		}
	}

	private bool IsExpanded
	{
		get
		{
			return this.isUnitDesignComboBoxExpanded;
		}
		set
		{
			this.isUnitDesignComboBoxExpanded = value;
			this.UpdateUIScreenRect();
		}
	}

	public void Release()
	{
		this.ReleaseCurrentMode();
		if (this.cursorService != null)
		{
			this.cursorService.CursorChanged -= this.CursorService_CursorChanged;
		}
		if (this.gameService != null)
		{
			this.gameService.GameChange -= this.GameService_GameChange;
		}
		if (this.playerControllerRepository != null)
		{
			this.playerControllerRepository.ActivePlayerControllerChange -= this.PlayerControllerRepository_ActivePlayerControllerChange;
		}
		if (this.game != null)
		{
			this.OnReleaseGame();
		}
		if (this.inputGrabAgeTransform != null)
		{
			this.inputGrabAgeTransform.Visible = false;
			this.inputGrabAgeTransform = null;
		}
		this.aiScheduler = null;
		this.playerControllerRepository = null;
		this.gameService = null;
		this.cursorService = null;
		this.guiService = null;
	}

	protected override IEnumerator Start()
	{
		yield return base.StartCoroutine(base.Start());
		GameObject gameObject = GameObject.Find("06-AIDebugInputGrab");
		if (gameObject == null)
		{
			Diagnostics.LogWarning("Can't retrieve AIDebugInputGrab object.");
		}
		else
		{
			this.inputGrabAgeTransform = gameObject.GetComponent<AgeTransform>();
			Diagnostics.Assert(this.inputGrabAgeTransform != null);
			this.inputGrabAgeTransform.Position = ((!this.IsExpanded) ? this.uiReducedScreenRect : this.uiExpandedScreenRect);
			this.inputGrabAgeTransform.Visible = true;
		}
		this.uiExpandedBackground = this.MakeTexture((int)this.uiExpandedScreenRect.width, (int)this.uiExpandedScreenRect.height, this.uiBackgroundColor);
		this.uiReducedBackground = this.MakeTexture((int)this.uiReducedScreenRect.width, (int)this.uiReducedScreenRect.height, this.uiBackgroundColor);
		this.UpdateUIScreenRect();
		this.InitializeDecisionMakerDebugger();
		this.cursorService = Services.GetService<ICursorService>();
		this.cursorTargetService = Services.GetService<ICursorTargetService>();
		if (this.cursorService != null)
		{
			this.cursorService.CursorChanged += this.CursorService_CursorChanged;
		}
		else
		{
			this.isEnabled = true;
			this.currentDebugMode = AIDebugMode.DebugMode.Debug;
		}
		this.guiService = Services.GetService<IGuiService>();
		this.gameService = Services.GetService<IGameService>();
		if (this.gameService != null)
		{
			if (this.gameService.Game != null)
			{
				this.GameService_GameChange(this, new GameChangeEventArgs(GameChangeAction.Created, this.gameService.Game as global::Game));
			}
			this.gameService.GameChange += this.GameService_GameChange;
		}
		if (this.currentDebugMode != AIDebugMode.DebugMode.Debug)
		{
			this.BackToDefaultMode();
		}
		yield break;
	}

	private void AgentCriticityLabel(Agent agent, params GUILayoutOption[] options)
	{
		if (agent == null)
		{
			UnityEngine.GUILayout.Label("-", options);
			return;
		}
		float num = (agent.CriticityMax == null) ? 0f : agent.CriticityMax.Intensity;
		Color gray = Color.gray;
		if (agent.Enable)
		{
			gray = new Color(Mathf.Clamp01(0.5f + num), Mathf.Clamp01(1.5f - num), 0.5f);
		}
		Amplitude.Unity.UI.GUILayout.Label(num.ToString("F"), gray, options);
	}

	private void BackToDefaultMode()
	{
		this.ReleaseCurrentMode();
		this.InitializeDefaultDebugger();
	}

	private void CursorService_CursorChanged(object sender, CursorChangeEventArgs eventArgs)
	{
		DistrictWorldCursor districtWorldCursor = eventArgs.CurrentCursor as DistrictWorldCursor;
		if (districtWorldCursor != null)
		{
			this.ReleaseCurrentMode();
			this.InitializeCityDebugger(districtWorldCursor.City);
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.City)
		{
			this.BackToDefaultMode();
		}
		ArmyWorldCursor armyWorldCursor = eventArgs.CurrentCursor as ArmyWorldCursor;
		FortressWorldCursor fortressWorldCursor = eventArgs.CurrentCursor as FortressWorldCursor;
		if (armyWorldCursor != null)
		{
			this.ReleaseCurrentMode();
			if (armyWorldCursor.Army.HasSeafaringUnits())
			{
				this.InitializeNavalArmyDebugger(armyWorldCursor.Army);
			}
			else
			{
				this.InitializeArmyDebugger(armyWorldCursor.Army);
			}
		}
		else if (fortressWorldCursor != null)
		{
			this.ReleaseCurrentMode();
			this.InitializeNavalFortressDebugger(fortressWorldCursor.Fortress);
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.Army || this.currentDebugMode == AIDebugMode.DebugMode.NavalArmy)
		{
			this.BackToDefaultMode();
		}
		EncounterWorldCursor encounterWorldCursor = eventArgs.CurrentCursor as EncounterWorldCursor;
		if (encounterWorldCursor != null)
		{
			if (!(eventArgs.LastCursor is EncounterWorldCursor))
			{
				this.ReleaseCurrentMode();
				this.InitializeEncounterDebugger(encounterWorldCursor.Encounter);
				return;
			}
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.Encounter)
		{
			this.BackToDefaultMode();
		}
	}

	private void DiplayNotImplementedDebugger()
	{
		UnityEngine.GUILayout.Label("Not implemented debugger", this.uiTitleStyle, new GUILayoutOption[0]);
	}

	private void GameService_GameChange(object sender, GameChangeEventArgs eventArgs)
	{
		GameChangeAction action = eventArgs.Action;
		if (action == GameChangeAction.Created)
		{
			this.OnCreateGame(eventArgs.Game as global::Game);
			return;
		}
		if (action - GameChangeAction.Releasing > 1)
		{
			return;
		}
		this.OnReleaseGame();
	}

	private Texture2D MakeTexture(int width, int height, Color color)
	{
		Color[] array = new Color[width * height];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = color;
		}
		Texture2D texture2D = new Texture2D(width, height);
		texture2D.SetPixels(array);
		texture2D.Apply();
		return texture2D;
	}

	private void OnCreateGame(global::Game game)
	{
		Diagnostics.Log("[AIDebugMode] Create game.");
		this.game = game;
		if (this.game == null)
		{
			Diagnostics.LogError("Created game is null.");
			return;
		}
		this.playerControllerRepository = this.game.Services.GetService<IPlayerControllerRepositoryService>();
		if (this.playerControllerRepository != null)
		{
			this.playerControllerRepository.ActivePlayerControllerChange += this.PlayerControllerRepository_ActivePlayerControllerChange;
		}
		this.worldPositionningService = game.Services.GetService<IWorldPositionningService>();
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		global::Session session = service.Session as global::Session;
		if (session == null)
		{
			Diagnostics.LogError("Session is null.");
			return;
		}
		GameServer gameServer = session.GameServer as GameServer;
		if (gameServer == null)
		{
			Diagnostics.LogError("Gameserver is null.");
			return;
		}
		this.aiScheduler = gameServer.AIScheduler;
		if (this.aiScheduler == null)
		{
			Diagnostics.LogError("AIScheduler is null.");
			return;
		}
		this.isEnabled = true;
	}

	private void OnGUI()
	{
		if (!this.isEnabled)
		{
			return;
		}
		UnityEngine.GUILayout.BeginArea(this.uiScreenRect, (!this.IsExpanded) ? this.uiReducedBackground : this.uiExpandedBackground, this.uiBackgroundStyle);
		UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		if (UnityEngine.GUILayout.Button((!this.IsExpanded) ? "+" : "-", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(28f)
		}))
		{
			this.IsExpanded = !this.IsExpanded;
		}
		UnityEngine.GUILayout.Label(string.Format("AI Debugger - {0}", this.currentDebugMode), this.uiTitleStyle, new GUILayoutOption[0]);
		if (UnityEngine.GUILayout.Button((!this.IsDockedToLeft) ? "<" : ">", new GUILayoutOption[]
		{
			UnityEngine.GUILayout.Width(28f)
		}))
		{
			this.IsDockedToLeft = !this.IsDockedToLeft;
		}
		UnityEngine.GUILayout.EndHorizontal();
		this.scrollPosition = UnityEngine.GUILayout.BeginScrollView(this.scrollPosition, new GUILayoutOption[0]);
		switch (this.currentDebugMode)
		{
		case AIDebugMode.DebugMode.Default:
			this.DiplayDefaultDebugger();
			break;
		case AIDebugMode.DebugMode.Debug:
			this.DiplayDecisionMakerDebugger();
			break;
		case AIDebugMode.DebugMode.Encounter:
			this.DiplayEncounterDebugger();
			break;
		case AIDebugMode.DebugMode.Empire:
			this.DiplayEmpireDebugger();
			break;
		case AIDebugMode.DebugMode.Espionage:
			this.DiplayEspionageDebugger();
			break;
		case AIDebugMode.DebugMode.Army:
			this.DiplayArmyDebugger();
			break;
		case AIDebugMode.DebugMode.City:
			this.DiplayCityDebugger();
			break;
		case AIDebugMode.DebugMode.CityList:
			this.DiplayCityListDebugger();
			break;
		case AIDebugMode.DebugMode.Diplomacy:
			this.DiplayDiplomacyDebugger();
			break;
		case AIDebugMode.DebugMode.Negotiation:
			this.DiplayNegotiationDebugger();
			break;
		case AIDebugMode.DebugMode.Research:
			this.DiplayResearchDebugger();
			break;
		case AIDebugMode.DebugMode.UnitDesign:
			this.DiplayUnitDesignDebugger();
			break;
		case AIDebugMode.DebugMode.Altar:
			this.DiplayAltarDebugger();
			break;
		case AIDebugMode.DebugMode.NavalArmy:
			this.DiplayNavalArmyDebugger();
			break;
		default:
			this.DiplayNotImplementedDebugger();
			break;
		}
		UnityEngine.GUILayout.EndScrollView();
		UnityEngine.GUILayout.EndArea();
	}

	private void OnReleaseGame()
	{
		Diagnostics.Log("[AIDebugMode] Release game.");
		this.ReleaseCurrentMode();
		this.currentDebugMode = AIDebugMode.DebugMode.None;
		if (this.playerControllerRepository != null)
		{
			this.playerControllerRepository.ActivePlayerControllerChange -= this.PlayerControllerRepository_ActivePlayerControllerChange;
		}
		this.playerControllerRepository = null;
		this.game = null;
		this.aiScheduler = null;
	}

	private void PlayerControllerRepository_ActivePlayerControllerChange(object sender, ActivePlayerControllerChangeEventArgs e)
	{
		this.BackToDefaultMode();
	}

	private void ReleaseCurrentMode()
	{
		switch (this.currentDebugMode)
		{
		case AIDebugMode.DebugMode.Default:
			this.ReleaseDefaultDebugger();
			return;
		case AIDebugMode.DebugMode.Encounter:
			this.ReleaseEncounterDebugger();
			return;
		case AIDebugMode.DebugMode.Empire:
			this.ReleaseEmpireDebugger();
			return;
		case AIDebugMode.DebugMode.Espionage:
			this.ReleaseEspionageDebugger();
			return;
		case AIDebugMode.DebugMode.Army:
			this.ReleaseArmyDebugger();
			return;
		case AIDebugMode.DebugMode.City:
			this.ReleaseCityDebugger();
			return;
		case AIDebugMode.DebugMode.CityList:
			this.ReleaseCityListDebugger();
			return;
		case AIDebugMode.DebugMode.Diplomacy:
			this.ReleaseDiplomacyDebugger();
			return;
		case AIDebugMode.DebugMode.Negotiation:
			this.ReleaseNegotiationDebugger();
			return;
		case AIDebugMode.DebugMode.Research:
			this.ReleaseResearchDebugger();
			return;
		case AIDebugMode.DebugMode.UnitDesign:
			this.ReleaseUnitDesignDebugger();
			return;
		case AIDebugMode.DebugMode.Altar:
			this.ReleaseAltarDebugger();
			return;
		case AIDebugMode.DebugMode.NavalArmy:
			this.ReleaseNavalArmyDebugger();
			return;
		case AIDebugMode.DebugMode.None:
			return;
		}
		Diagnostics.LogError("Not implemented mode: {0}.", new object[]
		{
			this.currentDebugMode
		});
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Keypad5))
		{
			this.IsExpanded = !this.IsExpanded;
		}
		if (Input.GetKeyDown(KeyCode.Keypad4))
		{
			this.IsDockedToLeft = true;
		}
		if (Input.GetKeyDown(KeyCode.Keypad6))
		{
			this.IsDockedToLeft = false;
		}
		bool flag = false;
		if (this.guiService != null)
		{
			GameResearchScreen guiPanel = this.guiService.GetGuiPanel<GameResearchScreen>();
			flag = (guiPanel != null && guiPanel.IsVisible);
		}
		if (this.currentDebugMode != AIDebugMode.DebugMode.Research && flag)
		{
			this.InitializeResearchDebugger();
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.Research && !flag)
		{
			this.BackToDefaultMode();
		}
		bool flag2 = false;
		if (this.guiService != null)
		{
			GameAltarOfAurigaScreen guiPanel2 = this.guiService.GetGuiPanel<GameAltarOfAurigaScreen>();
			flag2 = (guiPanel2 != null && guiPanel2.IsVisible);
		}
		if (this.currentDebugMode != AIDebugMode.DebugMode.Altar && flag2)
		{
			this.InitializeAltarDebugger();
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.Altar && !flag2)
		{
			this.BackToDefaultMode();
		}
		bool flag3 = false;
		if (this.guiService != null)
		{
			GameDiplomacyScreen guiPanel3 = this.guiService.GetGuiPanel<GameDiplomacyScreen>();
			flag3 = (guiPanel3 != null && guiPanel3.IsVisible);
		}
		if (this.currentDebugMode != AIDebugMode.DebugMode.Diplomacy && flag3)
		{
			this.InitializeDiplomacyDebugger();
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.Diplomacy && !flag3)
		{
			this.BackToDefaultMode();
		}
		bool flag4 = false;
		if (this.guiService != null)
		{
			GameNegotiationScreen guiPanel4 = this.guiService.GetGuiPanel<GameNegotiationScreen>();
			flag4 = (guiPanel4 != null && guiPanel4.IsVisible);
		}
		if (this.currentDebugMode != AIDebugMode.DebugMode.Negotiation && flag4)
		{
			this.InitializeNegotiationDebugger();
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.Negotiation && !flag4)
		{
			this.BackToDefaultMode();
		}
		bool flag5 = false;
		if (this.guiService != null)
		{
			GameEspionageScreen guiPanel5 = this.guiService.GetGuiPanel<GameEspionageScreen>();
			flag5 = (guiPanel5 != null && guiPanel5.IsVisible);
		}
		if (this.currentDebugMode != AIDebugMode.DebugMode.Espionage && flag5)
		{
			this.InitializeEspionageDebugger();
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.Espionage && !flag5)
		{
			this.BackToDefaultMode();
		}
		bool flag6 = false;
		if (this.guiService != null)
		{
			GameEmpireScreen guiPanel6 = this.guiService.GetGuiPanel<GameEmpireScreen>();
			flag6 = (guiPanel6 != null && guiPanel6.IsVisible);
			IGuiService service = Services.GetService<IGuiService>();
			this.isEmpirePlanPanelVisible = (flag6 && service.GetGuiPanel<EmpirePlanModalPanel>().IsVisible);
		}
		if (this.currentDebugMode != AIDebugMode.DebugMode.Empire && flag6)
		{
			this.InitializeEmpireDebugger();
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.Empire && !flag6)
		{
			this.BackToDefaultMode();
		}
		bool flag7 = false;
		if (this.guiService != null)
		{
			GameCityListScreen guiPanel7 = this.guiService.GetGuiPanel<GameCityListScreen>();
			flag7 = (guiPanel7 != null && guiPanel7.IsVisible);
		}
		if (this.currentDebugMode != AIDebugMode.DebugMode.CityList && flag7)
		{
			this.InitializeCityListDebugger();
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.CityList && !flag7)
		{
			this.BackToDefaultMode();
		}
		bool flag8 = false;
		if (this.guiService != null)
		{
			GameMilitaryScreen guiPanel8 = this.guiService.GetGuiPanel<GameMilitaryScreen>();
			flag8 = (guiPanel8 != null && guiPanel8.IsVisible);
			if (guiPanel8)
			{
				this.selectedUnitDesign = guiPanel8.SelectedDesign;
			}
		}
		bool flag9 = false;
		if (this.guiService != null)
		{
			HeroInspectionModalPanel guiPanel9 = this.guiService.GetGuiPanel<HeroInspectionModalPanel>();
			flag9 = (guiPanel9 != null && guiPanel9.IsVisible);
			if (guiPanel9 && guiPanel9.Hero != null)
			{
				this.selectedUnitDesign = guiPanel9.Hero.UnitDesign;
			}
		}
		if (!flag8 && !flag9)
		{
			this.selectedUnitDesign = null;
		}
		if (this.currentDebugMode != AIDebugMode.DebugMode.UnitDesign && (flag8 || flag9))
		{
			this.InitializeUnitDesignDebugger();
		}
		else if (this.currentDebugMode == AIDebugMode.DebugMode.UnitDesign && !flag8 && !flag9)
		{
			this.BackToDefaultMode();
		}
		AIDebugMode.DebugMode debugMode = this.currentDebugMode;
		if (debugMode != AIDebugMode.DebugMode.Encounter)
		{
			if (debugMode == AIDebugMode.DebugMode.Negotiation)
			{
				this.UpdateNegotiationDebugger();
				return;
			}
		}
		else
		{
			this.UpdateEncounterDebugger();
		}
	}

	private void UpdateUIScreenRect()
	{
		Rect rect = (!this.IsExpanded) ? this.uiReducedScreenRect : this.uiExpandedScreenRect;
		this.uiScreenRect = new Rect((!this.IsDockedToLeft) ? ((float)Screen.width - rect.x - rect.width) : rect.x, rect.y, rect.width, rect.height);
		if (this.inputGrabAgeTransform != null)
		{
			this.inputGrabAgeTransform.Position = this.uiScreenRect;
		}
	}

	private void WeightLabel(float weight, params GUILayoutOption[] options)
	{
		Color color = new Color(Mathf.Clamp01(0.5f + weight), Mathf.Clamp01(1.5f - weight), 0.5f);
		Amplitude.Unity.UI.GUILayout.Label(weight.ToString("F"), color, options);
	}

	private void InitializeNavalFortressDebugger(Fortress fortress)
	{
		Diagnostics.Assert(this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null);
		Amplitude.Unity.Game.Empire empire = fortress.Empire;
		Diagnostics.Assert(this.aiScheduler != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.aiScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity_Empire entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity == null)
			{
				Diagnostics.LogError("Failed to retrieve ai entity empire.");
				this.ReleaseNavalArmyDebugger();
				return;
			}
			this.navyLayer = entity.GetLayer<AILayer_Navy>();
		}
		else
		{
			AIPlayer_NavalEmpire aiplayer_NavalEmpire;
			if (!this.aiScheduler.TryGetNavalEmpireAIPlayer(out aiplayer_NavalEmpire))
			{
				Diagnostics.LogError("Failed to retrieve ai entity empire.");
				this.ReleaseNavalArmyDebugger();
				return;
			}
			AIEntity_NavalEmpire entity2 = aiplayer_MajorEmpire.GetEntity<AIEntity_NavalEmpire>();
			if (entity2 == null)
			{
				Diagnostics.LogError("Failed to retrieve ai entity empire.");
				this.ReleaseNavalArmyDebugger();
				return;
			}
			this.navyLayer = entity2.GetLayer<AILayer_Raiders>();
		}
		Diagnostics.Assert(this.navyLayer != null);
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		NavyCommander navyCommander = this.navyLayer.NavyCommanders.Find((BaseNavyCommander match) => match.RegionData.WaterRegionIndex == fortress.Region.Index) as NavyCommander;
		this.selectedNavalArmy = navyCommander.NavyFortresses.Find((NavyFortress match) => match.Fortress.GUID == fortress.GUID);
		this.currentDebugMode = AIDebugMode.DebugMode.NavalArmy;
	}

	private Blackboard altarBlackboard;

	private ElementEvaluationGUILayoutDebugger<ConstructibleElement, InterpreterContext> altarDecisionMakerDebugger;

	private ElementEvaluationGUILayoutDebugger<SeasonEffect, InterpreterContext> seasonEffectDecisionMakerDebugger;

	private AILayer_Altar aiLayerAltar;

	private IAIDataRepositoryAIHelper aiDataRepository;

	private AILayer_ArmyManagement aiLayerArmyManagement;

	private AILayer_ArmyRecruitment aiLayerArmyRecruitment;

	private AILayer_Manta aiLayerManta;

	private Army selectedArmy;

	private Amplitude.Unity.UI.GUILayout.Table<EvaluableMessage> cityEvaluableMessageTable;

	private AIEntity_City aiEntityCity;

	private AILayer_AmasCity aiLayerAmasCity;

	private AILayer_CityAntiSpy aiLayerCityAntiSpy;

	private AILayer_Production aiLayerProduction;

	private AIData_City aiCityData;

	private Comparison<EvaluableMessage_CityBooster> boosterProductionMessageComparison = delegate(EvaluableMessage_CityBooster left, EvaluableMessage_CityBooster right)
	{
		if (left.State != right.State)
		{
			return -1 * left.State.CompareTo(right.State);
		}
		float num = 0f;
		if (left.ProductionEvaluations.Count > 0)
		{
			num = left.ProductionEvaluations.Max((ProductionEvaluation match) => match.ProductionFinalScore);
		}
		float value = 0f;
		if (right.ProductionEvaluations.Count > 0)
		{
			value = right.ProductionEvaluations.Max((ProductionEvaluation match) => match.ProductionFinalScore);
		}
		return -1 * num.CompareTo(value);
	};

	private List<EvaluableMessage_CityBooster> boosterProductionMessages = new List<EvaluableMessage_CityBooster>();

	private Comparison<EvaluableMessage_BuildingProduction> buildingProductionMessageComparison = delegate(EvaluableMessage_BuildingProduction left, EvaluableMessage_BuildingProduction right)
	{
		if (left.State != right.State)
		{
			return -1 * left.State.CompareTo(right.State);
		}
		float num = 0f;
		if (left.ProductionEvaluations.Count > 0)
		{
			num = left.ProductionEvaluations.Max((ProductionEvaluation match) => match.ProductionFinalScore);
		}
		float value = 0f;
		if (right.ProductionEvaluations.Count > 0)
		{
			value = right.ProductionEvaluations.Max((ProductionEvaluation match) => match.ProductionFinalScore);
		}
		return -1 * num.CompareTo(value);
	};

	private Comparison<EvaluableMessage_Wonder> wonderProductionMessageComparison = delegate(EvaluableMessage_Wonder left, EvaluableMessage_Wonder right)
	{
		if (left.State != right.State)
		{
			return -1 * left.State.CompareTo(right.State);
		}
		float num = 0f;
		if (left.ProductionEvaluations.Count > 0)
		{
			num = left.ProductionEvaluations.Max((ProductionEvaluation match) => match.ProductionFinalScore);
		}
		float value = 0f;
		if (right.ProductionEvaluations.Count > 0)
		{
			value = right.ProductionEvaluations.Max((ProductionEvaluation match) => match.ProductionFinalScore);
		}
		return -1 * num.CompareTo(value);
	};

	private List<EvaluableMessage_BuildingProduction> buildingProductionMessages = new List<EvaluableMessage_BuildingProduction>();

	private List<EvaluableMessage_Wonder> wonderProductionMessages = new List<EvaluableMessage_Wonder>();

	private AgentGroup cityAmasAgentGroup;

	private Blackboard cityBlackboard;

	private bool[] cityStateModifierGroups;

	private ElementEvaluationGUILayoutDebugger<ConstructibleElement, InterpreterContext> productionDecisionMakerDebugger = new ElementEvaluationGUILayoutDebugger<ConstructibleElement, InterpreterContext>("Production evaluations", true);

	private Comparison<EvaluableMessageWithUnitDesign> unitProductionMessageComparison = delegate(EvaluableMessageWithUnitDesign left, EvaluableMessageWithUnitDesign right)
	{
		if (left.State != right.State)
		{
			return -1 * left.State.CompareTo(right.State);
		}
		float num = 0f;
		if (left.ProductionEvaluations.Count > 0)
		{
			num = left.ProductionEvaluations.Max((ProductionEvaluation match) => match.ProductionFinalScore);
		}
		float value = 0f;
		if (right.ProductionEvaluations.Count > 0)
		{
			value = right.ProductionEvaluations.Max((ProductionEvaluation match) => match.ProductionFinalScore);
		}
		return -1 * num.CompareTo(value);
	};

	private List<EvaluableMessageWithUnitDesign> unitProductionMessages = new List<EvaluableMessageWithUnitDesign>();

	private List<DecisionMakerEvaluationData<AIDebugMode.Element, AIDebugMode.Context>> evaluationDataHistoric = new List<DecisionMakerEvaluationData<AIDebugMode.Element, AIDebugMode.Context>>();

	private DecisionMakerGUILayoutDebugger<AIDebugMode.Element, AIDebugMode.Context> guiLayoutDebugger;

	private bool ignoreDebugNulModifiers;

	private DiplomaticRelationScore score;

	private AILayer_Strategy aiLayerStrategy;

	private Predicate<AICommander> commanderWithActiveMissionsPredicate = (AICommander match) => match.Missions.Any((AICommanderMission mission) => mission.AIDataArmyGUID != GameEntityGUID.Zero || mission.State == TickableState.NeedTick);

	private bool displayAllCommanders;

	private bool[] empireStateModifierGroups;

	private List<GlobalObjectiveMessage> globalObjectiveMessages = new List<GlobalObjectiveMessage>();

	private Amplitude.Unity.UI.GUILayout.Table<GlobalObjectiveMessage> globalObjectiveMessageTable;

	private AILayer_Diplomacy aiLayerDiplomacy;

	private Blackboard diplomacyBlackboard;

	private ElementEvaluationGUILayoutDebugger<DiplomaticTerm, InterpreterContext> diplomacyDecisionMakerDebugger = new ElementEvaluationGUILayoutDebugger<DiplomaticTerm, InterpreterContext>("Diplomatic term evaluations", true);

	private AILayer_AccountManager aiLayerAccountManager;

	private AILayer_Economy aiLayerEconomy;

	private AILayer_EmpirePlan aiLayerEmpirePlan;

	private Blackboard empireBlackboard;

	private SimulationDecisionMakerGUILayoutDebugger<ConstructibleElement> empirePlanDecisionMakerDebugger = new SimulationDecisionMakerGUILayoutDebugger<ConstructibleElement>("Empire plan evaluations", true);

	private List<EvaluableMessage_EmpirePlan> empirePlanMessages = new List<EvaluableMessage_EmpirePlan>(4);

	private List<EvaluableMessage> evaluableMessages = new List<EvaluableMessage>();

	private List<EvaluableMessage> moneyEvaluableMessages = new List<EvaluableMessage>();

	private Amplitude.Unity.UI.GUILayout.Table<EvaluableMessage> moneyEvaluableMessageTable;

	private List<EvaluableMessage> empirePointEvaluableMessages = new List<EvaluableMessage>();

	private Amplitude.Unity.UI.GUILayout.Table<EvaluableMessage> empirePointEvaluableMessageTable;

	private List<EvaluableMessage> orbEvaluableMessages = new List<EvaluableMessage>();

	private Amplitude.Unity.UI.GUILayout.Table<EvaluableMessage> orbEvaluableMessageTable;

	private bool isEmpirePlanPanelVisible;

	private global::Empire empire;

	private AILayer_Encounter activePlayerAILayerEncounter;

	private Dictionary<GameEntityGUID, AILayer_Encounter> aiLayerEncounterByContenderGuid = new Dictionary<GameEntityGUID, AILayer_Encounter>();

	private SimulationDecisionMakerGUILayoutDebugger<AIEncounterStrategyDefinition> encounterStrategyDecisionMakerDebugger = new SimulationDecisionMakerGUILayoutDebugger<AIEncounterStrategyDefinition>("Encounter strategy evaluations", true);

	private WeakReference highlightedWorldBattleUnitReference = new WeakReference(null);

	private WeakReference selectedWorldBattleUnitReference = new WeakReference(null);

	private string researchContextGroupName = string.Empty;

	private AILayer_EmpireAntiSpy aiLayerEmpireAntiSpy;

	private AIEntityContext aiEntityContext;

	private AILayer_BaseNavy navyLayer;

	private BaseNavyArmy selectedNavalArmy;

	private AILayer_Attitude aiLayerAttitude;

	private AILayer_Diplomacy aiLayerDiplomacyOtherEmpire;

	private global::Empire negotiationEngagedWithEmpire;

	private AILayer_AmasEmpire aiLayerAmasEmpire;

	private AILayer_Research aiLayerResearch;

	private Blackboard researchBlackboard;

	private ElementEvaluationGUILayoutDebugger<ConstructibleElement, InterpreterContext> researchDecisionMakerDebugger = new ElementEvaluationGUILayoutDebugger<ConstructibleElement, InterpreterContext>("Technology evaluations", true);

	private Amas resourceAmas;

	private AILayer_UnitDesigner aiLayerUnitDesigner;

	private bool[] encounterDataFoldout;

	private bool isUnitDesignComboBoxExpanded;

	private UnitDesign selectedUnitDesign;

	private bool showEncounterData;

	private bool showModifierData;

	private bool showUnitPatternAffinities;

	private bool showOldUnitDesignScoreDetail;

	private bool showNewUnitDesignScoreDetail;

	private IAIUnitDesignDataRepository unitDesignDataRepository;

	private SimulationDecisionMakerGUILayoutDebugger<AIItemData> unitDesignDecisionMakerDebugger = new SimulationDecisionMakerGUILayoutDebugger<AIItemData>("UnitDesign evaluations", true);

	private AIScheduler aiScheduler;

	private AIDebugMode.DebugMode currentDebugMode = AIDebugMode.DebugMode.None;

	private ICursorService cursorService;

	private ICursorTargetService cursorTargetService;

	private global::Game game;

	private IGameService gameService;

	private IGuiService guiService;

	private AgeTransform inputGrabAgeTransform;

	private bool isDockedToLeft;

	private bool isEnabled;

	private IPlayerControllerRepositoryService playerControllerRepository;

	private IWorldPositionningService worldPositionningService;

	private Vector2 scrollPosition;

	[SerializeField]
	private Color uiBackgroundColor;

	[SerializeField]
	private GUIStyle uiBackgroundStyle;

	private Texture uiExpandedBackground;

	[SerializeField]
	private Rect uiExpandedScreenRect;

	private Texture uiReducedBackground;

	[SerializeField]
	private Rect uiReducedScreenRect;

	private Rect uiScreenRect;

	[SerializeField]
	private GUIStyle uiTitleStyle;

	private AILayer_Colossus aiLayerColossus;

	private AILayer_HeroAssignation aILayerHeroAssignation;

	private AILayer_Victory aiLayerVictory;

	private AILayer_QuestManager aiLayerQuestManager;

	private AILayer_KaijuManagement aiLayerKaiju;

	public class Element
	{
		public Element(string name, float score)
		{
			this.Name = name;
			this.Score = score;
		}

		public override string ToString()
		{
			return this.Name;
		}

		public string Name;

		public float Score;
	}

	public class Context : IDecisionMakerContext
	{
		public void Register(string name, object value)
		{
			if (this.Content.ContainsKey(name))
			{
				this.Content[name] = value;
				return;
			}
			this.Content.Add(name, value);
		}

		public Dictionary<string, object> Content = new Dictionary<string, object>();
	}

	public class AIParameter : IAIParameter<AIDebugMode.Context>
	{
		public AIParameter(StaticString name, float value)
		{
			this.Name = name;
			this.Value = value;
		}

		public StaticString Name { get; set; }

		public float GetValue(AIDebugMode.Context context)
		{
			return this.Value;
		}

		public override string ToString()
		{
			return string.Format("AIParameter - Value: {0}", this.Value);
		}

		public float Value;
	}

	public class Converter : IAIParameterConverter<AIDebugMode.Context>
	{
		public Converter(StaticString outputAIParameterName, float ratio)
		{
			this.OutputAIParameterName = outputAIParameterName;
			this.Ratio = ratio;
		}

		public StaticString OutputAIParameterName { get; set; }

		protected float Ratio { get; set; }

		public float GetValue(AIDebugMode.Context context)
		{
			if (!context.Content.ContainsKey("Input"))
			{
				return 0f;
			}
			return (float)context.Content["Input"] * this.Ratio;
		}

		public override string ToString()
		{
			return string.Format("Converter - Function: Input * {0}", this.Ratio);
		}
	}

	public class RandomPrerequisite : IAIPrerequisite<AIDebugMode.Context>
	{
		public StaticString[] Flags
		{
			get
			{
				return new StaticString[]
				{
					"Random"
				};
			}
		}

		public bool Check(AIDebugMode.Context context)
		{
			return UnityEngine.Random.value > 0.25f;
		}

		public override string ToString()
		{
			return string.Format("RandomPrerequisite 25%", new object[0]);
		}
	}

	private enum DebugMode
	{
		Default,
		Debug,
		Encounter,
		Empire,
		Espionage,
		Army,
		City,
		CityList,
		Diplomacy,
		Negotiation,
		Research,
		UnitDesign,
		Altar,
		NavalArmy,
		None
	}
}
