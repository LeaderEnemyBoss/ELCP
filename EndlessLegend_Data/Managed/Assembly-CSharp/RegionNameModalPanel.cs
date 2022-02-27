using System;
using System.Collections;
using Amplitude;
using UnityEngine;

public class RegionNameModalPanel : GuiModalPanel
{
	public RegionNameModalPanel()
	{
		this.profanityError = string.Empty;
		this.invalidColor = new Color(0.7529412f, 0.2509804f, 0.2509804f);
	}

	public Region Region { get; private set; }

	public WorldPosition WorldPosition { get; private set; }

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.NameLabel.Text = this.Region.LocalizedName;
		AgeManager.Instance.FocusedControl = this.NameTextfield;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.NameTextfield.ValidChars = AgeLocalizer.Instance.LocalizeString("%ArmyValidChars");
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		Diagnostics.Assert(parameters.Length == 1, "Invalid parameters number when calling RegionNameModalPanel.Show()");
		Diagnostics.Assert(parameters[0] is Region, "Invalid Region when calling RegionNameModalPanel.Show()");
		this.Region = (parameters[0] as Region);
		this.ValidateButton.Enable = true;
		this.RefreshContent();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.NameLabel.TintColor = Color.white;
		this.NameSelectionFrame.TintColor = Color.white;
		this.ValidateButton.AgeTooltip.Content = null;
		yield return base.OnHide(instant);
		yield break;
	}

	private void OnChangeNameCB(GameObject obj)
	{
		this.ValidateButton.Enable = this.ValidateCityName();
		this.StartProfanityFiltering();
	}

	private void OnValidateCB(GameObject obj)
	{
		if (this.Region == null)
		{
			return;
		}
		if (this.NameLabel.Text.Trim() == this.Region.LocalizedName)
		{
			this.Hide(false);
			return;
		}
		if (this.ValidateCityName())
		{
			IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service != null && service.ActivePlayerController != null)
			{
				OrderChangeRegionUserDefinedName order = new OrderChangeRegionUserDefinedName(this.Region.Owner.Index, this.Region.Index, this.NameLabel.Text.Trim());
				service.ActivePlayerController.PostOrder(order);
			}
		}
		this.Hide(false);
	}

	private void OnCancelCB(GameObject obj)
	{
		this.Hide(false);
	}

	private bool IsCityNameAlreadyUsed()
	{
		string b = this.NameLabel.Text.Trim().ToLower();
		Game game = base.Game as Game;
		Region[] regions = game.World.Regions;
		for (int i = 0; i < regions.Length; i++)
		{
			if (regions[i].Index != this.Region.Index && regions[i].LocalizedName.ToLower() == b)
			{
				return true;
			}
		}
		return false;
	}

	private bool ValidateCityName()
	{
		bool flag = true;
		if (this.NameLabel.Text.Trim().Length == 0)
		{
			flag = false;
			this.ValidateButton.AgeTooltip.Content = "%CityNameCannotBeEmptyDescription";
		}
		else if (this.IsCityNameAlreadyUsed())
		{
			flag = false;
			this.ValidateButton.AgeTooltip.Content = "%CityNameAlreadyExistsDescription";
		}
		if (flag)
		{
			this.NameLabel.TintColor = Color.white;
			this.NameSelectionFrame.TintColor = Color.white;
			this.ValidateButton.AgeTooltip.Content = null;
		}
		else
		{
			this.NameLabel.TintColor = Color.red;
			this.NameSelectionFrame.TintColor = Color.red;
		}
		if (this.profanityError != string.Empty)
		{
			this.ValidateButton.Enable = false;
			this.ValidateButton.AgeTooltip.Content = "%Failure" + this.profanityError + "Description";
			flag = false;
		}
		return flag;
	}

	private void StartProfanityFiltering()
	{
	}

	public AgeControlTextField NameTextfield;

	public AgePrimitiveLabel NameLabel;

	public AgePrimitiveImage NameSelectionFrame;

	public AgeTransform ValidateButton;

	private string profanityError;

	private UnityEngine.Coroutine profanityFilterCoroutine;

	private Color invalidColor;
}
