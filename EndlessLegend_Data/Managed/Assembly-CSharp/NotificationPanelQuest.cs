using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class NotificationPanelQuest : NotificationPanelBase
{
	private Quest Quest { get; set; }

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (this.guiNotification != null && this.notificationItem != null && this.guiNotification.RaisedEvent is GameEvent)
		{
			base.SetHeader();
			this.hiddenQuest = this.Quest.QuestDefinition.Tags.Contains(QuestDefinition.TagHidden);
			this.ContentColorSwitch.AgeTransform.Visible = !this.hiddenQuest;
			QuestItem.DisplayQuestType(base.GuiService.GuiPanelHelper, this.Quest, this.empire, false, null, this.QuestTypeIcon);
			GuiElement guiElement;
			if (base.GuiService.GuiPanelHelper.TryGetGuiElement(this.Quest.QuestDefinition.Name, out guiElement))
			{
				this.Title.Text = this.Title.Text + " : " + AgeLocalizer.Instance.LocalizeString(guiElement.Title);
				Texture2D image;
				if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
				{
					this.ImmersiveImage.Image = image;
				}
				this.questGuiElement = (guiElement as QuestGuiElement);
				Diagnostics.Assert(this.questGuiElement != null);
			}
			else
			{
				Diagnostics.LogWarning("Could not find GuiElement for quest: {0}", new object[]
				{
					this.Quest.QuestDefinition.Name
				});
				this.Title.Text = this.Title.Text + " : " + this.Quest.QuestDefinition.Name;
				this.ImmersiveImage.Image = null;
				this.questGuiElement = null;
			}
			if (this.guiNotification is GuiNotificationQuestBegun)
			{
				this.DisplayQuestBegun();
			}
			else if (this.guiNotification is GuiNotificationQuestStepChanged)
			{
				this.DisplayQuestStepChanged();
			}
			else if (this.guiNotification is GuiNotificationQuestComplete)
			{
				this.DisplayQuestComplete();
			}
			else if (this.guiNotification is GuiNotificationQuestFailed)
			{
				this.DisplayQuestFailed();
			}
			this.UpdateShowLocationButton();
		}
	}

	protected override void DefineAutoPopupStatus()
	{
		base.DefineAutoPopupStatus();
		this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationQuest;
	}

	protected override void OnSwitchAutoPopupCB(GameObject obj)
	{
		this.guiNotificationSettingsService.AutoPopupNotificationQuest = this.autoPopupToggle.State;
		base.DefineAutoPopupStatus();
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		IEventService eventService = Services.GetService<IEventService>();
		if (eventService != null)
		{
			eventService.EventRaise += this.EventService_EventRaise;
		}
		this.guiNotificationQuest = (this.guiNotification as GuiNotificationQuest);
		if (this.guiNotificationQuest == null)
		{
			Diagnostics.LogError("Missing GuiNotificationQuest");
			this.Hide(true);
		}
		this.Quest = this.guiNotificationQuest.Quest;
		if (this.Quest == null)
		{
			Diagnostics.LogError("Missing Quest");
			this.Hide(true);
		}
		GameEvent gameEvent = this.guiNotification.RaisedEvent as GameEvent;
		this.empire = (gameEvent.Empire as global::Empire);
		if (this.empire == null)
		{
			Diagnostics.LogError("Missing Empire");
			this.Hide(true);
		}
		this.departmentOfForeignAffairs = this.empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		if (this.Quest.QuestDefinition.Steps == null || this.Quest.QuestDefinition.Steps.Length == 0)
		{
			Diagnostics.LogError("No step found in the quest {0}", new object[]
			{
				this.Quest.QuestDefinition.Name
			});
			this.Hide(true);
		}
		this.RefreshContent();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.LeftSummary.Hide();
		this.RightSummary.Hide();
		this.LeftStepItem.Hide();
		this.RightStepItem.Hide();
		this.game = null;
		this.empire = null;
		this.Quest = null;
		this.guiNotificationQuest = null;
		IEventService eventService = Services.GetService<IEventService>();
		if (eventService != null)
		{
			eventService.EventRaise -= this.EventService_EventRaise;
		}
		yield return base.OnHide(instant);
		yield break;
	}

	private void DisplayQuestBegun()
	{
		this.WriteFullDescription();
		if (this.questGuiElement != null)
		{
			this.LeftSummary.Show(this.Quest, "%QuestSummaryTitle", this.questGuiElement.Objective);
		}
		else
		{
			this.LeftSummary.Show(this.Quest, "%QuestSummaryTitle", this.Quest.QuestDefinition.Name);
		}
		this.LeftStepItem.Hide();
		this.RightTitleLabel.Text = "%NotificationQuestNewObjectiveTitle";
		this.RightSummary.Hide();
		this.RightStepItem.Show(this.Quest, this.Quest.QuestDefinition.Steps[0], 0, this.empire);
	}

	private void DisplayQuestStepChanged()
	{
		this.WriteSummaryDescription();
		GuiNotificationQuestStepChanged guiNotificationQuestStepChanged = this.guiNotification as GuiNotificationQuestStepChanged;
		Diagnostics.Assert(guiNotificationQuestStepChanged != null);
		int lastCompletedStep = guiNotificationQuestStepChanged.LastCompletedStep;
		int newStep = guiNotificationQuestStepChanged.NewStep;
		QuestStep step = this.Quest.QuestDefinition.Steps[lastCompletedStep];
		QuestStep step2 = this.Quest.QuestDefinition.Steps[newStep];
		if (this.Quest.StepStates[lastCompletedStep] == QuestState.Completed)
		{
			this.LeftTitleLabel.Text = "%NotificationQuestCompletedObjectiveTitle";
		}
		else
		{
			this.LeftTitleLabel.Text = "%NotificationQuestFailedObjectiveTitle";
		}
		this.LeftSummary.Hide();
		this.LeftStepItem.Show(this.Quest, step, lastCompletedStep, this.empire);
		this.RightTitleLabel.Text = "%NotificationQuestNewObjectiveTitle";
		this.RightSummary.Hide();
		this.RightStepItem.Show(this.Quest, step2, newStep, this.empire);
	}

	private void DisplayQuestComplete()
	{
		this.WriteSummaryDescription();
		int num = this.Quest.QuestDefinition.Steps.Length - 1;
		QuestStep step = this.Quest.QuestDefinition.Steps[num];
		this.LeftTitleLabel.Text = "%NotificationQuestCompletedObjectiveTitle";
		this.LeftSummary.Hide();
		this.LeftStepItem.Show(this.Quest, step, num, this.empire);
		if (this.questGuiElement != null)
		{
			this.RightSummary.Show(this.Quest, "%QuestOutcomeTitle", this.questGuiElement.Outcome);
		}
		else
		{
			this.RightSummary.Show(this.Quest, "%QuestSummaryTitle", this.Quest.QuestDefinition.Name);
		}
		this.RightStepItem.Hide();
	}

	private void DisplayQuestFailed()
	{
		this.WriteSummaryDescription();
		int failedStepRank = (this.guiNotification as GuiNotificationQuestFailed).FailedStepRank;
		QuestStep step;
		if (failedStepRank >= 0 && failedStepRank < this.Quest.QuestDefinition.Steps.Length)
		{
			step = this.Quest.QuestDefinition.Steps[failedStepRank];
		}
		else
		{
			step = null;
		}
		Diagnostics.Assert(failedStepRank != -1, "There wasn't any step in progress when the quest failed");
		this.LeftTitleLabel.Text = "%NotificationQuestFailedObjectiveTitle";
		this.LeftSummary.Hide();
		this.LeftStepItem.Show(this.Quest, step, failedStepRank, this.empire);
		string arg = this.Quest.QuestDefinition.Name;
		if (this.questGuiElement != null)
		{
			arg = AgeLocalizer.Instance.LocalizeString(this.questGuiElement.Title);
		}
		string text = string.Empty;
		bool flag = false;
		if (this.Quest.QuestDefinition.IsGlobal && this.Quest.QuestDefinition.GlobalWinner == GlobalQuestWinner.First && this.Quest.QuestState == QuestState.Failed)
		{
			flag = true;
			QuestRegisterVariable questRegisterVariable = null;
			if (this.Quest.QuestVariables.TryGetValue(QuestDefinition.WinnerVariableName, out questRegisterVariable) && questRegisterVariable != null)
			{
				if (this.game == null)
				{
					IGameService service = Services.GetService<IGameService>();
					Diagnostics.Assert(service != null && service.Game != null && service.Game is global::Game);
					this.game = (service.Game as global::Game);
				}
				int value = questRegisterVariable.Value;
				if (this.departmentOfForeignAffairs.GetDiplomaticRelation(this.game.Empires[value]).State.Name == DiplomaticRelationState.Names.Unknown)
				{
					text = AgeLocalizer.Instance.LocalizeString("%UnknownEmpire");
				}
				else
				{
					GuiEmpire guiEmpire = new GuiEmpire(this.game.Empires[value]);
					text = guiEmpire.GetColorizedLocalizedNameAndFaction(this.empire, false);
				}
			}
		}
		string text2;
		if (flag)
		{
			text2 = string.Format(AgeLocalizer.Instance.LocalizeString("%NotificationQuestFailedCompetitiveDescription"), arg);
			if (!string.IsNullOrEmpty(text.Trim()))
			{
				text2 = text2 + "\n" + string.Format(AgeLocalizer.Instance.LocalizeString("%NotificationQuestFailedCompetitiveWinnerDescription"), text);
			}
		}
		else
		{
			text2 = string.Format(AgeLocalizer.Instance.LocalizeString("%NotificationQuestFailedDescription"), arg);
		}
		this.RightSummary.Show(this.Quest, "%QuestOutcomeTitle", text2);
		this.RightStepItem.Hide();
	}

	private void WriteFullDescription()
	{
		this.QuestDescriptionScrollView.ResetUp();
		if (this.questGuiElement == null)
		{
			this.QuestDescription.Text = this.Quest.QuestDefinition.Name;
			return;
		}
		if (this.QuestDescription.Text != this.questGuiElement.Description)
		{
			this.QuestDescription.AgeTransform.Height = 0f;
			this.QuestDescription.Text = QuestStepItem.ComputeLocalizedQuestText(this.questGuiElement.Description, this.Quest.LocalizationVariables);
			if (NotificationPanelBase.FirstOpening)
			{
				this.temp.Length = 0;
				AgeUtils.CleanLine(this.QuestDescription.Text, ref this.temp);
				AgeModifierTypewriter component = this.QuestDescription.GetComponent<AgeModifierTypewriter>();
				component.Duration = 0.02f * (float)this.temp.Length;
				component.StartAnimation();
			}
			else
			{
				AgeModifierTypewriter component2 = this.QuestDescription.GetComponent<AgeModifierTypewriter>();
				component2.Reset();
				this.QuestDescription.CurrentLine = -1;
				this.QuestDescription.CurrentCharInLine = -1;
			}
		}
	}

	private void WriteSummaryDescription()
	{
		this.QuestDescriptionScrollView.ResetUp();
		if (this.questGuiElement == null)
		{
			this.QuestDescription.Text = this.Quest.QuestDefinition.Name;
			return;
		}
		string text = QuestStepItem.ComputeLocalizedQuestText(this.questGuiElement.Objective, this.Quest.LocalizationVariables);
		if (this.QuestDescription.Text != text)
		{
			this.QuestDescription.AgeTransform.Height = 0f;
			this.QuestDescription.Text = text;
		}
	}

	private void UpdateShowLocationButton()
	{
		IQuestManagementService service = base.Game.Services.GetService<IQuestManagementService>();
		Diagnostics.Assert(service != null);
		List<QuestMarker> list = service.GetMarkersByQuestGUID(this.Quest.GUID).ToList<QuestMarker>();
		list.RemoveAll((QuestMarker match) => !match.IsVisibleFor(this.empire));
		this.questLocations = list;
		this.ShowLocationButton.AgeTransform.Visible = (list.Count > 0);
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		EventQuestUpdated eventQuestUpdated = e.RaisedEvent as EventQuestUpdated;
		if (eventQuestUpdated != null && eventQuestUpdated.Quest.GUID == this.Quest.GUID)
		{
			this.RefreshContent();
		}
	}

	private void OnContentClickCB(GameObject obj)
	{
		if (!this.hiddenQuest)
		{
			base.GuiService.Show(typeof(GameQuestScreen), new object[]
			{
				(this.guiNotification as GuiNotificationQuest).Quest
			});
			this.Hide(false);
		}
	}

	private void OnShowLocationCB(GameObject obj)
	{
		if (this.questLocations != null)
		{
			IViewService service = Services.GetService<IViewService>();
			if (this.locatorIndex >= this.questLocations.Count<QuestMarker>())
			{
				this.locatorIndex = 0;
			}
			service.SelectAndCenter(this.questLocations.ElementAt(this.locatorIndex).WorldPosition);
			this.locatorIndex++;
			this.Hide(false);
		}
	}

	public const float CharAppearanceTime = 0.02f;

	public AgePrimitiveImage QuestTypeIcon;

	public AgePrimitiveLabel QuestDescription;

	public AgeControlScrollView QuestDescriptionScrollView;

	public AgePrimitiveLabel LeftTitleLabel;

	public QuestSummary LeftSummary;

	public QuestStepItem LeftStepItem;

	public AgePrimitiveLabel RightTitleLabel;

	public QuestSummary RightSummary;

	public QuestStepItem RightStepItem;

	public AgeControlButton ShowLocationButton;

	public AgeModifierColorSwitch ContentColorSwitch;

	protected IEnumerable<QuestMarker> questLocations;

	private global::Empire empire;

	private GuiNotificationQuest guiNotificationQuest;

	private QuestGuiElement questGuiElement;

	private global::Game game;

	private bool hiddenQuest;

	private StringBuilder temp = new StringBuilder();

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private int locatorIndex;
}
