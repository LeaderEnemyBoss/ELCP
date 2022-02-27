using System;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.View;
using UnityEngine;

public class EncounterSpellCursor : EncounterWorldCursor, IWorldPlacementCursorWithCaption
{
	public EncounterSpellCursor()
	{
		this.IsAbleToBackOnLeftClick = false;
		this.IsAbleToBackOnRightClick = true;
		base.IsStatic = false;
		base.AllowSelection = false;
		this.InterpreterContext = new InterpreterContext(null);
	}

	string IWorldPlacementCursorWithCaption.CaptionText
	{
		get
		{
			return "%SpellCursorCaption";
		}
	}

	public TerrainCursorTarget Target { get; set; }

	public SpellDefinition SpellDefinition { get; set; }

	public GameObject SpellCursor { get; private set; }

	public IWorldEntityMappingService WorldEntityMappingService { get; private set; }

	private protected InterpreterContext InterpreterContext { protected get; private set; }

	private Contender Contender { get; set; }

	private WorldPosition WorldPosition { get; set; }

	protected virtual void CreateHighlights()
	{
		if (this.SpellCursor == null)
		{
			GameObject gameObject = this.WorldEntityMappingService.Instantiate(null, "PlacementCursor.Ghost", this.InterpreterContext);
			if (gameObject != null)
			{
				this.SpellCursor = UnityEngine.Object.Instantiate<GameObject>(gameObject);
				if (this.SpellCursor != null)
				{
					this.SpellCursor.SetActive(false);
				}
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	protected virtual void CreateCastedSpellGhost()
	{
		this.InterpreterContext.Clear();
		this.InterpreterContext.Register("SpellDefinitionName", this.SpellDefinition.Name);
		GameObject gameObject = this.WorldEntityMappingService.Instantiate(null, "CastedSpellGhost", this.InterpreterContext);
		if (gameObject != null)
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject);
			if (gameObject2 != null && base.WorldEncounter != null)
			{
				WorldPatch worldPatch = base.GlobalPositionningService.GetWorldPatch(WorldCursor.HighlightedWorldPosition);
				if (worldPatch == null)
				{
					return;
				}
				gameObject2.SetActive(true);
				gameObject2.transform.parent = worldPatch.RootedTransform;
				gameObject2.transform.position = base.GlobalPositionningService.Get3DPosition(WorldCursor.HighlightedWorldPosition);
				base.WorldEncounter.RegisterCastedSpellGhost(gameObject2);
			}
			UnityEngine.Object.Destroy(gameObject);
			this.SetWorldAreaHexagonRendererPosition(gameObject2, WorldCursor.HighlightedWorldPosition, true);
		}
	}

	protected virtual void DestroyHighlights()
	{
		if (this.SpellCursor != null)
		{
			UnityEngine.Object.DestroyImmediate(this.SpellCursor);
			this.SpellCursor = null;
		}
	}

	protected virtual void InitializeInterpreterContext()
	{
		this.InterpreterContext.Clear();
		this.InterpreterContext.Register("PlacementCursorClass", base.GetType().ToString());
		this.InterpreterContext.Register("Type", "Spell");
		this.InterpreterContext.Register("AffinityMapping", "RageWizards");
		this.InterpreterContext.Register("SpellDefinitionName", this.SpellDefinition.Name);
	}

	protected override void ChangeCursor(Type cursorType, global::CursorTarget cursorTarget, IGameEntity gameEntity)
	{
		if (gameEntity is Encounter && base.Encounter == gameEntity)
		{
			return;
		}
		base.ChangeCursor(cursorType, cursorTarget, gameEntity);
	}

	protected override void ChangeCursorFromEncounterState(EncounterState encounterState)
	{
	}

	protected override void GameChange(IGame game)
	{
		base.GameChange(game);
		if (game != null)
		{
			base.PlayerControllerRepositoryService = game.Services.GetService<IPlayerControllerRepositoryService>();
		}
		else
		{
			base.PlayerControllerRepositoryService = null;
		}
	}

	protected override void OnCursorActivate(params object[] parameters)
	{
		base.OnCursorActivate(parameters);
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			Diagnostics.LogError("Unable to retrieve gameService");
			return;
		}
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		if (this.worldPositionningService == null)
		{
			Diagnostics.LogError("Unable to retrieve worldPositionningService");
			return;
		}
		this.Selection = null;
		this.WorldPosition = WorldPosition.Invalid;
		if (parameters != null)
		{
			this.SpellDefinition = (parameters.FirstOrDefault((object iterator) => iterator != null && iterator.GetType() == typeof(SpellDefinition)) as SpellDefinition);
		}
		global::Empire empire = base.PlayerControllerRepositoryService.ActivePlayerController.Empire as global::Empire;
		this.Contender = base.Encounter.GetFirstAlliedContenderFromEmpireWithUnits(empire);
		WorldCursor.HighlightedWorldPositionChange += this.WorldCursor_HighlightedWorldPositionChange;
		this.InitializeInterpreterContext();
		this.CreateHighlights();
	}

