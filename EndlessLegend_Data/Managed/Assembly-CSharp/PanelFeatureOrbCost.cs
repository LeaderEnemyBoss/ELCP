using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui.SimulationEffect;
using Amplitude.Unity.Simulation;

public class PanelFeatureOrbCost : PanelFeatureEffects
{
	protected override IEnumerator OnShow(params object[] parameters)
	{
		IPropertyEffectFeatureProvider provider = this.context as IPropertyEffectFeatureProvider;
		if (this.context is OrbCostTooltipData)
		{
			provider = (this.context as OrbCostTooltipData).Context;
			this.Value.Text = string.Empty;
		}
		if (provider != null)
		{
			SimulationObject simulationObject = provider.GetSimulationObject();
			float orbCostFromPastWinters = simulationObject.GetPropertyValue("PrayerCostByPastWinter") * simulationObject.GetPropertyValue("NumberOfPastWinters");
			float orbCostFromTurns = simulationObject.GetPropertyValue("PrayerCostByTurnsSinceSeasonStart") * simulationObject.GetPropertyValue("NumberOfTurnsSinceSummerStart");
			List<EffectDescription> effectDescriptions = new List<EffectDescription>();
			if (orbCostFromPastWinters > 0f)
			{
				string orbCostFromPastWintersDescription = AgeLocalizer.Instance.LocalizeString("%FeatureOrbCostFromPastWinters").Replace("$Value", orbCostFromPastWinters.ToString());
				effectDescriptions.Add(new EffectDescription(orbCostFromPastWintersDescription));
			}
			if (orbCostFromTurns > 0f)
			{
				string orbCostFromTurnsDescription = AgeLocalizer.Instance.LocalizeString("%FeatureOrbCostFromTurns").Replace("$Value", orbCostFromTurns.ToString());
				effectDescriptions.Add(new EffectDescription(orbCostFromTurnsDescription));
			}
			this.EffectMapper.LoadEffects(effectDescriptions, true);
			float totalOrbCost = 0f;
			IGameService gameService = Services.GetService<IGameService>();
			if (gameService != null)
			{
				IPlayerControllerRepositoryService playerControllerRepository = gameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
				ISeasonService seasonService = gameService.Game.Services.GetService<ISeasonService>();
				if (seasonService != null && playerControllerRepository != null)
				{
					totalOrbCost = seasonService.ComputePrayerOrbCost(playerControllerRepository.ActivePlayerController.Empire as global::Empire);
				}
			}
			this.Value.Text = totalOrbCost.ToString() + base.GuiService.FormatSymbol(DepartmentOfTheTreasury.Resources.Orb);
		}
		yield return base.OnShow(parameters);
		yield break;
	}

	public AgePrimitiveLabel Value;
}
