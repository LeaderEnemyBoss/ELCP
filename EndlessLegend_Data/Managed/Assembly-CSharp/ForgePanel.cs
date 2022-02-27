using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class ForgePanel : GuiPanel
{
	public global::PlayerController PlayerController { get; private set; }

	public global::Empire Empire { get; private set; }

	public new AgeTransform AgeTransform
	{
		get
		{
			AgeTransform component = base.GetComponent<AgeTransform>();
			Diagnostics.Assert(component != null, "The ForgePanel does not contain a AgeTransform component");
			return component;
		}
	}

	public List<ForgePanel.ObsolescenceGroup> HighestItemTiers { get; set; }

	public void Bind(global::Empire empire)
	{
		if (this.Empire != null)
		{
			this.Unbind();
		}
		this.Empire = empire;
		this.departmentOfTheTreasury = this.Empire.GetAgency<DepartmentOfTheTreasury>();
	}

	public void Unbind()
	{
		if (this.Empire != null)
		{
			this.Empire = null;
		}
	}

	public override void RefreshContent()
	{
		if (Services.GetService<IDownloadableContentService>().IsShared(DownloadableContent16.ReadOnlyName) && this.editableUnitDesign.UnitBodyDefinition.Tags.Contains(DownloadableContent16.TagSeafaring))
		{
			this.ForgeTitle.Text = "%ArsenalPanelTitle";
		}
		else
		{
			this.ForgeTitle.Text = "%ForgePanelTitle";
		}
		this.filter = ForgePanel.ItemCategory.Default;
		for (int i = 0; i < this.FilterTable.GetChildren().Count; i++)
		{
			AgeControlToggle ageControlToggle = this.filterToggles[i];
			if (ageControlToggle.State)
			{
				this.filter |= this.filtersByToggle[ageControlToggle];
			}
		}
		this.filteredItems.Clear();
		this.itemEnableState.Clear();
		if (this.editableUnitDesign != null)
		{
			foreach (ItemDefinition itemDefinition in this.allItems)
			{
				if (!itemDefinition.Hidden)
				{
					this.failuresFlags.Clear();
					DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.editableUnitDesign.GetSimulationObject(), itemDefinition, ref this.failuresFlags, new string[]
					{
						ConstructionFlags.Prerequisite
					});
					if (!this.failuresFlags.Contains(ConstructionFlags.Discard))
					{
						bool flag = this.failuresFlags.Count > 0;
						bool flag2 = false;
						int j = 0;
						while (j < itemDefinition.Descriptors.Length)
						{
							if (itemDefinition.Descriptors[j].Type == "ItemCategory")
							{
								if ((this.filter & ForgePanel.ItemCategory.Weapon) > ForgePanel.ItemCategory.Default)
								{
									flag2 = (itemDefinition.Descriptors[j].Name == "ItemCategory" + ForgePanel.ItemCategory.Weapon);
								}
								if (!flag2 && (this.filter & ForgePanel.ItemCategory.Armor) > ForgePanel.ItemCategory.Default)
								{
									flag2 = (itemDefinition.Descriptors[j].Name == "ItemCategory" + ForgePanel.ItemCategory.Armor);
								}
								if (!flag2 && (this.filter & ForgePanel.ItemCategory.Accessory) > ForgePanel.ItemCategory.Default)
								{
									flag2 = (itemDefinition.Descriptors[j].Name == "ItemCategory" + ForgePanel.ItemCategory.Accessory);
									break;
								}
								break;
							}
							else
							{
								j++;
							}
						}
						if (flag2 && !this.ShowMissingResourceToggle.State)
						{
							for (int k = 0; k < itemDefinition.Descriptors.Length; k++)
							{
								if (itemDefinition.Descriptors[k].Type == "ItemMaterial")
								{
									this.resourceName = itemDefinition.Descriptors[k].Name;
									this.resourceName = this.resourceName.Remove(0, "ItemMaterial".Length);
									float num;
									if (!this.departmentOfTheTreasury.TryGetResourceStockValue(this.Empire.SimulationObject, this.resourceName, out num, true))
									{
										num = 1f;
									}
									if (num < 1f)
									{
										flag2 = false;
									}
								}
							}
						}
						if (flag2 && !this.ShowObsoleteToggle.State && !itemDefinition.Tags.Contains("NeverObsolete"))
						{
							string itemType = string.Empty;
							string text = string.Empty;
							string strA = string.Empty;
							itemType = itemDefinition.SubCategory;
							for (int l = 0; l < itemDefinition.Descriptors.Length; l++)
							{
								if (itemDefinition.Descriptors[l].Type == "ItemTier")
								{
									strA = itemDefinition.Descriptors[l].Name;
								}
								if (itemDefinition.Descriptors[l].Type == "ItemMaterial")
								{
									text = itemDefinition.Descriptors[l].Name;
								}
							}
							int m = 0;
							while (m < this.HighestItemTiers.Count)
							{
								if (this.HighestItemTiers[m].MatchesTypeAndRessource(itemType, text))
								{
									if (string.Compare(strA, this.HighestItemTiers[m].Tier) < 0)
									{
										flag2 = false;
										break;
									}
									break;
								}
								else
								{
									m++;
								}
							}
						}
						if (flag2)
						{
							this.filteredItems.Add(itemDefinition);
							this.itemEnableState.Add(!flag);
						}
					}
				}
			}
		}
		this.ForgeItemTable.Height = 0f;
		this.ForgeItemTable.ReserveChildren(this.filteredItems.Count, this.ForgeItemPrefab, "Item");
		this.ForgeItemTable.RefreshChildrenIList<ItemDefinition>(this.filteredItems, this.refreshForgeItemDelegate, true, false);
		this.ForgeItemTable.ArrangeChildren();
		base.RefreshContent();
	}

	public void CancelDrag()
	{
		List<AgeControlDragArea> children = this.ForgeItemTable.GetChildren<AgeControlDragArea>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].gameObject.SendMessage("OnResetInteraction");
			AgeControlButton componentInChildren = children[i].GetComponentInChildren<AgeControlButton>();
			if (componentInChildren != null)
			{
				componentInChildren.gameObject.SendMessage("OnResetInteraction");
			}
		}
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.filteredItems = new List<ItemDefinition>();
		this.itemEnableState = new List<bool>();
		this.filter = ForgePanel.ItemCategory.Default;
		this.filterToggles = new List<AgeControlToggle>();
		this.filtersByToggle = new Dictionary<AgeControlToggle, ForgePanel.ItemCategory>();
		for (int i = 0; i < this.FilterTable.GetChildren().Count; i++)
		{
			this.filterToggles.Add(this.FilterTable.GetChildren()[i].GetComponent<AgeControlToggle>());
			this.filterToggles[i].OnSwitchMethod = "OnFilterSwitch";
			this.filterToggles[i].OnSwitchObject = base.gameObject;
			this.filterToggles[i].State = true;
		}
		this.filtersByToggle.Add(this.filterToggles[0], ForgePanel.ItemCategory.Weapon | ForgePanel.ItemCategory.Armor | ForgePanel.ItemCategory.Accessory);
		this.filtersByToggle.Add(this.filterToggles[1], ForgePanel.ItemCategory.Weapon);
		this.filtersByToggle.Add(this.filterToggles[2], ForgePanel.ItemCategory.Armor);
		this.filtersByToggle.Add(this.filterToggles[3], ForgePanel.ItemCategory.Accessory);
		this.ShowObsoleteToggle.State = false;
		this.ShowMissingResourceToggle.State = false;
		this.HighestItemTiers = new List<ForgePanel.ObsolescenceGroup>();
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		IPlayerControllerRepositoryService playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		while (playerControllerRepository.ActivePlayerController == null)
		{
			yield return null;
		}
		this.PlayerController = playerControllerRepository.ActivePlayerController;
		this.allItems = Databases.GetDatabase<ItemDefinition>(false).GetValues();
		this.refreshForgeItemDelegate = new AgeTransform.RefreshTableItem<ItemDefinition>(this.RefreshForgeItem);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		Diagnostics.Assert(parameters.Length == 1);
		this.editableUnitDesign = (parameters[0] as UnitDesign);
		this.filter = (ForgePanel.ItemCategory.Weapon | ForgePanel.ItemCategory.Armor | ForgePanel.ItemCategory.Accessory);
		for (int i = 0; i < this.filterToggles.Count; i++)
		{
			this.filterToggles[i].State = (i == 0);
		}
		this.FilterHighestItemTier();
		this.RefreshContent();
		this.ForgeScrollview.ResetUp();
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.refreshForgeItemDelegate = null;
		this.allItems = null;
		if (this.PlayerController != null)
		{
			this.PlayerController = null;
		}
		this.ForgeItemTable.DestroyAllChildren();
		base.OnUnloadGame(game);
	}

	protected override void OnUnload()
	{
		this.filtersByToggle.Clear();
		this.filter = ForgePanel.ItemCategory.Default;
		this.filterToggles = null;
		this.filtersByToggle = null;
		this.filteredItems.Clear();
		this.itemEnableState.Clear();
		this.filteredItems = null;
		this.itemEnableState = null;
		base.OnUnload();
	}

	public void FilterHighestItemTier()
	{
		this.HighestItemTiers.Clear();
		string itemType = string.Empty;
		string text = string.Empty;
		StaticString x = StaticString.Empty;
		foreach (ItemDefinition itemDefinition in this.allItems)
		{
			if (!itemDefinition.Hidden && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.editableUnitDesign.GetSimulationObject(), itemDefinition, new string[]
			{
				"Discard"
			}))
			{
				itemType = itemDefinition.SubCategory;
				for (int i = 0; i < itemDefinition.Descriptors.Length; i++)
				{
					if (itemDefinition.Descriptors[i].Type == "ItemTier")
					{
						x = itemDefinition.Descriptors[i].Name;
					}
					if (itemDefinition.Descriptors[i].Type == "ItemMaterial")
					{
						text = itemDefinition.Descriptors[i].Name;
					}
				}
				bool flag = false;
				if (x != ForgePanel.ItemTier4)
				{
					for (int j = 0; j < this.HighestItemTiers.Count; j++)
					{
						if (this.HighestItemTiers[j].MatchesTypeAndRessource(itemType, text))
						{
							if (string.Compare(x, this.HighestItemTiers[j].Tier) > 0)
							{
								this.HighestItemTiers[j].Tier = x;
							}
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					this.HighestItemTiers.Add(new ForgePanel.ObsolescenceGroup(itemType, text, x));
				}
			}
		}
	}

	private void OnAutoAssignCB(GameObject obj)
	{
		AgeControlButton component = obj.GetComponent<AgeControlButton>();
		foreach (ItemDefinition itemDefinition in this.allItems)
		{
			if (itemDefinition.Name == component.OnDoubleClickData)
			{
				this.DragReceiver.SendMessage("OnAutoAssignItem", itemDefinition, SendMessageOptions.RequireReceiver);
				break;
			}
		}
	}

	private void OnEquipmentDragStartedCB()
	{
	}

	private void OnEquipmentDragCompletedCB()
	{
	}

	private void OnFilterSwitch(GameObject obj)
	{
		for (int i = 0; i < this.filterToggles.Count; i++)
		{
			this.filterToggles[i].State = (this.filterToggles[i].gameObject == obj);
		}
		this.RefreshContent();
		this.ForgeScrollview.ResetUp();
	}

	private void OnForgeDragStartedCB(GameObject obj)
	{
		ForgeGuiItem component = obj.GetComponent<ForgeGuiItem>();
		if (component != null)
		{
			ImageDragPanel guiPanel = base.GuiService.GetGuiPanel<ImageDragPanel>();
			guiPanel.Show(new object[]
			{
				component.ItemDefinition
			});
			AgeTransform component2 = guiPanel.GetComponent<AgeTransform>();
			component2.X = AgeManager.Instance.Cursor.x - 0.5f * component2.Width;
			component2.Y = AgeManager.Instance.Cursor.y - 0.5f * component2.Height;
			if (this.DragReceiver != null)
			{
				this.DragReceiver.SendMessage("OnForgeDragStartedCB", component.ItemDefinition, SendMessageOptions.RequireReceiver);
			}
		}
		else
		{
			obj.SendMessage("OnResetInteraction", SendMessageOptions.DontRequireReceiver);
		}
	}

	private void OnForgeDragCompletedCB(GameObject obj)
	{
		if (this.DragReceiver != null)
		{
			this.DragReceiver.SendMessage("OnForgeDragCompletedCB", SendMessageOptions.RequireReceiver);
		}
		base.GuiService.GetGuiPanel<ImageDragPanel>().Hide(true);
	}

	private void OnShowObsoleteCB(GameObject obj)
	{
		this.RefreshContent();
	}

	private void OnShowMissingResourceCB(GameObject obj)
	{
		this.RefreshContent();
	}

	private void RefreshForgeItem(AgeTransform tableitem, ItemDefinition itemDefinition, int index)
	{
		ForgeGuiItem component = tableitem.GetComponent<ForgeGuiItem>();
		component.RefreshContent(this.Empire, itemDefinition, base.gameObject, this.itemEnableState[index]);
	}

	public const string ItemMaterialString = "ItemMaterial";

	public const string NeverObsolete = "NeverObsolete";

	public AgePrimitiveLabel ForgeTitle;

	public AgeControlScrollView ForgeScrollview;

	public AgeTransform ForgeItemTable;

	public AgeTransform FilterTable;

	public AgeTransform Foreground;

	public Transform ForgeItemPrefab;

	public AgeControlToggle ShowObsoleteToggle;

	public AgeControlToggle ShowMissingResourceToggle;

	public GameObject DragReceiver;

	private ForgePanel.ItemCategory filter;

	private UnitDesign editableUnitDesign;

	private List<AgeControlToggle> filterToggles;

	private Dictionary<AgeControlToggle, ForgePanel.ItemCategory> filtersByToggle;

	private List<ItemDefinition> filteredItems;

	private List<bool> itemEnableState;

	private AgeTransform.RefreshTableItem<ItemDefinition> refreshForgeItemDelegate;

	private string resourceName = string.Empty;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private IEnumerable<ItemDefinition> allItems;

	private List<StaticString> failuresFlags = new List<StaticString>();

	private static StaticString ItemTier3 = "ItemTier3";

	private static StaticString ItemTier4 = "ItemTier4";

	[Flags]
	public enum ItemCategory
	{
		Default = 0,
		Weapon = 1,
		Armor = 2,
		Accessory = 4
	}

	public class ObsolescenceGroup
	{
		public ObsolescenceGroup(string itemType, string resourceName, string tier)
		{
			this.ItemType = itemType;
			this.ResourceName = resourceName;
			this.Tier = tier;
		}

		public bool MatchesTypeAndRessource(string itemType, string resourceName)
		{
			return itemType == this.ItemType && resourceName == this.ResourceName;
		}

		public string ItemType;

		public string ResourceName;

		public string Tier;
	}
}
