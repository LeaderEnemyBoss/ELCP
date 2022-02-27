using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class Alteration_UnitStatChange : Alteration
{
	public override StaticString Name
	{
		get
		{
			return this.name;
		}
	}

	protected WorldBattleUnit WorldBattleUnit { get; set; }

	public override Alteration.Response HandleEvent(StaticString eventName, params object[] parameters)
	{
		if (eventName == Alteration.Events.BattleEnd)
		{
			this.HideHealthStatus();
		}
		else if (eventName == Alteration.Events.RoundStart || eventName == Alteration.Events.TargetingPhaseEnd || eventName == Alteration.Events.DeploymentEnd)
		{
			if (!this.WorldBattleUnit.EncounterUnit.IsOnBattlefield)
			{
				return Alteration.Response.Pass;
			}
			if (this.isHealthStatusDisplayed)
			{
				return Alteration.Response.Pass;
			}
			this.battleHealthStatus.SetMaxHealth(this.WorldBattleUnit.EncounterUnit.GetPropertyValue(SimulationProperties.MaximumHealth));
			this.battleHealthStatus.SetCurrentHealth(this.WorldBattleUnit.EncounterUnit.GetPropertyValue(SimulationProperties.Health));
			this.battleHealthStatus.SetCurrentAttackingHitInfo(this.WorldBattleUnit.EncounterUnit.GetPropertyValue(SimulationProperties.AttackingHitInfo));
			this.battleHealthStatus.SetCurrentMoral(this.WorldBattleUnit.EncounterUnit.GetPropertyValue(SimulationProperties.BattleMorale));
			this.battleHealthStatus.SetMaxArmor(this.WorldBattleUnit.EncounterUnit.GetPropertyValue(SimulationProperties.MaximumArmor));
			this.battleHealthStatus.SetCurrentArmor(this.WorldBattleUnit.EncounterUnit.GetPropertyValue(SimulationProperties.Armor));
			if (this.WorldBattleUnit.Unit.Garrison.Empire != null)
			{
				Color factionColor = this.WorldBattleUnit.Unit.Garrison.Empire.Color;
				IGameService service = Services.GetService<IGameService>();
				IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
				if (service2.ActivePlayerController != null && service2.ActivePlayerController.Empire != null)
				{
					if (service2.ActivePlayerController.Empire.Index != this.WorldBattleUnit.Unit.Garrison.Empire.Index)
					{
						Army army = this.WorldBattleUnit.Unit.Garrison as Army;
						if (army != null && army.IsPrivateers)
						{
							factionColor = global::Game.PrivateersColor;
						}
					}
					else
					{
						this.battleHealthStatus.AddRemoveAlteration(this.WorldBattleUnit.EncounterUnit.Strategy, true, -1f, true);
						this.isStrategyDisplayed = true;
					}
				}
				this.battleHealthStatus.SetFactionColor(factionColor);
			}
			if (!this.WorldBattleUnit.Dead)
			{
				this.ShowHealthStatus();
			}
		}
		else if (eventName == Alteration.Events.UnitDead)
		{
			this.HideHealthStatus();
		}
		else if (eventName == Alteration.Events.UnitRevived)
		{
			this.ShowHealthStatus();
		}
		else if (eventName == Alteration.Events.AlterationUnitStatChange)
		{
			if (parameters == null || parameters.Length < 4)
			{
				return Alteration.Response.Pass;
			}
			StaticString x = (StaticString)parameters[0];
			float num = (float)parameters[1];
			if (Mathf.Abs(num) <= 1.401298E-45f)
			{
			}
			bool critical = (bool)parameters[3];
			if (x == SimulationProperties.Health)
			{
				this.battleHealthStatus.ChangeHealth(num, critical);
			}
			else if (x == SimulationProperties.Armor)
			{
				this.battleHealthStatus.ChangeArmor(num, critical);
			}
			else if (x == SimulationProperties.AttackingHitInfo)
			{
				this.battleHealthStatus.ChangeAttackingHitInfo(num);
			}
			else if (x == SimulationProperties.BattleMorale)
			{
				this.battleHealthStatus.ChangeMoral(num, critical);
			}
		}
		else if (eventName == Alteration_UnitStatChange.EventAltitude)
		{
			if (parameters == null || parameters.Length < 1)
			{
				return Alteration.Response.Pass;
			}
			if (!this.unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
			{
				bool flag = (bool)parameters[0];
				float altitudeDifference = 0f;
				if (flag)
				{
					altitudeDifference = (float)parameters[1];
				}
				this.DisplayAltitudeBonus(flag, altitudeDifference);
			}
		}
		else if (eventName == Alteration.Events.AlterationStatusFeedback)
		{
			if (parameters.Length < 1)
			{
				return Alteration.Response.Pass;
			}
			bool add = true;
			float duration = -1f;
			IUnitReportInstruction unitReportInstruction = null;
			string alterationName = (string)parameters[0];
			if (parameters.Length >= 2)
			{
				if (parameters[1] is bool)
				{
					add = (bool)parameters[1];
					if (parameters.Length >= 3)
					{
						if (parameters[2] is float)
						{
							duration = (float)parameters[2];
							if (parameters.Length >= 4 && parameters[3] is IUnitReportInstruction)
							{
								unitReportInstruction = (IUnitReportInstruction)parameters[3];
							}
						}
						else if (parameters[2] is IUnitReportInstruction)
						{
							unitReportInstruction = (IUnitReportInstruction)parameters[2];
						}
					}
				}
				else if (parameters[1] is float)
				{
					duration = (float)parameters[1];
					if (parameters.Length >= 3 && parameters[2] is IUnitReportInstruction)
					{
						unitReportInstruction = (IUnitReportInstruction)parameters[2];
					}
				}
				else if (parameters[1] is IUnitReportInstruction)
				{
					unitReportInstruction = (IUnitReportInstruction)parameters[1];
				}
			}
			bool isCumulable = true;
			if (unitReportInstruction != null && unitReportInstruction is BattleEffectUpdateInstruction)
			{
				BattleEffectUpdateInstruction battleEffectUpdateInstruction = unitReportInstruction as BattleEffectUpdateInstruction;
				isCumulable = battleEffectUpdateInstruction.IsCumulable;
			}
			this.battleHealthStatus.AddRemoveAlteration(alterationName, add, duration, isCumulable);
		}
		else if (eventName == Alteration.Events.AlterationUnitMove)
		{
			if (parameters.Length < 1)
			{
				return Alteration.Response.Pass;
			}
			if ((int)parameters[0] == 0)
			{
			}
		}
		else if (eventName == Alteration.Events.UnitStrategyChanged)
		{
			if (!this.isStrategyDisplayed)
			{
				return Alteration.Response.Pass;
			}
			if (parameters.Length < 2)
			{
				return Alteration.Response.Pass;
			}
			StaticString x2 = (StaticString)parameters[0];
			StaticString staticString = (StaticString)parameters[1];
			if (x2 == staticString)
			{
				return Alteration.Response.Pass;
			}
			if (x2 != null)
			{
				this.battleHealthStatus.AddRemoveAlteration(x2, false, -1f, true);
			}
			if (staticString != null)
			{
				this.battleHealthStatus.AddRemoveAlteration(staticString, true, -1f, true);
			}
		}
		else if (eventName == Alteration.Events.UnitPotentialTarget)
		{
			if (parameters.Length < 3)
			{
				return Alteration.Response.Pass;
			}
			bool add2 = (bool)parameters[0];
			UnitAvailableTarget.TargetAccessibilityType targetAccessibilityType = (UnitAvailableTarget.TargetAccessibilityType)((int)parameters[1]);
			bool flag2 = (bool)parameters[2];
			string alterationName2 = "AvailableTarget" + targetAccessibilityType.ToString() + ((!flag2) ? string.Empty : "Ally");
			this.battleHealthStatus.AddRemoveAlteration(alterationName2, add2, -1f, true);
		}
		else if (eventName == Alteration.Events.UnitPotentialOpportunityTarget)
		{
			if (parameters.Length < 2)
			{
				return Alteration.Response.Pass;
			}
			bool add3 = (bool)parameters[0];
			bool flag3 = (bool)parameters[1];
			string alterationName3 = "AvailableOpportunityTarget" + ((!flag3) ? string.Empty : "Ally");
			this.battleHealthStatus.AddRemoveAlteration(alterationName3, add3, -1f, true);
		}
		return Alteration.Response.Pass;
	}

	public override void OnAlterationAdded()
	{
		base.OnAlterationAdded();
		this.unit = null;
		this.WorldBattleUnit = base.gameObject.GetComponent<WorldBattleUnit>();
		if (this.WorldBattleUnit != null && this.positionService != null)
		{
			this.unit = this.WorldBattleUnit.Unit;
		}
		UnityEngine.Object @object = Resources.Load(Alteration_UnitStatChange.HealthStatusPrefabName);
		if (@object != null)
		{
			this.battleHealthStatusGameObject = (UnityEngine.Object.Instantiate(@object) as GameObject);
			this.battleHealthStatusGameObject.transform.parent = this.WorldBattleUnit.transform;
			this.battleHealthStatus = this.battleHealthStatusGameObject.transform.GetComponent<BattleHealthStatus>();
			if (this.battleHealthStatus == null)
			{
				Diagnostics.LogError("In the prefab {0} there is no component of type {1}.", new object[]
				{
					Alteration_UnitStatChange.HealthStatusPrefabName,
					"BattleHealthStatus"
				});
			}
		}
		else
		{
			Diagnostics.LogError("There is no prefab named {0} in the solution.", new object[]
			{
				Alteration_UnitStatChange.HealthStatusPrefabName
			});
		}
	}

	public virtual void OnEnable()
	{
		this.isHealthStatusDisplayed = false;
	}

	public void Update()
	{
		this.UpdatePosition();
	}

	protected override void Awake()
	{
		base.Awake();
	}

	protected void UpdatePosition()
	{
		if (this.battleHealthStatus != null)
		{
			this.battleHealthStatus.SetPosition(this.WorldBattleUnit.PawnBarycenterDummy.transform.position);
		}
	}

	private void DisplayAltitudeBonus(bool visible, float altitudeDifference)
	{
		if (visible)
		{
			int num = 0;
			if (Mathf.Abs(altitudeDifference) > 0.0001f)
			{
				num = ((altitudeDifference <= 0f) ? -1 : 1);
			}
			if (num != this.previousAltitudeDifference)
			{
				if (this.previousAltitudeDifference != 0)
				{
					this.battleHealthStatus.DisplayAltitudeBonus(false, 0f);
				}
				this.previousAltitudeDifference = num;
				this.battleHealthStatus.DisplayAltitudeBonus(true, (float)num);
			}
		}
		else if (this.previousAltitudeDifference != 0)
		{
			this.previousAltitudeDifference = 0;
			this.battleHealthStatus.DisplayAltitudeBonus(false, 0f);
		}
	}

	private void HideHealthStatus()
	{
		this.isHealthStatusDisplayed = false;
		this.battleHealthStatus.HideWithAnimation();
	}

	private void ShowHealthStatus()
	{
		this.isHealthStatusDisplayed = true;
		this.battleHealthStatus.ShowWithAnimation();
	}

	public static StaticString EventAltitude = new StaticString("Alteration_UnitStatChange.Altitude");

	public static string HealthStatusPrefabName = "Prefabs/Armies/WorldUnit_HealthStatus";

	protected Unit unit;

	private new readonly StaticString name = new StaticString("Alteration_UnitStatChange");

	private GameObject battleHealthStatusGameObject;

	private BattleHealthStatus battleHealthStatus;

	private int previousAltitudeDifference;

	private bool isHealthStatusDisplayed;

	private bool isStrategyDisplayed;
}
