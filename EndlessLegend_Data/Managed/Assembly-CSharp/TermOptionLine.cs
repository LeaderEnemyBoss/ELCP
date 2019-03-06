using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class TermOptionLine : SortedLine
{
	public GuiDiplomaticTerm GuiDiplomaticTerm { get; private set; }

	public bool IsOtherEmpire
	{
		get
		{
			IGameService service = Services.GetService<IGameService>();
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			return this.GuiDiplomaticTerm.Term.EmpireWhichProvides != service2.ActivePlayerController.Empire;
		}
	}

	public GuiDiplomaticTerm TermType
	{
		get
		{
			return this.GuiDiplomaticTerm;
		}
	}

	public string LocalizedName { get; private set; }

	public float Cost
	{
		get
		{
			return this.GuiDiplomaticTerm.EmpirePointCost;
		}
	}

	public virtual void Bind(GuiDiplomaticTerm guiDiplomaticTerm, TermOptionsListPanel parent)
	{
		this.GuiDiplomaticTerm = guiDiplomaticTerm;
		this.LocalizedName = this.GuiDiplomaticTerm.Title;
		this.parent = parent;
		this.TitleLabel.SetTextTruncated(this.LocalizedName, '.');
		this.IconImage.Image = this.GuiDiplomaticTerm.IconTexture;
		this.IconImage.TintColor = this.GuiDiplomaticTerm.IconColor;
		this.GuiDiplomaticTerm.GenerateTooltip(this.NameGroup.AgeTooltip);
		this.GuiDiplomaticTerm.DisplayCost(this.CostLabel);
		if (this.CostGroup.AgeTooltip != null)
		{
			this.CostGroup.AgeTooltip.Class = "DiplomaticTermPriceModifiers";
			this.CostGroup.AgeTooltip.Content = "%NegotiationTermCostDescription";
			this.CostGroup.AgeTooltip.ClientData = this.GuiDiplomaticTerm;
		}
	}

	public void Unbind()
	{
		if (this.CostGroup.AgeTooltip != null)
		{
			this.CostGroup.AgeTooltip.ClientData = null;
		}
		this.parent = null;
		this.GuiDiplomaticTerm = null;
	}

	public void RefreshContent()
	{
		if (this.GuiDiplomaticTerm == null)
		{
			Diagnostics.LogError("No TermOption bound to the TermOptionLine");
			return;
		}
	}

	private void OnClickLineCB(GameObject obj)
	{
		if (this.parent != null)
		{
			this.parent.OnClickTermOptionLine(this);
		}
	}

	private void OnMouseEnterCB(GameObject gameObject)
	{
		if (this.parent != null)
		{
			this.parent.OnEnterTermOptionLine(this);
		}
	}

	private void OnMouseLeaveCB(GameObject gameObject)
	{
		if (this.parent != null)
		{
			this.parent.OnLeaveTermOptionLine(this);
		}
	}

	public AgeTransform NameGroup;

	public AgePrimitiveImage IconImage;

	public AgePrimitiveLabel TitleLabel;

	public AgeTransform CostGroup;

	public AgePrimitiveLabel CostLabel;

	protected TermOptionsListPanel parent;
}
