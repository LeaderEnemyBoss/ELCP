using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;

public class ELCPUtilities
{
	public static bool UseELCPPeacePointRulseset
	{
		get
		{
			return ELCPUtilities.useELCPPeacePointRulseset;
		}
		set
		{
			ELCPUtilities.useELCPPeacePointRulseset = value;
		}
	}

	public static void SetupELCPSettings()
	{
		Diagnostics.Log("Setting up ELCP settings ...");
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		ELCPUtilities.ELCPShackleAI = service.Session.GetLobbyData<bool>("ShackleAI", false);
		Diagnostics.Log("ELCPShackleAI is {0}", new object[]
		{
			ELCPUtilities.ELCPShackleAI
		});
		ELCPUtilities.UseELCPPeacePointRulseset = service.Session.GetLobbyData<bool>("PeacePointRulseset", true);
		Diagnostics.Log("ELCPPeacePointRuleset is {0}", new object[]
		{
			ELCPUtilities.UseELCPPeacePointRulseset
		});
		ELCPUtilities.UseELCPFortificationPointRuleset = (service.Session.GetLobbyData<string>("FortificationRules", "Vanilla") == "ELCP");
		Diagnostics.Log("ELCPFortificationPointRuleset is {0}", new object[]
		{
			ELCPUtilities.UseELCPFortificationPointRuleset
		});
		ELCPUtilities.UseELCPStockpileRulseset = (service.Session.GetLobbyData<string>("StockpileRules", "Vanilla") == "ELCP");
		Diagnostics.Log("ELCPStockpileRulseset is {0}", new object[]
		{
			ELCPUtilities.UseELCPStockpileRulseset
		});
		ELCPUtilities.UseELCPBlackspotRuleset = service.Session.GetLobbyData<bool>("BlackspotRules", true);
		Diagnostics.Log("ELCPBlackspotRules is {0}", new object[]
		{
			ELCPUtilities.UseELCPBlackspotRuleset
		});
		string lobbyData = service.Session.GetLobbyData<string>("ArmySpeedScaleFactor", "Vanilla");
		double elcparmySpeedScaleFactor = 1.0;
		if (lobbyData == "Vanilla" || !double.TryParse(lobbyData, out elcparmySpeedScaleFactor))
		{
			ELCPUtilities.ELCPArmySpeedScaleFactor = 1.0;
		}
		else
		{
			ELCPUtilities.ELCPArmySpeedScaleFactor = elcparmySpeedScaleFactor;
		}
		Diagnostics.Log("ELCPArmySpeedScaleFactor is {0}", new object[]
		{
			ELCPUtilities.ELCPArmySpeedScaleFactor
		});
	}

