using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using UnityEngine;

public class GameEndedModalPanel : global::GuiModalPanel
{
	public GuiNotificationGameEnded GuiNotificationGameEnded { get; private set; }

	public override void RefreshContent()
	{
		base.RefreshContent();
		IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(service != null);
		this.playerGuiEmpire = new GuiEmpire(service.ActivePlayerController.Empire as Empire);
		this.BuildSortedWinningEmpiresInfo();
		bool flag = this.sortedWinningEmpiresInfo.Any((EmpireInfo info) => info.EmpireIndex == this.playerGuiEmpire.Index);
		if (flag)
		{
			if (TutorialManager.IsActivated)
			{
				this.MainTitle.Text = "%TutorialGameEndedVictoryTitle";
			}
			else if (this.sortedWinningEmpiresInfo.Count > 1)
			{
				bool flag2 = true;
				foreach (EmpireInfo empireInfo in this.sortedWinningEmpiresInfo)
				{
					if (empireInfo.AlliedIndexList.Count < this.sortedWinningEmpiresInfo.Count - 1)
					{
						flag2 = false;
						break;
					}
					foreach (EmpireInfo empireInfo2 in this.sortedWinningEmpiresInfo)
					{
						if (empireInfo2.EmpireIndex != empireInfo.EmpireIndex)
						{
							if (!empireInfo.AlliedIndexList.Contains(empireInfo2.EmpireIndex))
							{
								flag2 = false;
								break;
							}
						}
					}
					if (!flag2)
					{
						break;
					}
				}
				if (flag2)
				{
					this.MainTitle.Text = "%GameEndedSharedVictoryTitle";
				}
				else
				{
					this.MainTitle.Text = "%GameEndedDrawTitle";
				}
			}
			else
			{
				this.MainTitle.Text = "%GameEndedVictoryTitle";
			}
		}
		else
		{
			this.MainTitle.Text = "%GameEndedDefeatTitle";
		}
		bool flag3 = this.sortedWinningEmpiresInfo.Count == 0;
		if (flag3)
		{
			this.Description.Text = "%GameEndedLooserFormat";
			this.VictoryImage.Image = this.playerGuiEmpire.GuiFaction.GetImageTexture(global::GuiPanel.IconSize.MoodDefeat, false);
		}
		else
		{
			this.Description.Text = this.GetWinningEmpiresListText();
			string text = string.Empty;
			text = this.sortedWinningGuiEmpires[0].GuiFaction.Faction.AffinityMapping.Name;
			text = text.Replace("AffinityMapping", string.Empty);
			string path = "Gui/DynamicBitmaps/Factions/majorFaction" + text + "MoodVictory" + this.sortedWinningEmpiresInfo[0].VictoryConditions[0].Name;
			this.VictoryImage.Image = AgeManager.Instance.FindDynamicTexture(path, false);
		}
		this.TutorialGroup.Visible = TutorialManager.IsActivated;
		this.RefreshButtons();
	}

	public override bool HandleCancelRequest()
	{
		return true;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		while (Amplitude.Unity.Gui.GuiModalPanel.GuiModalManager.CurrentModalPanel != null)
		{
			Amplitude.Unity.Gui.GuiModalPanel.GuiModalManager.CurrentModalPanel.Hide(false);
			yield return null;
		}
		yield return base.OnShow(parameters);
		Diagnostics.Assert(parameters != null & parameters.Length > 0);
		this.GuiNotificationGameEnded = (parameters[0] as GuiNotificationGameEnded);
		IPlayerControllerRepositoryService playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(playerControllerRepository != null && playerControllerRepository.ActivePlayerController != null);
		this.RefreshContent();
		yield break;
	}

	private void OnContinuePlayingCB(GameObject obj)
	{
		if (ELCPUtilities.SpectatorMode && this.sortedWinningEmpiresInfo.Count == 0)
		{
			Services.GetService<IGuiNotificationService>().DestroyNotification(this.GuiNotificationGameEnded);
			IEventService service = Services.GetService<IEventService>();
			Diagnostics.Assert(service != null);
			this.Hide(false);
			service.Notify(new EventELCPSpectator());
			return;
		}
		ISessionService service2 = Services.GetService<ISessionService>();
		Diagnostics.Assert(service2 != null);
		Diagnostics.Assert(service2.Session != null);
		if (service2.Session.SessionMode == SessionMode.Single)
		{
			Services.GetService<IGuiNotificationService>().DestroyNotification(this.GuiNotificationGameEnded);
			IEventService service3 = Services.GetService<IEventService>();
			Diagnostics.Assert(service3 != null);
			service3.Notify(new EventDealtWithGameEndingConditions());
			this.Hide(false);
			return;
		}
		Diagnostics.LogError("Cannot continue playing at the moment (session mode: '{0}').", new object[]
		{
			service2.Session.SessionMode
		});
	}

