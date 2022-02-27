using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class Booster : SimulationObjectWrapper, IGameEntity, IDescriptorEffectProvider
{
	public Booster()
	{
		this.GUID = 0UL;
		this.TurnWhenStarted = -1;
		this.RemainingTime = -1;
	}

	public Booster(GameEntityGUID boosterGUID, BoosterDefinition boosterDefinition, global::Empire empire, SimulationObjectWrapper context, GameEntityGUID contextGUID, int duration, int instigatorEmpireIndex) : base("Booster#" + boosterGUID)
	{
		this.GUID = boosterGUID;
		this.BoosterDefinition = boosterDefinition;
		this.empire = empire;
		this.Duration = duration;
		this.TurnWhenStarted = -1;
		this.RemainingTime = -1;
		this.Context = context;
		this.TargetGUID = contextGUID;
		this.InstigatorEmpireIndex = instigatorEmpireIndex;
		IGameService service = Services.GetService<IGameService>();
		if (service != null)
		{
			this.game = (service.Game as global::Game);
		}
		this.simulationDescriptorsDatatable = Databases.GetDatabase<SimulationDescriptor>(false);
	}

	public BoosterDefinition BoosterDefinition { get; set; }

	public SimulationObjectWrapper Context { get; private set; }

	public int Duration { get; set; }

	public int InstigatorEmpireIndex { get; set; }

	public GameEntityGUID GUID { get; private set; }

	public int RemainingTime
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.RemainingTime));
		}
		set
		{
			base.SetPropertyBaseValue(SimulationProperties.RemainingTime, (float)value);
			this.Refresh(false);
		}
	}

	public GameEntityGUID TargetGUID { get; private set; }

	public int TurnWhenStarted { get; set; }

	public StaticString DefaultClass
	{
		get
		{
			return "ClassEmpire";
		}
	}

	public static bool CanActivate(BoosterDefinition boosterDefinition, SimulationObjectWrapper context)
	{
		return Booster.CheckActivationPrerequisites(boosterDefinition, context) && (boosterDefinition.Effects == null || Booster.CanApplyAtLeastOneEffect(boosterDefinition, context));
	}

	public static bool CheckPrerequisites(BoosterEffect effect, SimulationObjectWrapper context)
	{
		if (effect.Prerequisites == null || effect.Prerequisites.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < effect.Prerequisites.Length; i++)
		{
			if (!effect.Prerequisites[i].Check(context))
			{
				return false;
			}
		}
		return true;
	}

	public static BoosterDefinition.TargetType GetTargetType(StaticString boosterDefinitionName)
	{
		IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
		BoosterDefinition boosterDefinition;
		if (!database.TryGetValue(boosterDefinitionName, out boosterDefinition))
		{
			Diagnostics.LogError("Wasn't able to retrieve booster definition '{0}.'", new object[]
			{
				boosterDefinitionName
			});
			return BoosterDefinition.TargetType.Empire;
		}
		return boosterDefinition.Target;
	}

	public static bool IsTargetValid(BoosterDefinition boosterDefinition, SimulationObjectWrapper context)
	{
		return boosterDefinition.TargetPath == null || string.IsNullOrEmpty(boosterDefinition.TargetPath.ToString()) || boosterDefinition.TargetPath.IsSimulationObjectValid(context);
	}

	public IEnumerable<SimulationDescriptor> GetDescriptors()
	{
		int num;
		for (int index = 0; index < this.BoosterDefinition.Descriptors.Length; index = num + 1)
		{
			yield return this.BoosterDefinition.Descriptors[index];
			num = index;
		}
		if (this.BoosterDefinition.Effects != null)
		{
			for (int index = 0; index < this.BoosterDefinition.Effects.Length; index = num + 1)
			{
				BoosterEffect effect = this.BoosterDefinition.Effects[index];
				if (Booster.CheckPrerequisites(effect, this.Context))
				{
					for (int i = 0; i < effect.SimulationDescriptors.Length; i = num + 1)
					{
						yield return effect.SimulationDescriptors[i];
						num = i;
					}
				}
				effect = null;
				num = index;
			}
		}
		yield break;
	}

	public bool IsExpired()
	{
		return this.RemainingTime <= 0;
	}

	public bool IsActive()
	{
		return this.TurnWhenStarted > -1;
	}

	public void OnEndTurn()
	{
		if (this.BoosterDefinition.BoosterType != BoosterDefinition.Type.Instant)
		{
			this.RemainingTime--;
			if (this.IsExpired())
			{
				this.Deactivate();
			}
			this.Refresh(true);
		}
	}

	public void Activate()
	{
		if (!Booster.CanActivate(this.BoosterDefinition, this.Context))
		{
			return;
		}
		if (this.IsActive())
		{
			this.OnReactivation();
			if (this.BoosterDefinition.BoosterType == BoosterDefinition.Type.ResettingTime)
			{
				return;
			}
		}
		else
		{
			if (this.BoosterDefinition.BoosterType == BoosterDefinition.Type.Instant)
			{
				this.Duration = 0;
			}
			else
			{
				this.TurnWhenStarted = this.game.Turn;
				this.Context.AddChild(this);
				this.ApplyClassTimedDescriptor();
				this.ApplyDescriptors(true);
				this.RemainingTime = this.Duration;
			}
			this.ApplyEffects();
			this.Context.Refresh(false);
		}
		if (this.game != null)
		{
			IVisibilityService service = this.game.Services.GetService<IVisibilityService>();
			if (service != null && this.empire != null)
			{
				service.NotifyVisibilityHasChanged(this.empire);
			}
		}
		if (this.BoosterDefinition.Effects != null)
		{
			for (int i = 0; i < this.BoosterDefinition.Effects.Length; i++)
			{
				BoosterEffect effect = this.BoosterDefinition.Effects[i];
				if (Booster.CheckPrerequisites(effect, this.Context))
				{
					this.ExecuteCommands(effect);
				}
			}
		}
		IEventService service2 = Services.GetService<IEventService>();
		if (service2 != null)
		{
			service2.Notify(new EventBoosterActivated(this.empire, this));
		}
	}

	public void ApplyEffects()
	{
		if (this.BoosterDefinition.Effects == null)
		{
			return;
		}
		for (int i = 0; i < this.BoosterDefinition.Effects.Length; i++)
		{
			BoosterEffect boosterEffect = this.BoosterDefinition.Effects[i];
			if (Booster.CheckPrerequisites(boosterEffect, this.Context))
			{
				this.ExecuteTransferResource(boosterEffect);
				if (boosterEffect.SimulationDescriptors != null)
				{
					for (int j = 0; j < boosterEffect.SimulationDescriptors.Length; j++)
					{
						base.AddDescriptor(boosterEffect.SimulationDescriptors[j], false);
					}
				}
			}
		}
	}

	public void RemoveEffects()
	{
		if (this.BoosterDefinition.Effects == null)
		{
			return;
		}
		for (int i = 0; i < this.BoosterDefinition.Effects.Length; i++)
		{
			BoosterEffect boosterEffect = this.BoosterDefinition.Effects[i];
			if (boosterEffect.SimulationDescriptors != null)
			{
				for (int j = 0; j < boosterEffect.SimulationDescriptors.Length; j++)
				{
					base.RemoveDescriptor(boosterEffect.SimulationDescriptors[j]);
				}
			}
		}
	}

	public void ExecuteCommands(BoosterEffect effect)
	{
		if (effect.Commands == null || effect.Commands.Length == 0)
		{
			return;
		}
		global::PlayerController server = this.empire.PlayerControllers.Server;
		if (server == null)
		{
			return;
		}
		for (int i = 0; i < effect.Commands.Length; i++)
		{
			BoosterEffect.Command command = effect.Commands[i];
			if (command != null && !string.IsNullOrEmpty(command.Name))
			{
				List<StaticString> list = new List<StaticString>();
				for (int j = 0; j < command.Arguments.Length; j++)
				{
					list.Add(command.Arguments[j]);
				}
				string name = command.Name;
				if (name != null)
				{
					if (Booster.<>f__switch$map12 == null)
					{
						Booster.<>f__switch$map12 = new Dictionary<string, int>(1)
						{
							{
								"CreateUnit",
								0
							}
						};
					}
					int num;
					if (Booster.<>f__switch$map12.TryGetValue(name, out num))
					{
						if (num == 0)
						{
							for (int k = 0; k < list.Count; k++)
							{
								OrderSpawnUnit order = new OrderSpawnUnit(this.empire.Index, 0, true, list[k], this.TargetGUID, command.AllowGarrisonOverflow);
								server.PostOrder(order);
								if (effect.SimulationDescriptorReferences.Any((SimulationDescriptorReference descriptor) => descriptor.Name == AchievementManager.BattlebornDesignName))
								{
									IEventService service = Services.GetService<IEventService>();
									if (service != null)
									{
										EventBattlebornCreated eventToNotify = new EventBattlebornCreated(this.empire);
										service.Notify(eventToNotify);
									}
								}
							}
							goto IL_1A6;
						}
					}
				}
				Diagnostics.LogWarning("No process defined for command '{0}'", new object[]
				{
					command.Name
				});
			}
			IL_1A6:;
		}
	}

	public void ExecuteTransferResource(BoosterEffect effect)
	{
		if (effect.Transfer == null)
		{
			return;
		}
		for (int i = 0; i < effect.Transfer.Length; i++)
		{
			BoosterEffect.TransferResource transferResource = effect.Transfer[i];
			if (string.IsNullOrEmpty(transferResource.ResourceName))
			{
				Diagnostics.LogWarning("Booster '{0}': Transfer number {1} resource name is null or empty.", new object[]
				{
					this.BoosterDefinition.Name,
					i
				});
			}
			else
			{
				DepartmentOfTheTreasury agency = this.empire.GetAgency<DepartmentOfTheTreasury>();
				if (agency != null)
				{
					agency.TryTransferResources(this.Context, transferResource.ResourceName, (float)transferResource.Amount);
				}
			}
		}
	}

	public void OnReactivation()
	{
		BoosterDefinition.Type boosterType = this.BoosterDefinition.BoosterType;
		if (boosterType != BoosterDefinition.Type.DelayingTime)
		{
			if (boosterType == BoosterDefinition.Type.ResettingTime)
			{
				this.Deactivate();
				this.Activate();
			}
		}
		else
		{
			this.RemainingTime += this.Duration;
		}
	}

	public void ApplyClassTimedDescriptor()
	{
		SimulationDescriptor descriptor;
		if (this.BoosterDefinition.BoosterType != BoosterDefinition.Type.Instant && this.simulationDescriptorsDatatable.TryGetValue("ClassTimedBonus", out descriptor))
		{
			base.AddDescriptor(descriptor, false);
		}
	}

	public void ApplyDescriptors(bool applyDeportedDescriptors = true)
	{
		if (this.BoosterDefinition.SimulationDescriptorReferences != null && this.BoosterDefinition.SimulationDescriptorReferences.Length > 0)
		{
			for (int i = 0; i < this.BoosterDefinition.SimulationDescriptorReferences.Length; i++)
			{
				string text = this.BoosterDefinition.SimulationDescriptorReferences[i];
				if (!string.IsNullOrEmpty(text))
				{
					SimulationDescriptor descriptor;
					if (this.simulationDescriptorsDatatable.TryGetValue(text, out descriptor))
					{
						base.AddDescriptor(descriptor, false);
					}
				}
			}
		}
		if (applyDeportedDescriptors && this.BoosterDefinition.DeportedSimulationDescriptors != null)
		{
			this.ApplyDeportedDescriptor(this.BoosterDefinition.DeportedSimulationDescriptors);
		}
	}

	public void RemoveDescriptors()
	{
		if (this.BoosterDefinition.SimulationDescriptorReferences != null && this.BoosterDefinition.SimulationDescriptorReferences.Length > 0)
		{
			for (int i = 0; i < this.BoosterDefinition.SimulationDescriptorReferences.Length; i++)
			{
				string text = this.BoosterDefinition.SimulationDescriptorReferences[i];
				if (!string.IsNullOrEmpty(text))
				{
					SimulationDescriptor descriptor;
					if (this.simulationDescriptorsDatatable.TryGetValue(text, out descriptor))
					{
						base.RemoveDescriptor(descriptor);
					}
				}
			}
		}
		if (this.BoosterDefinition.DeportedSimulationDescriptors != null)
		{
			this.RemoveDeportedDescriptor(this.BoosterDefinition.DeportedSimulationDescriptors);
		}
	}

	protected static bool CheckActivationPrerequisites(BoosterDefinition boosterDefinition, SimulationObjectWrapper context)
	{
		if (boosterDefinition.Prerequisites == null || boosterDefinition.Prerequisites.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < boosterDefinition.Prerequisites.Length; i++)
		{
			if (!boosterDefinition.Prerequisites[i].Check(context))
			{
				return false;
			}
		}
		return true;
	}

	protected static bool CanApplyAtLeastOneEffect(BoosterDefinition boosterDefinition, SimulationObjectWrapper context)
	{
		if (boosterDefinition.Effects == null)
		{
			return false;
		}
		for (int i = 0; i < boosterDefinition.Effects.Length; i++)
		{
			if (Booster.CheckPrerequisites(boosterDefinition.Effects[i], context))
			{
				return true;
			}
		}
		return false;
	}

	protected void Deactivate()
	{
		this.TurnWhenStarted = -1;
		this.RemoveDescriptors();
		this.RemoveEffects();
		this.Context.RemoveChild(this);
		this.Context.Refresh(false);
	}

	private IDatabase<SimulationDescriptor> simulationDescriptorsDatatable;

	private global::Game game;

	private global::Empire empire;
}
