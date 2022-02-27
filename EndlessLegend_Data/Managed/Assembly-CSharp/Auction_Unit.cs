using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude.Unity.Game.Orders;

public class Auction_Unit : SimulationAuction<AuctionItem_GameEntity>
{
	[XmlAttribute]
	public bool AllowSettlerSale { get; set; }

	public override void GatherAuctionItems(Empire empire)
	{
		base.GatherAuctionItems(empire);
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			for (int j = 0; j < agency.Armies[i].StandardUnits.Count; j++)
			{
				if (this.CanSellUnit(agency.Armies[i].StandardUnits[j]))
				{
					float score = this.ComputeUnitScore(agency.Armies[i].StandardUnits[j]);
					base.AuctionItems.Add(new AuctionItem_GameEntity(agency.Armies[i].StandardUnits[j], score));
				}
			}
		}
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		for (int k = 0; k < agency2.Cities.Count; k++)
		{
			for (int l = 0; l < agency2.Cities[k].StandardUnits.Count; l++)
			{
				if (this.CanSellUnit(agency2.Cities[k].StandardUnits[l]))
				{
					float score2 = this.ComputeUnitScore(agency2.Cities[k].StandardUnits[l]);
					base.AuctionItems.Add(new AuctionItem_GameEntity(agency2.Cities[k].StandardUnits[l], score2));
				}
			}
		}
	}

	protected override Amplitude.Unity.Game.Orders.Order SellOne(Empire empire, AuctionItem_GameEntity item, ref List<AuctionInstruction> instructions)
	{
		Unit unit = item.GameEntity as Unit;
		string parentName = string.Empty;
		if (unit.Garrison is City)
		{
			parentName = (unit.Garrison as City).LocalizedName;
		}
		else if (unit.Garrison is Army)
		{
			parentName = (unit.Garrison as Army).LocalizedName;
		}
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		if (agency != null && agency.GetTechnologyState(TechnologyDefinition.Names.MarketplaceMercenaries) == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			instructions.Add(new AuctionInstruction(AuctionInstruction.AuctionType.Unit, AuctionInstruction.AuctionAction.Sell, unit.UnitDesign.LocalizedName, 1, parentName));
		}
		else
		{
			instructions.Add(new AuctionInstruction(AuctionInstruction.AuctionType.Unit, AuctionInstruction.AuctionAction.Destroy, unit.UnitDesign.LocalizedName, 1, parentName));
		}
		return new OrderSelloutTradableUnits(empire.Index, new GameEntityGUID[]
		{
			item.ItemGuid
		});
	}

	private bool CanSellUnit(Unit unit)
	{
		return !unit.CheckUnitAbility(UnitAbility.ReadonlyUnsalable, -1) && (this.AllowSettlerSale || !unit.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1));
	}

	private float ComputeUnitScore(Unit unit)
	{
		return base.ComputeSimulationScore(unit);
	}
}