	private void OnScoreScreenCB(GameObject obj)
	{
		IRuntimeService service = Services.GetService<IRuntimeService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Runtime != null);
		service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_OutGame), new object[]
		{
			"GameEnded",
			this.GuiNotificationGameEnded.EmpireInfo
		});
		this.Hide(false);
	}

	private void OnMenuCB(GameObject obj)
	{
		IRuntimeService service = Services.GetService<IRuntimeService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Runtime != null);
		service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_OutGame), new object[0]);
		this.Hide(false);
	}

	private void BuildSortedWinningEmpiresInfo()
	{
		new List<int>();
		EmpireInfo[] empireInfo = this.GuiNotificationGameEnded.EmpireInfo;
		this.sortedWinningEmpiresInfo = (from info in empireInfo
		where info.VictoryConditions != null && info.VictoryConditions.Length > 0 && info.VictoryConditions[0].Category == VictoryCondition.ReadOnlyVictory
		select info).ToList<EmpireInfo>();
		this.sortedWinningEmpiresInfo.Sort();
		Empire[] empires = ((Game)base.Game).Empires;
		this.sortedWinningGuiEmpires = (from info in this.sortedWinningEmpiresInfo
		select new GuiEmpire(empires[info.EmpireIndex])).ToList<GuiEmpire>();
	}

	private string GetWinningEmpiresListText()
	{
		string text = string.Empty;
		for (int i = 0; i < this.sortedWinningEmpiresInfo.Count; i++)
		{
			if (!string.IsNullOrEmpty(text))
			{
				text += "\n";
			}
			string text2 = string.Empty;
			for (int j = 0; j < this.sortedWinningEmpiresInfo[i].VictoryConditions.Length; j++)
			{
				if (!string.IsNullOrEmpty(text2))
				{
					text2 += ", ";
				}
				string x = "VictoryCondition" + this.sortedWinningEmpiresInfo[i].VictoryConditions[j].Name;
				GuiElement guiElement;
				if (base.GuiService.GuiPanelHelper.TryGetGuiElement(x, out guiElement))
				{
					text2 += AgeLocalizer.Instance.LocalizeString(guiElement.Title);
				}
			}
			text += string.Format(AgeLocalizer.Instance.LocalizeString((!TutorialManager.IsActivated) ? "%GameEndedWinnerFormat" : "%TutorialGameEndedWinnerFormat"), this.sortedWinningGuiEmpires[i].GetColorizedLocalizedNameAndFaction(this.playerGuiEmpire.Empire, false), text2);
		}
		return text;
	}

	private void RefreshButtons()
	{
		if (this.ContinuePlayingButton == null)
		{
			return;
		}
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Session != null);
		if (service.Session.SessionMode == SessionMode.Single)
		{
			Diagnostics.Assert(this.ContinuePlayingButton.AgeTransform != null);
			this.ContinuePlayingButton.AgeTransform.Visible = true;
		}
		else
		{
			Diagnostics.Assert(this.ContinuePlayingButton.AgeTransform != null);
			this.ContinuePlayingButton.AgeTransform.Visible = false;
		}
		if (this.playerGuiEmpire.Empire.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated))
		{
			if (!ELCPUtilities.SpectatorMode)
			{
				this.ContinuePlayingButton.AgeTransform.Visible = false;
			}
			else if (this.sortedWinningEmpiresInfo.Count == 0)
			{
				this.ContinuePlayingButton.AgeTransform.Visible = true;
				this.ContinuePlayingButton.AgeTransform.GetComponentInChildren<AgePrimitiveLabel>().Text = "%NotificationEncounterParticipationModeSpectatorTitle";
			}
		}
		bool isActivated = TutorialManager.IsActivated;
		if (isActivated)
		{
			this.ContinuePlayingButton.AgeTransform.Visible = false;
		}
		this.ScoreButton.AgeTransform.enabled = !isActivated;
		this.ScoreButton.AgeTransform.Visible = !isActivated;
		this.MenuButton.AgeTransform.Visible = isActivated;
		this.MenuButton.AgeTransform.Enable = isActivated;
	}

	public AgePrimitiveLabel MainTitle;

	public AgePrimitiveImage VictoryImage;

	public AgePrimitiveLabel Description;

	public AgeTransform TutorialGroup;

	public AgeControlButton ContinuePlayingButton;

	public AgeControlButton ScoreButton;

	public AgeControlButton MenuButton;

	private List<EmpireInfo> sortedWinningEmpiresInfo;

	private List<GuiEmpire> sortedWinningGuiEmpires;

	private GuiEmpire playerGuiEmpire;
}
