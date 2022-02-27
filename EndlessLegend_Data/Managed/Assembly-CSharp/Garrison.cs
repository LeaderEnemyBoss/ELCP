using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public abstract class Garrison : SimulationObjectWrapper, Amplitude.Xml.Serialization.IXmlSerializable, IGarrison, IGameEntity
{
	public Garrison(StaticString name) : base(name)
	{
		IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
		if (database != null)
		{
			SimulationDescriptor descriptor;
			if (database.TryGetValue("Garrison", out descriptor))
			{
				base.AddDescriptor(descriptor, false);
			}
			else
			{
				Diagnostics.LogError("Cannot found the Garrison descriptor.");
			}
		}
		this.ExternalEncounters = new List<Encounter>();
	}

	protected Garrison()
	{
		this.ExternalEncounters = new List<Encounter>();
	}

	public event EventHandler<HeroChangeEventArgs> HeroChange;

	public event EventHandler<EventArgs> EncounterChange;

	public event CollectionChangeEventHandler StandardUnitCollectionChange;

	void IGarrison.JoinEncounterAsContender(Encounter encounter)
	{
		this.Encounter = encounter;
		this.OnJoinEncounter();
	}

	void IGarrison.JoinEncounterAsSpectator(Encounter encounter)
	{
		if (!this.ExternalEncounters.Contains(encounter))
		{
			this.ExternalEncounters.Add(encounter);
		}
	}

	void IGarrison.LeaveEncounterAsSpectator(Encounter encounter)
	{
		this.ExternalEncounters.Remove(encounter);
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Units");
		for (int i = 0; i < attribute; i++)
		{
			Unit unit = DepartmentOfDefense.ReadUnit(reader, agency);
			if (unit != null)
			{
				this.AddUnit(unit);
				if (base.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && !unit.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor))
				{
					IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
					SimulationDescriptor descriptor;
					if (database != null && database.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out descriptor))
					{
						unit.AddDescriptor(descriptor, true);
					}
				}
			}
		}
		reader.ReadEndElement("Units");
		Unit unit2 = DepartmentOfDefense.ReadUnit(reader, agency);
		if (unit2 != null && unit2.UnitDesign is UnitProfile)
		{
			this.SetHero(unit2);
			if (base.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && !unit2.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor))
			{
				IDatabase<SimulationDescriptor> database2 = Databases.GetDatabase<SimulationDescriptor>(false);
				SimulationDescriptor descriptor2;
				if (database2 != null && database2.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out descriptor2))
				{
					unit2.AddDescriptor(descriptor2, true);
				}
			}
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("Units");
		writer.WriteAttributeString<int>("Count", this.standardUnits.Count);
		Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable;
		for (int i = 0; i < this.standardUnits.Count; i++)
		{
			xmlSerializable = this.standardUnits[i];
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
		xmlSerializable = this.Hero;
		writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable);
	}

	public virtual int CurrentUnitSlot
	{
		get
		{
			return (int)this.GetPropertyValue(SimulationProperties.UnitSlotCount);
		}
	}

	public virtual Empire Empire { get; set; }

	public Encounter Encounter { get; private set; }

	public List<Encounter> ExternalEncounters { get; private set; }

	public abstract GameEntityGUID GUID { get; }

	public virtual bool InjureHeroOnClean
	{
		get
		{
			return true;
		}
	}

	public bool IsEmpty
	{
		get
		{
			return this.Hero == null && (this.standardUnits == null || this.standardUnits.Count == 0);
		}
	}

	public bool IsInEncounter
	{
		get
		{
			return this.Encounter != null || this.ExternalEncounters.Count > 0;
		}
	}

	public bool IsSettler
	{
		get
		{
			if (this.standardUnits == null)
			{
				return false;
			}
			for (int i = 0; i < this.standardUnits.Count; i++)
			{
				if (this.standardUnits[i].CheckUnitAbility(UnitAbility.ReadonlyColonize, -1) || this.standardUnits[i].CheckUnitAbility(UnitAbility.ReadonlyResettle, -1))
				{
					return true;
				}
			}
			return false;
		}
	}

	public Unit Hero { get; private set; }

	public virtual int MaximumUnitSlot
	{
		get
		{
			if (base.SimulationObject == null)
			{
				return 0;
			}
			return (int)this.GetPropertyValue(SimulationProperties.MaximumUnitSlotCount);
		}
	}

	public ReadOnlyCollection<Unit> StandardUnits
	{
		get
		{
			if (this.readOnlyStandardUnits == null)
			{
				this.readOnlyStandardUnits = this.standardUnits.AsReadOnly();
			}
			return this.readOnlyStandardUnits;
		}
	}

	public Unit[] StandardUnitsAsArray
	{
		get
		{
			return this.standardUnits.ToArray();
		}
	}

	public IEnumerable<Unit> Units
	{
		get
		{
			if (this.Hero != null)
			{
				yield return this.Hero;
			}
			for (int index = 0; index < this.standardUnits.Count; index++)
			{
				yield return this.standardUnits[index];
			}
			yield break;
		}
	}

	public int UnitsCount
	{
		get
		{
			Diagnostics.Assert(this.standardUnits != null);
			int num = this.standardUnits.Count;
			if (this.Hero != null)
			{
				num++;
			}
			return num;
		}
	}

	[XmlIgnore]
	public virtual string LocalizedName
	{
		get
		{
			return string.Empty;
		}
	}

	public virtual void AddUnit(Unit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (!unit.GUID.IsValid)
		{
			Diagnostics.LogError("Cannot add unit with invalid guid.");
			return;
		}
		if (this.standardUnits.Exists((Unit other) => other == unit || other.GUID == unit.GUID))
		{
			Diagnostics.Assert(unit.Garrison == this);
			Diagnostics.LogWarning("Cannot add unit twice to the same garrison, ignoring...");
			return;
		}
		if (unit.Garrison != null)
		{
			Diagnostics.Assert(unit.Garrison != this);
			if (!unit.Garrison.RemoveUnit(unit))
			{
				Diagnostics.LogError("{0} Cannot add unit {1} which is owned by another collection ({2}).", new object[]
				{
					base.Name,
					unit.Name,
					(unit.Garrison as Garrison).Name
				});
				return;
			}
		}
		this.standardUnits.Add(unit);
		this.OnAddUnit(unit);
		unit.ChangeGarrison(this);
		unit.Refresh(false);
		this.OnUnitCollectionChange(CollectionChangeAction.Add, unit);
	}

	public virtual void CleanAfterEncounter(Encounter encounter)
	{
		Diagnostics.Assert(this.Empire != null);
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		agency.CleanGarrisonAfterEncounter(this);
		this.LeaveEncounter();
	}

	public virtual void ClearUnits()
	{
		while (this.StandardUnits.Count > 0)
		{
			this.RemoveUnit(this.StandardUnits[0]);
		}
		if (this.Hero != null)
		{
			this.SetHero(null);
		}
	}

	public virtual bool ContainsUnit(Unit unit)
	{
		return this.Hero == unit || this.standardUnits.Contains(unit);
	}

	public virtual bool ContainsUnit(GameEntityGUID guid)
	{
		return (this.Hero != null && this.Hero.GUID == guid) || this.standardUnits.Exists((Unit match) => match.GUID == guid);
	}

	public virtual Color GetEmpireColor(Empire empireLooking)
	{
		return this.Empire.Color;
	}

	public virtual string GetLocalizedName(Empire empireLooking)
	{
		return string.Empty;
	}

	public bool HasOnlySeafaringUnits(bool includeHero)
	{
		for (int i = 0; i < this.StandardUnits.Count; i++)
		{
			if (!this.StandardUnits[i].IsSeafaring)
			{
				return false;
			}
		}
		return !includeHero || this.Hero == null || this.Hero.IsSeafaring;
	}

	public bool HasSeafaringUnits()
	{
		for (int i = 0; i < this.StandardUnits.Count; i++)
		{
			if (this.StandardUnits[i].IsSeafaring)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasTag(StaticString tag)
	{
		return base.SimulationObject.Tags.Contains(tag);
	}

	public virtual bool IsAcceptingHeroAssignments(out Garrison.IsAcceptingHeroAssignmentReasonsEnum reason)
	{
		DepartmentOfEducation agency = this.Empire.GetAgency<DepartmentOfEducation>();
		if (agency != null && agency.Heroes.Count == 0)
		{
			reason = Garrison.IsAcceptingHeroAssignmentReasonsEnum.NoHeroes;
			return false;
		}
		if (this.standardUnits != null)
		{
			for (int i = 0; i < this.standardUnits.Count; i++)
			{
				if (this.standardUnits[i].UnitDesign != null && this.standardUnits[i].UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
				{
					reason = Garrison.IsAcceptingHeroAssignmentReasonsEnum.IsSolitary;
					return false;
				}
			}
		}
		reason = Garrison.IsAcceptingHeroAssignmentReasonsEnum.None;
		return true;
	}

	public virtual void UpdateLifeAfterEncounter(Encounter encounter)
	{
		Diagnostics.Assert(this.Empire != null);
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		agency.UpdateLifeAfterEncounter(this);
	}

	public virtual bool RemoveUnit(Unit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (!unit.GUID.IsValid)
		{
			Diagnostics.LogError("Cannot remove unit with invalid guid.");
			return false;
		}
		if (!this.standardUnits.Exists((Unit other) => other == unit || other.GUID == unit.GUID))
		{
			Diagnostics.Assert(unit.Garrison != this);
			Diagnostics.LogWarning("{0}: Cannot remove unit {1} that does not belong to the garrison, ignoring...", new object[]
			{
				base.Name,
				unit.Name
			});
			return false;
		}
		Diagnostics.Assert(unit.Garrison == this);
		unit.ChangeGarrison(null);
		this.standardUnits.Remove(unit);
		this.OnRemoveUnit(unit);
		this.OnUnitCollectionChange(CollectionChangeAction.Remove, unit);
		this.Refresh(false);
		return true;
	}

	public virtual bool RemoveUnit(GameEntityGUID guid)
	{
		if (!guid.IsValid)
		{
			Diagnostics.LogError("Invalid guid.");
			return false;
		}
		Unit unit = this.standardUnits.Find((Unit other) => other.GUID == guid);
		if (unit == null)
		{
			Diagnostics.LogWarning("Cannot remove unit with guid #{0} because it does not belong to the army.", new object[]
			{
				guid.ToString()
			});
			return false;
		}
		return this.RemoveUnit(unit);
	}

	public virtual void SetHero(Unit hero)
	{
		if (this.Hero == hero)
		{
			return;
		}
		Unit hero2 = this.Hero;
		if (hero2 != null)
		{
			this.Hero = null;
			hero2.ChangeGarrison(null);
			base.RemoveChild(hero2);
		}
		this.Hero = hero;
		if (this.Hero != null)
		{
			base.AddChild(this.Hero);
			this.Hero.ChangeGarrison(this);
		}
		this.OnHeroChange(hero2);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing && this.standardUnits != null)
		{
			for (int i = 0; i < this.standardUnits.Count; i++)
			{
				this.standardUnits[i].Dispose();
			}
		}
		if (this.standardUnits != null)
		{
			this.standardUnits.Clear();
		}
		if (this.Hero != null)
		{
			this.Hero = null;
		}
		this.Empire = null;
	}

	protected void LeaveEncounter()
	{
		this.Encounter = null;
		this.OnLeaveEncounter();
	}

	protected virtual void OnAddUnit(Unit unit)
	{
		base.AddChild(unit);
	}

	protected virtual void OnRemoveUnit(Unit unit)
	{
		base.RemoveChild(unit);
	}

	private void OnHeroChange(Unit lastHero)
	{
		if (this.HeroChange != null)
		{
			this.HeroChange(this, new HeroChangeEventArgs(lastHero, this.Hero));
		}
		this.Refresh(false);
	}

	private void OnUnitCollectionChange(CollectionChangeAction action, object element)
	{
		if (this.StandardUnitCollectionChange != null)
		{
			this.StandardUnitCollectionChange(this, new CollectionChangeEventArgs(action, element));
		}
	}

	private void OnJoinEncounter()
	{
		if (this.EncounterChange != null)
		{
			this.EncounterChange(this, new EventArgs());
		}
	}

	private void OnLeaveEncounter()
	{
		if (this.EncounterChange != null)
		{
			this.EncounterChange(this, new EventArgs());
		}
	}

	private List<Unit> standardUnits = new List<Unit>(20);

	private ReadOnlyCollection<Unit> readOnlyStandardUnits;

	public enum IsAcceptingHeroAssignmentReasonsEnum
	{
		None,
		NoHeroes,
		NoTechnologyShip,
		IsSolitary,
		IsMinor,
		IsCatspaw,
		IsKaiju
	}
}