	protected override void OnCursorDeactivate()
	{
		base.OnCursorDeactivate();
		this.Selection = null;
		base.CursorTargetService.Select(null);
		this.InterpreterContext.Clear();
		this.DestroyHighlights();
		this.worldPositionningService = null;
		WorldCursor.HighlightedWorldPositionChange -= this.WorldCursor_HighlightedWorldPositionChange;
	}

	protected override void OnCursorClick(MouseButton mouseButton, Amplitude.Unity.View.CursorTarget[] cursorTargets)
	{
		if (mouseButton != MouseButton.Left)
		{
			base.OnCursorClick(mouseButton, cursorTargets);
			return;
		}
		OrderBuyoutSpellAndPlayBattleAction order = new OrderBuyoutSpellAndPlayBattleAction(base.Encounter.GUID, this.Contender.GUID, WorldCursor.HighlightedWorldPosition, this.SpellDefinition);
		base.PlayerControllerRepositoryService.ActivePlayerController.PostOrder(order);
		this.CreateCastedSpellGhost();
		base.CursorService.ChangeCursor(typeof(EncounterWorldCursor), new object[]
		{
			base.CursorTarget,
			base.WorldEncounter
		});
	}

	protected override void WorldViewTechniqueChange(WorldViewTechnique technique)
	{
		base.WorldViewTechniqueChange(technique);
		if (technique != null)
		{
			this.WorldEntityMappingService = technique.GetService<IWorldEntityMappingService>();
		}
		else
		{
			this.WorldEntityMappingService = null;
		}
	}

	protected void WorldCursor_HighlightedWorldPositionChange(object sender, HighlightedWorldPositionChangeEventArgs e)
	{
		if (base.WorldEncounter == null || base.WorldEncounter.Encounter == null || base.WorldEncounter.Encounter.BattleZone == null)
		{
			return;
		}
		bool flag = base.WorldEncounter.Encounter.BattleZone.Contains(e.WorldPosition);
		if (this.SpellCursor != null)
		{
			WorldPatch worldPatch = base.GlobalPositionningService.GetWorldPatch(WorldCursor.HighlightedWorldPosition);
			if (worldPatch == null)
			{
				return;
			}
			this.SpellCursor.SetActive(true);
			this.SpellCursor.transform.parent = worldPatch.RootedTransform;
			this.SpellCursor.transform.position = base.GlobalPositionningService.Get3DPosition(WorldCursor.HighlightedWorldPosition);
			Renderer[] componentsInChildren = this.SpellCursor.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (flag)
				{
					for (int j = 0; j < componentsInChildren[i].materials.Length; j++)
					{
						if (componentsInChildren[i].materials[j].HasProperty("_TintColor0"))
						{
							Color color = componentsInChildren[i].materials[j].GetColor("_TintColor0");
							componentsInChildren[i].materials[j].SetColor("_TintColor", color);
						}
					}
				}
				else
				{
					for (int k = 0; k < componentsInChildren[i].materials.Length; k++)
					{
						if (componentsInChildren[i].materials[k].HasProperty("_TintColor1"))
						{
							Color color2 = componentsInChildren[i].materials[k].GetColor("_TintColor1");
							componentsInChildren[i].materials[k].SetColor("_TintColor", color2);
						}
					}
				}
			}
			this.SetWorldAreaHexagonRendererPosition(this.SpellCursor, WorldCursor.HighlightedWorldPosition, false);
		}
	}

	private void SetWorldAreaHexagonRendererPosition(GameObject worldAreaHexagonRendererOwner, WorldPosition position, bool resetVisibilityTimer)
	{
		WorldAreaHexagonRenderer[] componentsInChildren = worldAreaHexagonRendererOwner.GetComponentsInChildren<WorldAreaHexagonRenderer>(true);
		if (componentsInChildren != null && componentsInChildren.Length != 0)
		{
			WorldPosition[] worldArea = this.SpellDefinition.GetAffectedPosition(this.Contender, position, this.Contender.WorldOrientation, this.worldPositionningService.World.WorldParameters).ToArray();
			foreach (WorldAreaHexagonRenderer worldAreaHexagonRenderer in componentsInChildren)
			{
				worldAreaHexagonRenderer.ClearContentASAP();
				worldAreaHexagonRenderer.SetWorldArea(worldArea, position, base.GlobalPositionningService);
				worldAreaHexagonRenderer.SetMaterialSelectionStatus(true, true, resetVisibilityTimer);
				worldAreaHexagonRenderer.SetOnDestroyLaunchAutokillAnimation(2f);
			}
		}
	}

	private IWorldPositionningService worldPositionningService;
}
