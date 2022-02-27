using System;
using Amplitude;
using UnityEngine;

public class GuiEmpire
{
	public GuiEmpire(Empire empire)
	{
		this.Empire = empire;
		this.GuiFaction = new GuiFaction(empire.Faction);
	}

	public Empire Empire { get; private set; }

	public GuiFaction GuiFaction { get; private set; }

	public int Index
	{
		get
		{
			return this.Empire.Index;
		}
	}

	public Color Color
	{
		get
		{
			return this.Empire.Color;
		}
	}

	public static string Colorize(Empire empire, string name)
	{
		string str;
		AgeUtils.ColorToHexaKey(empire.Color, out str);
		return str + name + "#REVERT#";
	}

	public static bool IsKnownByLookingPlayer(Empire empire, Empire empireLooking)
	{
		if (empire.Index == empireLooking.Index)
		{
			return true;
		}
		DepartmentOfForeignAffairs agency = empireLooking.GetAgency<DepartmentOfForeignAffairs>();
		if (agency != null)
		{
			DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empire);
			return diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown;
		}
		return false;
	}

	public static string GetFactionSymbolString(Empire empire, Empire empireLooking)
	{
		if (GuiEmpire.IsKnownByLookingPlayer(empire, empireLooking))
		{
			GuiFaction guiFaction = new GuiFaction(empire.Faction);
			return guiFaction.Icon;
		}
		return "\\7800\\";
	}

	public Texture2D GetImageTexture(StaticString size, Empire empireLooking)
	{
		bool flag = this.Empire is MajorEmpire && (this.Empire as MajorEmpire).IsSpectator;
		if (this.Empire.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated) && !flag)
		{
			if (size == GuiPanel.IconSize.LogoSmall)
			{
				return AgeManager.Instance.FindDynamicTexture("eliminatedLogoSmall", false);
			}
			return AgeManager.Instance.FindDynamicTexture("Gui/DynamicBitmaps/Factions/elimination" + size, false);
		}
		else
		{
			if (GuiEmpire.IsKnownByLookingPlayer(this.Empire, empireLooking) || flag)
			{
				return this.GuiFaction.GetImageTexture(size, false);
			}
			return this.GuiFaction.GetRandomImageTexture(size);
		}
	}

	public string GetColorizedLocalizedName(Empire empireLooking, bool useYou = false)
	{
		if (useYou && this.Empire == empireLooking)
		{
			return GuiEmpire.Colorize(this.Empire, AgeLocalizer.Instance.LocalizeString("%YouTitle"));
		}
		return GuiEmpire.Colorize(this.Empire, this.Empire.LocalizedName);
	}

	public string GetColorizedLocalizedNameAndFaction(Empire empireLooking, bool useYou = false)
	{
		if (useYou && this.Empire == empireLooking)
		{
			return GuiEmpire.Colorize(this.Empire, AgeLocalizer.Instance.LocalizeString("%YouTitle"));
		}
		if (TutorialManager.IsActivated)
		{
			return GuiEmpire.Colorize(this.Empire, this.Empire.LocalizedName);
		}
		return GuiEmpire.Colorize(this.Empire, this.Empire.LocalizedName + " - " + this.GuiFaction.LocalizedName);
	}
}
