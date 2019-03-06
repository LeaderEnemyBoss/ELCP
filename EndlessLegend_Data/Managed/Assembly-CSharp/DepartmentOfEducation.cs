using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[OrderProcessor(typeof(OrderGenerateHero), "GenerateHero")]
[OrderProcessor(typeof(OrderCaptureHero), "CaptureHero")]
[OrderProcessor(typeof(OrderImmolateUnits), "ImmolateUnits")]
[OrderProcessor(typeof(OrderForceShiftUnits), "ForceShiftUnits")]
[OrderProcessor(typeof(OrderChangeHeroAssignment), "ChangeHeroAssignment")]
[OrderProcessor(typeof(OrderInjureHero), "InjureHero")]
[OrderProcessor(typeof(OrderInjureHeroByInfiltration), "InjureHeroByInfiltration")]
[OrderProcessor(typeof(OrderReleasePrisoner), "ReleasePrisoner")]
[OrderProcessor(typeof(OrderRemoveHero), "RemoveHero")]
[OrderProcessor(typeof(OrderRestoreHero), "RestoreHero")]
[OrderProcessor(typeof(OrderUnlockUnitSkillLevel), "UnlockUnitSkillLevel")]
[OrderProcessor(typeof(OrderCaptureHeroByInfiltration), "CaptureHeroByInfiltration")]
public class DepartmentOfEducation : Agency, IXmlSerializable
{
	public DepartmentOfEducation(global::Empire empire)
	{
		this.prisoners = new List<Prisoner>();
		this.myCapturedHeroes = new List<CapturedHero>();
		base..ctor(empire);
	}

	public event CollectionChangeEventHandler VaultItemsCollectionChange;

	public event CollectionChangeEventHandler HeroCollectionChange;

	public List<Prisoner> Prisoners
	{
		get
		{
			return this.prisoners;
		}
	}

	public static int GetNumberOfTurnBeforeRelease(Unit unit, global::Empire jailer)
	{
		int result = 0;
		DepartmentOfEducation agency = jailer.GetAgency<DepartmentOfEducation>();
		for (int i = 0; i < agency.prisoners.Count; i++)
		{
			if (agency.prisoners[i].UnitGuid == unit.GUID)
			{
				result = DepartmentOfEducation.ComputeNumberOfTurn(unit, agency.prisoners[i]);
			}
		}
		return result;
	}

	public static void ReleaseCapturedHero(GameEntityGUID unitGuid, global::Empire jailer, global::Empire owner, bool isReleaseHarmless)
	{
		DepartmentOfEducation agency = jailer.GetAgency<DepartmentOfEducation>();
		DepartmentOfEducation agency2 = owner.GetAgency<DepartmentOfEducation>();
		agency2.ReleaseCapturedHeroBy(unitGuid, isReleaseHarmless);
		agency.ReleaseCapturedHeroFrom(unitGuid);
	}

	public static float GetPrisonerUpkeep(global::Empire observer, GameEntityGUID guid)
	{
		DepartmentOfEducation agency = observer.GetAgency<DepartmentOfEducation>();
		for (int i = 0; i < agency.myCapturedHeroes.Count; i++)
		{
			if (agency.myCapturedHeroes[i].Hero.GUID == guid)
			{
				return 0f;
			}
		}
		for (int j = 0; j < agency.Prisoners.Count; j++)
		{
			if (agency.Prisoners[j].UnitGuid == guid)
			{
				return agency.Prisoners[j].PrisonerSimulationObject.GetPropertyValue("JailUpkeep");
			}
		}
		return 0f;
	}

	public global::Empire GetJailerEmpire(GameEntityGUID heroGuid)
	{
		int i = 0;
		while (i < this.myCapturedHeroes.Count)
		{
			if (this.myCapturedHeroes[i].Hero.GUID == heroGuid)
			{
				if (!this.myCapturedHeroes[i].SpyNoticed)
				{
					return null;
				}
				return this.myCapturedHeroes[i].JailerEmpire;
			}
			else
			{
				i++;
			}
		}
		int j = this.prisoners.Count - 1;
		while (j >= 0)
		{
			if (this.prisoners[j].UnitGuid == heroGuid)
			{
				if (!this.prisoners[j].CaptureNoticed)
				{
					return null;
				}
				return base.Empire as global::Empire;
			}
			else
			{
				j--;
			}
		}
		return null;
	}

	public int GetNumberOfTurnBeforeRelease(GameEntityGUID heroGuid)
	{
		for (int i = 0; i < this.myCapturedHeroes.Count; i++)
		{
			if (this.myCapturedHeroes[i].Hero.GUID == heroGuid)
			{
				return DepartmentOfEducation.GetNumberOfTurnBeforeRelease(this.myCapturedHeroes[i].Hero, this.myCapturedHeroes[i].JailerEmpire);
			}
		}
		int j = this.prisoners.Count - 1;
		while (j >= 0)
		{
			if (this.prisoners[j].UnitGuid == heroGuid)
			{
				IGameEntity gameEntity;
				if (this.GameEntityRepositoryService.TryGetValue(this.prisoners[j].UnitGuid, out gameEntity) && gameEntity is Unit)
				{
					return DepartmentOfEducation.ComputeNumberOfTurn(gameEntity as Unit, this.prisoners[j]);
				}
				break;
			}
			else
			{
				j--;
			}
		}
		return 0;
	}

	public bool ReleaseCapturedHero(GameEntityGUID unitGuid, bool isReleaseHarmless)
	{
		for (int i = 0; i < this.myCapturedHeroes.Count; i++)
		{
			if (this.myCapturedHeroes[i].Hero.GUID == unitGuid)
			{
				DepartmentOfEducation.ReleaseCapturedHero(unitGuid, this.myCapturedHeroes[i].JailerEmpire, base.Empire as global::Empire, isReleaseHarmless);
				return true;
			}
		}
		for (int j = 0; j < this.prisoners.Count; j++)
		{
			if (this.prisoners[j].UnitGuid == unitGuid)
			{
				DepartmentOfEducation.ReleaseCapturedHero(unitGuid, base.Empire as global::Empire, (this.GameService.Game as global::Game).Empires[this.prisoners[j].OwnerEmpireIndex], isReleaseHarmless);
				return true;
			}
		}
		return false;
	}

	internal static void CaptureHero(Unit unit, global::Empire jailerEmpire, global::Empire heroOwnerEmpire, bool noticed, bool notify = true)
	{
		DepartmentOfEducation agency = heroOwnerEmpire.GetAgency<DepartmentOfEducation>();
		if (unit.CheckUnitAbility(UnitAbility.ReadonlyElusive, -1))
		{
			agency.InjureHero(unit, notify);
			return;
		}
		DepartmentOfEducation agency2 = jailerEmpire.GetAgency<DepartmentOfEducation>();
		agency.RegisterCapturedHeroBy(unit, jailerEmpire, noticed, notify);
		agency2.RegisterCapturedHeroFrom(unit, heroOwnerEmpire, noticed, notify);
	}

	private static int ComputeNumberOfTurn(Unit unit, Prisoner prisoner)
	{
		float propertyValue = prisoner.PrisonerSimulationObject.GetPropertyValue(SimulationProperties.JailPower);
		float propertyValue2 = unit.GetPropertyValue(SimulationProperties.JailHeroPower);
		float propertyValue3 = unit.GetPropertyValue(SimulationProperties.JailHeroGainPerTurn);
		if (propertyValue3 <= 0f)
		{
			return int.MaxValue;
		}
		return Mathf.CeilToInt((propertyValue - propertyValue2) / propertyValue3);
	}

	private void InitializeJail()
	{
		this.jail = new SimulationObject("Jail");
		SimulationDescriptor descriptor;
		if (this.SimulationDescriptorDatabase.TryGetValue("ClassJail", out descriptor))
		{
			this.jail.AddDescriptor(descriptor);
		}
		if (this.SimulationDescriptorDatabase.TryGetValue("Garrison", out descriptor))
		{
			this.jail.AddDescriptor(descriptor);
		}
		if (this.SimulationDescriptorDatabase.TryGetValue("ClassPrisoner", out descriptor))
		{
			this.prisonerDescriptor = descriptor;
		}
		if (this.SimulationDescriptorDatabase.TryGetValue("HeroStatusCaptured", out descriptor))
		{
			this.capturedStatusDescriptor = descriptor;
		}
		base.Empire.SimulationObject.AddChild(this.jail);
	}

	private void ReleaseJail()
	{
		base.Empire.SimulationObject.RemoveChild(this.jail);
		for (int i = 0; i < this.prisoners.Count; i++)
		{
			this.jail.RemoveChild(this.prisoners[i].PrisonerSimulationObject);
			this.prisoners[i].PrisonerSimulationObject.Dispose();
		}
		this.prisoners.Clear();
		this.myCapturedHeroes.Clear();
	}

