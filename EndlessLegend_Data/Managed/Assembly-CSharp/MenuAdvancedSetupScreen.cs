using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;
using UnityEngine;

public class MenuAdvancedSetupScreen : GuiMenuScreen
{
	public override string MenuName
	{
		get
		{
			return "%BreadcrumbAdvancedSettings" + this.category + "Title";
		}
		set
		{
		}
	}

	private bool AdvancedDataChanged { get; set; }

	private Dictionary<StaticString, OptionDefinition> OptionDefinitions { get; set; }

	private global::Session Session
	{
		get
		{
			return this.session;
		}
		set
		{
			if (this.session != null)
			{
				this.session.LobbyDataChange -= this.Session_LobbyDataChange;
			}
			this.session = value;
			if (this.session != null)
			{
				this.session.LobbyDataChange += this.Session_LobbyDataChange;
			}
		}
	}

	public override bool HandleCancelRequest()
	{
		if (base.IsShowing)
		{
			base.StartCoroutine(this.HandleCancelRequestWhenShowingFinished());
			return true;
		}
		base.GuiService.GetGuiPanel<MenuNewGameScreen>().ShowWhenFinishedHiding(new object[0]);
		return true;
	}

	public override void RefreshContent()
	{
		List<MenuSettingsGroup> children = this.SettingsGroupsContainer.GetChildren<MenuSettingsGroup>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].RefreshContent(this.optionValuesByName, this.readOnly);
		}
		base.RefreshContent();
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.readOnly = false;
		if (parameters != null)
		{
			if (parameters.Length > 0)
			{
				this.category = (parameters[0] as string);
			}
			if (parameters.Length > 1)
			{
				string readOnlyString = parameters[1] as string;
				if (readOnlyString != null && readOnlyString == "readonly")
				{
					this.readOnly = true;
				}
			}
		}
		if (string.IsNullOrEmpty(this.category))
		{
			yield break;
		}
		ISessionService sessionService = Services.GetService<ISessionService>();
		Diagnostics.Assert(sessionService != null);
		this.Session = (sessionService.Session as global::Session);
		if (this.Session == null)
		{
			yield break;
		}
		this.OptionDefinitions = new Dictionary<StaticString, OptionDefinition>();
		if (this.category == "Game")
		{
			IDatabase<OptionDefinition> optionDefinitionDatabase = Databases.GetDatabase<OptionDefinition>(true);
			foreach (OptionDefinition optionDefinition4 in optionDefinitionDatabase)
			{
				if (optionDefinition4.Category == this.category && !this.OptionDefinitions.ContainsKey(optionDefinition4.Name))
				{
					this.OptionDefinitions.Add(optionDefinition4.Name, optionDefinition4);
				}
			}
		}
		else if (this.category == "World")
		{
			IDatabase<WorldGeneratorOptionDefinition> worldGeneratorOptionDefinitionDatabase = Databases.GetDatabase<WorldGeneratorOptionDefinition>(true);
			foreach (OptionDefinition optionDefinition2 in worldGeneratorOptionDefinitionDatabase)
			{
				if (optionDefinition2.Category == this.category && !this.OptionDefinitions.ContainsKey(optionDefinition2.Name))
				{
					this.OptionDefinitions.Add(optionDefinition2.Name, optionDefinition2);
				}
			}
			IDatabase<OptionDefinition> optionDefinitionDatabase2 = Databases.GetDatabase<OptionDefinition>(true);
			foreach (OptionDefinition optionDefinition3 in optionDefinitionDatabase2)
			{
				if (optionDefinition3.Category == "Game" && !this.OptionDefinitions.ContainsKey(optionDefinition3.Name))
				{
					this.OptionDefinitions.Add(optionDefinition3.Name, optionDefinition3);
				}
			}
		}
		if (this.OptionDefinitions != null)
		{
			this.filteredOptionDefinitions = from optionDefinition in this.OptionDefinitions.Values
			where optionDefinition.Category == this.category && optionDefinition.IsAdvanced
			select optionDefinition;
			this.BuildOptionValuesByNames();
			this.BuildSettingsBySubCategory();
			this.SettingsGroupsContainer.DestroyAllChildren();
			int groupIndex = 0;
			float offsetX = this.SettingsGroupsContainer.HorizontalMargin;
			foreach (KeyValuePair<string, List<OptionDefinition>> kvp in this.settingsBySubCategory)
			{
				string settingsGroupName = "SettingsGroup" + kvp.Key;
				AgeTransform settingsGroupTransform = this.SettingsGroupsContainer.InstanciateChild(this.SettingsGroupPrefab, settingsGroupName);
				Diagnostics.Assert(settingsGroupTransform != null, "Failed to instanciate the {0} with prefab {1}", new object[]
				{
					settingsGroupName,
					this.SettingsGroupPrefab.name
				});
				settingsGroupTransform.X = offsetX;
				offsetX += settingsGroupTransform.Width + this.SettingsGroupsContainer.HorizontalSpacing;
				this.setupAdvancedOptionsDelegate(settingsGroupTransform, kvp, groupIndex);
				groupIndex++;
			}
		}
		else
		{
			this.filteredOptionDefinitions = null;
			this.BuildOptionValuesByNames();
			this.BuildSettingsBySubCategory();
			this.SettingsGroupsContainer.DestroyAllChildren();
		}
		this.AdvancedDataChanged = false;
		base.NeedRefresh = true;
		this.RefreshButtons();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.Session = null;
		this.OptionDefinitions = null;
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.setupAdvancedOptionsDelegate = new AgeTransform.RefreshTableItem<KeyValuePair<string, List<OptionDefinition>>>(this.SetupAdvancedOptions);
		yield break;
	}

	protected override void OnUnload()
	{
		this.setupAdvancedOptionsDelegate = null;
		base.OnUnload();
	}

	private void BuildOptionValuesByNames()
	{
		if (this.filteredOptionDefinitions == null)
		{
			return;
		}
		this.optionValuesByName.Clear();
		Diagnostics.Assert(this.OptionDefinitions != null);
		foreach (OptionDefinition optionDefinition in this.filteredOptionDefinitions)
		{
			object lobbyData = this.Session.GetLobbyData(optionDefinition.Name);
			string value = (lobbyData == null) ? string.Empty : lobbyData.ToString();
			this.optionValuesByName.Add(optionDefinition.Name, value);
			if (!this.optionValuesByName.ContainsKey(optionDefinition.SubCategory) && this.OptionDefinitions.ContainsKey(optionDefinition.SubCategory))
			{
				value = this.Session.GetLobbyData(optionDefinition.SubCategory).ToString();
				this.optionValuesByName.Add(optionDefinition.SubCategory, value);
				OptionDefinition optionDefinition2;
				if (this.OptionDefinitions.TryGetValue(optionDefinition.SubCategory, out optionDefinition2) && optionDefinition2.ItemDefinitions != null)
				{
					foreach (OptionDefinition.ItemDefinition itemDefinition in optionDefinition2.ItemDefinitions)
					{
						if (itemDefinition.OptionDefinitionConstraints != null)
						{
							foreach (OptionDefinitionConstraint optionDefinitionConstraint in from iterator in itemDefinition.OptionDefinitionConstraints
							where iterator.Type == OptionDefinitionConstraintType.Conditional
							select iterator)
							{
								if (!this.optionValuesByName.ContainsKey(optionDefinitionConstraint.OptionName) && this.OptionDefinitions.ContainsKey(optionDefinitionConstraint.OptionName))
								{
									value = this.Session.GetLobbyData(optionDefinitionConstraint.OptionName).ToString();
									this.optionValuesByName.Add(optionDefinitionConstraint.OptionName, value);
								}
							}
						}
					}
				}
			}
			if (optionDefinition.ItemDefinitions != null)
			{
				foreach (OptionDefinition.ItemDefinition itemDefinition2 in optionDefinition.ItemDefinitions)
				{
					if (itemDefinition2.OptionDefinitionConstraints != null)
					{
						foreach (OptionDefinitionConstraint optionDefinitionConstraint2 in from iterator in itemDefinition2.OptionDefinitionConstraints
						where iterator.Type == OptionDefinitionConstraintType.Conditional
						select iterator)
						{
							if (!this.optionValuesByName.ContainsKey(optionDefinitionConstraint2.OptionName) && this.OptionDefinitions.ContainsKey(optionDefinitionConstraint2.OptionName))
							{
								value = this.Session.GetLobbyData(optionDefinitionConstraint2.OptionName).ToString();
								this.optionValuesByName.Add(optionDefinitionConstraint2.OptionName, value);
							}
						}
					}
				}
			}
		}
	}

	private void BuildSettingsBySubCategory()
	{
		if (this.filteredOptionDefinitions == null)
		{
			return;
		}
		this.settingsBySubCategory.Clear();
		foreach (OptionDefinition optionDefinition in this.filteredOptionDefinitions)
		{
			string subCategory = optionDefinition.SubCategory;
			if (!string.IsNullOrEmpty(subCategory))
			{
				if (!this.settingsBySubCategory.ContainsKey(subCategory))
				{
					this.settingsBySubCategory.Add(subCategory, new List<OptionDefinition>());
				}
				this.settingsBySubCategory[subCategory].Add(optionDefinition);
			}
		}
	}

	private void RefreshButtons()
	{
		this.ApplyButton.AgeTransform.Enable = !this.readOnly;
		this.DefaultButton.AgeTransform.Enable = !this.readOnly;
	}

	private void OnApplyCB(GameObject obj)
	{
		if (this.Session != null && this.Session.IsOpened && this.optionValuesByName != null)
		{
			foreach (KeyValuePair<string, string> keyValuePair in this.optionValuesByName)
			{
				this.Session.SetLobbyData(keyValuePair.Key, keyValuePair.Value, true);
				OptionDefinition optionDefinition;
				if (this.OptionDefinitions != null && this.OptionDefinitions.TryGetValue(keyValuePair.Key, out optionDefinition))
				{
					Amplitude.Unity.Framework.Application.Registry.SetValue(optionDefinition.RegistryPath, keyValuePair.Value);
				}
			}
		}
		this.Hide(false);
		base.GuiService.GetGuiPanel<MenuNewGameScreen>().Show(new object[0]);
	}

	private void OnChangeStandardOption(object[] optionAndItem)
	{
		Diagnostics.Assert(optionAndItem.Length == 2);
		OptionDefinition optionDefinition = optionAndItem[0] as OptionDefinition;
		Diagnostics.Assert(optionDefinition != null);
		OptionDefinition.ItemDefinition itemDefinition = optionAndItem[1] as OptionDefinition.ItemDefinition;
		Diagnostics.Assert(itemDefinition != null);
		if (itemDefinition.Name != this.optionValuesByName[optionDefinition.Name])
		{
			this.AdvancedDataChanged = true;
			this.optionValuesByName[optionDefinition.Name] = itemDefinition.Name;
			if (itemDefinition.OptionDefinitionConstraints != null)
			{
				foreach (OptionDefinitionConstraint optionDefinitionConstraint in from element in itemDefinition.OptionDefinitionConstraints
				where element.Type == OptionDefinitionConstraintType.Control
				select element)
				{
					if (optionDefinitionConstraint.Keys != null && optionDefinitionConstraint.Keys.Length != 0)
					{
						string value;
						if (this.optionValuesByName.TryGetValue(optionDefinitionConstraint.OptionName, out value))
						{
							if (!optionDefinitionConstraint.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(value))
							{
								this.optionValuesByName[optionDefinitionConstraint.OptionName] = optionDefinitionConstraint.Keys[0].Name;
							}
						}
					}
				}
			}
			Diagnostics.Assert(this.OptionDefinitions != null);
			OptionDefinition optionDefinition2;
			if (this.OptionDefinitions.TryGetValue(optionDefinition.SubCategory, out optionDefinition2) && optionDefinition2.Name != optionDefinition.Name && optionDefinition2.ItemDefinitions != null)
			{
				if ((from item in optionDefinition2.ItemDefinitions
				select item.Name).Contains("Custom") && this.optionValuesByName.ContainsKey(optionDefinition.SubCategory))
				{
					this.optionValuesByName[optionDefinition.SubCategory] = "Custom";
				}
			}
			base.NeedRefresh = true;
		}
	}

	private void OnDefaultCB(GameObject obj)
	{
		if (this.OptionDefinitions != null)
		{
			List<string> list = this.optionValuesByName.Keys.ToList<string>();
			foreach (string text in list)
			{
				OptionDefinition optionDefinition;
				if (this.OptionDefinitions.TryGetValue(text, out optionDefinition))
				{
					this.optionValuesByName[text] = optionDefinition.DefaultName;
				}
			}
			this.AdvancedDataChanged = true;
			base.NeedRefresh = true;
		}
	}

	private void OnCancelCB(GameObject obj)
	{
		this.Hide(false);
		base.GuiService.GetGuiPanel<MenuNewGameScreen>().Show(new object[0]);
	}

	private void Session_LobbyDataChange(object sender, LobbyDataChangeEventArgs e)
	{
		string a;
		if (this.optionValuesByName.TryGetValue(e.Key, out a) && a != e.Data.ToString())
		{
			this.optionValuesByName[e.Key] = e.Data.ToString();
			base.NeedRefresh = true;
		}
	}

	private void SetupAdvancedOptions(AgeTransform tableItem, KeyValuePair<string, List<OptionDefinition>> kvp, int tableIndex)
	{
		MenuSettingsGroup component = tableItem.GetComponent<MenuSettingsGroup>();
		component.SetContent(kvp.Key, kvp.Value, base.gameObject, this.OptionDefinitions);
	}

	private IEnumerator HandleCancelRequestWhenShowingFinished()
	{
		while (base.IsShowing)
		{
			yield return null;
		}
		this.HandleCancelRequest();
		yield break;
	}

	public static Faction SelectedFaction;

	public AgeControlButton ApplyButton;

	public AgeControlButton DefaultButton;

	public AgeControlButton CancelButton;

	public AgeTransform SettingsGroupsContainer;

	public Transform SettingsGroupPrefab;

	private IEnumerable<OptionDefinition> filteredOptionDefinitions;

	private Dictionary<string, List<OptionDefinition>> settingsBySubCategory = new Dictionary<string, List<OptionDefinition>>();

	private Dictionary<string, string> optionValuesByName = new Dictionary<string, string>();

	private string category;

	private global::Session session;

	private bool readOnly;

	private AgeTransform.RefreshTableItem<KeyValuePair<string, List<OptionDefinition>>> setupAdvancedOptionsDelegate;
}