	public static bool CanTeleportToCity(City city, Army army, Region originRegion, IWorldPositionningService worldPositionningService, IEncounterRepositoryService encounterRepositoryService)
	{
		WorldPosition position;
		return city != null && city.GUID.IsValid && originRegion.City != null && city.Empire.Index == army.Empire.Index && city != originRegion.City && (encounterRepositoryService == null || !encounterRepositoryService.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(city.GUID, false))) && army.Empire.GetAgency<DepartmentOfTransportation>().TryGetFirstCityTileAvailableForTeleport(city, out position) && position.IsValid && !worldPositionningService.IsWaterTile(position);
	}

	public static bool IsELCPCityBattle(List<IGarrison> MainContenders, out City city)
	{
		city = null;
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		foreach (IGarrison garrison in MainContenders)
		{
			District district = service.GetDistrict((garrison as IWorldPositionable).WorldPosition);
			if (!service.IsWaterTile((garrison as IWorldPositionable).WorldPosition) && district != null && (District.IsACityTile(district) || district.Type == DistrictType.Exploitation))
			{
				city = district.City;
				return true;
			}
		}
		return false;
	}

	public static bool TryGetFirstNodeOfType<T>(BehaviourTreeNodeController controller, out T Node)
	{
		foreach (BehaviourTreeNode behaviourTreeNode in controller.Children)
		{
			if (behaviourTreeNode is T)
			{
				Node = (T)((object)behaviourTreeNode);
				return true;
			}
			if (behaviourTreeNode is BehaviourTreeNodeController)
			{
				T t = default(T);
				if (ELCPUtilities.TryGetFirstNodeOfType<T>(behaviourTreeNode as BehaviourTreeNodeController, out t))
				{
					Node = t;
					return true;
				}
			}
			if (behaviourTreeNode is QuestBehaviourTreeNode_Decorator_InteractWith)
			{
				foreach (QuestBehaviourTreeNode_ConditionCheck questBehaviourTreeNode_ConditionCheck in (behaviourTreeNode as QuestBehaviourTreeNode_Decorator_InteractWith).ConditionChecks)
				{
					if (questBehaviourTreeNode_ConditionCheck is T)
					{
						Node = (T)((object)questBehaviourTreeNode_ConditionCheck);
						return true;
					}
				}
			}
		}
		Node = default(T);
		return false;
	}

	public static bool TryGetNodeOfType<T>(BehaviourTreeNodeController controller, out T Node, ref int startvalue, int position = 1)
	{
		if (position >= 1 && startvalue < position)
		{
			foreach (BehaviourTreeNode behaviourTreeNode in controller.Children)
			{
				if (behaviourTreeNode is T)
				{
					startvalue++;
					if (startvalue == position)
					{
						Node = (T)((object)behaviourTreeNode);
						return true;
					}
				}
				if (behaviourTreeNode is BehaviourTreeNodeController)
				{
					T t = default(T);
					if (ELCPUtilities.TryGetNodeOfType<T>(behaviourTreeNode as BehaviourTreeNodeController, out t, ref startvalue, position))
					{
						Node = t;
						return true;
					}
				}
				if (behaviourTreeNode is QuestBehaviourTreeNode_Decorator_InteractWith)
				{
					foreach (QuestBehaviourTreeNode_ConditionCheck questBehaviourTreeNode_ConditionCheck in (behaviourTreeNode as QuestBehaviourTreeNode_Decorator_InteractWith).ConditionChecks)
					{
						if (questBehaviourTreeNode_ConditionCheck is T)
						{
							startvalue++;
							if (startvalue == position)
							{
								Node = (T)((object)questBehaviourTreeNode_ConditionCheck);
								return true;
							}
						}
					}
				}
			}
		}
		Node = default(T);
		return false;
	}

	public static bool UseELCPFortificationPointRuleset
	{
		get
		{
			return ELCPUtilities.useELCPFortificationPointRuleset;
		}
		set
		{
			ELCPUtilities.useELCPFortificationPointRuleset = value;
		}
	}

	public static bool UseELCPStockpileRulseset
	{
		get
		{
			return ELCPUtilities.useELCPStockpileRulseset;
		}
		set
		{
			ELCPUtilities.useELCPStockpileRulseset = value;
		}
	}

	public static bool GetsFortificationBonus(IGarrison garrison, City city)
	{
		if (garrison == null || city == null)
		{
			return false;
		}
		District district = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>().GetDistrict((garrison as IWorldPositionable).WorldPosition);
		return district != null && District.IsACityTile(district) && district.City == city && district.City.Empire == garrison.Empire;
	}

	public static double ELCPArmySpeedScaleFactor
	{
		get
		{
			return ELCPUtilities.armySpeedScaleFactor;
		}
		set
		{
			ELCPUtilities.armySpeedScaleFactor = value;
		}
	}

	public static bool ELCPShackleAI
	{
		get
		{
			return ELCPUtilities.useELCPShackleAI;
		}
		set
		{
			ELCPUtilities.useELCPShackleAI = value;
		}
	}

	public static bool UseELCPBlackspotRuleset
	{
		get
		{
			return ELCPUtilities.useELCPBlackspotRuleset;
		}
		set
		{
			ELCPUtilities.useELCPBlackspotRuleset = value;
		}
	}

	private static bool useELCPPeacePointRulseset;

	private static bool useELCPFortificationPointRuleset;

	private static bool useELCPStockpileRulseset;

	private static double armySpeedScaleFactor;

	private static bool useELCPShackleAI;

	private static bool useELCPBlackspotRuleset;

	public static class AIVictoryFocus
	{
		public static readonly string Economy = "Economy";

		public static readonly string Diplomacy = "Diplomacy";

		public static readonly string Military = "Military";

		public static readonly string Technology = "MostTechnologiesDiscovered";
	}
}
