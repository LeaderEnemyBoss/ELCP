using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;
using UnityEngine;

public class MenuSetting : MonoBehaviour
{
	public OptionDefinition OptionDefinition { get; private set; }

	public void InitializeContent(OptionDefinition optionDefinition, GameObject client)
	{
		this.OptionDefinition = optionDefinition;
		this.client = client;
		this.Unavailable.Visible = false;
		string str = "%" + optionDefinition.GetType().ToString().Replace("Definition", string.Empty);
		this.Title.Text = str + optionDefinition.Name + "Title";
		AgeTooltip ageTooltip = this.Title.AgeTransform.AgeTooltip;
		if (ageTooltip != null)
		{
			ageTooltip.Content = str + optionDefinition.Name + "Description";
		}
		if (this.OptionDefinition.GuiControlType == MenuSetting.GuiControlTypeToggle)
		{
			Diagnostics.Assert(this.OptionDefinition.ItemDefinitions.Length == 2);
			Diagnostics.Assert(this.OptionDefinition.ItemDefinitions[0].Name == "False");
			Diagnostics.Assert(this.OptionDefinition.ItemDefinitions[1].Name == "True");
			this.DropList.AgeTransform.Visible = false;
			this.Toggle.AgeTransform.Visible = true;
			this.TextField.AgeTransform.Visible = false;
		}
		else if (this.OptionDefinition.GuiControlType == MenuSetting.GuiControlTypeDropList)
		{
			Diagnostics.Assert(this.OptionDefinition.ItemDefinitions.Length > 0);
			this.DropList.AgeTransform.Visible = true;
			this.Toggle.AgeTransform.Visible = false;
			this.TextField.AgeTransform.Visible = false;
			string[] array = new string[optionDefinition.ItemDefinitions.Length];
			string[] array2 = new string[optionDefinition.ItemDefinitions.Length];
			for (int i = 0; i < optionDefinition.ItemDefinitions.Length; i++)
			{
				array[i] = str + optionDefinition.Name + optionDefinition.ItemDefinitions[i].Name + "Title";
				array2[i] = str + optionDefinition.Name + optionDefinition.ItemDefinitions[i].Name + "Description";
			}
			this.DropList.ItemTable = array;
			this.DropList.TooltipTable = array2;
		}
		else if (this.OptionDefinition.GuiControlType == MenuSetting.GuiControlTypeTextField)
		{
			Diagnostics.Assert(this.OptionDefinition.ItemDefinitions.Length == 1);
			Diagnostics.Assert(this.OptionDefinition.ItemDefinitions[0].KeyValuePairs.Length == 1);
			this.DropList.AgeTransform.Visible = false;
			this.Toggle.AgeTransform.Visible = false;
			this.TextField.AgeTransform.Visible = true;
			if (!string.IsNullOrEmpty(this.OptionDefinition.ValidChars))
			{
				this.TextField.ValidChars = this.OptionDefinition.ValidChars;
			}
			if (this.OptionDefinition.TextMaxLength > 0)
			{
				this.TextField.TextMaxLength = this.OptionDefinition.TextMaxLength;
			}
			string value = this.OptionDefinition.ItemDefinitions[0].KeyValuePairs[0].Value;
			this.TextField.ReplaceInputText(value);
		}
	}

	public void InitializeSimple(StaticString subCategory)
	{
		this.OptionDefinition = null;
		this.client = null;
		this.DropList.AgeTransform.Visible = false;
		this.Toggle.AgeTransform.Visible = false;
		this.TextField.AgeTransform.Visible = false;
		this.Unavailable.Visible = false;
		this.Title.Text = "%" + subCategory + "Title";
		AgeTooltip ageTooltip = this.Title.AgeTransform.AgeTooltip;
		if (ageTooltip != null)
		{
			ageTooltip.Content = "%" + subCategory + "Description";
		}
	}

