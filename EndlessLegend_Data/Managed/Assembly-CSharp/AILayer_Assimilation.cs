using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AILayer_Assimilation : AILayer
{
	protected global::Empire Empire
	{
		get
		{
			if (base.AIEntity == null)
			{
				return null;
			}
			return base.AIEntity.Empire;
		}
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.departmentOfTheInterior = this.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfIndustry = this.Empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.departmentOfTheTreasury = this.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfDefense = this.Empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		this.unitBodyDefinitionDatabase = Databases.GetDatabase<UnitBodyDefinition>(false);
		Diagnostics.Assert(this.unitBodyDefinitionDatabase != null);
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		Diagnostics.Assert(this.intelligenceAIHelper != null);
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.game = (Services.GetService<IGameService>().Game as global::Game);
		foreach (string text in this.MinorfactionScores.Keys.ToList<string>())
		{
			this.MinorfactionScores[text] = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Assimilation.RegistryPath, text), 0f);
		}
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILAyer_Assimilation_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILAyer_Assimilation_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILAyer_Assimilation_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		this.departmentOfTheInterior = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfDefense = null;
		this.departmentOfIndustry = null;
		this.unitBodyDefinitionDatabase = null;
		this.intelligenceAIHelper = null;
		this.personalityAIHelper = null;
		this.game = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		float propertyValue = this.Empire.GetPropertyValue(SimulationProperties.MinorFactionSlotCount);
		int count = this.departmentOfTheInterior.AssimilatedFactions.Count;
		bool flag = false;
		if (propertyValue <= (float)count)
		{
			flag = true;
			if (this.game.Turn - 14 <= this.lastAssimilationTurn)
			{
				return;
			}
		}
		List<MinorFaction> list = new List<MinorFaction>();
		this.departmentOfTheInterior.GetAssimilableMinorFactions(ref list);
		if (list.Count == 0)
		{
			return;
		}
		this.UpdateUnitCategories();
		this.FactionToDeassimilate = string.Empty;
		int num = -1;
		float num2 = 0f;
		UnitBodyDefinition[] values = this.unitBodyDefinitionDatabase.GetValues();
		int index = -1;
		float num3 = float.MaxValue;
		for (int i = 0; i < list.Count; i++)
		{
			Faction faction = list[i];
			bool flag2 = this.departmentOfTheInterior.IsAssimilated(faction);
			float num4;
			if (this.MinorfactionScores.TryGetValue(faction.Name, out num4))
			{
				float num5 = (float)this.departmentOfTheInterior.GetNumberOfOwnedMinorFactionVillages(list[i], true);
				float num6 = (float)this.departmentOfTheInterior.GetNumberOfOwnedMinorFactionVillages(list[i], false) - num5;
				float num7 = num4 * (num5 + num6 * 0.5f);
				if (flag2)
				{
					num7 *= 1.7f;
				}
				float num8 = 0f;
				using (IEnumerator<UnitBodyDefinition> enumerator = (from match in values
				where match.Affinity != null && match.Affinity.Name == faction.Affinity.Name
				select match).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						UnitBodyDefinition unitBodyDefinition = enumerator.Current;
						if (flag2)
						{
							int num9 = 0;
							Func<Construction, bool> <>9__2;
							foreach (City gameEntity in this.departmentOfTheInterior.Cities)
							{
								ConstructionQueue constructionQueue = this.departmentOfIndustry.GetConstructionQueue(gameEntity);
								if (constructionQueue != null)
								{
									IEnumerable<Construction> pendingConstructions = constructionQueue.PendingConstructions;
									Func<Construction, bool> selector;
									if ((selector = <>9__2) == null)
									{
										selector = (<>9__2 = ((Construction c) => c.ConstructibleElement is UnitDesign && (c.ConstructibleElement as UnitDesign).UnitBodyDefinition == unitBodyDefinition && (c.GetSpecificConstructionStock(DepartmentOfTheTreasury.Resources.Production) > 0f || c.IsBuyout)));
									}
									int num10 = pendingConstructions.Count(selector);
									num9 += num10;
								}
							}
							if (this.departmentOfTheInterior.Cities.Count < num9 * 4)
							{
								goto IL_2E5;
							}
						}
						float num11 = this.intelligenceAIHelper.GetAIStrengthBelief(base.AIEntity.Empire.Index, unitBodyDefinition.Name);
						if (this.UnitTypes[unitBodyDefinition.SubCategory] == 0 || (flag2 && this.UnitTypes[unitBodyDefinition.SubCategory] == 1))
						{
							num11 = AILayer.Boost(num11, 0.8f);
						}
						num8 = Mathf.Max(num8, num11);
					}
				}
				float num12 = num7 * num8;
				if (!flag2 && (num < 0 || num12 > num2))
				{
					num = i;
					num2 = num12;
				}
				if (flag2 && num12 < num3)
				{
					index = i;
					num3 = num12;
				}
			}
			IL_2E5:;
		}
		if (num < 0 || num2 < 0.7f)
		{
			return;
		}
		if (flag)
		{
			if (num2 <= num3)
			{
				return;
			}
			this.FactionToDeassimilate = list[index].Name;
		}
		Faction faction2 = list[num];
		EvaluableMessage_Assimilation evaluableMessage_Assimilation = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_Assimilation>(BlackboardLayerID.Empire, (EvaluableMessage_Assimilation match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate);
		if (evaluableMessage_Assimilation == null)
		{
			evaluableMessage_Assimilation = new EvaluableMessage_Assimilation(AILayer_AccountManager.AssimilationAccountName);
			base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_Assimilation);
		}
		evaluableMessage_Assimilation.Refresh(1f, 1f, faction2.Name);
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		foreach (EvaluableMessage_Assimilation evaluableMessage_Assimilation in base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_Assimilation>(BlackboardLayerID.Empire, (EvaluableMessage_Assimilation match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate))
		{
			evaluableMessage_Assimilation.UpdateBuyEvaluation("Assimilation", 0UL, DepartmentOfTheInterior.GetAssimilationCost(this.Empire, 0), 2, 0f, 0UL);
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		using (IEnumerator<EvaluableMessage_Assimilation> enumerator = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_Assimilation>(BlackboardLayerID.Empire, (EvaluableMessage_Assimilation match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.ChosenBuyEvaluation != null).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				EvaluableMessage_Assimilation evaluableMessage_Assimilation = enumerator.Current;
				this.validatedAssimilationMessage = evaluableMessage_Assimilation;
				AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_Assimilate));
			}
		}
	}

	private void AssimilationOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderAssimilateFaction orderAssimilateFaction = e.Order as OrderAssimilateFaction;
		if (this.validatedAssimilationMessage != null && orderAssimilateFaction.Instructions.Length != 0 && this.validatedAssimilationMessage.MinorFactionName == orderAssimilateFaction.Instructions[0].FactionName)
		{
			if (e.Result != PostOrderResponse.Processed)
			{
				this.validatedAssimilationMessage.SetFailedToObtain();
				return;
			}
			this.validatedAssimilationMessage.SetObtained();
		}
	}

	private SynchronousJobState SynchronousJob_Assimilate()
	{
		if (this.validatedAssimilationMessage == null)
		{
			return SynchronousJobState.Failure;
		}
		if (this.FactionToDeassimilate != string.Empty)
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("[AILayer_Assimilation] {0}: Sending deassimilation order for faction {1}", new object[]
				{
					this.Empire,
					this.FactionToDeassimilate
				});
			}
			OrderAssimilateFaction order = new OrderAssimilateFaction(this.Empire.Index, this.FactionToDeassimilate, false);
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
			this.FactionToDeassimilate = string.Empty;
			return SynchronousJobState.Running;
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("[AILayer_Assimilation] {0}: Sending assimilation order for faction {1}", new object[]
			{
				this.Empire,
				this.validatedAssimilationMessage.MinorFactionName
			});
		}
		OrderAssimilateFaction order2 = new OrderAssimilateFaction(this.Empire.Index, this.validatedAssimilationMessage.MinorFactionName, true);
		Ticket ticket;
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2, out ticket, new EventHandler<TicketRaisedEventArgs>(this.AssimilationOrder_TicketRaised));
		this.lastAssimilationTurn = this.game.Turn;
		return SynchronousJobState.Success;
	}

	private void UpdateUnitCategories()
	{
		foreach (string key in this.UnitTypes.Keys.ToList<string>())
		{
			this.UnitTypes[key] = 0;
		}
		foreach (UnitBodyDefinition unitBodyDefinition in this.departmentOfDefense.UnitDesignDatabase.AvailableUnitBodyDefinitions)
		{
			if (!unitBodyDefinition.Tags.Contains("Hidden") && !unitBodyDefinition.Tags.Contains("Colossus") && !unitBodyDefinition.Tags.Contains("Settler") && (unitBodyDefinition.SubCategory == "SubCategoryInfantry" || unitBodyDefinition.SubCategory == "SubCategoryCavalry" || unitBodyDefinition.SubCategory == "SubCategoryRanged" || unitBodyDefinition.SubCategory == "SubCategorySupport" || unitBodyDefinition.SubCategory == "SubCategoryFlying"))
			{
				Dictionary<string, int> unitTypes = this.UnitTypes;
				string text = unitBodyDefinition.SubCategory;
				string key2 = text;
				int num = unitTypes[key2];
				unitTypes[key2] = num + 1;
			}
		}
	}

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private IDatabase<UnitBodyDefinition> unitBodyDefinitionDatabase;

	private EvaluableMessage_Assimilation validatedAssimilationMessage;

	private DepartmentOfDefense departmentOfDefense;

	private IPersonalityAIHelper personalityAIHelper;

	private global::Game game;

	private int lastAssimilationTurn;

	private string FactionToDeassimilate;

	private Dictionary<string, float> MinorfactionScores = new Dictionary<string, float>
	{
		{
			"Bos",
			0f
		},
		{
			"Ceratan",
			0f
		},
		{
			"Delvers",
			0f
		},
		{
			"Erycis",
			0f
		},
		{
			"Haunts",
			0f
		},
		{
			"Kazanji",
			0f
		},
		{
			"Silics",
			0f
		},
		{
			"SistersOfMercy",
			0f
		},
		{
			"Urces",
			0f
		},
		{
			"Jotus",
			0f
		},
		{
			"Hurnas",
			0f
		},
		{
			"Gauran",
			0f
		},
		{
			"Birdhive",
			0f
		},
		{
			"Geldirus",
			0f
		},
		{
			"EyelessOnes",
			0f
		},
		{
			"Dorgeshi",
			0f
		},
		{
			"DawnShua",
			0f
		}
	};

	private Dictionary<string, int> UnitTypes = new Dictionary<string, int>
	{
		{
			"SubCategoryInfantry",
			0
		},
		{
			"SubCategoryCavalry",
			0
		},
		{
			"SubCategoryRanged",
			0
		},
		{
			"SubCategorySupport",
			0
		},
		{
			"SubCategoryFlying",
			0
		}
	};

	private static string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Assimilation";

	private DepartmentOfIndustry departmentOfIndustry;
}