	private IEnumerator GameClient_EndTurn_VerifyCapturedHeroCooldown(string context, string name)
	{
		global::Game game = this.GameService.Game as global::Game;
		for (int index = this.prisoners.Count - 1; index >= 0; index--)
		{
			IGameEntity gameEntity;
			if (!this.GameEntityRepositoryService.TryGetValue(this.prisoners[index].UnitGuid, out gameEntity) || !(gameEntity is Unit))
			{
				this.ReleaseCapturedHeroFrom(this.prisoners[index]);
			}
			else
			{
				Unit hero = gameEntity as Unit;
				float jailPower = this.prisoners[index].PrisonerSimulationObject.GetPropertyValue(SimulationProperties.JailPower);
				float jailHeroPower = hero.GetPropertyValue(SimulationProperties.JailHeroPower);
				float jailHeroPowerPerTurn = hero.GetPropertyValue(SimulationProperties.JailHeroGainPerTurn);
				jailHeroPower += jailHeroPowerPerTurn;
				hero.SetPropertyBaseValue(SimulationProperties.JailHeroPower, jailHeroPower);
				if (jailHeroPower > jailPower)
				{
					DepartmentOfEducation ownerEducation = game.Empires[this.prisoners[index].OwnerEmpireIndex].GetAgency<DepartmentOfEducation>();
					ownerEducation.ReleaseCapturedHeroBy(this.prisoners[index].UnitGuid, false);
					this.ReleaseCapturedHeroFrom(this.prisoners[index]);
					this.OnHeroCollectionChange(null, CollectionChangeAction.Refresh);
				}
				else
				{
					float heroUpkeep = hero.GetPropertyValue(SimulationProperties.HeroUpkeep);
					this.prisoners[index].PrisonerSimulationObject.SetPropertyBaseValue(SimulationProperties.JailHeroUpkeep, heroUpkeep);
				}
			}
		}
		yield break;
	}

	private bool ReleaseCapturedHeroFrom(GameEntityGUID unitGuid)
	{
		for (int i = 0; i < this.prisoners.Count; i++)
		{
			if (this.prisoners[i].UnitGuid == unitGuid)
			{
				return this.ReleaseCapturedHeroFrom(this.prisoners[i]);
			}
		}
		return false;
	}

	private bool ReleaseCapturedHeroFrom(Prisoner prisoner)
	{
		this.jail.RemoveChild(prisoner.PrisonerSimulationObject);
		prisoner.PrisonerSimulationObject.Dispose();
		this.jail.Refresh();
		return this.prisoners.Remove(prisoner);
	}

	private bool ReleaseCapturedHeroBy(GameEntityGUID unitGuid, bool isReleaseHarmless = false)
	{
		for (int i = 0; i < this.myCapturedHeroes.Count; i++)
		{
			Unit hero = this.myCapturedHeroes[i].Hero;
			if (hero.GUID == unitGuid)
			{
				base.Empire.AddChild(hero);
				hero.RemoveDescriptor(this.capturedStatusDescriptor);
				hero.SetPropertyBaseValue(SimulationProperties.AssignmentCooldown, 2.14748365E+09f);
				if (!isReleaseHarmless)
				{
					this.InjureHero(hero, true);
					UnitEquipmentSet unitEquipmentSet = hero.UnitDesign.DefaultUnitEquipmentSet.Clone() as UnitEquipmentSet;
					DepartmentOfDefense.RemoveEquipmentSet(hero);
					UnitDesign unitDesign = hero.UnitDesign;
					unitDesign.UnitEquipmentSet = unitEquipmentSet;
					unitDesign.ResetCaches();
					hero.RetrofitTo(unitDesign);
					hero.UpdateExperienceReward(base.Empire);
					if (hero.Garrison is SimulationObjectWrapper)
					{
						(hero.Garrison as SimulationObjectWrapper).Refresh(false);
					}
				}
				else
				{
					float propertyValue = hero.GetPropertyValue(SimulationProperties.MaximumHealth);
					hero.SetPropertyBaseValue(SimulationProperties.Health, propertyValue);
				}
				this.jail.Refresh();
				hero.Refresh(false);
				int index = this.myCapturedHeroes[i].JailerEmpire.Index;
				this.myCapturedHeroes.RemoveAt(i);
				if (this.EventService != null)
				{
					this.EventService.Notify(new EventPrisonerReleased(base.Empire, hero, index));
				}
				return true;
			}
		}
		return false;
	}

	private bool LoadCapturedHeroBy(Prisoner prisoner, global::Empire jailerEmpire)
	{
		for (int i = 0; i < this.hallOfFame.Count; i++)
		{
			if (this.hallOfFame[i].GUID == prisoner.UnitGuid)
			{
				this.jail.AddChild(this.hallOfFame[i]);
				this.myCapturedHeroes.Add(new CapturedHero(this.hallOfFame[i], base.Empire as global::Empire, jailerEmpire, prisoner.CaptureNoticed));
				float propertyValue = this.hallOfFame[i].GetPropertyValue(SimulationProperties.HeroUpkeep);
				prisoner.PrisonerSimulationObject.SetPropertyBaseValue(SimulationProperties.JailHeroUpkeep, propertyValue);
				this.jail.Refresh();
				return true;
			}
		}
		return false;
	}

	private void RegisterCapturedHeroBy(Unit unit, global::Empire jailerEmpire, bool spyNoticed, bool notify)
	{
		this.departmentOfIntelligence.StopInfiltration(unit, true, false);
		this.UnassignHero(unit);
		this.jail.AddChild(unit);
		unit.AddDescriptor(this.capturedStatusDescriptor, false);
		unit.SetPropertyBaseValue(SimulationProperties.JailHeroPower, 0f);
		this.myCapturedHeroes.Add(new CapturedHero(unit, base.Empire as global::Empire, jailerEmpire, spyNoticed));
		this.jail.Refresh();
		if (this.EventService != null && notify)
		{
			this.EventService.Notify(new EventPrisonerCaptured(base.Empire, unit, jailerEmpire.Index));
		}
	}

	private void RegisterCapturedHeroFrom(Unit unit, global::Empire heroOwnerEmpire, bool captureNoticed, bool notify)
	{
		SimulationObject simulationObject = new SimulationObject("Prisoner-" + unit.GUID);
		simulationObject.AddDescriptor(this.prisonerDescriptor);
		Prisoner prisoner = new Prisoner(unit.GUID, heroOwnerEmpire.Index, captureNoticed, simulationObject);
		this.jail.AddChild(simulationObject);
		float propertyValue = unit.GetPropertyValue(SimulationProperties.HeroUpkeep);
		prisoner.PrisonerSimulationObject.SetPropertyBaseValue(SimulationProperties.JailHeroUpkeep, propertyValue);
		this.prisoners.Add(prisoner);
		base.Empire.Refresh(false);
	}

	public static bool CanAssignHero(Unit hero)
	{
		if (!(hero.UnitDesign is UnitProfile))
		{
			return false;
		}
		UnitProfile unitProfile = hero.UnitDesign as UnitProfile;
		return unitProfile.IsHero && !DepartmentOfEducation.IsLocked(hero) && !DepartmentOfEducation.IsInjured(hero) && !DepartmentOfIntelligence.IsHeroInfiltrating(hero) && !DepartmentOfEducation.IsCaptured(hero);
	}

