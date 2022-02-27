using System;
using System.IO;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;

public class DiplomaticTermCityExchange : DiplomaticTerm, IDiplomaticTermManagement
{
	public DiplomaticTermCityExchange(DiplomaticTermCityExchangeDefinition definition, global::Empire empireWhichProposes, global::Empire empireWhichProvides, global::Empire empireWhichReceives, GameEntityGUID cityGUID) : base(definition, empireWhichProposes, empireWhichProvides, empireWhichReceives)
	{
		this.CityGUID = cityGUID;
	}

	void IDiplomaticTermManagement.ApplyEffects()
	{
		DiplomaticTermCityExchangeDefinition diplomaticTermCityExchangeDefinition = base.Definition as DiplomaticTermCityExchangeDefinition;
		Diagnostics.Assert(diplomaticTermCityExchangeDefinition != null);
		Diagnostics.Assert(base.EmpireWhichProvides != null && base.EmpireWhichReceives != null);
		DepartmentOfTheInterior agency = base.EmpireWhichProvides.GetAgency<DepartmentOfTheInterior>();
		DepartmentOfTheInterior agency2 = base.EmpireWhichReceives.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null && agency2 != null);
		City city = null;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city2 = agency.Cities[i];
			if (city2.GUID == this.CityGUID)
			{
				city = city2;
				break;
			}
		}
		if (city == null)
		{
			Diagnostics.LogError("DiplomaticTermCityExchange.ApplyEffect failed, can't retrieve the city {0} from the empire which provides the term ({1}).", new object[]
			{
				this.CityGUID,
				base.EmpireWhichProvides
			});
			return;
		}
		IEventService service = Services.GetService<IEventService>();
		if (service == null)
		{
			Diagnostics.LogError("Failed to retrieve the event service.");
			return;
		}
		bool flag = base.EmpireWhichReceives.SimulationObject.Tags.Contains("FactionTraitCultists9");
		bool flag2 = agency2.Cities.Count >= 1;
		if (flag && flag2)
		{
			IGameService service2 = Services.GetService<IGameService>();
			IPlayerControllerRepositoryService service3 = service2.Game.Services.GetService<IPlayerControllerRepositoryService>();
			IPlayerControllerRepositoryControl playerControllerRepositoryControl = service3 as IPlayerControllerRepositoryControl;
			if (playerControllerRepositoryControl != null)
			{
				global::PlayerController playerControllerById = playerControllerRepositoryControl.GetPlayerControllerById("server");
				if (playerControllerById != null)
				{
					float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
					DepartmentOfScience agency3 = base.EmpireWhichReceives.GetAgency<DepartmentOfScience>();
					bool flag3 = agency3 != null && agency3.GetTechnologyState("TechnologyDefinitionCultists12") == DepartmentOfScience.ConstructibleElement.State.Researched;
					int num = 0;
					while ((float)num < propertyValue)
					{
						OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(base.EmpireWhichReceives.Index, "BoosterIndustry", 0UL, false);
						playerControllerById.PostOrder(order);
						if (flag3)
						{
							order = new OrderBuyoutAndActivateBooster(base.EmpireWhichReceives.Index, "BoosterScience", 0UL, false);
							playerControllerById.PostOrder(order);
						}
						num++;
					}
					OrderDestroyCity order2 = new OrderDestroyCity(city.Empire.Index, city.GUID, true, true, base.EmpireWhichReceives.Index);
					playerControllerById.PostOrder(order2);
					EventCityRazed eventToNotify = new EventCityRazed(city.Empire, city.Region, base.EmpireWhichReceives, false);
					service.Notify(eventToNotify);
				}
			}
		}
		else
		{
			agency.SwapCityOwner(city, base.EmpireWhichReceives);
			service.Notify(new EventSwapCity(base.EmpireWhichProvides, city, base.EmpireWhichProvides.Index, base.EmpireWhichReceives.Index, false));
			service.Notify(new EventSwapCity(base.EmpireWhichReceives, city, base.EmpireWhichProvides.Index, base.EmpireWhichReceives.Index, false));
		}
	}

	public GameEntityGUID CityGUID { get; private set; }

	public override bool CanApply(IDiplomaticContract diplomaticContract, params string[] flags)
	{
		if (!base.CanApply(diplomaticContract, new string[0]))
		{
			return false;
		}
		Diagnostics.Assert(base.EmpireWhichProvides != null && base.EmpireWhichReceives != null);
		DepartmentOfTheInterior agency = base.EmpireWhichProvides.GetAgency<DepartmentOfTheInterior>();
		DepartmentOfTheInterior agency2 = base.EmpireWhichReceives.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null && agency2 != null);
		City city = agency.Cities.FirstOrDefault((City match) => match.GUID == this.CityGUID);
		if (city == null)
		{
			return false;
		}
		if (city.BesiegingEmpire != null)
		{
			return false;
		}
		if (diplomaticContract.EmpireWhichProposes.Index != base.EmpireWhichProvides.Index)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IVisibilityService service2 = service.Game.Services.GetService<IVisibilityService>();
			Diagnostics.Assert(service2 != null);
			District cityCenter = city.GetCityCenter();
			if (cityCenter == null || !service2.IsWorldPositionExploredFor(cityCenter.WorldPosition, base.EmpireWhichReceives))
			{
				return false;
			}
		}
		if (base.Definition.Name == DiplomaticTermCityExchange.MimicsCityDeal)
		{
			if (agency.MainCity == city)
			{
				return false;
			}
			if (base.EmpireWhichProvides.Faction.Affinity.Name == base.EmpireWhichReceives.Faction.Affinity.Name)
			{
				return false;
			}
			if (city.IsInfected)
			{
				return false;
			}
			if (base.EmpireWhichReceives.GetAgency<DepartmentOfPlanificationAndDevelopment>().HasIntegratedFaction(base.EmpireWhichProvides.Faction))
			{
				return false;
			}
			if (agency2.InfectedCities.Any((City c) => c.LastNonInfectedOwner != null && (c.LastNonInfectedOwner == base.EmpireWhichProvides || c.LastNonInfectedOwner.Faction == base.EmpireWhichProvides.Faction)))
			{
				return false;
			}
		}
		int num = agency.Cities.Count;
		int num2 = agency2.Cities.Count;
		for (int i = 0; i < diplomaticContract.Terms.Count; i++)
		{
			DiplomaticTermCityExchange diplomaticTermCityExchange = diplomaticContract.Terms[i] as DiplomaticTermCityExchange;
			if (diplomaticTermCityExchange != null && !(diplomaticTermCityExchange.CityGUID == this.CityGUID))
			{
				if (base.Definition.Name == DiplomaticTermCityExchange.MimicsCityDeal)
				{
					return false;
				}
				if (diplomaticTermCityExchange.EmpireWhichProvides.Index == base.EmpireWhichProvides.Index)
				{
					num--;
					num2++;
				}
				else if (diplomaticTermCityExchange.EmpireWhichProvides.Index == base.EmpireWhichReceives.Index)
				{
					num2--;
					num++;
				}
				else
				{
					Diagnostics.LogError("Can't identify the empire which provides the term {0}.", new object[]
					{
						diplomaticTermCityExchange
					});
				}
			}
		}
		num--;
		num2++;
		return num >= 1 && num2 >= 1;
	}

	public override bool Equals(DiplomaticTerm other)
	{
		DiplomaticTermCityExchange diplomaticTermCityExchange = other as DiplomaticTermCityExchange;
		return diplomaticTermCityExchange != null && this.CityGUID == diplomaticTermCityExchange.CityGUID;
	}

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		writer.Write(this.CityGUID);
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		this.CityGUID = reader.ReadElementString<ulong>("CityGUID");
	}

	public override string ToString()
	{
		return string.Format("{0} CityGUID: {1}", base.ToString(), this.CityGUID);
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		this.CityGUID = reader.ReadUInt64();
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteElementString<ulong>("CityGUID", this.CityGUID);
	}

	public static StaticString MimicsCityDeal = "MimicsCityDeal";
}
