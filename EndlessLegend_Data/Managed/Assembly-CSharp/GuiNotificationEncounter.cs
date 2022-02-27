using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;

public class GuiNotificationEncounter : global::GuiNotification
{
	public GuiNotificationEncounter()
	{
		base.Priority = Amplitude.Unity.Gui.GuiNotification.GuiNotificationPriority.Urgent;
		this.autoshow = Services.GetService<IGuiNotificationSettingsService>().AutoPopupNotificationEncounter;
		base.GuiNotificationPanelType = typeof(NotificationPanelEncounterSetup);
		this.guiElementName = EventEncounterStateChange.Name;
		this.Encounter = null;
		this.EncounterGUID = GameEntityGUID.Zero;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null && service.Game != null);
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(service2 != null);
		this.activePlayerController = service2.ActivePlayerController;
		Diagnostics.Assert(this.activePlayerController != null && this.activePlayerController.Empire != null);
		this.notificationItem = null;
	}

	public Encounter Encounter { get; protected set; }

	public GameEntityGUID EncounterGUID { get; protected set; }

	public List<GuiGarrison> AlliedGuiGarrisons { get; private set; }

	public List<GuiGarrison> EnemyGuiGarrisons { get; private set; }

	private protected NotificationItem NotificationItem
	{
		protected get
		{
			if (this.notificationItem == null)
			{
				global::IGuiService service = Services.GetService<global::IGuiService>();
				Amplitude.Unity.Gui.GuiPanel[] array;
				if (service.TryGetGuiPanelByType(typeof(NotificationListPanel), out array) && array != null)
				{
					NotificationListPanel notificationListPanel = array.First<Amplitude.Unity.Gui.GuiPanel>() as NotificationListPanel;
					if (notificationListPanel != null)
					{
						AgeTransform ageTransform = notificationListPanel.NotificationTable.GetChildren().FirstOrDefault((AgeTransform match) => match.GetComponent<NotificationItem>().GuiNotification == this);
						if (ageTransform != null)
						{
							this.notificationItem = ageTransform.GetComponent<NotificationItem>();
						}
					}
				}
			}
			return this.notificationItem;
		}
		private set
		{
			this.notificationItem = value;
		}
	}

	public override bool CanDismiss()
	{
		return this.Encounter != null && this.Encounter.EncounterState == EncounterState.BattleHasEnded;
	}

	public override bool CanProcessEndTurn()
	{
		return this.CanDismiss();
	}

	public override string GetGuiElementName()
	{
		return this.guiElementName;
	}

	public override void OnItemClick(object itemData)
	{
		if (this.Encounter.EncounterState == EncounterState.Setup || this.Encounter.EncounterState == EncounterState.BattleHasEnded)
		{
			base.OnItemClick(itemData);
		}
		else
		{
			IViewService service = Services.GetService<IViewService>();
			if (service != null)
			{
				service.SelectAndCenter(this.Encounter);
			}
		}
	}

	public override string FormatDescription(string baseFormat, Amplitude.Unity.Gui.IGuiService guiService)
	{
		Diagnostics.Assert(this.EnemyGuiGarrisons.Count > 0);
		return string.Format(AgeLocalizer.Instance.LocalizeString(baseFormat), this.EnemyGuiGarrisons[0].GetLocalizedNameAndFaction(this.playerEmpire, true));
	}

	public override bool LoadEvent(Event notification)
	{
		if (notification is EventEncounterStateChange)
		{
			EventEncounterStateChange eventEncounterStateChange = notification as EventEncounterStateChange;
			if (eventEncounterStateChange == null || eventEncounterStateChange.EventArgs.Encounter == null)
			{
				return false;
			}
			if (eventEncounterStateChange.EventArgs.EncounterState == EncounterState.Setup && eventEncounterStateChange.EventArgs.Encounter.Instant)
			{
				return false;
			}
			if (eventEncounterStateChange.EventArgs.EncounterState == EncounterState.Deployment && eventEncounterStateChange.EventArgs.Encounter.Instant)
			{
				return false;
			}
			if (eventEncounterStateChange.EventArgs.EncounterState == EncounterState.Setup)
			{
				base.GuiNotificationPanelType = typeof(NotificationPanelEncounterSetup);
				this.playerEmpire = (eventEncounterStateChange.Empire as global::Empire);
				this.guiElementName = EventEncounterStateChange.Name + eventEncounterStateChange.EventArgs.EncounterState.ToString();
				this.Encounter = eventEncounterStateChange.EventArgs.Encounter;
				this.EncounterGUID = this.Encounter.GUID;
				this.Encounter.ContenderCollectionChange += this.Encounter_ContenderCollectionChange;
				this.Encounter.EncounterStateChange += this.Encounter_EncounterStateChange;
				this.Encounter.ContenderStateChange += this.Encounter_ContenderStateChange;
				this.Encounter.EncounterDisposed += this.Encounter_EncounterDisposed;
				this.BuildGuiGarrisons();
				return base.LoadEvent(notification);
			}
			if (eventEncounterStateChange.EventArgs.EncounterState == EncounterState.BattleHasEnded && (eventEncounterStateChange.EventArgs.Encounter.OrderCreateEncounter.Instant || eventEncounterStateChange.EventArgs.Encounter.Instant))
			{
				base.GuiNotificationPanelType = typeof(NotificationPanelEncounterEnded);
				this.playerEmpire = (eventEncounterStateChange.Empire as global::Empire);
				this.guiElementName = EventEncounterStateChange.Name + eventEncounterStateChange.EventArgs.EncounterState.ToString();
				this.Encounter = eventEncounterStateChange.EventArgs.Encounter;
				this.EncounterGUID = this.Encounter.GUID;
				this.Encounter.ContenderCollectionChange += this.Encounter_ContenderCollectionChange;
				this.Encounter.EncounterStateChange += this.Encounter_EncounterStateChange;
				this.Encounter.ContenderStateChange += this.Encounter_ContenderStateChange;
				this.Encounter.EncounterDisposed += this.Encounter_EncounterDisposed;
				this.BuildGuiGarrisons();
				return base.LoadEvent(notification);
			}
		}
		return false;
	}

	public override void ReleaseEvent()
	{
		base.ReleaseEvent();
		this.AlliedGuiGarrisons = null;
		this.EnemyGuiGarrisons = null;
		this.Encounter.ContenderCollectionChange -= this.Encounter_ContenderCollectionChange;
		this.Encounter.EncounterStateChange -= this.Encounter_EncounterStateChange;
		this.Encounter.ContenderStateChange -= this.Encounter_ContenderStateChange;
		this.Encounter.EncounterDisposed -= this.Encounter_EncounterDisposed;
		this.Encounter = null;
		this.EncounterGUID = GameEntityGUID.Zero;
	}

	private void Encounter_ContenderCollectionChange(object sender, ContenderCollectionChangeEventArgs e)
	{
		if (e.Action == ContenderCollectionChangeAction.Add)
		{
			GuiGarrison item = new GuiGarrison(e.Contender);
			if (e.Contender.Group == this.AlliedGuiGarrisons[0].Group)
			{
				this.AlliedGuiGarrisons.Add(item);
			}
			else
			{
				this.EnemyGuiGarrisons.Add(item);
			}
		}
		else if (e.Action == ContenderCollectionChangeAction.Remove)
		{
			if (e.Contender.Group == this.AlliedGuiGarrisons[0].Group)
			{
				this.AlliedGuiGarrisons.RemoveAll((GuiGarrison guiGarrison) => guiGarrison.GUID == e.Contender.GUID);
			}
			else
			{
				this.EnemyGuiGarrisons.RemoveAll((GuiGarrison guiGarrison) => guiGarrison.GUID == e.Contender.GUID);
			}
		}
		else if (e.Action == ContenderCollectionChangeAction.Clear)
		{
			this.AlliedGuiGarrisons.Clear();
			this.EnemyGuiGarrisons.Clear();
		}
	}

	private void Encounter_ContenderStateChange(object sender, ContenderStateChangeEventArgs e)
	{
	}

	private void Encounter_EncounterStateChange(object sender, EncounterStateChangeEventArgs e)
	{
		if (e.EncounterState == EncounterState.BattleHasEnded)
		{
			NotificationPanelEncounterBase notificationPanelEncounter = this.GetNotificationPanelEncounter();
			if (notificationPanelEncounter != null && notificationPanelEncounter.IsVisible)
			{
				notificationPanelEncounter.Hide(true);
			}
			this.notificationItem = null;
			this.BuildGuiGarrisons();
			this.guiElementName = EventEncounterStateChange.Name + e.EncounterState.ToString();
			base.GuiNotificationPanelType = typeof(NotificationPanelEncounterEnded);
			NotificationPanelEncounterBase notificationPanelEncounter2 = this.GetNotificationPanelEncounter();
			if (notificationPanelEncounter2 != null && this.NotificationItem != null)
			{
				notificationPanelEncounter2.Show(new object[]
				{
					this,
					this.NotificationItem
				});
			}
		}
		if (base.GuiService.GetGuiPanel<NotificationListPanel>() != null)
		{
			base.GuiService.GetGuiPanel<NotificationListPanel>().RefreshContent();
		}
	}

	private void Encounter_EncounterDisposed(object sender, EventArgs e)
	{
		base.GuiNotificationService.DestroyNotification(this);
	}

	private NotificationPanelEncounterBase GetNotificationPanelEncounter()
	{
		global::IGuiService service = Services.GetService<global::IGuiService>();
		Amplitude.Unity.Gui.GuiPanel[] array;
		if (service.TryGetGuiPanelByType(base.GuiNotificationPanelType, out array) && array != null)
		{
			return array[0] as NotificationPanelEncounterBase;
		}
		return null;
	}

	private void BuildGuiGarrisons()
	{
		if (this.Encounter != null && this.Encounter.Contenders != null)
		{
			this.AlliedGuiGarrisons = new List<GuiGarrison>();
			this.EnemyGuiGarrisons = new List<GuiGarrison>();
			IEnumerable<Contender> alliedContendersFromEmpire = this.Encounter.GetAlliedContendersFromEmpire(this.playerEmpire);
			if (alliedContendersFromEmpire != null)
			{
				foreach (Contender contender in alliedContendersFromEmpire)
				{
					this.AlliedGuiGarrisons.Add(new GuiGarrison(contender));
				}
			}
			IEnumerable<Contender> enemiesContenderFromEmpire = this.Encounter.GetEnemiesContenderFromEmpire(this.playerEmpire);
			if (enemiesContenderFromEmpire != null)
			{
				foreach (Contender contender2 in enemiesContenderFromEmpire)
				{
					this.EnemyGuiGarrisons.Add(new GuiGarrison(contender2));
				}
			}
		}
	}

	protected string guiElementName;

	protected global::Empire playerEmpire;

	protected global::PlayerController activePlayerController;

	protected NotificationItem notificationItem;
}
