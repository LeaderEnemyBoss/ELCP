using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

[Diagnostics.TagAttribute("AI")]
public abstract class AICommander : IXmlSerializable
{
	public AICommander(AICommanderMissionDefinition.AICommanderCategory category = AICommanderMissionDefinition.AICommanderCategory.Undefined)
	{
		this.Category = category;
		this.Missions = new List<AICommanderMission>();
		this.ForceArmyGUID = GameEntityGUID.Zero;
		this.InternalGUID = GameEntityGUID.Zero;
	}

	public virtual void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		if (num >= 5)
		{
			this.InternalGUID = reader.GetAttribute<ulong>("InternalGUID");
		}
		if (this.InternalGUID == GameEntityGUID.Zero)
		{
			this.InternalGUID = AIScheduler.Services.GetService<IAIEntityGUIDAIHelper>().GenerateAIEntityGUID();
		}
		this.ForceArmyGUID = reader.GetAttribute<ulong>("ForceArmyGUID", 0UL);
		reader.ReadStartElement();
		try
		{
			if (reader.IsStartElement("MissionList") && reader.IsEmptyElement())
			{
				reader.Skip();
			}
			else
			{
				reader.ReadStartElement("MissionList");
				while (reader.IsStartElement())
				{
					string attribute = reader.GetAttribute("AssemblyQualifiedName");
					if (!string.IsNullOrEmpty(attribute))
					{
						Type type = Type.GetType(attribute);
						AICommanderMission aicommanderMission = (AICommanderMission)Activator.CreateInstance(type);
						aicommanderMission.Initialize(this);
						reader.ReadElementSerializable<AICommanderMission>(ref aicommanderMission);
						this.Missions.Add(aicommanderMission);
						Diagnostics.Assert(AIScheduler.Services != null);
						ITickableRepositoryAIHelper service = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
						Diagnostics.Assert(service != null);
						service.Register(aicommanderMission);
					}
					else
					{
						reader.Skip();
					}
				}
				reader.ReadEndElement("MissionList");
			}
		}
		catch (Exception ex)
		{
			Diagnostics.LogError("Fail to load the mission: {0}", new object[]
			{
				ex.ToString()
			});
			reader.Skip();
		}
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(5);
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteAttributeString<ulong>("InternalGUID", this.InternalGUID);
		writer.WriteAttributeString<ulong>("ForceArmyGUID", this.ForceArmyGUID);
		writer.WriteStartElement("MissionList");
		for (int i = 0; i < this.Missions.Count; i++)
		{
			if (this.Missions[i].IsActive)
			{
				IXmlSerializable xmlSerializable = this.Missions[i];
				writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
			}
		}
		writer.WriteEndElement();
	}

	~AICommander()
	{
	}

	public AIPlayer AIPlayer { get; set; }

	public AICommanderMissionDefinition.AICommanderCategory Category { get; set; }

	public global::Empire Empire { get; set; }

	public GameEntityGUID ForceArmyGUID { get; set; }

	public GameEntityGUID InternalGUID { get; set; }

	public List<AICommanderMission> Missions { get; set; }

	public static AICommanderMissionDefinition GetFirstMissionDefinitionFromCategory(StaticString tag)
	{
		IDatabase<AICommanderMissionDefinition> database = Databases.GetDatabase<AICommanderMissionDefinition>(false);
		Diagnostics.Assert(database != null);
		foreach (AICommanderMissionDefinition aicommanderMissionDefinition in database)
		{
			if (aicommanderMissionDefinition.Category.Contains(tag))
			{
				return aicommanderMissionDefinition;
			}
		}
		return null;
	}

	public virtual bool CanEndTurn()
	{
		if (this.Missions.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < this.Missions.Count; i++)
		{
			if (this.Missions[i].State == TickableState.NeedTick)
			{
				return false;
			}
		}
		return true;
	}

	public virtual void CheckObjectiveInProgress(bool forceStop = false)
	{
		for (int i = this.Missions.Count - 1; i >= 0; i--)
		{
			AICommanderMission aicommanderMission = this.Missions[i];
			if (aicommanderMission.IsActive)
			{
				switch (aicommanderMission.Completion)
				{
				case AICommanderMission.AICommanderMissionCompletion.Initializing:
					if (forceStop)
					{
						this.CancelMission(this.Missions[i]);
					}
					break;
				case AICommanderMission.AICommanderMissionCompletion.Success:
					this.CancelMission(this.Missions[i]);
					break;
				case AICommanderMission.AICommanderMissionCompletion.Fail:
					this.CancelMission(this.Missions[i]);
					break;
				case AICommanderMission.AICommanderMissionCompletion.Interrupted:
					this.CancelMission(this.Missions[i]);
					break;
				}
			}
		}
	}

	public int ComputeCommanderMissionNumber(AICommanderMissionDefinition.AICommanderCategory category)
	{
		return 0;
	}

	public void CreateMission()
	{
		this.PopulateMission();
		this.EvaluateMission();
		this.PromoteMission();
	}

	public virtual void EvaluateMission()
	{
	}

	public abstract float GetPriority(AICommanderMission mission);

	public virtual float GetPillageModifier(AICommanderMission mission)
	{
		return 1f;
	}

	public virtual void Initialize()
	{
		if (this.InternalGUID == GameEntityGUID.Zero)
		{
			this.InternalGUID = AIScheduler.Services.GetService<IAIEntityGUIDAIHelper>().GenerateAIEntityGUID();
		}
		this.endTurnSerice = Services.GetService<IEndTurnService>();
		IGameService service = Services.GetService<IGameService>();
		if (service != null)
		{
			this.seasonService = service.Game.Services.GetService<ISeasonService>();
		}
	}

	public virtual bool IsMissionFinished(bool forceStop)
	{
		for (int i = 0; i < this.Missions.Count; i++)
		{
			if (!forceStop || this.Missions[i].Completion != AICommanderMission.AICommanderMissionCompletion.Initializing)
			{
				if (this.Missions[i].Completion != AICommanderMission.AICommanderMissionCompletion.Fail && this.Missions[i].Completion != AICommanderMission.AICommanderMissionCompletion.Success && this.Missions[i].Completion != AICommanderMission.AICommanderMissionCompletion.Interrupted && this.Missions[i].Completion != AICommanderMission.AICommanderMissionCompletion.Cancelled)
				{
					return false;
				}
			}
		}
		return true;
	}

	public virtual void Load()
	{
		for (int i = 0; i < this.Missions.Count; i++)
		{
			this.Missions[i].Load();
		}
	}

	public abstract void PopulateMission();

	public void PromoteMission()
	{
		if (this.Missions.Count == 0)
		{
			return;
		}
		for (int i = 0; i < this.Missions.Count; i++)
		{
			if (!this.Missions[i].IsActive)
			{
				this.Missions[i].Promote();
			}
		}
	}

	public virtual void RefreshMission()
	{
		for (int i = 0; i < this.Missions.Count; i++)
		{
			this.Missions[i].State = TickableState.NeedTick;
			this.Missions[i].Refresh();
		}
	}

	public virtual void RefreshObjective()
	{
	}

	public virtual void Release()
	{
		for (int i = this.Missions.Count - 1; i >= 0; i--)
		{
			this.CancelMission(this.Missions[i]);
		}
		this.Missions.Clear();
		this.seasonService = null;
		this.ForceArmyGUID = GameEntityGUID.Zero;
		this.Empire = null;
	}

	public bool MayUseFrozenTiles()
	{
		if (this.Empire == null)
		{
			return false;
		}
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		if (agency != null && agency.TechnologyDefinitionShipState == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			return true;
		}
		Season nextSeason = this.seasonService.GetNextSeason(null);
		if (nextSeason != null && nextSeason.SeasonDefinition.SeasonType == Season.ReadOnlyWinter)
		{
			int num = 2;
			return this.endTurnSerice.Turn - nextSeason.StartTurn >= num;
		}
		return false;
	}

	public bool MayUseShift(AICommanderMission mission)
	{
		if (mission == null)
		{
			return false;
		}
		if (StaticString.IsNullOrEmpty(mission.SeasonToSwitchTo))
		{
			return false;
		}
		Season currentSeason = this.seasonService.GetCurrentSeason();
		return currentSeason != null && currentSeason.SeasonDefinition.SeasonType != mission.SeasonToSwitchTo;
	}

	public bool HasMissionRunning()
	{
		for (int i = 0; i < this.Missions.Count; i++)
		{
			if (this.Missions[i].Completion == AICommanderMission.AICommanderMissionCompletion.Pending || this.Missions[i].Completion == AICommanderMission.AICommanderMissionCompletion.Running)
			{
				if (this.Missions[i].AIDataArmyGUID.IsValid)
				{
					return true;
				}
			}
		}
		return false;
	}

	protected virtual void CancelMission(AICommanderMission mission)
	{
		this.Missions.Remove(mission);
		mission.Release();
	}

	protected virtual AICommanderMission GenerateMission(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		if (!string.IsNullOrEmpty(missionDefinition.Type.ToString()))
		{
			Type type = Type.GetType(missionDefinition.Type.ToString());
			AICommanderMission aicommanderMission = (AICommanderMission)Activator.CreateInstance(type);
			aicommanderMission.Initialize(this);
			aicommanderMission.SetParameters(missionDefinition, parameters);
			aicommanderMission.Load();
			return aicommanderMission;
		}
		return null;
	}

	protected AICommanderMission PopulationFirstMissionFromCategory(StaticString tag, params object[] parameters)
	{
		IDatabase<AICommanderMissionDefinition> database = Databases.GetDatabase<AICommanderMissionDefinition>(false);
		Diagnostics.Assert(database != null);
		foreach (AICommanderMissionDefinition aicommanderMissionDefinition in database)
		{
			if (aicommanderMissionDefinition.Category.Contains(tag))
			{
				AICommanderMission aicommanderMission = this.GenerateMission(aicommanderMissionDefinition, parameters);
				if (aicommanderMission != null)
				{
					this.Missions.Add(aicommanderMission);
					return aicommanderMission;
				}
			}
		}
		return null;
	}

	protected AICommanderMission PopulationFirstMissionFromCategory(Tags tags, params object[] parameters)
	{
		IDatabase<AICommanderMissionDefinition> database = Databases.GetDatabase<AICommanderMissionDefinition>(false);
		Diagnostics.Assert(database != null);
		foreach (AICommanderMissionDefinition aicommanderMissionDefinition in database)
		{
			if (aicommanderMissionDefinition.Category.Contains(tags))
			{
				AICommanderMission aicommanderMission = this.GenerateMission(aicommanderMissionDefinition, parameters);
				if (aicommanderMission != null)
				{
					this.Missions.Add(aicommanderMission);
					return aicommanderMission;
				}
			}
		}
		return null;
	}

	private bool TryGetMissionByClassSortedByScore(string missionClass, out List<AICommanderMission> missionList)
	{
		missionList = null;
		if (string.IsNullOrEmpty(missionClass))
		{
			Diagnostics.Assert("Invalide Class of Mission is Null Or Empty", new object[0]);
			return false;
		}
		missionList = new List<AICommanderMission>();
		for (int i = 0; i < this.Missions.Count; i++)
		{
			if (this.Missions[i].GetType() == Type.GetType(missionClass))
			{
				missionList.Add(this.Missions[i]);
			}
		}
		if (missionList.Count > 0)
		{
			missionList.Sort((AICommanderMission left, AICommanderMission right) => -1 * left.Score.CompareTo(right.Score));
			return true;
		}
		return false;
	}

	private ISeasonService seasonService;

	private IEndTurnService endTurnSerice;
}