	public static bool CanAssignHeroTo(Unit hero, IGarrison garrison)
	{
		if (!DepartmentOfEducation.CanAssignHero(hero))
		{
			return false;
		}
		if (hero.Garrison != null && hero.Garrison == garrison)
		{
			return false;
		}
		if (garrison != null && garrison is Army && (garrison as Army).HasCatspaw)
		{
			return false;
		}
		if (garrison != null)
		{
			foreach (Unit unit in garrison.StandardUnits)
			{
				if (unit.UnitDesign != null && unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
				{
					return false;
				}
			}
		}
		if (garrison != null && garrison.Hero != null)
		{
			if (DepartmentOfEducation.IsLocked(garrison.Hero))
			{
				return false;
			}
			if (DepartmentOfEducation.CheckGarrisonAgainstSiege(garrison.Hero, garrison))
			{
				return false;
			}
		}
		if (hero.Garrison != null && hero.Garrison.IsInEncounter)
		{
			return false;
		}
		if (hero.Garrison is SpiedGarrison)
		{
			DepartmentOfIntelligence agency = hero.Garrison.Empire.GetAgency<DepartmentOfIntelligence>();
			if (agency != null && !agency.CanUnassignSpy(hero))
			{
				return false;
			}
		}
		else
		{
			if ((DepartmentOfEducation.CheckGarrisonAgainstSiege(hero, garrison) || DepartmentOfEducation.CheckGarrisonAgainstSiege(hero, hero.Garrison)) && !DepartmentOfEducation.CheckHeroExchangeAgainstSiege(hero.Garrison, garrison))
			{
				return false;
			}
			if (garrison is City)
			{
				City city = garrison as City;
				if (city != null && city.IsInfected)
				{
					return false;
				}
			}
		}
		return garrison == null || !garrison.IsInEncounter;
	}

	public static ConstructionCost[] GetEmpireMoneyRestoreCost(Unit hero)
	{
		ConstructionCost[] array = new ConstructionCost[]
		{
			new ConstructionCost(DepartmentOfTheTreasury.Resources.EmpireMoney, 0f, true, false)
		};
		float propertyValue = hero.GetPropertyValue(SimulationProperties.CurrentInjuredValue);
		if (propertyValue > 0f)
		{
			array[0].Value = propertyValue * hero.GetPropertyValue(SimulationProperties.InjuredValueToEmpireMoneyConversion);
		}
		return array;
	}

	public static bool IsInjured(Unit hero)
	{
		return hero.GetPropertyValue(SimulationProperties.CurrentInjuredValue) > 0f;
	}

	public static bool IsCaptured(Unit hero)
	{
		return hero.SimulationObject.Tags.Contains("HeroStatusCaptured");
	}

	public static bool IsLocked(Unit hero)
	{
		float propertyValue = hero.GetPropertyValue(SimulationProperties.AssignmentCooldown);
		float propertyValue2 = hero.GetPropertyValue(SimulationProperties.MaximumAssignmentCooldown);
		return propertyValue < propertyValue2;
	}

	public static int LockedRemainingTurns(Unit hero)
	{
		float propertyValue = hero.GetPropertyValue(SimulationProperties.AssignmentCooldown);
		float propertyValue2 = hero.GetPropertyValue(SimulationProperties.MaximumAssignmentCooldown);
		return Mathf.Max(Mathf.RoundToInt(propertyValue2 - propertyValue), 0);
	}

	public static bool CheckGarrisonAgainstSiege(Unit hero, IGarrison garrison)
	{
		if (garrison == null)
		{
			return false;
		}
		if (hero != null && hero.CheckUnitAbility(UnitAbility.UnitAbilityAllowAssignationUnderSiege, -1))
		{
			return false;
		}
		if (garrison is City)
		{
			City city = garrison as City;
			if (city.BesiegingEmpire != null)
			{
				return true;
			}
		}
		if (garrison is SpiedGarrison)
		{
			SpiedGarrison spiedGarrison = garrison as SpiedGarrison;
			if (garrison.Empire != null && spiedGarrison.GUID != GameEntityGUID.Zero)
			{
				IGameService service = Services.GetService<IGameService>();
				Diagnostics.Assert(service != null);
				Diagnostics.Assert(service.Game != null);
				IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				Diagnostics.Assert(service2 != null);
				IGameEntity gameEntity;
				if (service2.TryGetValue(spiedGarrison.GUID, out gameEntity))
				{
					City city2 = gameEntity as City;
					if (city2 != null && city2.BesiegingEmpire != null)
					{
						return true;
					}
				}
			}
		}
		if (garrison is Army)
		{
			Army army = garrison as Army;
			if (army.SimulationObject.Tags.Contains(DepartmentOfTheInterior.ArmyStatusDefenderDescriptorName))
			{
				return true;
			}
		}
		return false;
	}

	public static bool CheckHeroExchangeAgainstSiege(IGarrison source, IGarrison destination)
	{
		District district = null;
		District district2 = null;
		if (source is IWorldPositionable)
		{
			district = DepartmentOfEducation.worldPositionningService.GetDistrict((source as IWorldPositionable).WorldPosition);
		}
		if (destination is IWorldPositionable)
		{
			district2 = DepartmentOfEducation.worldPositionningService.GetDistrict((destination as IWorldPositionable).WorldPosition);
		}
		return district != null && district2 != null && district.Type != DistrictType.Exploitation && district.Type != DistrictType.Improvement && district2.Type != DistrictType.Exploitation && district2.Type != DistrictType.Improvement && district.City == district2.City;
	}

	public static int CountCadaverBoosters(Contender contender)
	{
		int num = 0;
		if (contender != null)
		{
			DepartmentOfEducation agency = contender.Empire.GetAgency<DepartmentOfEducation>();
			if (agency != null)
			{
				List<VaultItem> vaultItems = agency.GetVaultItems<BoosterDefinition>();
				for (int i = 0; i < vaultItems.Count; i++)
				{
					BoosterDefinition boosterDefinition = vaultItems[i].Constructible as BoosterDefinition;
					if (boosterDefinition != null && boosterDefinition.Name == "BoosterCadavers")
					{
						num++;
					}
				}
			}
		}
		return num;
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		int attribute = reader.GetAttribute<int>("Count");
		if (attribute != 0)
		{
			reader.ReadStartElement("Vault");
			for (int i = 0; i < attribute; i++)
			{
				GameEntityGUID guid = reader.GetAttribute<ulong>("GameEntityGuid");
				GameEntityGUID owner = reader.GetAttribute<ulong>("Owner");
				reader.ReadStartElement("VaultItem");
				StaticString key = reader.ReadElementString("Constructible");
				ConstructibleElement value = this.ConstructibleElementDatabase.GetValue(key);
				if (value != null)
				{
					this.AddVaultItem(new VaultItem(guid, value)
					{
						Owner = owner
					});
				}
				reader.ReadEndElement();
			}
			reader.ReadEndElement();
		}
		else
		{
			reader.Skip();
		}
		int attribute2 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("UnassignedHeroes");
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		for (int j = 0; j < attribute2; j++)
		{
			Unit unit = DepartmentOfDefense.ReadUnit(reader, agency);
			if (unit != null && unit.UnitDesign is UnitProfile)
			{
				this.AddHero(unit);
			}
		}
		reader.ReadEndElement("UnassignedHeroes");
		if (reader.IsStartElement("Prisoners"))
		{
			int attribute3 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("Prisoners");
			for (int k = 0; k < attribute3; k++)
			{
				int attribute4 = reader.GetAttribute<int>("OwnerIndex");
				GameEntityGUID unitGuid = reader.GetAttribute<ulong>("UnitGuid");
				bool attribute5 = reader.GetAttribute<bool>("CaptureNoticed");
				reader.ReadStartElement("Prisoner");
				string attribute6 = reader.GetAttribute("Name");
				SimulationObject simulationObject = new SimulationObject(attribute6);
				reader.ReadElementSerializable<SimulationObject>(ref simulationObject);
				this.prisoners.Add(new Prisoner(unitGuid, attribute4, attribute5, simulationObject));
				this.jail.AddChild(simulationObject);
				reader.ReadEndElement();
			}
			reader.ReadEndElement();
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("Vault");
		writer.WriteAttributeString<int>("Count", this.vault.Count);
		for (int i = 0; i < this.vault.Count; i++)
		{
			writer.WriteStartElement("VaultItem");
			writer.WriteAttributeString<ulong>("GameEntityGuid", this.vault[i].GUID);
			writer.WriteAttributeString<ulong>("Owner", this.vault[i].Owner);
			writer.WriteElementString<StaticString>("Constructible", this.vault[i].Constructible.Name);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("UnassignedHeroes");
		writer.WriteAttributeString<int>("Count", this.hallOfFame.Count((Unit match) => match.Garrison == null));
		for (int j = 0; j < this.hallOfFame.Count; j++)
		{
			if (this.hallOfFame[j].Garrison == null)
			{
				IXmlSerializable xmlSerializable = this.hallOfFame[j];
				writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
			}
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Prisoners");
		writer.WriteAttributeString<int>("Count", this.prisoners.Count);
		for (int k = 0; k < this.prisoners.Count; k++)
		{
			writer.WriteStartElement("Prisoner");
			writer.WriteAttributeString<int>("OwnerIndex", this.prisoners[k].OwnerEmpireIndex);
			writer.WriteAttributeString<ulong>("UnitGuid", this.prisoners[k].UnitGuid);
			writer.WriteAttributeString<bool>("CaptureNoticed", this.prisoners[k].CaptureNoticed);
			IXmlSerializable prisonerSimulationObject = this.prisoners[k].PrisonerSimulationObject;
			writer.WriteElementSerializable<IXmlSerializable>(ref prisonerSimulationObject);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	private bool CaptureHeroPreprocessor(OrderCaptureHero order)
	{
		if (this.hallOfFame.Find((Unit match) => match.GUID == order.UnitGuid) == null)
		{
			return false;
		}
		global::Game game = this.GameService.Game as global::Game;
		if (game == null || order.JailerEmpireIndex >= game.Empires.Length)
		{
			return false;
		}
		MajorEmpire majorEmpire = game.Empires[order.JailerEmpireIndex] as MajorEmpire;
		return majorEmpire != null && !majorEmpire.IsEliminated;
	}

	private IEnumerator CaptureHeroProcessor(OrderCaptureHero order)
	{
		Unit hero = this.hallOfFame.Find((Unit match) => match.GUID == order.UnitGuid);
		if (hero == null)
		{
			yield break;
		}
		global::Game game = this.GameService.Game as global::Game;
		if (game == null || order.JailerEmpireIndex >= game.Empires.Length)
		{
			yield break;
		}
		DepartmentOfEducation.CaptureHero(hero, game.Empires[order.JailerEmpireIndex], base.Empire as global::Empire, false, true);
		yield break;
	}

	private bool CaptureHeroByInfiltrationPreprocessor(OrderCaptureHeroByInfiltration order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			return false;
		}
		if (city.Hero == null)
		{
			return false;
		}
		bool? flag = new bool?(true);
		InfiltrationAction.ComputeConstructionCost((global::Empire)base.Empire, order, ref flag);
		if (!flag.Value)
		{
			return false;
		}
		if (Services.GetService<IGameService>() == null)
		{
			return false;
		}
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult(city, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		return true;
	}

	private IEnumerator CaptureHeroByInfiltrationProcessor(OrderCaptureHeroByInfiltration order)
	{
		InfiltrationAction.TryTransferResources(base.Empire as global::Empire, order);
		InfiltrationAction.TryTransferLevelCost(base.Empire as global::Empire, order);
		InfiltrationAction.ApplyExperienceReward(base.Empire as global::Empire, order);
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			yield break;
		}
		City infiltratedCity = gameEntity as City;
		if (infiltratedCity != null && infiltratedCity.Hero != null)
		{
			DepartmentOfEducation.CaptureHero(infiltratedCity.Hero, base.Empire as global::Empire, infiltratedCity.Empire, order.AntiSpyResult != DepartmentOfIntelligence.AntiSpyResult.Nothing, true);
			InfiltrationAction.TryNotifyInfiltrationActionResult((global::Empire)base.Empire, (global::Empire)base.Empire, order);
			InfiltrationAction.TryNotifyInfiltrationActionResult(infiltratedCity.Empire, (global::Empire)base.Empire, order);
		}
		DepartmentOfIntelligence departmentOfIntelligence = base.Empire.GetAgency<DepartmentOfIntelligence>();
		if (departmentOfIntelligence != null)
		{
			departmentOfIntelligence.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
			yield break;
		}
		yield break;
	}

	private bool ChangeHeroAssignmentPreprocessor(OrderChangeHeroAssignment order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.HeroGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid.");
			return false;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			return false;
		}
		Unit hero = gameEntity as Unit;
		IGarrison garrison = null;
		if (order.AssignmentGUID.IsValid)
		{
			if (!this.GameEntityRepositoryService.Contains(order.AssignmentGUID))
			{
				return false;
			}
			gameEntity = this.GameEntityRepositoryService[order.AssignmentGUID];
			if (!(gameEntity is IGarrison))
			{
				Diagnostics.LogError("Order preprocessing failed because the target assignment game entity is not a garrison.");
				return false;
			}
			garrison = (gameEntity as IGarrison);
		}
		return DepartmentOfEducation.CanAssignHeroTo(hero, garrison);
	}

	private IEnumerator ChangeHeroAssignmentProcessor(OrderChangeHeroAssignment order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.HeroGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			yield break;
		}
		Unit hero = gameEntity as Unit;
		IGarrison garrison = null;
		if (order.AssignmentGUID.IsValid)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.AssignmentGUID, out gameEntity))
			{
				Diagnostics.LogWarning("Order preprocessing failed because the target assignment game entity is not found");
				yield break;
			}
			if (!(gameEntity is IGarrison))
			{
				Diagnostics.LogError("Order preprocessing failed because the target assignment game entity is not a garrison.");
				yield break;
			}
			garrison = (gameEntity as IGarrison);
		}
		IGarrison lastAssignment = hero.Garrison;
		this.ChangeAssignment(hero, garrison);
		if (this.EventService != null)
		{
			EventHeroAssignment eventHeroAssignment = new EventHeroAssignment(base.Empire, hero, lastAssignment, garrison);
			this.EventService.Notify(eventHeroAssignment);
			yield break;
		}
		yield break;
	}