	public void RefreshContent(global::Session session, bool readOnly)
	{
		if (this.OptionDefinition == null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(this.OptionDefinition.EnableOn))
		{
			string a = session.GetLobbyData(this.OptionDefinition.EnableOn) as string;
			this.AgeTransform.Enable = (a == "True");
		}
		if (!string.IsNullOrEmpty(this.OptionDefinition.SubCategory) && this.OptionDefinition.SubCategory != this.OptionDefinition.Name)
		{
			string lobbyData = session.GetLobbyData<string>(this.OptionDefinition.SubCategory, null);
			if (lobbyData == "Random")
			{
				this.SetAvailable(false);
				return;
			}
		}
		this.SetAvailable(true);
		string lobbyData2 = session.GetLobbyData<string>(this.OptionDefinition.Name, null);
		if (this.OptionDefinition.GuiControlType == MenuSetting.GuiControlTypeToggle)
		{
			this.RefreshToggle(lobbyData2, readOnly);
		}
		else if (this.OptionDefinition.GuiControlType == MenuSetting.GuiControlTypeDropList)
		{
			this.RefreshDropList(lobbyData2, readOnly);
			if (this.OptionDefinition.ItemDefinitions != null)
			{
				for (int i = 0; i < this.OptionDefinition.ItemDefinitions.Length; i++)
				{
					OptionDefinition.ItemDefinition itemDefinition = this.OptionDefinition.ItemDefinitions[i];
					if (itemDefinition.OptionDefinitionConstraints != null)
					{
						bool flag = true;
						foreach (OptionDefinitionConstraint optionDefinitionConstraint in from element in itemDefinition.OptionDefinitionConstraints
						where element.Type == OptionDefinitionConstraintType.Conditional
						select element)
						{
							if (string.IsNullOrEmpty(optionDefinitionConstraint.OptionName))
							{
								flag = false;
								break;
							}
							string lobbyData3 = session.GetLobbyData<string>(optionDefinitionConstraint.OptionName, null);
							if (string.IsNullOrEmpty(lobbyData3))
							{
								Diagnostics.LogWarning("Unhandled constraint on option '{0}' from option definition '{1}', item '{2}'.", new object[]
								{
									optionDefinitionConstraint.OptionName,
									this.OptionDefinition.Name,
									itemDefinition.Name
								});
							}
							else
							{
								if (optionDefinitionConstraint.Keys == null || optionDefinitionConstraint.Keys.Length == 0)
								{
									flag = false;
									break;
								}
								if (!optionDefinitionConstraint.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(lobbyData3))
								{
									flag = false;
									break;
								}
							}
						}
						if (flag)
						{
							IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
							if (service != null)
							{
								foreach (OptionDefinitionConstraint optionDefinitionConstraint2 in from element in itemDefinition.OptionDefinitionConstraints
								where element.Type == OptionDefinitionConstraintType.DownloadableContentConditional
								select element)
								{
									if (string.IsNullOrEmpty(optionDefinitionConstraint2.OptionName))
									{
										flag = false;
										break;
									}
									if (!service.IsShared(optionDefinitionConstraint2.OptionName))
									{
										flag = false;
										break;
									}
								}
							}
						}
						this.DropList.EnableItem(i, flag);
					}
				}
			}
		}
		else if (this.OptionDefinition.GuiControlType == MenuSetting.GuiControlTypeTextField)
		{
			this.RefreshTextField(lobbyData2, readOnly);
		}
	}

