using System;
using System.Collections;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class PanelFeatureDismantleNode : GuiPanelFeature
{
	public override StaticString InternalName
	{
		get
		{
			return "DismantleNode";
		}
		protected set
		{
		}
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		IGameService gameService = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		if (this.context == null || !(this.context is CreepingNode))
		{
			base.AgeTransform.Visible = false;
		}
		else
		{
			CreepingNode node = this.context as CreepingNode;
			this.StatusLabel.Text = string.Empty;
			base.AgeTransform.Height = this.StatusLabel.AgeTransform.Height;
			this.DefenseGaugeGroup.Visible = true;
			float growth = node.Life;
			float maximumGrowth = node.MaxLife;
			bool isDismantling = node.DismantlingArmy != null;
			float progress = 0f;
			int numberOfTurn = 0;
			float progressLeft = growth;
			float progressRight = growth;
			if (isDismantling)
			{
				IGameEntity gameEntity;
				if (this.gameEntityRepositoryService.TryGetValue(node.DismantlingArmyGUID, out gameEntity))
				{
					Army army = gameEntity as Army;
					if (army != null)
					{
						progress = army.GetPropertyValue(SimulationProperties.CreepingNodeDismantlePower);
					}
				}
				if (progress > 0f)
				{
					numberOfTurn = Mathf.CeilToInt(growth / progress);
				}
				progress *= -1f;
				progressLeft += progress;
				this.DefenseProgress.TintColor = this.DismantlingColor;
				this.StatusLabel.Text = AgeLocalizer.Instance.LocalizeString("%FeatureDismantleNodeDismantlingStateTitle");
			}
			else
			{
				progress = node.NodeDefinition.GrowthPerTurn;
				if (progress > 0f)
				{
					numberOfTurn = Mathf.CeilToInt((maximumGrowth - growth) / progress);
				}
				progressRight += progress;
				this.DefenseProgress.TintColor = this.RecoveryColor;
				this.StatusLabel.Text = AgeLocalizer.Instance.LocalizeString("%FeatureDismantleNodeRecoveringStateTitle");
			}
			AgePrimitiveLabel statusLabel = this.StatusLabel;
			string text = statusLabel.Text;
			statusLabel.Text = string.Concat(new string[]
			{
				text,
				" (",
				numberOfTurn.ToString(),
				AgeLocalizer.Instance.LocalizeString("%TurnSymbol"),
				")"
			});
			this.DefenseGauge.AgeTransform.PercentRight = Mathf.Clamp(growth / maximumGrowth, 0f, 1f) * 100f;
			this.DefenseProgress.AgeTransform.PercentLeft = Mathf.Clamp(progressLeft / maximumGrowth, 0f, 1f) * 100f;
			this.DefenseProgress.AgeTransform.PercentRight = Mathf.Clamp(progressRight / maximumGrowth, 0f, 1f) * 100f;
			base.AgeTransform.Height += this.DefenseGaugeGroup.Height;
			if (progressLeft != maximumGrowth)
			{
				this.DefenseProgress.AgeTransform.Visible = true;
			}
			else
			{
				this.DefenseProgress.AgeTransform.Visible = false;
			}
			if (this.StatusLabel.AgeTransform.PixelMarginTop == this.TitleLabel.AgeTransform.PixelMarginTop)
			{
				this.StatusLabel.AgeTransform.PixelMarginLeft = 2f * this.TitleLabel.AgeTransform.PixelMarginLeft + this.TitleLabel.Font.ComputeTextWidth(AgeLocalizer.Instance.LocalizeString(this.TitleLabel.Text), this.TitleLabel.ForceCaps, false);
			}
		}
		yield return base.OnShow(parameters);
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		base.AgeTransform.Visible = (this.StatusLabel.Text != string.Empty && downloadableContentService.IsShared(DownloadableContent20.ReadOnlyName));
		yield break;
	}

	public AgePrimitiveLabel TitleLabel;

	public AgePrimitiveLabel StatusLabel;

	public AgeTransform DefenseGaugeGroup;

	public AgePrimitiveImage DefenseGauge;

	public AgePrimitiveImage DefenseProgress;

	public AgePrimitiveLabel DefenseValue;

	public Color DismantlingColor;

	public Color RecoveryColor;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IWorldPositionningService worldPositionningService;
}