	private bool ForceShiftUnitsPreprocessor(OrderForceShiftUnits order)
	{
		if (order.ShiftingUnitGuids == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit array is null.");
			return false;
		}
		List<Unit> list = new List<Unit>();
		IGameEntity gameEntity = null;
		for (int i = 0; i < order.ShiftingUnitGuids.Length; i++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.ShiftingUnitGuids[i], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing failed because the unit entity guid at index {0} is not valid.", new object[]
				{
					i
				});
				return false;
			}
			Unit unit = gameEntity as Unit;
			if (unit == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the unit entity at index {0} is owned by nobody.", new object[]
				{
					i
				});
				return false;
			}
			if (unit.Garrison.IsInEncounter)
			{
				Diagnostics.LogError("Order preprocessing failed because the unit's garrison is in an encounter.");
				return false;
			}
			if (!unit.IsShifter())
			{
				Diagnostics.LogError("Order preprocessing failed because the unit's does not possess the shifting ability.");
				return false;
			}
			if (!unit.IsInCurrentSeasonForm())
			{
				Diagnostics.LogError("Order preprocessing failed because the unit's is already shifted in the opposite form.");
				return false;
			}
			list.Add(unit);
		}
		DepartmentOfTheTreasury agency = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		if (agency == null)
		{
			Diagnostics.LogError("Order preprocessing failed getting department of treasury.");
			return false;
		}
		ConstructionCost[] unitForceShiftingCost = agency.GetUnitForceShiftingCost(list);
		if (!agency.CanAfford(unitForceShiftingCost))
		{
			Diagnostics.LogError("Order preprocessing failed because player can't afford the shifting price.");
			return false;
		}
		return true;
	}

	private IEnumerator ForceShiftUnitsProcessor(OrderForceShiftUnits order)
	{
		Diagnostics.Assert(order.ShiftingUnitGuids != null);
		List<Unit> units = new List<Unit>();
		IGameEntity gameEntity = null;
		Unit unit = null;
		for (int index = 0; index < order.ShiftingUnitGuids.Length; index++)
		{
			if (this.GameEntityRepositoryService.TryGetValue(order.ShiftingUnitGuids[index], out gameEntity))
			{
				unit = (gameEntity as Unit);
				if (unit != null)
				{
					if (SimulationGlobal.GlobalTagsContains("Winter"))
					{
						unit.SetPropertyBaseValue("ShiftingForm", 0f);
					}
					else
					{
						unit.SetPropertyBaseValue("ShiftingForm", 1f);
					}
					unit.Refresh(false);
				}
				units.Add(unit);
			}
		}
		if (unit != null && unit.Garrison != null)
		{
			Army unitArmy = unit.Garrison as Army;
			if (unitArmy != null)
			{
				unitArmy.ShiftingFormHasChange(true);
			}
		}
		DepartmentOfTheTreasury departmentOfTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		if (departmentOfTreasury == null)
		{
			Diagnostics.LogError("Order preprocessing failed getting department of treasury.");
			yield break;
		}
		ConstructionCost[] costs = departmentOfTreasury.GetUnitForceShiftingCost(units);
		for (int index2 = 0; index2 < costs.Length; index2++)
		{
			float value = -costs[index2].Value;
			if (!departmentOfTreasury.TryTransferResources(base.Empire, costs[index2].ResourceName, value))
			{
				Diagnostics.LogError("Order processing failed because we haven't enough resources.");
				yield break;
			}
		}
		yield break;
	}

	private bool GenerateHeroPreprocessor(OrderGenerateHero order)
	{
		if (StaticString.IsNullOrEmpty(order.UnitProfileName))
		{
			return false;
		}
		IDatabase<UnitProfile> database = Databases.GetDatabase<UnitProfile>(false);
		if (!database.ContainsKey(order.UnitProfileName))
		{
			return false;
		}
		order.HeroGUID = this.GameEntityRepositoryService.GenerateGUID();
		return true;
	}

	private IEnumerator GenerateHeroProcessor(OrderGenerateHero order)
	{
		IDatabase<UnitProfile> unitProfileDatabase = Databases.GetDatabase<UnitProfile>(false);
		UnitProfile unitProfile;
		if (unitProfileDatabase.TryGetValue(order.UnitProfileName, out unitProfile))
		{
			Unit hero = this.CreateHero(order.HeroGUID, unitProfile);
			if (hero != null)
			{
				this.GameEntityRepositoryService.Register(hero);
				hero.Refresh(true);
				hero.UpdateExperienceReward(base.Empire);
				hero.UpdateShiftingForm();
				if (this.EventService != null)
				{
					this.EventService.Notify(new EventHeroAvailable(base.Empire, hero));
				}
			}
		}
		yield break;
	}

	private bool ImmolateUnitsPreprocessor(OrderImmolateUnits order)
	{
		if (order.ImmolateUnitGuids == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit array is null.");
			return false;
		}
		List<Unit> list = new List<Unit>();
		IGameEntity gameEntity = null;
		for (int i = 0; i < order.ImmolateUnitGuids.Length; i++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.ImmolateUnitGuids[i], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing failed because the unit entity guid at index {0} is not valid.", new object[]
				{
					i
				});
				return false;
			}
			Unit unit = gameEntity as Unit;
			if (unit == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the unit entity at index {0} is owned by nobody.", new object[]
				{
					i
				});
				return false;
			}
			if (unit.Garrison.IsInEncounter)
			{
				Diagnostics.LogError("Order preprocessing failed because the unit's garrison is in an encounter.");
				return false;
			}
			if (!unit.IsImmolableUnit())
			{
				Diagnostics.LogError("Order preprocessing failed because the unit's does not possess the immolation ability.");
				return false;
			}
			if (unit.IsAlreadyImmolated())
			{
				Diagnostics.LogError("Order preprocessing failed because the unit's is already immolated.");
				return false;
			}
			list.Add(unit);
		}
		return true;
	}

	private IEnumerator ImmolateUnitsProcessor(OrderImmolateUnits order)
	{
		Diagnostics.Assert(order.ImmolateUnitGuids != null);
		IGameEntity gameEntity = null;
		Unit unit = null;
		for (int index = 0; index < order.ImmolateUnitGuids.Length; index++)
		{
			if (this.GameEntityRepositoryService.TryGetValue(order.ImmolateUnitGuids[index], out gameEntity))
			{
				unit = (gameEntity as Unit);
				if (unit != null)
				{
					unit.SetPropertyBaseValue("ImmolationState", 1f);
					unit.Refresh(false);
				}
			}
		}
		if (unit != null && unit.Garrison is Army)
		{
			(unit.Garrison as Army).NotifyUnitsImmolated(true);
		}
		yield break;
	}

	private bool InjureHeroPreprocessor(OrderInjureHero order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.HeroGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid.");
			return false;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			return false;
		}
		Unit unit = gameEntity as Unit;
		if (DepartmentOfEducation.IsCaptured(unit))
		{
			return false;
		}
		float propertyValue = unit.GetPropertyValue(SimulationProperties.CurrentInjuredValue);
		if (propertyValue > 0f)
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is already injured.");
			return false;
		}
		return true;
	}

	private IEnumerator InjureHeroProcessor(OrderInjureHero order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.HeroGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			yield break;
		}
		Unit hero = gameEntity as Unit;
		this.InjureHero(hero, true);
		yield break;
	}

	private bool InjureHeroByInfiltrationPreprocessor(OrderInjureHeroByInfiltration order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			return false;
		}
		if (city.Hero == null)
		{
			return false;
		}
		if (city.Hero.GUID != order.HeroGUID)
		{
			return false;
		}
		bool? flag = new bool?(true);
		InfiltrationAction.ComputeConstructionCost((global::Empire)base.Empire, order, ref flag);
		if (!flag.Value)
		{
			return false;
		}
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult(city, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		DepartmentOfEducation agency = city.Empire.GetAgency<DepartmentOfEducation>();
		return agency.InjureHeroPreprocessor(order);
	}

	private IEnumerator InjureHeroByInfiltrationProcessor(OrderInjureHeroByInfiltration order)
	{
		InfiltrationAction.TryTransferResources(base.Empire as global::Empire, order);
		InfiltrationAction.TryTransferLevelCost(base.Empire as global::Empire, order);
		InfiltrationAction.ApplyExperienceReward(base.Empire as global::Empire, order);
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			yield break;
		}
		City infiltratedCity = gameEntity as City;
		if (infiltratedCity != null && infiltratedCity.Hero != null)
		{
			DepartmentOfEducation education = infiltratedCity.Empire.GetAgency<DepartmentOfEducation>();
			yield return education.InjureHeroProcessor(order);
			InfiltrationAction.TryNotifyInfiltrationActionResult((global::Empire)base.Empire, (global::Empire)base.Empire, order);
			InfiltrationAction.TryNotifyInfiltrationActionResult(infiltratedCity.Empire, (global::Empire)base.Empire, order);
		}
		this.departmentOfIntelligence.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
		yield break;
	}

	private bool ReleasePrisonerPreprocessor(OrderReleasePrisoner order)
	{
		return this.prisoners.Exists((Prisoner match) => match.UnitGuid == order.UnitGuid);
	}

	private IEnumerator ReleasePrisonerProcessor(OrderReleasePrisoner order)
	{
		global::Game game = this.GameService.Game as global::Game;
		for (int index = this.prisoners.Count - 1; index >= 0; index--)
		{
			if (this.prisoners[index].UnitGuid == order.UnitGuid)
			{
				DepartmentOfEducation ownerEducation = game.Empires[this.prisoners[index].OwnerEmpireIndex].GetAgency<DepartmentOfEducation>();
				ownerEducation.ReleaseCapturedHeroBy(this.prisoners[index].UnitGuid, false);
				this.ReleaseCapturedHeroFrom(this.prisoners[index]);
				break;
			}
		}
		this.jail.Refresh();
		yield break;
	}

	private bool RemoveHeroPreprocessor(OrderRemoveHero order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.HeroGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid.");
			return false;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			return false;
		}
		return true;
	}

	private IEnumerator RemoveHeroProcessor(OrderRemoveHero order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.HeroGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the target unit game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order processing failed because the target game entity is not a unit.");
			yield break;
		}
		Unit hero = gameEntity as Unit;
		this.RemoveHero(hero);
		yield break;
	}

	private bool RestoreHeroPreprocessor(OrderRestoreHero order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.HeroGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid.");
			return false;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			return false;
		}
		Unit unit = gameEntity as Unit;
		if (DepartmentOfEducation.IsCaptured(unit))
		{
			return false;
		}
		float propertyValue = unit.GetPropertyValue(SimulationProperties.CurrentInjuredValue);
		if (propertyValue <= 0f)
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not injured.");
			return false;
		}
		ConstructionCost[] empireMoneyRestoreCost = DepartmentOfEducation.GetEmpireMoneyRestoreCost(unit);
		DepartmentOfTheTreasury agency = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		if (!agency.CanAfford(empireMoneyRestoreCost))
		{
			Diagnostics.LogError("Order preprocessing failed because the empire has not enough dust to restore the hero (Cost: {0}).", new object[]
			{
				empireMoneyRestoreCost[0]
			});
			return false;
		}
		return true;
	}

	private IEnumerator RestoreHeroProcessor(OrderRestoreHero order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.HeroGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			yield break;
		}
		Unit hero = gameEntity as Unit;
		ConstructionCost[] costs = DepartmentOfEducation.GetEmpireMoneyRestoreCost(hero);
		DepartmentOfTheTreasury treasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		for (int index = 0; index < costs.Length; index++)
		{
			float value = -costs[index].Value;
			if (!treasury.TryTransferResources(base.Empire, costs[index].ResourceName, value))
			{
				Diagnostics.LogError("Order processing failed because we haven't enough resources.");
				yield break;
			}
		}
		this.HealHero(hero);
		hero.Refresh(false);
		yield break;
	}

	private bool UnlockUnitSkillLevelPreprocessor(OrderUnlockUnitSkillLevel order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			return false;
		}
		Unit unit = gameEntity as Unit;
		if (DepartmentOfEducation.IsCaptured(unit))
		{
			return false;
		}
		if (!this.unitSkillDatabase.ContainsKey(order.UnitSkillName))
		{
			Diagnostics.LogError("Order preprocessing failed because the unit skill does not exists.");
			return false;
		}
		UnitSkill value = this.unitSkillDatabase.GetValue(order.UnitSkillName);
		if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(unit, value, new string[0]))
		{
			Diagnostics.LogError("Order preprocessing failed because the unit skill is not available for the unit.");
			return false;
		}
		if (!DepartmentOfEducation.IsUnitSkillUnlockable(unit, value))
		{
			Diagnostics.LogError("Order preprocessing failed because the unit skill need another skill to be unlocked before it.");
			return false;
		}
		if (order.UnitSkillLevel < 0 || order.UnitSkillLevel >= value.UnitSkillLevels.Length)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit skill level does not exists.");
			return false;
		}
		float num = unit.GetPropertyValue(SimulationProperties.SkillPointsSpent);
		int skillLevel = unit.GetSkillLevel(value.Name);
		for (int i = 0; i < value.UnitSkillLevels.Length; i++)
		{
			UnitSkillLevel unitSkillLevel = value.UnitSkillLevels[i];
			if (i > skillLevel && i <= order.UnitSkillLevel)
			{
				num += (float)unitSkillLevel.UnitSkillPointCost;
			}
			if (i == order.UnitSkillLevel)
			{
				break;
			}
		}
		float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumSkillPoints);
		if (num > propertyValue)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit does not have enough skill point to unlock the skill level.");
			return false;
		}
		EventUnitSkillUnlocked eventToNotify = new EventUnitSkillUnlocked(base.Empire, order.UnitGuid, skillLevel, order.UnitSkillLevel, order.UnitSkillName);
		this.EventService.Notify(eventToNotify);
		return true;
	}

	private IEnumerator UnlockUnitSkillLevelProcessor(OrderUnlockUnitSkillLevel order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			yield break;
		}
		Unit unit = gameEntity as Unit;
		unit.UnlockSkill(order.UnitSkillName, order.UnitSkillLevel);
		yield break;
	}

	public static UnitSkill[] GetAvailableUnitSkills(Unit unit)
	{
		List<UnitSkill> list = new List<UnitSkill>();
		DepartmentOfEducation.FillAvailableUnitSkills(unit, ref list);
		return list.ToArray();
	}

	public static void FillAvailableUnitSkills(Unit unit, ref List<UnitSkill> unitSkills)
	{
		IDatabase<UnitSkill> database = Databases.GetDatabase<UnitSkill>(true);
		unitSkills.AddRange(database.GetValues());
		unitSkills.RemoveAll((UnitSkill match) => match.UnitSkillLevels == null || match.UnitSkillLevels.Length == 0 || !DepartmentOfTheTreasury.CheckConstructiblePrerequisites(unit, match, new string[0]));
	}

	public static bool IsUnitSkillUnlockable(Unit unit, StaticString unitSkillName)
	{
		IDatabase<UnitSkill> database = Databases.GetDatabase<UnitSkill>(true);
		UnitSkill unitSkill;
		if (!database.TryGetValue(unitSkillName, out unitSkill))
		{
			Diagnostics.LogError("Order preprocessing failed because the unit skill does not exists.");
			return false;
		}
		return DepartmentOfEducation.IsUnitSkillUnlockable(unit, unitSkill);
	}

	public static bool IsUnitSkillUnlockable(Unit unit, UnitSkill unitSkill)
	{
		for (int i = 0; i < unitSkill.UnitSkillPrerequisites.Count; i++)
		{
			bool flag = false;
			for (int j = 0; j < unitSkill.UnitSkillPrerequisites[i].Length; j++)
			{
				if (unit.IsSkillUnlocked(unitSkill.UnitSkillPrerequisites[i][j]))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsUnitSkillUpgradable(Unit unit, UnitSkill unitSkill)
	{
		if (unit.IsSkillUnlocked(unitSkill.Name))
		{
			float num = (float)unit.GetSkillLevel(unitSkill.Name);
			return num < (float)(unitSkill.LevelCount - 1);
		}
		return DepartmentOfEducation.IsUnitSkillUnlockable(unit, unitSkill);
	}

	public ReadOnlyCollection<VaultItem> VaultItems
	{
		get
		{
			return this.vault.AsReadOnly();
		}
	}

	public int VaultCount
	{
		get
		{
			return this.vault.Count;
		}
	}

	public VaultItem this[int index]
	{
		get
		{
			return this.vault[index];
		}
	}

	public VaultItem this[GameEntityGUID guid]
	{
		get
		{
			return this.vault.Find((VaultItem match) => match.GUID == guid);
		}
	}

	public void AddVaultItem(VaultItem vaultItem)
	{
		if (!vaultItem.GUID.IsValid)
		{
			Diagnostics.LogError("Cannot add the vault item because its guid is not valid.");
			return;
		}
		if (this.vault.Exists((VaultItem match) => match.GUID == vaultItem.GUID))
		{
			Diagnostics.LogWarning("Cannot add vault item twice, ignoring.");
			return;
		}
		this.vault.Add(vaultItem);
		this.OnVaultItemsCollectionChange(vaultItem, CollectionChangeAction.Add);
	}

	public VaultItem CreateVaultItem(ConstructibleElement constructibleElement)
	{
		GameEntityGUID guid = this.GameEntityRepositoryService.GenerateGUID();
		VaultItem vaultItem = new VaultItem(guid, constructibleElement);
		this.vault.Add(vaultItem);
		this.OnVaultItemsCollectionChange(vaultItem, CollectionChangeAction.Add);
		return vaultItem;
	}

	public List<VaultItem> GetVaultItems<T>()
	{
		List<VaultItem> list = new List<VaultItem>();
		for (int i = 0; i < this.vault.Count; i++)
		{
			VaultItem vaultItem = this.vault[i];
			if (vaultItem != null && vaultItem.Constructible is T)
			{
				list.Add(vaultItem);
			}
		}
		return list;
	}

	public IEnumerable<VaultItem> GetVaultItems(Func<VaultItem, bool> predicate = null)
	{
		Diagnostics.Assert(this.vault != null);
		if (predicate == null)
		{
			return this.vault;
		}
		return this.vault.Where(predicate);
	}

	public void DestroyVaultItem(VaultItem vaultItem)
	{
		this.DestroyVaultItem(vaultItem.GUID);
	}

	public void DestroyVaultItem(GameEntityGUID vaultItemGuid)
	{
		if (vaultItemGuid.IsValid)
		{
			IGameEntity gameEntity;
			this.GameEntityRepositoryService.TryGetValue(vaultItemGuid, out gameEntity);
			this.vault.RemoveAll((VaultItem match) => match.GUID == vaultItemGuid);
			this.GameEntityRepositoryService.Unregister(vaultItemGuid);
			this.OnVaultItemsCollectionChange(gameEntity as VaultItem, CollectionChangeAction.Remove);
		}
	}

	public void SwapVaultItemOwner(GameEntityGUID itemGUID, global::Empire empireWhichReceives)
	{
		if (!itemGUID.IsValid)
		{
			Diagnostics.LogError("SwapVaultItemOwner failed, item's guid is invalid.");
			return;
		}
		VaultItem vaultItem = this.FirstOrDefault(itemGUID);
		if (vaultItem == null)
		{
			Diagnostics.LogError("SwapVaultItemOwner failed, can't retrieve the item {0}.", new object[]
			{
				itemGUID
			});
			return;
		}
		DepartmentOfEducation agency = empireWhichReceives.GetAgency<DepartmentOfEducation>();
		Diagnostics.Assert(agency != null);
		this.DestroyVaultItem(vaultItem);
		agency.AddVaultItem(vaultItem);
	}

	public bool Exist(GameEntityGUID guid)
	{
		return this.vault.Exists((VaultItem match) => match.GUID == guid);
	}

	public int FindIndex(GameEntityGUID guid)
	{
		return this.vault.FindIndex((VaultItem match) => match.GUID == guid);
	}

	public VaultItem FirstOrDefault(GameEntityGUID guid)
	{
		return this.vault.FirstOrDefault((VaultItem match) => match.GUID == guid);
	}

	public VaultItem FirstOrDefault(Func<VaultItem, bool> predicate)
	{
		return this.vault.FirstOrDefault(predicate);
	}

	public int Count(Func<VaultItem, bool> predicate)
	{
		return this.vault.Count(predicate);
	}

	public IEnumerable<VaultItem> Where(Func<VaultItem, bool> predicate)
	{
		return this.vault.Where(predicate);
	}

	private void OnVaultItemsCollectionChange(VaultItem vaultItem, CollectionChangeAction action)
	{
		if (this.VaultItemsCollectionChange != null)
		{
			this.VaultItemsCollectionChange(this, new CollectionChangeEventArgs(action, vaultItem));
		}
	}

	public ReadOnlyCollection<Unit> Heroes
	{
		get
		{
			return this.hallOfFame.AsReadOnly();
		}
	}

	protected IEventService EventService { get; set; }

	private IDatabase<ConstructibleElement> ConstructibleElementDatabase { get; set; }

	private IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	private IGameService GameService { get; set; }

	private IDatabase<SimulationDescriptor> SimulationDescriptorDatabase { get; set; }

	public void ChangeAssignment(Unit hero, IGarrison newAssignation)
	{
		if (hero.Garrison != null)
		{
			IGarrison garrison = hero.Garrison;
			hero.Garrison.SetHero(null);
			if (garrison is Army)
			{
				Army army = garrison as Army;
				if (army.IsEmpty)
				{
					DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
					agency.RemoveArmy(army, true);
				}
				else
				{
					DepartmentOfDefense agency2 = base.Empire.GetAgency<DepartmentOfDefense>();
					agency2.UpdateDetection(army);
					army.SetSails();
					hero.RemoveDescriptorByName(PathfindingContext.MovementCapacitySailDescriptor);
				}
			}
			else if (this.departmentOfIntelligence != null && garrison is SpiedGarrison)
			{
				this.departmentOfIntelligence.StopInfiltration(hero, false, newAssignation == null);
			}
			base.Empire.AddChild(hero);
			base.Empire.Refresh(false);
		}
		if (newAssignation != null)
		{
			if (newAssignation.Hero != null)
			{
				Unit hero2 = newAssignation.Hero;
				newAssignation.SetHero(null);
				base.Empire.AddChild(hero2);
				base.Empire.Refresh(false);
			}
			bool active = false;
			if (newAssignation is Army)
			{
				active = ((Army)newAssignation).IsNaval;
			}
			newAssignation.SetHero(hero);
			hero.Refresh(true);
			hero.SetPropertyBaseValue(SimulationProperties.AssignmentCooldown, 0f);
			hero.Refresh(false);
			hero.UnitUnassignedTurnCount = 0;
			if (newAssignation is Army)
			{
				Army army2 = newAssignation as Army;
				DepartmentOfDefense agency3 = base.Empire.GetAgency<DepartmentOfDefense>();
				agency3.UpdateDetection(army2);
				hero.SwitchToEmbarkedUnit(active);
				army2.SetSails();
				army2.Refresh(false);
				SimulationDescriptor descriptor;
				if (army2.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && !hero.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && this.SimulationDescriptorDatabase != null && this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out descriptor))
				{
					hero.AddDescriptor(descriptor, true);
				}
			}
			else
			{
				hero.SwitchToEmbarkedUnit(false);
			}
		}
		else
		{
			hero.Refresh(false);
		}
		if (this.GameService != null && this.GameService.Game != null)
		{
			IVisibilityService service = this.GameService.Game.Services.GetService<IVisibilityService>();
			if (service != null)
			{
				service.NotifyVisibilityHasChanged((global::Empire)base.Empire);
			}
		}
	}

	public Unit CreateHero(Unit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		UnitDesign unitDesign = unit.UnitDesign;
		UnitProfile unitProfile = unit.UnitDesign as UnitProfile;
		if (unitProfile == null)
		{
			Diagnostics.LogError("Unit has not unit profile.");
			return null;
		}
		if (!unitProfile.IsHero)
		{
			Diagnostics.LogError("Unit has a unit profile but is not a hero.");
			return null;
		}
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		if (agency != null)
		{
			unit.UnitDesign = agency.RegisterHeroUnitDesign(unitDesign);
		}
		unit.SetPropertyBaseValue(SimulationProperties.AssignmentCooldown, float.MaxValue);
		unit.SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, float.MaxValue);
		this.AddHero(unit);
		this.EventService.Notify(new EventHeroCreated(base.Empire, unit));
		return unit;
	}

	public Unit CreateHero(GameEntityGUID guid, UnitDesign unitDesign)
	{
		UnitProfile unitProfile = unitDesign as UnitProfile;
		if (unitProfile == null)
		{
			Diagnostics.LogError("Unit design is not a unit profile.");
			return null;
		}
		if (!unitProfile.IsHero)
		{
			Diagnostics.LogError("Unit profile is not a hero profile.");
			return null;
		}
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		UnitDesign unitDesign2 = agency.RegisterHeroUnitDesign(unitDesign);
		Unit unit = DepartmentOfDefense.CreateUnitByDesign(guid, unitDesign2);
		Diagnostics.Assert(this.GameService != null);
		Diagnostics.Assert(this.GameService.Game != null);
		IHeroManagementService service = this.GameService.Game.Services.GetService<IHeroManagementService>();
		if (service != null)
		{
			service.AssignNewUserDefinedName(unit);
		}
		unit.SetPropertyBaseValue(SimulationProperties.AssignmentCooldown, float.MaxValue);
		unit.SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, float.MaxValue);
		this.AddHero(unit);
		float propertyValue = unit.GetPropertyValue(SimulationProperties.UnitExperienceRewardAtCreation);
		unit.GainXp(propertyValue, false, false);
		this.EventService.Notify(new EventHeroCreated(base.Empire, unit));
		return unit;
	}

	public IEnumerable<IGarrison> GetGarrisons()
	{
		DepartmentOfDefense defense = base.Empire.GetAgency<DepartmentOfDefense>();
		if (defense != null)
		{
			for (int index = 0; index < defense.Armies.Count; index++)
			{
				if (defense.Armies[index].Hero != null)
				{
					defense.Armies[index].Hero.SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, float.MaxValue);
				}
				yield return defense.Armies[index];
			}
		}
		DepartmentOfTheInterior interior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (interior != null)
		{
			for (int index2 = 0; index2 < interior.Cities.Count; index2++)
			{
				if (interior.Cities[index2].Hero != null)
				{
					interior.Cities[index2].Hero.SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, float.MaxValue);
				}
				yield return interior.Cities[index2];
			}
		}
		if (this.departmentOfIntelligence != null)
		{
			for (int index3 = 0; index3 < this.departmentOfIntelligence.SpiedGarrisons.Count; index3++)
			{
				yield return this.departmentOfIntelligence.SpiedGarrisons[index3];
			}
		}
		yield break;
	}

	public void InjureHero(GameEntityGUID heroGuid, bool notify = true)
	{
		this.InjureHero(this.hallOfFame.Find((Unit match) => match.GUID == heroGuid), notify);
	}

	public void InjureHero(Unit unit, bool notify = true)
	{
		if (unit == null || !(unit.UnitDesign is UnitProfile))
		{
			return;
		}
		if (this.departmentOfIntelligence != null)
		{
			this.departmentOfIntelligence.StopInfiltration(unit, true, true);
		}
		if (this.departmentOfPlanificationAndDevelopment != null)
		{
			this.departmentOfPlanificationAndDevelopment.RemoveBoostersFromTarget(unit.GUID, -1);
		}
		unit.SetPropertyBaseValue(SimulationProperties.CurrentInjuredValue, unit.GetPropertyValue(SimulationProperties.MaximumInjuredValue));
		if (unit.Garrison != null)
		{
			this.ChangeAssignment(unit, null);
		}
		unit.SetPropertyBaseValue(SimulationProperties.AssignmentCooldown, float.MaxValue);
		unit.SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, float.MaxValue);
		if (this.EventService != null && notify)
		{
			this.EventService.Notify(new EventHeroInjured(base.Empire, unit));
		}
	}

	public void UnassignHero(Unit hero)
	{
		this.ChangeAssignment(hero, null);
	}

	internal virtual void OnEmpireEliminated(global::Empire empire, bool authorized)
	{
		if (empire.Index == base.Empire.Index)
		{
			global::Game game = this.GameService.Game as global::Game;
			global::Empire jailer = base.Empire as global::Empire;
			while (this.prisoners.Count > 0)
			{
				Prisoner prisoner = this.prisoners[0];
				global::Empire owner = game.Empires[prisoner.OwnerEmpireIndex];
				DepartmentOfEducation.ReleaseCapturedHero(prisoner.UnitGuid, jailer, owner, true);
			}
			for (int i = 0; i < this.Heroes.Count; i++)
			{
				this.UnassignHero(this.Heroes[i]);
			}
		}
	}

	internal void InternalChangeAssignment(GameEntityGUID unitGuid, IGarrison newAssignation)
	{
		for (int i = 0; i < this.hallOfFame.Count; i++)
		{
			if (this.hallOfFame[i].GUID == unitGuid)
			{
				this.ChangeAssignment(this.hallOfFame[i], newAssignation);
				return;
			}
		}
	}

	internal void InternalChangeAssignment(Unit unit, IGarrison newAssignation)
	{
		this.ChangeAssignment(unit, newAssignation);
	}

	internal void InternalCreateHero(params StaticString[] unitProfileNames)
	{
		if (unitProfileNames == null)
		{
			return;
		}
		IDatabase<UnitProfile> database = Databases.GetDatabase<UnitProfile>(false);
		for (int i = 0; i < unitProfileNames.Length; i++)
		{
			UnitProfile unitDesign;
			if (database.TryGetValue(unitProfileNames[i], out unitDesign))
			{
				GameEntityGUID guid = this.GameEntityRepositoryService.GenerateGUID();
				this.CreateHero(guid, unitDesign);
			}
		}
	}

	internal void InternalRemoveHero(Unit unit)
	{
		this.RemoveHero(unit);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		this.Clear();
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.departmentOfIntelligence = base.Empire.GetAgency<DepartmentOfIntelligence>();
		this.departmentOfPlanificationAndDevelopment = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		this.unitSkillDatabase = Databases.GetDatabase<UnitSkill>(false);
		Diagnostics.Assert(this.unitSkillDatabase != null);
		this.ConstructibleElementDatabase = Databases.GetDatabase<ConstructibleElement>(false);
		Diagnostics.Assert(this.ConstructibleElementDatabase != null);
		this.SimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		Diagnostics.Assert(this.SimulationDescriptorDatabase != null);
		this.GameService = Services.GetService<IGameService>();
		Diagnostics.Assert(this.GameService != null);
		this.GameEntityRepositoryService = this.GameService.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.GameEntityRepositoryService != null);
		this.EventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.EventService != null);
		if (DepartmentOfEducation.worldPositionningService == null)
		{
			DepartmentOfEducation.worldPositionningService = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		}
		base.Empire.RegisterPass("GameClientState_Turn_End", "HeroAssignmentCooldown", new Agency.Action(this.GameClientState_Turn_End_HeroAssignmentCooldown), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "HeroRecovery", new Agency.Action(this.GameClientState_Turn_End_HeroRecovery), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "HeroHeal", new Agency.Action(this.GameClientState_Turn_End_UnitHealthPerTurnGain), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "VerifyCapturedHeroCooldown", new Agency.Action(this.GameClient_EndTurn_VerifyCapturedHeroCooldown), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "UnassignHeroNotification", new Agency.Action(this.GameClientState_Turn_Begin_UnassignHeroNotification), new string[0]);
		this.maximalTurnUnassigned = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Agencies/DepartmentOfEducation/MaximalTurnUnassigned", this.maximalTurnUnassigned);
		this.InitializeJail();
		DepartmentOfScience agency = base.Empire.GetAgency<DepartmentOfScience>();
		if (agency != null)
		{
			agency.TechnologyUnlocked += this.DepartmentOfScience_TechnologyUnlocked;
		}
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		for (int index = 0; index < this.hallOfFame.Count; index++)
		{
			if (this.hallOfFame[index].Garrison == null)
			{
				this.hallOfFame[index].SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, float.MaxValue);
				this.GameEntityRepositoryService.Register(this.hallOfFame[index]);
			}
		}
		foreach (IGarrison garrison in this.GetGarrisons())
		{
			if (garrison.Hero != null && !this.hallOfFame.Contains(garrison.Hero))
			{
				this.hallOfFame.Add(garrison.Hero);
			}
		}
		global::Game myGame = game as global::Game;
		for (int index2 = 0; index2 < this.prisoners.Count; index2++)
		{
			DepartmentOfEducation ownerEducation = myGame.Empires[this.prisoners[index2].OwnerEmpireIndex].GetAgency<DepartmentOfEducation>();
			ownerEducation.LoadCapturedHeroBy(this.prisoners[index2], base.Empire as global::Empire);
		}
		this.jail.Refresh();
		for (int index3 = 0; index3 < this.vault.Count; index3++)
		{
			this.GameEntityRepositoryService.Register(this.vault[index3]);
			IGameEntity gameEntity;
			if (this.vault[index3].Owner.IsValid && this.GameEntityRepositoryService.TryGetValue(this.vault[index3].Owner, out gameEntity) && gameEntity is Unit)
			{
				Unit unit = gameEntity as Unit;
				bool found = false;
				UnitEquipmentSet equipmentSet = unit.UnitDesign.UnitEquipmentSet;
				int equipmentSlotIndex = 0;
				while (equipmentSlotIndex < equipmentSet.Slots.Length)
				{
					if (equipmentSet.Slots[equipmentSlotIndex].ItemName == this.vault[index3].Constructible.Name)
					{
						found = true;
						break;
					}
					index3++;
				}
				if (!found)
				{
					this.vault[index3].Owner = GameEntityGUID.Zero;
				}
			}
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		DepartmentOfEducation.worldPositionningService = null;
		this.unitSkillDatabase = null;
		this.ConstructibleElementDatabase = null;
		this.SimulationDescriptorDatabase = null;
		this.GameService = null;
		this.GameEntityRepositoryService = null;
		this.EventService = null;
		this.departmentOfIntelligence = null;
		this.ReleaseJail();
	}

	private void AddHero(Unit unit)
	{
		this.hallOfFame.Add(unit);
		base.Empire.AddChild(unit);
		base.Empire.Refresh(false);
		this.OnHeroCollectionChange(unit, CollectionChangeAction.Add);
	}

	private void Clear()
	{
		this.hallOfFame.Clear();
		this.OnHeroCollectionChange(null, CollectionChangeAction.Refresh);
	}

	private IEnumerator GameClientState_Turn_Begin_UnassignHeroNotification(string context, string name)
	{
		for (int index = 0; index < this.hallOfFame.Count; index++)
		{
			if (this.hallOfFame[index].Garrison == null && !DepartmentOfEducation.IsInjured(this.hallOfFame[index]) && !DepartmentOfEducation.IsLocked(this.hallOfFame[index]))
			{
				this.hallOfFame[index].UnitUnassignedTurnCount++;
				if (this.hallOfFame[index].UnitUnassignedTurnCount > this.maximalTurnUnassigned)
				{
					this.hallOfFame[index].UnitUnassignedTurnCount = 0;
					this.EventService.Notify(new EventHeroUnassigned(base.Empire, this.hallOfFame[index]));
				}
			}
			else
			{
				this.hallOfFame[index].UnitUnassignedTurnCount = 0;
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_HeroAssignmentCooldown(string context, string name)
	{
		for (int index = 0; index < this.hallOfFame.Count; index++)
		{
			float previousCooldown = this.hallOfFame[index].GetPropertyValue(SimulationProperties.AssignmentCooldown);
			previousCooldown += 1f;
			float maximumCooldown = this.hallOfFame[index].GetPropertyValue(SimulationProperties.MaximumAssignmentCooldown);
			if (previousCooldown > maximumCooldown)
			{
				previousCooldown = maximumCooldown;
			}
			this.hallOfFame[index].SetPropertyBaseValue(SimulationProperties.AssignmentCooldown, previousCooldown);
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_HeroRecovery(string context, string name)
	{
		for (int index = 0; index < this.hallOfFame.Count; index++)
		{
			float currentInjuredValue = this.hallOfFame[index].GetPropertyValue(SimulationProperties.CurrentInjuredValue);
			if (currentInjuredValue > 0f)
			{
				float recoveryPerTurn = this.hallOfFame[index].GetPropertyValue(SimulationProperties.InjuredRecoveryPerTurn);
				currentInjuredValue -= recoveryPerTurn;
				int turnsBeforeRecovery = GuiHero.ComputeTurnsBeforRecovery(currentInjuredValue, recoveryPerTurn);
				if (turnsBeforeRecovery > 0)
				{
					this.hallOfFame[index].SetPropertyBaseValue(SimulationProperties.CurrentInjuredValue, currentInjuredValue);
				}
				else
				{
					if (this.EventService != null)
					{
						this.EventService.Notify(new EventHeroRecovered(base.Empire, this.hallOfFame[index]));
					}
					this.HealHero(this.hallOfFame[index]);
				}
			}
			this.hallOfFame[index].Refresh(false);
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UnitHealthPerTurnGain(string context, string name)
	{
		float regenModifier = base.Empire.GetPropertyValue(SimulationProperties.UnassignedHeroRegenModifier);
		for (int index = 0; index < this.hallOfFame.Count; index++)
		{
			if (this.hallOfFame[index].Garrison == null)
			{
				DepartmentOfDefense.RegenUnit(this.hallOfFame[index], regenModifier, 0);
				float currentInjuredValue = this.hallOfFame[index].GetPropertyValue(SimulationProperties.CurrentInjuredValue);
				float health = this.hallOfFame[index].GetPropertyValue(SimulationProperties.Health);
				if (health <= 0f && currentInjuredValue <= 0f)
				{
					this.InjureHero(this.hallOfFame[index], true);
				}
			}
		}
		yield break;
	}

	private void HealHero(Unit hero)
	{
		float propertyValue = hero.GetPropertyValue(SimulationProperties.MaximumHealth);
		hero.SetPropertyBaseValue(SimulationProperties.Health, propertyValue);
		hero.SetPropertyBaseValue(SimulationProperties.CurrentInjuredValue, 0f);
		hero.UnitUnassignedTurnCount = 0;
	}

	private void OnHeroCollectionChange(Unit hero, CollectionChangeAction action)
	{
		if (this.HeroCollectionChange != null)
		{
			this.HeroCollectionChange(this, new CollectionChangeEventArgs(action, hero));
		}
	}

	private void RemoveHero(Unit hero)
	{
		if (this.hallOfFame.Remove(hero))
		{
			if (hero.Garrison == null)
			{
				base.Empire.RemoveChild(hero);
				base.Empire.Refresh(false);
			}
			else
			{
				IGarrison garrison = hero.Garrison;
				hero.Garrison.SetHero(null);
				if (garrison is Army)
				{
					Army army = garrison as Army;
					if (army.IsEmpty)
					{
						DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
						agency.RemoveArmy(army, true);
					}
					else
					{
						army.SetSails();
					}
				}
				else if (this.departmentOfIntelligence != null && garrison is SpiedGarrison)
				{
					this.departmentOfIntelligence.StopInfiltration(hero, true, true);
				}
			}
			this.OnHeroCollectionChange(hero, CollectionChangeAction.Remove);
		}
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (!ELCPUtilities.UseELCPStockpileRulseset)
		{
			return;
		}
		float num;
		if (e.ConstructibleElement.Name == "TechnologyDefinitionAllBoosterLevel1")
		{
			DepartmentOfScience.ConstructibleElement technology;
			if (!base.Empire.GetAgency<DepartmentOfScience>().TechnologyDatabase.TryGetValue("TechnologyDefinitionAllBoosterLevel2", out technology) || base.Empire.GetAgency<DepartmentOfScience>().GetTechnologyState(technology) == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				return;
			}
			num = 0.5f;
		}
		else
		{
			if (!(e.ConstructibleElement.Name == "TechnologyDefinitionAllBoosterLevel2"))
			{
				return;
			}
			DepartmentOfScience.ConstructibleElement technology2;
			if (!base.Empire.GetAgency<DepartmentOfScience>().TechnologyDatabase.TryGetValue("TechnologyDefinitionAllBoosterLevel1", out technology2) || base.Empire.GetAgency<DepartmentOfScience>().GetTechnologyState(technology2) == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				num = 0.5f;
			}
			else
			{
				num = 0.75f;
			}
		}
		if (num > 0f)
		{
			List<VaultItem> vaultItems = this.GetVaultItems<BoosterDefinition>();
			List<VaultItem> list = new List<VaultItem>();
			List<VaultItem> list2 = new List<VaultItem>();
			List<VaultItem> list3 = new List<VaultItem>();
			for (int i = 0; i < vaultItems.Count; i++)
			{
				BoosterDefinition boosterDefinition = vaultItems[i].Constructible as BoosterDefinition;
				if (boosterDefinition != null && boosterDefinition.Name == "BoosterFood")
				{
					list.Add(vaultItems[i]);
				}
				if (boosterDefinition != null && boosterDefinition.Name == "BoosterScience")
				{
					list2.Add(vaultItems[i]);
				}
				if (boosterDefinition != null && (boosterDefinition.Name == "BoosterIndustry" || boosterDefinition.Name == "FlamesIndustryBooster"))
				{
					list3.Add(vaultItems[i]);
				}
			}
			int num2 = Mathf.FloorToInt((float)list.Count * num);
			for (int j = 0; j < num2; j++)
			{
				this.DestroyVaultItem(list[j]);
			}
			num2 = Mathf.FloorToInt((float)list2.Count * num);
			for (int k = 0; k < num2; k++)
			{
				this.DestroyVaultItem(list2[k]);
			}
			num2 = Mathf.FloorToInt((float)list3.Count * num);
			for (int l = 0; l < num2; l++)
			{
				this.DestroyVaultItem(list3[l]);
			}
		}
	}

	public List<CapturedHero> MyCapturedHeroes
	{
		get
		{
			return this.myCapturedHeroes;
		}
	}

	private SimulationObject jail;

	private SimulationDescriptor prisonerDescriptor;

	private SimulationDescriptor capturedStatusDescriptor;

	private List<Prisoner> prisoners;

	private List<CapturedHero> myCapturedHeroes;

	private IDatabase<UnitSkill> unitSkillDatabase;

	private List<VaultItem> vault = new List<VaultItem>();

	private static IWorldPositionningService worldPositionningService;

	private DepartmentOfIntelligence departmentOfIntelligence;

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment;

	private List<Unit> hallOfFame = new List<Unit>();

	private int maximalTurnUnassigned = 2;
}