	public void RefreshContent(Dictionary<string, string> optionValuesByName, bool readOnly)
	{
		if (this.OptionDefinition == null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(this.OptionDefinition.EnableOn))
		{
			string empty = string.Empty;
			if (optionValuesByName.TryGetValue(this.OptionDefinition.EnableOn, out empty))
			{
				this.AgeTransform.Enable = (empty == "True");
			}
		}
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null && this.OptionDefinition.OptionDefinitionConstraints != null)
		{
			foreach (OptionDefinitionConstraint optionDefinitionConstraint in from element in this.OptionDefinition.OptionDefinitionConstraints
			where element.Type == OptionDefinitionConstraintType.DownloadableContentConditional
			select element)
			{
				if (string.IsNullOrEmpty(optionDefinitionConstraint.OptionName))
				{
					this.AgeTransform.Enable = false;
					break;
				}
				if (!service.IsShared(optionDefinitionConstraint.OptionName))
				{
					this.AgeTransform.Enable = false;
					this.Title.AgeTransform.AgeTooltip.Content = "%RestrictedDownloadableContentTitle";
					break;
				}
			}
			ISessionService service2 = Services.GetService<ISessionService>();
			if (service2 != null && service2.Session != null)
			{
				foreach (OptionDefinitionConstraint optionDefinitionConstraint2 in from element in this.OptionDefinition.OptionDefinitionConstraints
				where element.Type == OptionDefinitionConstraintType.Conditional
				select element)
				{
					if (!string.IsNullOrEmpty(optionDefinitionConstraint2.OptionName) && optionDefinitionConstraint2.OptionName == "Multiplayer" && service2.Session.SessionMode == SessionMode.Single)
					{
						this.AgeTransform.Enable = false;
						this.Title.AgeTransform.AgeTooltip.Content = "%MultiplayerOnlyTitle";
						break;
					}
				}
			}
		}
		string value;
		if (!optionValuesByName.TryGetValue(this.OptionDefinition.Name, out value))
		{
			this.AgeTransform.Visible = false;
			return;
		}
		this.AgeTransform.Visible = true;
		string a;
		if (!string.IsNullOrEmpty(this.OptionDefinition.SubCategory) && this.OptionDefinition.SubCategory != this.OptionDefinition.Name && optionValuesByName.TryGetValue(this.OptionDefinition.SubCategory, out a) && a == "Random")
		{
			this.SetAvailable(false);
			return;
		}
		this.SetAvailable(true);
		if (this.OptionDefinition.GuiControlType == MenuSetting.GuiControlTypeToggle)
		{
			this.RefreshToggle(value, readOnly);
			return;
		}
		if (this.OptionDefinition.GuiControlType == MenuSetting.GuiControlTypeDropList)
		{
			this.RefreshDropList(value, readOnly);
			if (this.OptionDefinition.ItemDefinitions != null)
			{
				for (int i = 0; i < this.OptionDefinition.ItemDefinitions.Length; i++)
				{
					OptionDefinition.ItemDefinition itemDefinition = this.OptionDefinition.ItemDefinitions[i];
					if (itemDefinition.OptionDefinitionConstraints != null)
					{
						bool flag = true;
						foreach (OptionDefinitionConstraint optionDefinitionConstraint3 in from element in itemDefinition.OptionDefinitionConstraints
						where element.Type == OptionDefinitionConstraintType.Conditional
						select element)
						{
							if (string.IsNullOrEmpty(optionDefinitionConstraint3.OptionName))
							{
								flag = false;
								break;
							}
							string text;
							if (!optionValuesByName.TryGetValue(optionDefinitionConstraint3.OptionName, out text))
							{
								Diagnostics.LogWarning("Unhandled constraint on option '{0}' from option definition '{1}', item '{2}'.", new object[]
								{
									optionDefinitionConstraint3.OptionName,
									this.OptionDefinition.Name,
									itemDefinition.Name
								});
							}
							else if (string.IsNullOrEmpty(text))
							{
								Diagnostics.LogWarning("Unhandled constraint on option '{0}' from option definition '{1}', item '{2}'.", new object[]
								{
									optionDefinitionConstraint3.OptionName,
									this.OptionDefinition.Name,
									itemDefinition.Name
								});
							}
							else
							{
								if (optionDefinitionConstraint3.Keys == null || optionDefinitionConstraint3.Keys.Length == 0)
								{
									flag = false;
									break;
								}
								if (!optionDefinitionConstraint3.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(text))
								{
									flag = false;
									break;
								}
							}
						}
						if (flag && service != null)
						{
							foreach (OptionDefinitionConstraint optionDefinitionConstraint4 in from element in itemDefinition.OptionDefinitionConstraints
							where element.Type == OptionDefinitionConstraintType.DownloadableContentConditional
							select element)
							{
								if (string.IsNullOrEmpty(optionDefinitionConstraint4.OptionName))
								{
									flag = false;
									break;
								}
								if (!service.IsShared(optionDefinitionConstraint4.OptionName))
								{
									flag = false;
									break;
								}
							}
						}
						this.DropList.EnableItem(i, flag);
					}
				}
				return;
			}
		}
		else if (this.OptionDefinition.GuiControlType == MenuSetting.GuiControlTypeTextField)
		{
			this.RefreshTextField(value, readOnly);
		}
	}

	public void OnDependencyChanged(OptionDefinition optionDefinition, OptionDefinition.ItemDefinition optionItemDefinition)
	{
		Diagnostics.Assert(optionDefinition.Name == this.OptionDefinition.EnableOn);
		this.AgeTransform.Enable = (optionItemDefinition.Name == "True");
	}

	public void SetAvailable(bool available)
	{
		this.Unavailable.Visible = !available;
	}

	private void RefreshToggle(string value, bool readOnly)
	{
		this.Toggle.State = (value == "True");
		this.Toggle.AgeTransform.Enable = !readOnly;
	}

	private void RefreshDropList(string value, bool readOnly)
	{
		for (int i = 0; i < this.OptionDefinition.ItemDefinitions.Length; i++)
		{
			if (this.OptionDefinition.ItemDefinitions[i].Name == value)
			{
				this.DropList.SelectedItem = i;
				break;
			}
		}
		this.DropList.ReadOnly = readOnly;
	}

	private void RefreshTextField(string value, bool readOnly)
	{
		this.TextField.AgeTransform.Enable = !readOnly;
	}

	private void OnSelectOptionItemCB(GameObject obj)
	{
		OptionDefinition.ItemDefinition itemDefinition = this.OptionDefinition.ItemDefinitions[this.DropList.SelectedItem];
		object[] value = new object[]
		{
			this.OptionDefinition,
			itemDefinition
		};
		this.client.SendMessage("OnChangeStandardOption", value);
	}

	private void OnToggleOptionItemCB(GameObject obj)
	{
		string value = (!this.Toggle.State) ? "False" : "True";
		OptionDefinition.ItemDefinition itemDefinition2 = this.OptionDefinition.ItemDefinitions.First((OptionDefinition.ItemDefinition itemDefinition) => itemDefinition.Name == value);
		object[] value2 = new object[]
		{
			this.OptionDefinition,
			itemDefinition2
		};
		this.client.SendMessage("OnChangeStandardOption", value2);
	}

	private void OnTextFieldValidatedCB(GameObject obj)
	{
		AgeManager.Instance.FocusedControl = null;
	}

	private void OnTextFieldFocusLostCB(GameObject obj)
	{
		if (this.OptionDefinition == null)
		{
			return;
		}
		if (this.OptionDefinition.GuiControlType != MenuSetting.GuiControlTypeTextField)
		{
			return;
		}
		string text = this.TextField.AgePrimitiveLabel.Text;
		if (string.IsNullOrEmpty(text))
		{
			text = "0";
			this.TextField.ReplaceInputText("0");
		}
		int num;
		if (!int.TryParse(text, out num))
		{
			text = "0";
			this.TextField.ReplaceInputText("0");
		}
		OptionDefinition.ItemDefinition itemDefinition = this.OptionDefinition.ItemDefinitions[0];
		itemDefinition.KeyValuePairs[0].Value = text;
		object[] value = new object[]
		{
			this.OptionDefinition,
			itemDefinition
		};
		this.client.SendMessage("OnChangeStandardOption", value);
		if (this.OptionDefinition.Name == "SeedNumber")
		{
			Amplitude.Unity.Framework.Application.Registry.SetValue("Preferences/Lobby/Seed", text);
			return;
		}
		throw new NotImplementedException();
	}

	public static readonly StaticString GuiControlTypeDropList = new StaticString("DropList");

	public static readonly StaticString GuiControlTypeTextField = new StaticString("TextField");

	public static readonly StaticString GuiControlTypeToggle = new StaticString("Toggle");

	public AgeTransform AgeTransform;

	public AgePrimitiveLabel Title;

	public AgeControlDropList DropList;

	public AgeControlToggle Toggle;

	public AgeControlTextField TextField;

	public AgeTransform Unavailable;

	private GameObject client;
}
