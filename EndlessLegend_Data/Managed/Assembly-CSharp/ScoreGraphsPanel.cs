using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class ScoreGraphsPanel : GuiPanel
{
	public ScoreGraphsPanel()
	{
		this.visibleEmpires = new List<int>();
	}

	public GameScoreDefinition CurrentScoreDefinition { get; private set; }

	public global::Empire PlayerEmpire { get; set; }

	public EmpireInfo[] EmpireInfos { get; private set; }

	public bool HideUnknownEmpires { get; set; }

	public Snapshot Snapshot { get; private set; }

	public override void RefreshContent()
	{
		base.RefreshContent();
		IDatabase<GameScoreDefinition> database = Databases.GetDatabase<GameScoreDefinition>(false);
		GameScoreDefinition[] values = database.GetValues();
		Diagnostics.Assert(values != null && values.Length > 0);
		this.ScoreFiltersTable.ReserveChildren(values.Length, this.ScoreFilterTogglePrefab, "Item");
		this.ScoreFiltersTable.RefreshChildrenArray<GameScoreDefinition>(values, new AgeTransform.RefreshTableItem<GameScoreDefinition>(this.RefreshScoreFilter), true, false);
		this.CurrentScoreDefinition = values[0];
		IGameStatisticsManagementService service = Services.GetService<IGameStatisticsManagementService>();
		this.Snapshot = service.Snapshot;
		Snapshot[] snapshots = this.Snapshot.Snapshots;
		this.HorizontalGridContainer.DestroyAllChildren();
		if (snapshots.Length > 1)
		{
			float f = (float)snapshots.Length;
			float num = Mathf.Log10(f);
			float num2 = Mathf.Floor(num);
			int num3 = (int)Mathf.Pow(10f, num2);
			int num4 = -1;
			if (num - num2 < 0.7f)
			{
				num4 = num3 / 2;
			}
			for (int i = 1; i < snapshots.Length; i++)
			{
				if (i % num3 == 0 || (num4 > 0 && i % num4 == 0))
				{
					AgeTransform ageTransform = this.HorizontalGridContainer.InstanciateChild(this.ScoreHorizontalGridPrefab, "Grid" + i);
					ageTransform.X = this.HorizontalGridContainer.Width * ((float)i / (float)(snapshots.Length - 1));
					ageTransform.GetComponent<ScoreGridLabel>().GridLabel.Text = (i + 1).ToString();
				}
			}
		}
		this.OnSwitchScoreFilterCB(this.ScoreFiltersTable.GetChildren()[0].GetComponent<ScoreFilterToggle>().Toggle.gameObject);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.verticalGridValues = new List<float>();
		this.verticalGridHeights = new List<float>();
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		Diagnostics.Assert(parameters != null && parameters.Length > 0);
		this.EmpireInfos = (parameters[0] as EmpireInfo[]);
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		base.OnUnloadGame(game);
	}

	protected override void OnUnload()
	{
		this.verticalGridValues.Clear();
		this.verticalGridHeights.Clear();
		this.verticalGridValues = null;
		this.verticalGridHeights = null;
		base.OnUnload();
	}

	private void OnSwitchScoreFilterCB(GameObject gameObject)
	{
		AgeControlToggle component = gameObject.GetComponent<AgeControlToggle>();
		this.CurrentScoreDefinition = (component.OnSwitchDataObject as GameScoreDefinition);
		for (int i = 0; i < this.ScoreFiltersTable.GetChildren().Count; i++)
		{
			ScoreFilterToggle component2 = this.ScoreFiltersTable.GetChildren()[i].GetComponent<ScoreFilterToggle>();
			component2.Toggle.State = (component2.Toggle == component);
		}
		this.RefreshScores();
	}

	private void RefreshScores()
	{
		this.CalculateELCPScaling();
		this.ParticipantsScoreTable.ReserveChildren(this.EmpireInfos.Length, this.ParticipantScoreLinePrefab, "Item");
		this.ParticipantsScoreTable.RefreshChildrenArray<EmpireInfo>(this.EmpireInfos, new AgeTransform.RefreshTableItem<EmpireInfo>(this.RefreshParticipantScoreLine), true, false);
		this.ScoreHistogramContainer.ReserveChildren(this.EmpireInfos.Length, this.ScoreHistogramPrefab, "Item");
		this.ScoreHistogramContainer.RefreshChildrenArray<EmpireInfo>(this.EmpireInfos, new AgeTransform.RefreshTableItem<EmpireInfo>(this.RefreshParticipantHistogram), true, false);
		float num = float.NegativeInfinity;
		float num2 = 0f;
		for (int i = 0; i < this.ScoreHistogramContainer.GetChildren().Count; i++)
		{
			float[] values = this.ScoreHistogramContainer.GetChildren()[i].GetComponent<AgePrimitiveHistogramLinear>().Values;
			for (int j = 0; j < values.Length; j++)
			{
				if (values[j] > num)
				{
					num = values[j];
				}
				if (values[j] < num2)
				{
					num2 = values[j];
				}
			}
		}
		if (num - num2 != 0f)
		{
			for (int k = 0; k < this.ScoreHistogramContainer.GetChildren().Count; k++)
			{
				AgePrimitiveHistogramLinear component = this.ScoreHistogramContainer.GetChildren()[k].GetComponent<AgePrimitiveHistogramLinear>();
				float[] values2 = component.Values;
				for (int l = 0; l < values2.Length; l++)
				{
					values2[l] -= num2;
					values2[l] /= num - num2;
				}
				component.Values = values2;
			}
		}
		this.verticalGridValues.Clear();
		this.verticalGridHeights.Clear();
		if (num - num2 > 0f)
		{
			float num3 = Mathf.Log10(num - num2);
			float num4 = Mathf.Floor(num3);
			float num5 = Mathf.Pow(10f, num4);
			if (num3 - num4 < 0.7f && num4 > 0f)
			{
				num5 *= 0.5f;
			}
			int num6 = 0;
			if (num2 < 0f)
			{
				num6 = Mathf.CeilToInt(num2 / num5);
			}
			int num7 = 0;
			if (num > 0f)
			{
				num7 = Mathf.FloorToInt(num / num5);
			}
			float num8 = num5 * (float)num6;
			for (int m = num6; m <= num7; m++)
			{
				bool flag = false;
				for (int n = 0; n < this.verticalGridValues.Count; n++)
				{
					if (num8 == this.verticalGridValues[0])
					{
						flag = true;
						n = this.verticalGridValues.Count;
					}
				}
				if (!flag)
				{
					this.verticalGridValues.Add(num8);
					this.verticalGridHeights.Add(this.VerticalGridContainer.Height * (1f - (num8 - num2) / (num - num2)));
				}
				num8 += num5;
			}
		}
		this.VerticalGridContainer.ReserveChildren(this.verticalGridValues.Count, this.ScoreVerticalGridPrefab, "Item");
		this.VerticalGridContainer.RefreshChildrenIList<float>(this.verticalGridValues, new AgeTransform.RefreshTableItem<float>(this.RefreshVerticalGridLine), false, false);
	}

	private void RefreshScoreFilter(AgeTransform tableitem, GameScoreDefinition gameScoreDefinition, int index)
	{
		ScoreFilterToggle component = tableitem.GetComponent<ScoreFilterToggle>();
		component.RefreshContent(base.GuiService.GuiPanelHelper, gameScoreDefinition, base.gameObject);
	}

	private void RefreshParticipantScoreLine(AgeTransform tableitem, EmpireInfo empireInfo, int index)
	{
		tableitem.SetupCustomELCPScaling(this.ParticipantsScoreTableScale);
		if (this.visibleEmpires.Contains(empireInfo.EmpireIndex))
		{
			tableitem.Visible = true;
			ParticipantScoreLine component = tableitem.GetComponent<ParticipantScoreLine>();
			component.ParticipantName.AgeTransform.SetupCustomELCPScaling(this.ParticipantsScoreTableScale);
			component.ParticipantScore.AgeTransform.SetupCustomELCPScaling(this.ParticipantsScoreTableScale);
			component.ParticipantLogoBackground.TintColor = empireInfo.FactionColor;
			component.ParticipantName.Text = empireInfo.LocalizedName;
			GuiFaction guiFaction = new GuiFaction(empireInfo.Faction);
			component.ParticipantLogo.Image = guiFaction.GetImageTexture(GuiPanel.IconSize.LogoLarge, true);
			Snapshot[] snapshots = this.Snapshot.Snapshots;
			string name = string.Format("Turn #{0}", snapshots.Length - 1);
			Snapshot snapshot;
			Snapshot snapshot2;
			if (this.Snapshot.TryGetSnapshot(name, out snapshot) && snapshot.TryGetSnapshot(empireInfo.EmpireName, out snapshot2))
			{
				float num;
				snapshot2.TryRead(this.CurrentScoreDefinition.Name, out num);
				string str = "";
				int decimals = 0;
				component.ParticipantScore.WordWrap = false;
				if (num >= 1000f)
				{
					str = "k";
					if (num <= 10000f)
					{
						num = Mathf.Round(num / 100f);
						num /= 10f;
						decimals = 1;
					}
					else if (num <= 999999f)
					{
						num = Mathf.Round(num / 1000f);
					}
					else if (num <= 9999999f)
					{
						num = Mathf.Round(num / 100000f);
						num /= 10f;
						str = "M";
						decimals = 1;
					}
					else if (num > 9999999f)
					{
						num = Mathf.Round(num / 1000000f);
						str = "M";
					}
				}
				component.ParticipantScore.Text = GuiFormater.FormatGui(num, false, false, false, decimals) + str;
				return;
			}
		}
		else
		{
			tableitem.Visible = false;
		}
	}

	private void RefreshParticipantHistogram(AgeTransform tableitem, EmpireInfo empireInfo, int index)
	{
		EmpireInfo empireInfo2 = this.EmpireInfos.FirstOrDefault((EmpireInfo iterator) => iterator.IsActiveOrLocalPlayer);
		int observerIndex = -1;
		if (empireInfo2 != null)
		{
			observerIndex = empireInfo2.EmpireIndex;
		}
		if (this.IsEmpireVisible(empireInfo, observerIndex))
		{
			tableitem.Visible = true;
			AgePrimitiveHistogramLinear component = tableitem.GetComponent<AgePrimitiveHistogramLinear>();
			component.TintColor = empireInfo.FactionColor;
			Snapshot[] snapshots = this.Snapshot.Snapshots;
			float[] array = new float[snapshots.Length];
			for (int i = 0; i < snapshots.Length; i++)
			{
				string name = string.Format("Turn #{0}", i);
				Snapshot snapshot;
				Snapshot snapshot2;
				if (this.Snapshot.TryGetSnapshot(name, out snapshot) && snapshot.TryGetSnapshot(empireInfo.EmpireName, out snapshot2))
				{
					snapshot2.TryRead(this.CurrentScoreDefinition.Name, out array[i]);
				}
			}
			component.Values = array;
		}
		else
		{
			tableitem.Visible = false;
		}
	}

	private void RefreshVerticalGridLine(AgeTransform tableitem, float value, int index)
	{
		tableitem.Y = this.verticalGridHeights[index];
		tableitem.GetComponent<ScoreGridLabel>().GridLabel.Text = GuiFormater.FormatGui(this.verticalGridValues[index], false, false, false, 0);
	}

	private bool IsEmpireVisible(EmpireInfo empireInfo, int observerIndex = -1)
	{
		if (!this.HideUnknownEmpires)
		{
			return true;
		}
		bool flag = false;
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null && service2.ActivePlayerController.Empire != null && service2.ActivePlayerController.Empire.Index == empireInfo.EmpireIndex)
			{
				flag = true;
			}
		}
		else if (!string.IsNullOrEmpty(empireInfo.Players))
		{
			Steamworks.SteamUser steamUser = Steamworks.SteamAPI.SteamUser;
			if (steamUser != null)
			{
				flag = empireInfo.Players.Contains(steamUser.SteamID.ToString());
			}
		}
		if (flag)
		{
			return true;
		}
		if (observerIndex != -1)
		{
			switch (EmpireInfo.LastAccessibilityLevel)
			{
			case EmpireInfo.Accessibility.Default:
			{
				int num = empireInfo.EmpireExplorationBits & 1 << observerIndex;
				if (num != 0)
				{
					return true;
				}
				int num2 = empireInfo.EmpireInfiltrationBits & 1 << observerIndex;
				if (num2 != 0)
				{
					return true;
				}
				break;
			}
			case EmpireInfo.Accessibility.None:
				return false;
			case EmpireInfo.Accessibility.Partial:
			{
				int num3 = empireInfo.EmpireInfiltrationBits & 1 << observerIndex;
				if (num3 != 0)
				{
					return true;
				}
				break;
			}
			}
		}
		return false;
	}

	private void CalculateELCPScaling()
	{
		this.visibleEmpires.Clear();
		this.ParticipantsScoreTableScale = 1f;
		float num = (!AgeUtils.HighDefinition) ? 32f : (32f * AgeUtils.HighDefinitionFactor);
		float num2 = (!AgeUtils.HighDefinition) ? 4f : (4f * AgeUtils.HighDefinitionFactor);
		float num3 = (!AgeUtils.HighDefinition) ? 50f : (50f * AgeUtils.HighDefinitionFactor);
		EmpireInfo empireInfo = this.EmpireInfos.FirstOrDefault((EmpireInfo iterator) => iterator.IsActiveOrLocalPlayer);
		int observerIndex = -1;
		if (empireInfo != null)
		{
			observerIndex = empireInfo.EmpireIndex;
		}
		foreach (EmpireInfo empireInfo2 in this.EmpireInfos)
		{
			if (this.IsEmpireVisible(empireInfo2, observerIndex))
			{
				this.visibleEmpires.Add(empireInfo2.EmpireIndex);
			}
		}
		if (this.visibleEmpires.Count > 8 && this.visibleEmpires.Count < 11)
		{
			this.ParticipantsScoreTableScale = 0.82f;
		}
		if (this.visibleEmpires.Count > 10)
		{
			this.ParticipantsScoreTableScale = 0.7f;
			num2 *= 0.5f;
			num3 *= 0.6f;
		}
		num *= this.ParticipantsScoreTableScale;
		this.ParticipantsScoreTable.HorizontalSpacing = num;
		this.ParticipantsScoreTable.VerticalSpacing = num2;
		this.ParticipantsScoreTable.PixelMarginLeft = num3;
	}

	public AgeTransform ScoreFiltersTable;

	public Transform ScoreFilterTogglePrefab;

	public AgeTransform ParticipantsScoreTable;

	public Transform ParticipantScoreLinePrefab;

	public AgeTransform ScoreHistogramContainer;

	public Transform ScoreHistogramPrefab;

	public AgeTransform HorizontalGridContainer;

	public Transform ScoreHorizontalGridPrefab;

	public AgeTransform VerticalGridContainer;

	public Transform ScoreVerticalGridPrefab;

	private List<float> verticalGridValues;

	private List<float> verticalGridHeights;

	private List<int> visibleEmpires;

	private float ParticipantsScoreTableScale;
}
