using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Achievement;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using UnityEngine;

[Diagnostics.TagAttribute("DownloadableContent")]
public class DownloadableContentManager : Manager, IService, IEnumerable, IDownloadableContentService, IEnumerable<DownloadableContent>
{
	DownloadableContentAccessibility IDownloadableContentService.AddAccessibility(StaticString name, DownloadableContentAccessibility flags)
	{
		if (StaticString.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		DownloadableContent downloadableContent;
		if (this.DownloadableContents.TryGetValue(name, out downloadableContent))
		{
			downloadableContent.Accessibility |= flags;
			return downloadableContent.Accessibility;
		}
		return DownloadableContentAccessibility.None;
	}

	DownloadableContentAccessibility IDownloadableContentService.GetAccessibility(StaticString name)
	{
		if (StaticString.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		DownloadableContent downloadableContent;
		if (this.DownloadableContents.TryGetValue(name, out downloadableContent))
		{
			return downloadableContent.Accessibility;
		}
		return DownloadableContentAccessibility.None;
	}

	IEnumerator<DownloadableContent> IEnumerable<DownloadableContent>.GetEnumerator()
	{
		foreach (KeyValuePair<StaticString, DownloadableContent> keyValuePair in this.DownloadableContents)
		{
			yield return keyValuePair.Value;
		}
		yield break;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.DownloadableContents.Values.GetEnumerator();
	}

	bool IDownloadableContentService.IsInstalled(StaticString name)
	{
		if (StaticString.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		DownloadableContent downloadableContent;
		return this.DownloadableContents.TryGetValue(name, out downloadableContent) && downloadableContent.IsInstalled;
	}

	bool IDownloadableContentService.IsShared(StaticString name)
	{
		if (StaticString.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		DownloadableContent downloadableContent;
		return this.DownloadableContents.TryGetValue(name, out downloadableContent) && (downloadableContent.Accessibility & DownloadableContentAccessibility.Shared) == DownloadableContentAccessibility.Shared;
	}

	bool IDownloadableContentService.IsSubscribed(StaticString name)
	{
		if (StaticString.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		DownloadableContent downloadableContent;
		return this.DownloadableContents.TryGetValue(name, out downloadableContent) && downloadableContent.IsSubscribed;
	}

	DownloadableContentAccessibility IDownloadableContentService.RemoveAccessibility(StaticString name, DownloadableContentAccessibility flags)
	{
		if (StaticString.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		DownloadableContent downloadableContent;
		if (this.DownloadableContents.TryGetValue(name, out downloadableContent))
		{
			downloadableContent.Accessibility &= ~flags;
			return downloadableContent.Accessibility;
		}
		return DownloadableContentAccessibility.None;
	}

	bool IDownloadableContentService.TryGetValue(StaticString name, out DownloadableContent downloadableContent)
	{
		return this.DownloadableContents.TryGetValue(name, out downloadableContent);
	}

	public static IDownloadableContentService DownloadableContentService { get; private set; }

	private Dictionary<StaticString, DownloadableContent> DownloadableContents { get; set; }

	public override IEnumerator BindServices()
	{
		yield return base.BindServices();
		yield return base.BindService<IAchievementService>();
		this.DownloadableContents = new Dictionary<StaticString, DownloadableContent>();
		this.Register<DownloadableContent1>();
		this.Register<DownloadableContent2>();
		this.Register<DownloadableContent3>();
		this.Register<DownloadableContent4>();
		this.Register<DownloadableContent5>();
		this.Register<DownloadableContent7>();
		this.Register<DownloadableContent8>();
		this.Register<DownloadableContent9>();
		this.Register<DownloadableContent10>();
		this.Register<DownloadableContent11>();
		this.Register<DownloadableContent12>();
		this.Register<DownloadableContent13>();
		this.Register<DownloadableContent14>();
		this.Register<DownloadableContent15>();
		this.Register<DownloadableContent16>();
		this.Register<DownloadableContent17>();
		this.Register<DownloadableContent18>();
		this.Register<DownloadableContent19>();
		this.Register<DownloadableContent20>();
		this.Register<DownloadableContent21>();
		string[] descriptions = (from downloadableContent in this.DownloadableContents.Values
		where (downloadableContent.Accessibility & DownloadableContentAccessibility.Subscribed) != DownloadableContentAccessibility.None
		select downloadableContent.Description).ToArray<string>();
		if (descriptions.Length == 0)
		{
			Diagnostics.Log("No downloadable content was found.");
		}
		else
		{
			Diagnostics.Log("Playing with {0} downloadable content(s): {1}.", new object[]
			{
				descriptions.Length,
				string.Join(", ", descriptions)
			});
		}
		this.GetCommandLineOverrides();
		descriptions = (from downloadableContent in this.DownloadableContents.Values
		where (downloadableContent.Accessibility & DownloadableContentAccessibility.Subscribed) != DownloadableContentAccessibility.None
		where downloadableContent.IsDynamicActivationEnabled && (downloadableContent.Accessibility & DownloadableContentAccessibility.Activated) == DownloadableContentAccessibility.Activated
		select downloadableContent.Description).ToArray<string>();
		if (descriptions.Length != 0)
		{
			Diagnostics.Log("Playing with {0} activated downloadable content(s): {1}.", new object[]
			{
				descriptions.Length,
				string.Join(", ", descriptions)
			});
		}
		bool shallSupportOnLoadDynamicTexture = this.DownloadableContents.Values.Any(delegate(DownloadableContent downloadableContent)
		{
			bool result;
			if (downloadableContent.Restrictions != null)
			{
				result = downloadableContent.Restrictions.Any((DownloadableContentRestriction restriction) => restriction.Category == DownloadableContentRestrictionCategory.Bitmap);
			}
			else
			{
				result = false;
			}
			return result;
		});
		if (shallSupportOnLoadDynamicTexture)
		{
			Diagnostics.Log("Dynamic textures support required.");
			yield return base.BindService<Amplitude.Unity.Gui.IGuiService>(delegate(Amplitude.Unity.Gui.IGuiService service)
			{
				AgeManager.Instance.OnLoadDynamicTexture += this.OnLoadDynamicTexture;
			});
		}
		Services.AddService<IDownloadableContentService>(this);
		DownloadableContentManager.DownloadableContentService = this;
		yield break;
	}

	public bool TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory category, string contentId, out bool result)
	{
		result = true;
		if (category == DownloadableContentRestrictionCategory.Undefined)
		{
			return false;
		}
		if (string.IsNullOrEmpty(contentId))
		{
			return false;
		}
		bool flag = true;
		foreach (DownloadableContent downloadableContent in this.DownloadableContents.Values)
		{
			string text;
			flag &= downloadableContent.TryCheckAgainstRestrictions(category, contentId, out result, out text);
			if (!flag || !result)
			{
				return flag;
			}
		}
		return flag;
	}

	public bool TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory category, string contentId, out bool result, out string replacement)
	{
		result = true;
		replacement = string.Empty;
		if (category == DownloadableContentRestrictionCategory.Undefined)
		{
			return false;
		}
		if (string.IsNullOrEmpty(contentId))
		{
			return false;
		}
		bool result2 = false;
		string empty = string.Empty;
		string text = string.Empty;
		bool flag = false;
		bool flag2 = false;
		foreach (DownloadableContent downloadableContent in this.DownloadableContents.Values)
		{
			bool flag3;
			downloadableContent.TryCheckAgainstRestrictions(category, contentId, out flag3, out flag2, out empty);
			if (flag2)
			{
				result2 = true;
				flag = flag3;
				if (flag3)
				{
					break;
				}
				text = empty;
			}
		}
		replacement = text;
		result = flag;
		return result2;
	}

	protected override void Releasing()
	{
		base.Releasing();
		DownloadableContentManager.DownloadableContentService = null;
	}

	private void GetCommandLineOverrides()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string text = commandLineArgs[i];
			if (!string.IsNullOrEmpty(text))
			{
				bool flag = false;
				if (text.Equals("+dlc", StringComparison.InvariantCultureIgnoreCase))
				{
					flag = true;
				}
				else if (!text.Equals("-dlc", StringComparison.InvariantCultureIgnoreCase))
				{
					goto IL_14C;
				}
				for (i++; i < commandLineArgs.Length; i++)
				{
					string downloadableContentId = commandLineArgs[i];
					if (!string.IsNullOrEmpty(downloadableContentId))
					{
						if (!char.IsLetterOrDigit(downloadableContentId[0]))
						{
							break;
						}
						DownloadableContent downloadableContent;
						uint number;
						if (uint.TryParse(downloadableContentId, out number))
						{
							downloadableContent = (from dlc in this.DownloadableContents.Values
							where dlc.Number == number
							select dlc).FirstOrDefault<DownloadableContent>();
						}
						else
						{
							downloadableContent = (from dlc in this.DownloadableContents.Values
							where dlc.Name == downloadableContentId
							select dlc).FirstOrDefault<DownloadableContent>();
						}
						if (downloadableContent != null && downloadableContent.IsDynamicActivationEnabled)
						{
							if (flag)
							{
								downloadableContent.Accessibility |= DownloadableContentAccessibility.Activated;
							}
							else
							{
								downloadableContent.Accessibility &= ~DownloadableContentAccessibility.Activated;
							}
						}
					}
				}
			}
			IL_14C:;
		}
	}

	private bool OnLoadDynamicTexture(string path, out Texture2D texture)
	{
		bool flag;
		string text;
		if (this.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.Bitmap, path, out flag, out text) && !flag)
		{
			if (!string.IsNullOrEmpty(text))
			{
				texture = Resources.Load<Texture2D>(text);
			}
			else
			{
				texture = null;
			}
			return true;
		}
		texture = null;
		return false;
	}

	private void Register<T>() where T : DownloadableContent
	{
		try
		{
			DownloadableContent downloadableContent = Activator.CreateInstance(typeof(T)) as DownloadableContent;
			if (downloadableContent != null && !this.DownloadableContents.ContainsKey(downloadableContent.Name))
			{
				downloadableContent.Accessibility = DownloadableContentAccessibility.None;
				if (downloadableContent.IsSubscribed)
				{
					downloadableContent.Accessibility |= DownloadableContentAccessibility.Subscribed;
					if (downloadableContent.IsInstalled)
					{
						downloadableContent.Accessibility |= DownloadableContentAccessibility.Installed;
						if (downloadableContent.IsDynamicActivationEnabled)
						{
							string x = string.Format("Preferences/DownloadableContents/DownloadableContent{0}/Activated", downloadableContent.Number);
							bool value = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(x, true);
							if (value)
							{
								downloadableContent.Accessibility |= DownloadableContentAccessibility.Activated;
							}
						}
						else
						{
							downloadableContent.Accessibility |= DownloadableContentAccessibility.Activated;
						}
					}
				}
				this.DownloadableContents.Add(downloadableContent.Name, downloadableContent);
			}
		}
		catch
		{
		}
	}
}
