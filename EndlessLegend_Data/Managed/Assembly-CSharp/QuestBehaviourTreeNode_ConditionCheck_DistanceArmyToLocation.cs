using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_ConditionCheck_DistanceArmyToLocation : QuestBehaviourTreeNode_ConditionCheck
{
	public QuestBehaviourTreeNode_ConditionCheck_DistanceArmyToLocation()
	{
		this.Distance = -1;
		this.Location = WorldPosition.Invalid;
		this.ArmyGuid = GameEntityGUID.Zero;
		this.CheckAllArmies = true;
		this.RegionIndex = -1;
		this.ExcludeWaterTile = false;
	}

	[XmlAttribute("LocationVarName")]
	public string LocationVarName { get; set; }

	[XmlAttribute("ArmyGuidVarName")]
	public string ArmyGuidVarName { get; set; }

	[XmlAttribute("CheckAllArmies")]
	public bool CheckAllArmies { get; set; }

	[XmlAttribute("ExcludeWaterTile")]
	public bool ExcludeWaterTile { get; set; }

	[XmlAttribute("DistanceVarName")]
	public string DistanceVarName { get; set; }

	[XmlAttribute("RegionVarName")]
	public string RegionVarName { get; set; }

	[XmlElement]
	public WorldPosition Location { get; set; }

	[XmlElement]
	public GameEntityGUID ArmyGuid { get; set; }

	[XmlElement]
	public int Distance { get; set; }

	[XmlElement]
	public int RegionIndex { get; set; }

	public override State CheckCondition(QuestBehaviour questBehaviour, GameEvent gameEvent, params object[] parameters)
	{
		if (this.Location != WorldPosition.Invalid)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			global::Game x = service.Game as global::Game;
			if (x == null)
			{
				return State.Failure;
			}
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			global::Empire empire;
			if (gameEvent != null)
			{
				empire = (gameEvent.Empire as global::Empire);
			}
			else
			{
				empire = questBehaviour.Initiator;
			}
			DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
			if (this.CheckAllArmies)
			{
				for (int i = 0; i < agency.Armies.Count; i++)
				{
					WorldPosition worldPosition = agency.Armies[i].WorldPosition;
					int distance = service2.GetDistance(this.Location, worldPosition);
					int regionIndex = (int)service2.GetRegionIndex(worldPosition);
					bool flag = service2.IsWaterTile(worldPosition) || service2.IsOceanTile(worldPosition);
					if (distance == -1)
					{
						return State.Failure;
					}
					if (distance <= this.Distance && (this.RegionIndex == -1 || regionIndex == this.RegionIndex) && (!this.ExcludeWaterTile || !flag))
					{
						return State.Success;
					}
				}
			}
			else
			{
				Army army = agency.GetArmy(this.ArmyGuid);
				if (army == null)
				{
					return State.Failure;
				}
				int distance2 = service2.GetDistance(this.Location, army.WorldPosition);
				int regionIndex2 = (int)service2.GetRegionIndex(army.WorldPosition);
				bool flag2 = service2.IsWaterTile(army.WorldPosition) || service2.IsOceanTile(army.WorldPosition);
				if (distance2 == -1)
				{
					return State.Failure;
				}
				if (distance2 <= this.Distance && (this.RegionIndex == -1 || regionIndex2 == this.RegionIndex) && (!this.ExcludeWaterTile || !flag2))
				{
					return State.Success;
				}
			}
		}
		return State.Failure;
	}

	public override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (this.Location == WorldPosition.Invalid)
		{
			WorldPosition location;
			if (!questBehaviour.TryGetQuestVariableValueByName<WorldPosition>(this.LocationVarName, out location))
			{
				Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.LocationVarName
				});
				return false;
			}
			this.Location = location;
		}
		if (this.Distance == -1)
		{
			if (this.DistanceVarName == null)
			{
				Diagnostics.LogError("Cannot retrieve quest variable (varname: Distance)", new object[]
				{
					this.LocationVarName
				});
				return false;
			}
			this.Distance = int.Parse(this.DistanceVarName);
		}
		if (!this.CheckAllArmies)
		{
			ulong num;
			if (this.ArmyGuid == 0UL && !string.IsNullOrEmpty(this.ArmyGuidVarName) && questBehaviour.TryGetQuestVariableValueByName<ulong>(this.ArmyGuidVarName, out num))
			{
				if (num == 0UL)
				{
					Diagnostics.LogError("QuestBehaviourTreeNode_ConditionCheck_IsArmyAlive : Army guid is invalid");
					return false;
				}
				this.ArmyGuid = new GameEntityGUID(num);
			}
		}
		if (this.RegionIndex == -1)
		{
			Region region;
			if (!questBehaviour.TryGetQuestVariableValueByName<Region>(this.RegionVarName, out region))
			{
				Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.RegionVarName
				});
				return false;
			}
			if (region == null)
			{
				Diagnostics.LogError("Region is null or empty, quest variable (varname: '{0}')", new object[]
				{
					this.RegionVarName
				});
				return false;
			}
			this.RegionIndex = region.Index;
		}
		return base.Initialize(questBehaviour);
	}
}
