using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;

public abstract class ArmyActionWithCooldown : ArmyAction
{
	[XmlElement]
	public float CooldownDuration { get; set; }

	public static void ApplyCooldown(Army army, float cooldownDuration)
	{
		if (army != null && cooldownDuration > 0f)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null && service.Game != null)
			{
				ICooldownManagementService service2 = service.Game.Services.GetService<ICooldownManagementService>();
				if (service2 != null)
				{
					foreach (Unit unit in army.Units)
					{
						service2.AddCooldown(unit.GUID, cooldownDuration);
					}
				}
			}
		}
	}

	public float ComputeCooldownDuration(Army army)
	{
		if (this.CooldownDuration > 0f)
		{
			float num = this.CooldownDuration;
			if (!this.DoNotScale)
			{
				float propertyValue = army.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
				num *= propertyValue;
				if (army != null)
				{
					float cooldownDurationReduction = this.GetCooldownDurationReduction(army);
					num += cooldownDurationReduction;
				}
			}
			return Math.Max(1f, (float)Math.Floor((double)num));
		}
		return 0f;
	}

	public float ComputeRemainingCooldownDuration(Army army)
	{
		float num = 0f;
		if (army != null)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null && service.Game != null)
			{
				ICooldownManagementService service2 = service.Game.Services.GetService<ICooldownManagementService>();
				if (service2 != null)
				{
					foreach (Unit unit in army.Units)
					{
						Cooldown cooldown;
						if (service2.TryGetCooldown(unit.GUID, out cooldown))
						{
							float num2 = (float)cooldown.TurnWhenStarted + cooldown.Duration - (float)(service.Game as global::Game).Turn;
							if (num2 > num)
							{
								num = num2;
							}
						}
					}
				}
			}
		}
		return num;
	}

	public float GetCooldownDurationReduction(Army army)
	{
		float num = 0f;
		SimulationProperty property = army.SimulationObject.GetProperty(SimulationProperties.CooldownReduction);
		if (property == null)
		{
			foreach (Unit unit in army.Units)
			{
				property = unit.SimulationObject.GetProperty(SimulationProperties.CooldownReduction);
				if (property == null)
				{
					return 0f;
				}
				num = Math.Min(num, property.Value);
			}
			return num;
		}
		num = Math.Min(num, property.Value);
		return num;
	}

	public override bool IsConcernedByEvent(Event gameEvent, Army army)
	{
		return (gameEvent != null && gameEvent.EventName == EventBeginTurn.Name) || base.IsConcernedByEvent(gameEvent, army);
	}

	protected virtual bool CheckCooldownPrerequisites(Army army)
	{
		if (army != null)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null && service.Game != null)
			{
				ICooldownManagementService service2 = service.Game.Services.GetService<ICooldownManagementService>();
				if (service2 != null)
				{
					foreach (Unit unit in army.Units)
					{
						Cooldown cooldown;
						if (service2.TryGetCooldown(unit.GUID, out cooldown))
						{
							return false;
						}
					}
					return true;
				}
			}
		}
		return true;
	}

	[XmlElement]
	public bool DoNotScale { get; set; }

	public static readonly StaticString NoCanDoWhileCooldownInProgress = "ArmyActionCooldownInProgress";
}
