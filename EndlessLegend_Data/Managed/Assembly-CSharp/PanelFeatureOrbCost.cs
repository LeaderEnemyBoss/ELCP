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
		IPropertyEffectFeatureProvider propertyEffectFeatureProvider = this.context as IPropertyEffectFeatureProvider;
		if (this.context is OrbCostTooltipData)
		{
			propertyEffectFeatureProvider = (this.context as OrbCostTooltipData).Context;
			this.Value.Text = string.Empty;
		}
		if (propertyEffectFeatureProvider != null)
		{
			SimulationObject simulationObject = propertyEffectFeatureProvider.GetSimulationObject();
			float propertyValue = simulationObject.GetPropertyValue("NumberOfPastWinters");
			float num = simulationObject.GetPropertyValue("PrayerCostByTurnsSinceSeasonStart") * simulationObject.GetPropertyValue("NumberOfTurnsSinceSummerStart");
			List<EffectDescription> list = new List<EffectDescription>();
			if (propertyValue > 0f)
			{
				string toStringOverride = AgeLocalizer.Instance.LocalizeString("%FeatureOrbCostFromPastWinters").Replace("$Value", propertyValue.ToString());
				list.Add(new EffectDescription(toStringOverride));
			}
			if (num > 0f)
			{
				string toStringOverride2 = AgeLocalizer.Instance.LocalizeString("%FeatureOrbCostFromTurns").Replace("$Value", num.ToString());
				list.Add(new EffectDescription(toStringOverride2));
			}
			this.EffectMapper.LoadEffects(list, true);
			float num2 = 0f;
			IGameService service = Services.GetService<IGameService>();
			if (service != null)
			{
				IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
				ISeasonService service3 = service.Game.Services.GetService<ISeasonService>();
				if (service3 != null && service2 != null)
				{
					num2 = service3.ComputePrayerOrbCost(service2.ActivePlayerController.Empire as global::Empire);
				}
			}
			this.Value.Text = num2.ToString() + base.GuiService.FormatSymbol(DepartmentOfTheTreasury.Resources.Orb);
		}
		yield return base.OnShow(parameters);
		yield break;
	}

	public AgePrimitiveLabel Value;
}
