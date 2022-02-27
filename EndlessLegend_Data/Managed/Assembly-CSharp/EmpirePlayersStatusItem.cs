using System;
using System.Collections;
using Amplitude;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Session;
using UnityEngine;

public class EmpirePlayersStatusItem : MonoBehaviour
{
	public MajorEmpire MajorEmpire { get; private set; }

	public GuiFaction GuiFaction { get; private set; }

	private global::Empire ActivePlayerEmpire { get; set; }

	private global::Session Session { get; set; }

	private bool ArePlayersReady
	{
		get
		{
			for (int i = 0; i < this.MajorEmpire.Players.Count; i++)
			{
				Player player = this.MajorEmpire.Players[i];
				if (player.Type != PlayerType.AI && player.State != PlayerState.Ready)
				{
					return false;
				}
			}
			return true;
		}
	}

	private bool IsInEncounter
	{
		get
		{
			for (int i = 0; i < this.MajorEmpire.Players.Count; i++)
			{
				if (this.MajorEmpire.Players[i].State != PlayerState.InEncounter)
				{
					return false;
				}
			}
			return true;
		}
	}

	private bool IsKnownByActivePlayer
	{
		get
		{
			if (this.MajorEmpire.Index == this.ActivePlayerEmpire.Index)
			{
				return true;
			}
			DepartmentOfForeignAffairs agency = this.ActivePlayerEmpire.GetAgency<DepartmentOfForeignAffairs>();
			if (agency != null)
			{
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(this.MajorEmpire);
				return diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown;
			}
			return false;
		}
	}

	public void SetContent(MajorEmpire majorEmpire)
	{
		this.MajorEmpire = majorEmpire;
		Diagnostics.Assert(this.MajorEmpire != null);
		this.GuiFaction = new GuiFaction(this.MajorEmpire.Faction);
		IGameService service = Services.GetService<IGameService>();
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.ActivePlayerEmpire = (service2.ActivePlayerController.Empire as global::Empire);
		ISessionService service3 = Services.GetService<ISessionService>();
		this.Session = (service3.Session as global::Session);
		this.HighlightCircle.Visible = false;
	}

	public void UnsetContent()
	{
		this.MajorEmpire = null;
		this.GuiFaction = null;
		this.ActivePlayerEmpire = null;
		this.Session = null;
	}

	public void RefreshContent()
	{
		Amplitude.Unity.Gui.IGuiService service = Services.GetService<global::IGuiService>();
		if (this.ArePlayersReady && this.Session.SessionMode != SessionMode.Single)
		{
			this.ReadyImage.AgeTransform.Visible = true;
		}
		else
		{
			this.ReadyImage.AgeTransform.Visible = false;
		}
		if (this.MajorEmpire.IsEliminated && !this.MajorEmpire.IsSpectator)
		{
			this.AgeTransform.GetComponent<AgePrimitiveImage>().Image = AgeManager.Instance.FindDynamicTexture("eliminatedLogoSmall", false);
		}
		else if (this.IsInEncounter)
		{
			this.AgeTransform.GetComponent<AgePrimitiveImage>().Image = AgeManager.Instance.FindDynamicTexture("encounterLogoSmall", false);
		}
		else if (!this.IsKnownByActivePlayer && !this.MajorEmpire.IsSpectator)
		{
			this.AgeTransform.GetComponent<AgePrimitiveImage>().Image = AgeManager.Instance.FindDynamicTexture("majorFactionRandomLogoSmall", false);
		}
		else
		{
			this.LogoImage.Image = this.GuiFaction.GetImageTexture(global::GuiPanel.IconSize.LogoSmall, false);
		}
		if (this.IsKnownByActivePlayer)
		{
			this.AgeControlButton.OnMiddleClickMethod = "OnRightClick";
			this.AgeControlButton.OnRightClickMethod = "OnRightClick";
			this.AgeControlButton.OnMiddleClickObject = service.GetGuiPanel<EndTurnPanel>().gameObject;
			this.AgeControlButton.OnRightClickObject = service.GetGuiPanel<EndTurnPanel>().gameObject;
			this.AgeTransform.AgeTooltip.Content = this.MajorEmpire.LocalizedName + " - " + this.MajorEmpire.Faction.LocalizedName;
			if (!this.MajorEmpire.IsControlledByAI && this.MajorEmpire != this.ActivePlayerEmpire)
			{
				AgeTooltip ageTooltip = this.AgeTransform.AgeTooltip;
				ageTooltip.Content = ageTooltip.Content + "\n \n" + AgeLocalizer.Instance.LocalizeString("%ClickToWhisperTooltipContent");
				this.AgeControlButton.OnActivateMethod = "OnWhisperToEmpireCB";
				this.AgeControlButton.OnActivateObject = service.GetGuiPanel<InGameConsolePanel>().gameObject;
				this.AgeControlButton.OnActivateData = this.MajorEmpire.LocalizedName;
				return;
			}
		}
		else
		{
			GuiElement guiElement;
			if (!service.GuiPanelHelper.TryGetGuiElement(DiplomaticRelationState.Names.Unknown, out guiElement))
			{
				this.AgeTransform.AgeTooltip.Content = "Missing GuiElement " + DiplomaticRelationState.Names.Unknown;
				return;
			}
			this.AgeTransform.AgeTooltip.Content = guiElement.Title;
			this.AgeControlButton.OnActivateMethod = string.Empty;
			this.AgeControlButton.OnActivateObject = null;
			this.AgeControlButton.OnActivateData = string.Empty;
		}
	}

	public void HighlightReady()
	{
		this.HighlightCircle.Visible = true;
		this.HighlightCircle.StartAllModifiers(true, false);
		base.StartCoroutine(this.FadeCircle());
		IAudioEventService service = Services.GetService<IAudioEventService>();
		service.Play2DEvent("Gui/Interface/InGame/PlayerEndTurn");
	}

	private IEnumerator FadeCircle()
	{
		while (this.HighlightCircle.ModifiersRunning)
		{
			yield return null;
		}
		this.HighlightCircle.Visible = false;
		yield break;
	}

	public AgeTransform AgeTransform;

	public AgeControlButton AgeControlButton;

	public AgePrimitiveImage LogoImage;

	public AgePrimitiveImage ReadyImage;

	public AgeTransform HighlightCircle;
}
