using System;
using System.Collections;
using UnityEngine;

public class MenuCreditScreen : GuiMenuScreen
{
	public override bool HandleCancelRequest()
	{
		base.GuiService.Show(typeof(MenuMainScreen), new object[0]);
		this.Hide(false);
		return true;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.StopCoroutine("WaitEndOfDuration");
		yield return base.OnHide(instant);
		for (int i = 0; i < this.CreditsContent.GetChildren().Count; i++)
		{
			CreditImageItem component = this.CreditsContent.GetChildren()[i].GetComponent<CreditImageItem>();
			if (component != null && component.ImagePrimitive != null)
			{
				AgeManager.Instance.ReleaseDynamicTexture(component.ImagePrimitive.name);
			}
		}
		this.CreditsContent.DestroyAllChildren();
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.CreditsContent.Visible = false;
		yield return null;
		TextAsset textAsset = this.CreditsFile;
		string a;
		if (global::Application.ResolveChineseLanguage(out a))
		{
			if (a == "schinese")
			{
				textAsset = this.SChineseCreditsFile;
			}
			else if (a == "tchinese")
			{
				textAsset = this.TChineseCreditsFile;
			}
		}
		this.Credits = Credits.Deserialize(textAsset);
		this.currentY = 0f;
		if (this.Credits != null && this.Credits.Elements != null)
		{
			foreach (Credits.Element element in this.Credits.Elements)
			{
				if (element is Credits.H0)
				{
					this.AddCreditHeader((element as Credits.H0).Text, this.CreditH0Prefab);
				}
				else if (element is Credits.H1)
				{
					this.AddCreditHeader((element as Credits.H1).Text, this.CreditH0Prefab);
				}
				else if (element is Credits.CreditLine)
				{
					this.AddCreditLine((element as Credits.CreditLine).Title, (element as Credits.CreditLine).Name);
				}
				else if (element is Credits.Image)
				{
					this.AddCreditImage((element as Credits.Image).Src);
				}
				else if (element is Credits.Paragraph)
				{
					this.AddCreditParagraph((element as Credits.Paragraph).Text);
				}
			}
			this.Credits = null;
		}
		this.CreditsPositionModifier.StartY = base.AgeTransform.Height;
		this.CreditsPositionModifier.EndY = -this.currentY;
		this.CreditsPositionModifier.EndY -= 60f;
		this.CreditsPositionModifier.Duration = (this.CreditsPositionModifier.StartY - this.CreditsPositionModifier.EndY) / 30f;
		this.CreditsPositionModifier.StartAnimation();
		this.CreditsContent.Visible = true;
		base.StartCoroutine("WaitEndOfDuration");
		yield break;
	}

	private void OnCancelCB(GameObject gameObject)
	{
		this.HandleCancelRequest();
	}

	private void AddCreditHeader(string headerText, Transform prefab)
	{
		AgeTransform ageTransform = this.CreditsContent.InstanciateChild(prefab, "CreditItem");
		ageTransform.GetComponent<CreditHeaderItem>().HeaderTitle.Text = headerText;
		this.PlaceItem(ageTransform);
	}

	private void AddCreditImage(string imageSrc)
	{
		AgeTransform ageTransform = this.CreditsContent.InstanciateChild(this.CreditImagePrefab, "CreditItem");
		CreditImageItem component = ageTransform.GetComponent<CreditImageItem>();
		component.ImagePrimitive.Image = AgeManager.Instance.FindDynamicTexture(imageSrc, false);
		ageTransform.Width = (float)component.ImagePrimitive.Image.width;
		ageTransform.Height = (float)component.ImagePrimitive.Image.height;
		if (!AgeUtils.HighDefinition)
		{
			ageTransform.Width /= AgeUtils.HighDefinitionFactor;
			ageTransform.Height /= AgeUtils.HighDefinitionFactor;
		}
		ageTransform.PixelOffsetLeft = -ageTransform.Width * 0.5f;
		this.PlaceItem(ageTransform);
	}

	private void AddCreditLine(string creditTitle, string creditName)
	{
		AgeTransform ageTransform = this.CreditsContent.InstanciateChild(this.CreditLinePrefab, "CreditItem");
		CreditLineItem component = ageTransform.GetComponent<CreditLineItem>();
		component.Title.Text = creditTitle;
		component.Name.Text = creditName;
		this.PlaceItem(ageTransform);
	}

	private void AddCreditParagraph(string paragraphContent)
	{
		AgeTransform ageTransform = this.CreditsContent.InstanciateChild(this.CreditParagraphPrefab, "CreditItem");
		CreditParagraphItem component = ageTransform.GetComponent<CreditParagraphItem>();
		component.ParagraphContent.AgeTransform.Height = 0f;
		component.ParagraphContent.Text = paragraphContent;
		this.PlaceItem(ageTransform);
	}

	private void PlaceItem(AgeTransform creditItem)
	{
		creditItem.Y = this.currentY;
		this.currentY += creditItem.Height;
	}

	private IEnumerator WaitEndOfDuration()
	{
		yield return new WaitForSeconds(this.CreditsPositionModifier.Duration);
		this.HandleCancelRequest();
		yield break;
	}

	private Credits Credits { get; set; }

	public const int ScrollSpeed = 30;

	public AgeTransform CreditsContent;

	public AgeModifierPosition CreditsPositionModifier;

	public Transform CreditImagePrefab;

	public Transform CreditH0Prefab;

	public Transform CreditH1Prefab;

	public Transform CreditLinePrefab;

	public Transform CreditParagraphPrefab;

	public TextAsset CreditsFile;

	public TextAsset SChineseCreditsFile;

	public TextAsset TChineseCreditsFile;

	private float currentY;
}
