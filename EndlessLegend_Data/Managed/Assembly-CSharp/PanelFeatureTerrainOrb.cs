using System;
using System.Collections;
using Amplitude;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;

public class PanelFeatureTerrainOrb : GuiPanelFeature
{
	public override StaticString InternalName
	{
		get
		{
			return "TerrainOrb";
		}
		protected set
		{
		}
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.orbService = base.Game.Services.GetService<IOrbService>();
		this.playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.visibilityService = base.Game.Services.GetService<IVisibilityService>();
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		this.Value.Text = string.Empty;
		bool isVisible = false;
		WorldPosition worldPosition = WorldPosition.Invalid;
		if (this.context is WorldPosition)
		{
			worldPosition = (WorldPosition)this.context;
		}
		else if (this.context is IWorldPositionable)
		{
			worldPosition = (this.context as IWorldPositionable).WorldPosition;
		}
		global::Empire playerEmpire = this.playerControllerRepository.ActivePlayerController.Empire as global::Empire;
		DepartmentOfForeignAffairs departmentOfForeignAffairs = playerEmpire.GetAgency<DepartmentOfForeignAffairs>();
		bool orbIsVisible = false;
		bool haveSharedOrbVisibility = false;
		for (int index = 0; index < departmentOfForeignAffairs.DiplomaticRelations.Count; index++)
		{
			if (departmentOfForeignAffairs.DiplomaticRelations[index] != null)
			{
				DiplomaticRelation diplomaticRelation = departmentOfForeignAffairs.DiplomaticRelations[index];
				bool haveMapExchange = diplomaticRelation.HasActiveAbility(PanelFeatureTerrainOrb.MapExchangeDiplomaticAbilityName);
				bool haveOrbHunterTrait = (base.Game as global::Game).Empires[diplomaticRelation.OtherEmpireIndex].SimulationObject.Tags.Contains(PanelFeatureTerrainOrb.OrbHunterFactionTrait);
				if (haveMapExchange && haveOrbHunterTrait)
				{
					haveSharedOrbVisibility = true;
					break;
				}
			}
		}
		if (playerEmpire.SimulationObject.Tags.Contains(PanelFeatureTerrainOrb.OrbHunterFactionTrait) || haveSharedOrbVisibility)
		{
			orbIsVisible = this.visibilityService.IsWorldPositionExploredFor(worldPosition, playerEmpire);
		}
		else
		{
			orbIsVisible = this.visibilityService.IsWorldPositionVisibleFor(worldPosition, playerEmpire);
		}
		int orbValue = this.orbService.GetOrbValueAtPosition(worldPosition);
		if (worldPosition.IsValid && orbValue > 0 && orbIsVisible)
		{
			isVisible = true;
			this.Value.AgeTransform.PixelMarginLeft = this.Title.Font.ComputeTextWidth(AgeLocalizer.Instance.LocalizeString(this.Title.Text), false, false) + 3f * this.Title.AgeTransform.PixelMarginLeft;
			this.Value.Text = GuiFormater.FormatStock((float)orbValue, DepartmentOfTheTreasury.Resources.Orb, 0, true);
			if (this.Value.AgeTransform.PixelMarginTop == this.Title.AgeTransform.PixelMarginTop)
			{
				this.Value.AgeTransform.PixelMarginLeft = 2f * this.Title.AgeTransform.PixelMarginLeft + this.Title.Font.ComputeTextWidth(AgeLocalizer.Instance.LocalizeString(this.Title.Text), this.Title.ForceCaps, false);
			}
		}
		yield return base.OnShow(parameters);
		base.AgeTransform.Visible = isVisible;
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.orbService = null;
		base.OnUnloadGame(game);
	}

	public static readonly string MapExchangeDiplomaticAbilityName = "MapExchange";

	public static readonly string OrbHunterFactionTrait = "FactionTraitWinterShifters2";

	public AgePrimitiveLabel Title;

	public AgePrimitiveLabel Value;

	private IOrbService orbService;

	private IPlayerControllerRepositoryService playerControllerRepository;

	private IVisibilityService visibilityService;
}
