using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class KaijuResearchModalPanel : GuiPlayerControllerModalPanel
{
	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.departmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfTheTreasury.ResourcePropertyChange += this.DepartmentOfTheTreasury_ResourcePropertyChange;
		this.kaijuUnlocksFrame.SetupFrame(base.Empire, base.gameObject);
		this.luxuryResourcesEnumerator.ResourceType = ResourceDefinition.Type.Luxury;
		this.RefreshContent();
	}

	public override void RefreshContent()
	{
		if (base.Empire == null)
		{
			return;
		}
		if (base.IsVisible)
		{
			this.RefreshUnlocks();
			this.RefreshCosts();
			this.RefreshLuxuries();
		}
	}

	public override void Unbind()
	{
		base.Unbind();
		if (this.departmentOfTheTreasury != null)
		{
			this.departmentOfTheTreasury.ResourcePropertyChange -= this.DepartmentOfTheTreasury_ResourcePropertyChange;
			this.departmentOfTheTreasury = null;
		}
	}

	protected override IEnumerator OnHide(bool instant)
	{
		yield return base.OnHide(instant);
		this.luxuryResourcesEnumerator.Hide(instant);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.endTurnService = Services.GetService<IEndTurnService>();
		Diagnostics.Assert(this.endTurnService != null);
		this.endTurnService.GameClientStateChange += this.EndTurnService_GameClientStateChange;
		this.kaijuTechsService = base.GameService.Game.Services.GetService<IKaijuTechsService>();
		Diagnostics.Assert(this.kaijuTechsService != null);
		this.kaijuTechsService.KaijuTechnologyUnlocked += this.OnKaijuTechnologyUnlocked;
		this.kaijuTechsService.ResearchQueueChanged += this.OnKaijuResearchQueueChanged;
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.kaijuUnlocksFrame.SetSimpleMode(false);
		this.CreateKaijuPortraits();
		this.luxuryResourcesEnumerator.Show(new object[0]);
		this.layoutGroup.Visible = false;
		this.saveLayoutButton.AgeTransform.Visible = false;
		this.RefreshContent();
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Unbind();
		if (this.endTurnService != null)
		{
			this.endTurnService.GameClientStateChange -= this.EndTurnService_GameClientStateChange;
			this.endTurnService = null;
		}
		if (this.kaijuTechsService != null)
		{
			this.kaijuTechsService.KaijuTechnologyUnlocked -= this.OnKaijuTechnologyUnlocked;
			this.kaijuTechsService.ResearchQueueChanged -= this.OnKaijuResearchQueueChanged;
			this.kaijuTechsService = null;
		}
		base.OnUnloadGame(game);
	}

	private bool CanAffordTechs()
	{
		ConstructionQueue constructionQueueForEmpire = this.kaijuTechsService.GetConstructionQueueForEmpire(base.Empire);
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		for (int i = 0; i < constructionQueueForEmpire.Length; i++)
		{
			ConstructibleElement constructibleElement = constructionQueueForEmpire.PeekAt(i).ConstructibleElement;
			for (int j = 0; j < constructibleElement.Costs.Length; j++)
			{
				string key = constructibleElement.Costs[j].ResourceName;
				float num = -constructibleElement.Costs[j].GetValue(base.Empire);
				if (!dictionary.ContainsKey(constructibleElement.Costs[j].ResourceName))
				{
					dictionary.Add(constructibleElement.Costs[j].ResourceName, num);
				}
				else
				{
					num += dictionary[key];
					dictionary[key] = num;
				}
			}
		}
		foreach (KeyValuePair<string, float> keyValuePair in dictionary)
		{
			float value = keyValuePair.Value;
			if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, keyValuePair.Key, ref value))
			{
				return false;
			}
		}
		return true;
	}

	private void CreateKaijuPortraits()
	{
		List<Kaiju> list = this.ObtainKaijus();
		this.portratitsContainer.ReserveChildren(list.Count, this.kaijuPortraitPrefab, "Item");
		this.portratitsContainer.RefreshChildrenIList<Kaiju>(list, new AgeTransform.RefreshTableItem<Kaiju>(this.SetupKaijuPortraits), true, false);
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (e.Location != base.Empire.SimulationObject)
		{
			return;
		}
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private List<Kaiju> ObtainKaijus()
	{
		List<Kaiju> list = new List<Kaiju>();
		MajorEmpire[] array = Array.ConvertAll<global::Empire, MajorEmpire>(Array.FindAll<global::Empire>((base.Game as global::Game).Empires, (global::Empire match) => match is MajorEmpire), (global::Empire empire) => empire as MajorEmpire);
		for (int i = 0; i < array.Length; i++)
		{
			List<Kaiju> tamedKaijus = array[i].TamedKaijus;
			if (tamedKaijus.Count > 0)
			{
				for (int j = 0; j < tamedKaijus.Count; j++)
				{
					list.Add(tamedKaijus[j]);
				}
			}
		}
		KaijuEmpire[] array2 = Array.ConvertAll<global::Empire, KaijuEmpire>(Array.FindAll<global::Empire>((base.Game as global::Game).Empires, (global::Empire match) => match is KaijuEmpire), (global::Empire empire) => empire as KaijuEmpire);
		for (int k = 0; k < array2.Length; k++)
		{
			KaijuCouncil agency = array2[k].GetAgency<KaijuCouncil>();
			if (agency.Kaiju != null)
			{
				list.Add(agency.Kaiju);
			}
		}
		return list;
	}

	private void OnApplyCB(GameObject obj)
	{
		ConstructionQueue constructionQueueForEmpire = this.kaijuTechsService.GetConstructionQueueForEmpire(base.Empire);
		if (constructionQueueForEmpire != null)
		{
			int length = constructionQueueForEmpire.Length;
			for (int i = length - 1; i >= 0; i--)
			{
				this.buyoutButton.AgeTransform.Enable = false;
				Construction construction = constructionQueueForEmpire.PeekAt(i);
				if (construction == null)
				{
					return;
				}
				OrderBuyOutKaijuTechnology order = new OrderBuyOutKaijuTechnology(base.Empire.Index, construction.ConstructibleElement.Name);
				base.PlayerController.PostOrder(order);
			}
		}
	}

	private void OnCancelCB(GameObject obj)
	{
		this.HandleCancelRequest();
	}

	private void OnEditLayoutCB(GameObject obj)
	{
		this.saveLayoutButton.AgeTransform.Visible = this.layoutToggle.State;
		TechnologyFrame[] componentsInChildren = base.GetComponentsInChildren<TechnologyFrame>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].ActivateMarkup(this.layoutToggle.State);
		}
	}

	private void OnKaijuTechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private void OnKaijuResearchQueueChanged(object sender, ConstructionChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private void OnSaveLayoutCB(GameObject obj)
	{
	}

	private void OnSelectTechnology(AgeTransform technology)
	{
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private void RefreshCosts()
	{
		ConstructionQueue constructionQueueForEmpire = this.kaijuTechsService.GetConstructionQueueForEmpire(base.Empire);
		ConstructibleElement constructibleElement = null;
		if (constructionQueueForEmpire.Peek() != null)
		{
			constructibleElement = constructionQueueForEmpire.Peek().ConstructibleElement;
		}
		int num = 0;
		string text;
		PanelFeatureCost.ComputeCostAndTurn(base.GuiService, constructionQueueForEmpire.PendingConstructions, this.departmentOfTheTreasury, base.Empire, out text, out num);
		if (constructibleElement != null)
		{
			if (this.CanAffordTechs())
			{
				this.buyoutCostLabel.Text = text;
				this.buyoutButton.AgeTransform.Enable = true;
				this.buyoutButton.AgeTransform.Alpha = 1f;
				string text2 = AgeLocalizer.Instance.LocalizeString("%KaijuUnlockBuyoutAvailableFormat").Replace("$Cost", text);
			}
			else
			{
				this.buyoutCostLabel.Text = text;
				this.buyoutButton.AgeTransform.Enable = false;
				this.buyoutButton.AgeTransform.Alpha = 0.5f;
				string text2 = AgeLocalizer.Instance.LocalizeString("%KaijuUnlockBuyoutUnavailableFormat").Replace("$Cost", text);
			}
		}
		else
		{
			this.buyoutButton.AgeTransform.Enable = false;
			this.buyoutButton.AgeTransform.Alpha = 0.5f;
			this.buyoutCostLabel.Text = "%ResearchVoidSymbol";
			string text2 = AgeLocalizer.Instance.LocalizeString("%KaijuUnlockBuyoutNoSelectionDescription");
		}
	}

	private void RefreshLuxuries()
	{
		this.luxuryResourcesEnumerator.RefreshContent();
	}

	private void RefreshUnlocks()
	{
		this.kaijuUnlocksFrame.RefreshFrame();
	}

	private void SetupKaijuPortraits(AgeTransform tableitem, Kaiju kaiju, int index)
	{
		KaijuTechPortrait component = tableitem.GetComponent<KaijuTechPortrait>();
		component.SetupPortrait(kaiju, base.Empire);
		if (component.KaijuGuiElement != null)
		{
			component.AgeTransform.X = ((!AgeUtils.HighDefinition) ? component.KaijuGuiElement.X : (component.KaijuGuiElement.X * AgeUtils.HighDefinitionFactor));
			component.AgeTransform.Y = ((!AgeUtils.HighDefinition) ? component.KaijuGuiElement.Y : (component.KaijuGuiElement.Y * AgeUtils.HighDefinitionFactor));
		}
		else
		{
			Diagnostics.LogError("The Kaiju doesn't have a KaijuTechPortrait");
			component.AgeTransform.X = 0f;
			component.AgeTransform.Y = 0f;
		}
	}

	public override void Hide(bool instant = false)
	{
		this.kaijuTechsService.EmptyConstructionQueueForEmpire(base.Empire);
		base.Hide(instant);
	}

	[SerializeField]
	private AgeControlButton buyoutButton;

	[SerializeField]
	private AgePrimitiveLabel buyoutCostLabel;

	[SerializeField]
	private Transform kaijuPortraitPrefab;

	[SerializeField]
	private KaijuUnlocksFrame kaijuUnlocksFrame;

	[SerializeField]
	private AgeTransform layoutGroup;

	[SerializeField]
	public ResourceEnumerator luxuryResourcesEnumerator;

	[SerializeField]
	private AgeControlToggle layoutToggle;

	[SerializeField]
	private AgeTransform portratitsContainer;

	[SerializeField]
	private AgeControlButton saveLayoutButton;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private IEndTurnService endTurnService;

	private IKaijuTechsService kaijuTechsService;
}
