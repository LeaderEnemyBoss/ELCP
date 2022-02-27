using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class HeroPool : GameAncillary, IXmlSerializable, IService, IHeroManagementService
{
	public HeroPool()
	{
		this.GenerationNumber = 0;
		this.TurnWhenLastRefilled = -1;
	}

	bool IHeroManagementService.TryAllocateSkillPoints(Unit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		UnitProfile unitProfile = unit.UnitDesign as UnitProfile;
		if (unitProfile == null || !unitProfile.IsHero)
		{
			return false;
		}
		int num = (int)Math.Floor((double)(unit.GetPropertyValue(SimulationProperties.MaximumSkillPoints) - unit.GetPropertyValue(SimulationProperties.SkillPointsSpent)));
		if (num <= 0)
		{
			return false;
		}
		if (unitProfile.UnitSkillAllocationSchemeReference == null || StaticString.IsNullOrEmpty(unitProfile.UnitSkillAllocationSchemeReference))
		{
			return false;
		}
		IDatabase<Droplist> database = Databases.GetDatabase<Droplist>(false);
		if (database == null)
		{
			return false;
		}
		Droplist droplist;
		if (database.TryGetValue(unitProfile.UnitSkillAllocationSchemeReference, out droplist))
		{
			UnitSkill[] availableUnitSkills = DepartmentOfEducation.GetAvailableUnitSkills(unit);
			for (int i = 0; i < num; i++)
			{
				Droplist droplist2;
				DroppableString droppableString = droplist.Pick(null, out droplist2, new object[0]) as DroppableString;
				if (droppableString != null)
				{
					if (!string.IsNullOrEmpty(droppableString.Value))
					{
						StaticString subcategory = droppableString.Value;
						List<UnitSkill> list = (from iterator in availableUnitSkills
						where iterator.SubCategory == subcategory && DepartmentOfEducation.IsUnitSkillUnlockable(unit, iterator)
						select iterator).ToList<UnitSkill>();
						if (list.Count > 0)
						{
							while (list.Count > 0)
							{
								int index = UnityEngine.Random.Range(0, list.Count);
								int num2 = 0;
								if (unit.IsSkillUnlocked(list[index].Name))
								{
									num2 = unit.GetSkillLevel(list[index].Name) + 1;
									int num3 = 0;
									if (list[index].UnitSkillLevels != null)
									{
										num3 = list[index].UnitSkillLevels.Length - 1;
									}
									if (num2 > num3)
									{
										list.Remove(list[index]);
										continue;
									}
								}
								unit.UnlockSkill(list[index].Name, num2);
								num = (int)Math.Floor((double)(unit.GetPropertyValue(SimulationProperties.MaximumSkillPoints) - unit.GetPropertyValue(SimulationProperties.SkillPointsSpent)));
								break;
							}
						}
					}
				}
			}
		}
		return true;
	}

	bool IHeroManagementService.TryPick(GameEntityGUID gameEntityGUID, out Unit unit)
	{
		this.Refill();
		if (this.pool.Count > 0)
		{
			int index = this.pool.Count - 1;
			HeroPool.HeroDescriptor? heroDescriptor = this.pool[index];
			this.pool.RemoveAt(index);
			return this.GenerateHeroUnitFromHeroDescriptor(gameEntityGUID, heroDescriptor, out unit);
		}
		unit = null;
		return false;
	}

	bool IHeroManagementService.TryPick(GameEntityGUID gameEntityGUID, out Unit unit, Tags tags)
	{
		this.Refill();
		if (this.pool.Count > 0)
		{
			for (int i = this.pool.Count - 1; i >= 0; i--)
			{
				HeroPool.HeroDescriptor? heroDescriptor = this.pool[i];
				if (heroDescriptor.Value.UnitProfile.Tags.Contains(tags))
				{
					this.pool.RemoveAt(i);
					return this.GenerateHeroUnitFromHeroDescriptor(gameEntityGUID, heroDescriptor, out unit);
				}
			}
		}
		unit = null;
		return false;
	}

	public int GenerationNumber { get; private set; }

	public int TurnWhenLastRefilled { get; private set; }

	public void AssignNewUserDefinedName(Unit unit)
	{
		if (unit == null)
		{
			throw new NullReferenceException("unit");
		}
		UnitProfile unitProfile = unit.UnitDesign as UnitProfile;
		if (unitProfile == null || !unitProfile.IsHero)
		{
			Diagnostics.LogError("Won't assign a user defined name to the given unit because it is not a hero.");
			return;
		}
		List<string> list;
		if (!this.names.TryGetValue(unit.UnitDesign.Name, out list))
		{
			list = new List<string>();
		}
		if (list.Count == 0)
		{
			IDatabase<UnitProfileNames> database = Databases.GetDatabase<UnitProfileNames>(false);
			UnitProfileNames unitProfileNames;
			if (database != null && database.TryGetValue(unit.UnitDesign.Name, out unitProfileNames))
			{
				System.Random random = new System.Random();
				List<string> collection = unitProfileNames.Names.ToList<string>().Randomize(random);
				list.AddRange(collection);
			}
			if (list.Count > 0 && !this.names.ContainsKey(unit.UnitDesign.Name))
			{
				this.names.Add(unit.UnitDesign.Name, list);
			}
		}
		if (list.Count > 0)
		{
			unit.UnitDesign.LocalizationKey = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
		}
	}

	public virtual void ReadXml(XmlReader reader)
	{
		this.GenerationNumber = reader.GetAttribute<int>("GenerationNumber");
		this.TurnWhenLastRefilled = reader.GetAttribute<int>("TurnWhenLastRefilled");
		reader.ReadStartElement();
		IDatabase<UnitProfile> database = Databases.GetDatabase<UnitProfile>(false);
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Pool");
		this.pool.Clear();
		for (int i = 0; i < attribute; i++)
		{
			string attribute2 = reader.GetAttribute("UnitProfileName");
			reader.Skip("UnitProfileName");
			UnitProfile unitProfile;
			if (database != null && database.TryGetValue(attribute2, out unitProfile))
			{
				HeroPool.HeroDescriptor? item = new HeroPool.HeroDescriptor?(new HeroPool.HeroDescriptor(unitProfile));
				this.pool.Add(item);
			}
		}
		reader.ReadEndElement("Pool");
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("GenerationNumber", this.GenerationNumber);
		writer.WriteAttributeString<int>("TurnWhenLastRefilled", this.TurnWhenLastRefilled);
		writer.WriteStartElement("Pool");
		writer.WriteAttributeString<int>("Count", this.pool.Count);
		for (int i = 0; i < this.pool.Count; i++)
		{
			writer.WriteStartElement("HeroDescriptor");
			writer.WriteAttributeString<StaticString>("UnitProfileName", this.pool[i].Value.UnitProfile.Name);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	public bool IsEmpty
	{
		get
		{
			return this.pool.Count == 0;
		}
	}

	private IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		yield return base.BindService<IGameEntityRepositoryService>(serviceContainer, delegate(IGameEntityRepositoryService gameEntityRepositoryService)
		{
			this.GameEntityRepositoryService = gameEntityRepositoryService;
		});
		serviceContainer.AddService<IHeroManagementService>(this);
		yield break;
	}

	public override IEnumerator LoadGame(Game game)
	{
		yield return base.LoadGame(game);
		if (this.names.Count == 0)
		{
			IDatabase<UnitProfileNames> unitProfileNames = Databases.GetDatabase<UnitProfileNames>(false);
			if (unitProfileNames != null)
			{
				System.Random random = new System.Random();
				foreach (UnitProfileNames entry in unitProfileNames)
				{
					this.names.Add(entry.Name, entry.Names.ToList<string>().Randomize(random));
				}
			}
		}
		ISessionService sessionService = Services.GetService<ISessionService>();
		if (sessionService != null && sessionService.Session != null && sessionService.Session.IsHosting)
		{
			this.Refill();
		}
		yield break;
	}

	protected override void Releasing()
	{
		this.GameEntityRepositoryService = null;
		base.Releasing();
	}

	private bool IsAnyAffinityMimicsPlaying()
	{
		foreach (Empire empire in base.Game.Empires)
		{
			if (empire != null && empire is MajorEmpire && empire.Faction.Affinity.Name == "AffinityMimics")
			{
				return true;
			}
		}
		return false;
	}

	private bool GenerateHeroUnitFromHeroDescriptor(GameEntityGUID gameEntityGUID, HeroPool.HeroDescriptor? heroDescriptor, out Unit unit)
	{
		unit = DepartmentOfDefense.CreateUnitByDesign(gameEntityGUID, heroDescriptor.Value.UnitProfile);
		if (unit != null)
		{
			this.AssignNewUserDefinedName(unit);
			return true;
		}
		return false;
	}

	private int Refill()
	{
		if (this.pool.Count > 0)
		{
			return 0;
		}
		IDatabase<UnitProfile> database = Databases.GetDatabase<UnitProfile>(false);
		if (database == null)
		{
			Diagnostics.LogError("Cannot refill the hero pool because the unit profiles database does not exist.");
			return -1;
		}
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		foreach (UnitProfile unitProfile in database)
		{
			if (unitProfile.IsHero)
			{
				if (unitProfile.Tags.Contains(HeroPool.Poolable))
				{
					if (!unitProfile.Tags.Contains("DiscardAffinityMimics") || !this.IsAnyAffinityMimicsPlaying())
					{
						if (service != null)
						{
							bool flag;
							if (service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.UnitDesign, unitProfile.Name, out flag) && !flag)
							{
								continue;
							}
							bool flag2 = true;
							foreach (Prerequisite prerequisite in unitProfile.Prerequisites)
							{
								if (prerequisite is DownloadableContentPrerequisite)
								{
									DownloadableContentPrerequisite downloadableContentPrerequisite = prerequisite as DownloadableContentPrerequisite;
									if (!service.IsShared(downloadableContentPrerequisite.DownloadableContentName))
									{
										flag2 = false;
										break;
									}
								}
							}
							if (!flag2)
							{
								continue;
							}
						}
						HeroPool.HeroDescriptor value = new HeroPool.HeroDescriptor(unitProfile);
						this.pool.Add(new HeroPool.HeroDescriptor?(value));
					}
				}
			}
		}
		this.pool = this.pool.Randomize(null);
		this.TurnWhenLastRefilled = base.Game.Turn;
		this.GenerationNumber++;
		return this.pool.Count;
	}

	public static StaticString Poolable = new StaticString("Pool");

	private List<HeroPool.HeroDescriptor?> pool = new List<HeroPool.HeroDescriptor?>();

	private Dictionary<StaticString, List<string>> names = new Dictionary<StaticString, List<string>>();

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct HeroDescriptor
	{
		public HeroDescriptor(UnitProfile unitProfile)
		{
			this.UnitProfile = (UnitProfile)unitProfile.Clone();
		}

		public UnitProfile UnitProfile { get; private set; }
	}

	[CompilerGenerated]
	private sealed class TryAllocateSkillPoints>c__AnonStorey8A1
	{
		internal Unit unit;
	}

	[CompilerGenerated]
	private sealed class TryAllocateSkillPoints>c__AnonStorey8A0
	{
		internal bool <>m__2C9(UnitSkill iterator)
		{
			return iterator.SubCategory == this.subcategory && DepartmentOfEducation.IsUnitSkillUnlockable(this.<>f__ref$2209.unit, iterator);
		}

		internal StaticString subcategory;

		internal HeroPool.TryAllocateSkillPoints>c__AnonStorey8A1 <>f__ref$2209;
	}
}
