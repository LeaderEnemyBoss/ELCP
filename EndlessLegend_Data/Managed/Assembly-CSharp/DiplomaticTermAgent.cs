using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Amas.Simulation;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public abstract class DiplomaticTermAgent : SimulationAgent
{
	private protected DiplomaticRelationScore AttitudeScore { protected get; private set; }

	private protected DiplomaticRelation DiplomaticRelation { protected get; private set; }

	private protected global::Empire Empire { protected get; private set; }

	private protected global::Empire EmpireWhichReceives { protected get; private set; }

	public override void Initialize(AgentDefinition agentDefinition, AgentGroup parentGroup)
	{
		base.Initialize(agentDefinition, parentGroup);
		Diagnostics.Assert(base.ParentGroup != null && base.ParentGroup.Parent != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire = base.ParentGroup.Parent.ContextObject as AIPlayer_MajorEmpire;
		Diagnostics.Assert(aiplayer_MajorEmpire != null);
		this.Empire = aiplayer_MajorEmpire.MajorEmpire;
		if (this.Empire == null)
		{
			Diagnostics.LogError("The agent's parent context object is not an empire");
			return;
		}
		this.EmpireWhichReceives = (base.ParentGroup.ContextObject as global::Empire);
		if (this.EmpireWhichReceives == null)
		{
			Diagnostics.LogError("The agent context object is not an empire");
			return;
		}
		this.aiEntityEmpire = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		if (this.aiEntityEmpire == null)
		{
			Diagnostics.LogError("The AIPlayer has no ai entity empire.");
			return;
		}
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(this.aiEntityEmpire.Empire, this);
	}

	public override void Release()
	{
		this.Empire = null;
		this.EmpireWhichReceives = null;
		this.AttitudeScore = null;
		this.aiEntityEmpire = null;
		this.DiplomacyLayer = null;
		this.VictoryLayer = null;
		base.Release();
	}

	public override void Reset()
	{
		this.DiplomacyLayer = this.aiEntityEmpire.GetLayer<AILayer_Diplomacy>();
		this.VictoryLayer = this.aiEntityEmpire.GetLayer<AILayer_Victory>();
		AILayer_Attitude layer = this.aiEntityEmpire.GetLayer<AILayer_Attitude>();
		Diagnostics.Assert(layer != null);
		AILayer_Attitude.Attitude attitude = layer.GetAttitude(this.EmpireWhichReceives);
		if (attitude == null)
		{
			Diagnostics.LogError("Can't retrieve attitude between {0} and {1}.", new object[]
			{
				this.Empire,
				this.EmpireWhichReceives
			});
			base.Enable = false;
			return;
		}
		this.AttitudeScore = attitude.Score;
		if (this.AttitudeScore == null)
		{
			base.Enable = false;
			return;
		}
		DepartmentOfForeignAffairs agency = this.aiEntityEmpire.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		this.DiplomaticRelation = agency.GetDiplomaticRelation(this.EmpireWhichReceives);
		if (this.DiplomaticRelation == null)
		{
			base.Enable = false;
			return;
		}
		base.Reset();
		bool enable = base.Enable;
	}

	protected float GetValueFromAttitude()
	{
		if (this.attitudeMultipliers == null)
		{
			this.CacheAttitudeMultipliers();
		}
		float num = 0f;
		foreach (DiplomaticRelationScore.ModifiersData modifiersData in this.AttitudeScore.MofifiersDatas)
		{
			float num2;
			if (this.attitudeMultipliers.TryGetValue(modifiersData.Name, out num2) && num2 != 0f)
			{
				num += Mathf.Abs(modifiersData.TotalValue) * num2;
			}
		}
		return num;
	}

	protected float GetAbsAttitudeScoreByName(StaticString modifierName)
	{
		return Mathf.Abs(this.AttitudeScore.GetScoreByName(modifierName));
	}

	protected float ClampedBoost(float score, float boostFactor)
	{
		if (score < 0f)
		{
			return -AILayer.Boost(Mathf.Clamp01(-score), Mathf.Clamp(-boostFactor, -0.95f, 0.95f));
		}
		return AILayer.Boost(Mathf.Clamp01(score), Mathf.Clamp(boostFactor, -0.95f, 0.95f));
	}

	protected bool HasOtherEmpireClosedTheirBorders()
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		Diagnostics.Assert(service.Game is global::Game);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game.Empires.Length > this.DiplomaticRelation.OtherEmpireIndex);
		global::Empire empire = (service.Game as global::Game).Empires[this.DiplomaticRelation.OtherEmpireIndex];
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(this.Empire);
		Diagnostics.Assert(diplomaticRelation != null);
		return diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.CloseBorders);
	}

	protected bool AreTradeRoutesPossibleWithEmpire()
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		Diagnostics.Assert(service.Game is global::Game);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		Diagnostics.Assert(this.DiplomaticRelation != null && this.DiplomaticRelation.State != null);
		if (!this.DiplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.TradeRoute))
		{
			return false;
		}
		Func<City, bool> func = delegate(City city)
		{
			if (city.CadastralMap.ConnectedMovementCapacity == PathfindingMovementCapacity.None)
			{
				return false;
			}
			if (city.BesiegingEmpireIndex != -1)
			{
				return false;
			}
			int num = Mathf.FloorToInt(city.GetPropertyValue(SimulationProperties.MaximumNumberOfTradeRoutes));
			return num > 0;
		};
		foreach (City city2 in this.Empire.GetAgency<DepartmentOfTheInterior>().Cities)
		{
			if (func(city2))
			{
				foreach (Region.Border border in city2.Region.Borders)
				{
					Region region = service2.GetRegion(border.NeighbourRegionIndex);
					if (region.City != null && region.City.Empire.Index == this.EmpireWhichReceives.Index && func(region.City))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private void CacheAttitudeMultipliers()
	{
		IPersonalityAIHelper service = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		Diagnostics.Assert(service != null);
		this.attitudeMultipliers = new Dictionary<StaticString, float>();
		string arg = string.Format("AI/AgentDefinition/{0}", base.GetType().Name);
		for (int i = 0; i < AILayer_Attitude.AttitudeScoreDefinitionReferences.All.Length; i++)
		{
			StaticString staticString = AILayer_Attitude.AttitudeScoreDefinitionReferences.All[i];
			string regitryPath = string.Format("{0}/{1}{2}", arg, staticString.ToString().Substring("AttitudeScore".Length), "Multiplier");
			float registryValue = service.GetRegistryValue<float>(this.Empire, regitryPath, 0f);
			if (registryValue != 0f)
			{
				this.attitudeMultipliers[staticString] = registryValue;
			}
		}
	}

	private AIEntity aiEntityEmpire;

	private Dictionary<StaticString, float> attitudeMultipliers;

	protected AILayer_Diplomacy DiplomacyLayer;

	protected AILayer_Victory VictoryLayer;
}
