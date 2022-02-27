using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AIData_Army : AIData_GameEntity, ICommanderMissionProvider
{
	public Army Army
	{
		get
		{
			return base.GameEntity as Army;
		}
	}

	public float SupportScore { get; set; }

	public AIArmyMission ArmyMission { get; set; }

	public AICommanderMission CommanderMission { get; set; }

	public bool IsColossus
	{
		get
		{
			return this.isColossus;
		}
	}

	public bool IsManta
	{
		get
		{
			return this.isManta;
		}
	}

	public bool IsSolitary
	{
		get
		{
			return this.isSolitary;
		}
	}

	public bool IsKaijuArmy
	{
		get
		{
			return this.isKaijuArmy;
		}
	}

	public void AssignArmyMission(AICommander aiCommander, AIArmyMissionDefinition armyMissionDefinition, params object[] parameters)
	{
		if (this.ArmyMission != null)
		{
			this.UnassignArmyMission();
		}
		this.ArmyMission = new AIArmyMission
		{
			AIArmyMissionDefinition = armyMissionDefinition,
			Army = this.Army,
			AICommander = aiCommander
		};
		this.ArmyMission.Initialize(parameters);
	}

	public void UnassignArmyMission()
	{
		if (this.ArmyMission != null)
		{
			this.ArmyMission.Release();
			this.ArmyMission = null;
		}
	}

	public void ResetArmyMission()
	{
		if (this.ArmyMission != null)
		{
			this.ArmyMission.Reset();
		}
	}

	public bool AssignCommanderMission(AICommanderMission commanderMissionOwner)
	{
		if (this.CommanderMission != null)
		{
			Diagnostics.LogWarning(string.Format("LOCKING: {0} tries to recruit Army assigned to {1}", commanderMissionOwner.ToString(), this.CommanderMission.ToString()));
			return false;
		}
		this.CommanderMission = commanderMissionOwner;
		if (this.ArmyMission != null)
		{
			this.UnassignArmyMission();
		}
		IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		if (this.Army != null)
		{
			for (int i = 0; i < this.Army.StandardUnits.Count; i++)
			{
				AIData_Unit aidata_Unit;
				if (service.TryGetAIData<AIData_Unit>(this.Army.StandardUnits[i].GUID, out aidata_Unit))
				{
					if (aidata_Unit.IsUnitLocked())
					{
						if (!aidata_Unit.IsUnitLockedByMe(commanderMissionOwner.InternalGUID) && Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
						{
							Diagnostics.LogWarning(string.Format("{3} {4} LOCKING: Error when assigning commander mission {0} Guid {1}, current infroamtion {2}", new object[]
							{
								commanderMissionOwner.GetType().ToString(),
								commanderMissionOwner.InternalGUID,
								aidata_Unit.GetLockingStateString(),
								this.Army.Empire,
								this.Army.LocalizedName
							}));
						}
						aidata_Unit.ClearLock();
					}
					aidata_Unit.TryLockUnit(commanderMissionOwner.InternalGUID, commanderMissionOwner.GetType().ToString(), AIData_Unit.AIDataReservationExtraTag.GenericCommander, commanderMissionOwner.Commander.GetPriority(commanderMissionOwner));
				}
			}
		}
		return true;
	}

	public void UnassignCommanderMission()
	{
		if (this.CommanderMission != null)
		{
			IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
			if (this.Army != null)
			{
				for (int i = 0; i < this.Army.StandardUnits.Count; i++)
				{
					AIData_Unit aidata_Unit;
					if (service.TryGetAIData<AIData_Unit>(this.Army.StandardUnits[i].GUID, out aidata_Unit) && aidata_Unit.IsUnitLockedByMe(this.CommanderMission.InternalGUID))
					{
						aidata_Unit.TryUnLockUnit(this.CommanderMission.InternalGUID);
					}
				}
			}
		}
		this.CommanderMission = null;
		this.UnassignArmyMission();
	}

	public AIData_Army.AIDataArmyLockState GetArmyLockState()
	{
		if (AIScheduler.Services == null)
		{
			return AIData_Army.AIDataArmyLockState.Free;
		}
		IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		if (this.Army != null)
		{
			GameEntityGUID x = GameEntityGUID.Zero;
			for (int i = 0; i < this.Army.StandardUnits.Count; i++)
			{
				AIData_Unit aidata_Unit;
				if (service.TryGetAIData<AIData_Unit>(this.Army.StandardUnits[i].GUID, out aidata_Unit) && aidata_Unit.IsUnitLocked())
				{
					if (x == GameEntityGUID.Zero)
					{
						x = aidata_Unit.ReservingGUID;
					}
					else if (x != aidata_Unit.ReservingGUID)
					{
						return AIData_Army.AIDataArmyLockState.Hybrid;
					}
				}
			}
			if (x != GameEntityGUID.Zero)
			{
				return AIData_Army.AIDataArmyLockState.Locked;
			}
		}
		return AIData_Army.AIDataArmyLockState.Free;
	}

	public bool IsTaggedFreeForExploration()
	{
		if (AIScheduler.Services == null)
		{
			return false;
		}
		if (this.GetArmyLockState() != AIData_Army.AIDataArmyLockState.Free)
		{
			return false;
		}
		if (this.Army.HasSeafaringUnits())
		{
			return false;
		}
		IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		if (this.Army != null)
		{
			for (int i = 0; i < this.Army.StandardUnits.Count; i++)
			{
				AIData_Unit aidata_Unit;
				if (service.TryGetAIData<AIData_Unit>(this.Army.StandardUnits[i].GUID, out aidata_Unit) && aidata_Unit.ReservationExtraTag != AIData_Unit.AIDataReservationExtraTag.FreeForExploration)
				{
					return false;
				}
			}
		}
		return true;
	}

	public override void Release()
	{
		base.Release();
		this.UnassignCommanderMission();
	}

	public override void RunAIThread()
	{
		base.RunAIThread();
		for (int i = 0; i < this.Army.StandardUnits.Count; i++)
		{
			if (this.Army.StandardUnits[i].SimulationObject.Tags.Contains(DownloadableContent13.UnitTypeManta))
			{
				this.isManta = true;
			}
			if (this.Army.StandardUnits[i].UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
			{
				this.isSolitary = true;
			}
			if (this.Army.StandardUnits[i].UnitDesign.Tags.Contains(DownloadableContent9.TagColossus))
			{
				this.isColossus = true;
			}
			if (this.Army.StandardUnits[i].UnitDesign.Tags.Contains(DownloadableContent20.TagKaijuMonster))
			{
				this.isKaijuArmy = true;
			}
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		this.ArmyMission = reader.ReadElementSerializable<AIArmyMission>();
		if (this.ArmyMission != null)
		{
			this.ArmyMission.Army = this.Army;
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		IXmlSerializable armyMission = this.ArmyMission;
		writer.WriteElementSerializable<IXmlSerializable>(ref armyMission);
	}

	private bool isColossus;

	private bool isSolitary;

	private bool isManta;

	private bool isKaijuArmy;

	public enum AIDataArmyLockState
	{
		Free,
		Locked,
		Hybrid
	}
}
