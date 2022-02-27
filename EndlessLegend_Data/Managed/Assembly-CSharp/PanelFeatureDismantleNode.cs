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
			CreepingNode creepingNode = this.context as CreepingNode;
			this.StatusLabel.Text = string.Empty;
			base.AgeTransform.Height = this.StatusLabel.AgeTransform.Height;
			this.DefenseGaugeGroup.Visible = true;
			float life = creepingNode.Life;
			float maxLife = creepingNode.MaxLife;
			bool flag = creepingNode.DismantlingArmy != null;
			float num = 0f;
			int num2 = 0;
			float num3 = life;
			float num4 = life;
			if (flag)
			{
				IGameEntity gameEntity;
				if (this.gameEntityRepositoryService.TryGetValue(creepingNode.DismantlingArmyGUID, out gameEntity))
				{
					Army army = gameEntity as Army;
					if (army != null)
					{
						num = army.GetPropertyValue(SimulationProperties.CreepingNodeDismantlePower);
					}
				}
				if (num > 0f)
				{
					num2 = Mathf.CeilToInt(life / num);
				}
				num *= -1f;
				num3 += num;
				this.DefenseProgress.TintColor = this.DismantlingColor;
				this.StatusLabel.Text = AgeLocalizer.Instance.LocalizeString("%FeatureDismantleNodeDismantlingStateTitle");
			}
			else
			{
				num = creepingNode.NodeDefinition.GrowthPerTurn;
				if (num > 0f)
				{
					num2 = Mathf.CeilToInt((maxLife - life) / num);
				}
				num4 += num;
				this.DefenseProgress.TintColor = this.RecoveryColor;
				this.StatusLabel.Text = AgeLocalizer.Instance.LocalizeString("%FeatureDismantleNodeRecoveringStateTitle");
			}
			AgePrimitiveLabel statusLabel = this.StatusLabel;
			string text = statusLabel.Text;
			statusLabel.Text = string.Concat(new string[]
			{
				text,
				" (",
				num2.ToString(),
				AgeLocalizer.Instance.LocalizeString("%TurnSymbol"),
				")"
			});
			this.DefenseGauge.AgeTransform.PercentRight = Mathf.Clamp(life / maxLife, 0f, 1f) * 100f;
			this.DefenseProgress.AgeTransform.PercentLeft = Mathf.Clamp(num3 / maxLife, 0f, 1f) * 100f;
			this.DefenseProgress.AgeTransform.PercentRight = Mathf.Clamp(num4 / maxLife, 0f, 1f) * 100f;
			base.AgeTransform.Height += this.DefenseGaugeGroup.Height;
			if (num3 != maxLife)
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
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		base.AgeTransform.Visible = (this.StatusLabel.Text != string.Empty && service.IsShared(DownloadableContent20.ReadOnlyName));
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
