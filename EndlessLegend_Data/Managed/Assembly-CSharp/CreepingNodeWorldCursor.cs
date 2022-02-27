using System;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class CreepingNodeWorldCursor : WorldCursor
{
	public CreepingNode Node { get; private set; }

	[Ancillary]
	private protected IPlayerControllerRepositoryService PlayerControllerRepositoryService { protected get; private set; }

	[Ancillary]
	private protected IResourceRendererService ResourceRendererService { protected get; private set; }

	[Ancillary]
	private protected IVisibilityRendererService VisibilityRendererService { protected get; private set; }

	[Ancillary]
	private IWorldEntityFactoryService WorldEntityFactoryService { get; set; }

	[Ancillary]
	private IWorldPositionningService WorldPositionningService { get; set; }

	private WorldArea WorldArea { get; set; }

	public static bool CheckWhetherCanSelectCreepingNode(global::Empire empire, CreepingNode node)
	{
		return empire != null && node != null && node.Empire.Index == empire.Index;
	}

	public override bool HandleCancelRequest()
	{
		bool flag = base.HandleCancelRequest();
		if (flag)
		{
			WorldCursor.SelectedRegion = null;
		}
		return flag;
	}

	protected override void GameChange(IGame game)
	{
		base.GameChange(game);
		if (game != null)
		{
			this.PlayerControllerRepositoryService = game.Services.GetService<IPlayerControllerRepositoryService>();
			if (this.PlayerControllerRepositoryService == null)
			{
				Diagnostics.LogError("Invalid null player controller repository service.");
			}
			this.WorldPositionningService = game.Services.GetService<IWorldPositionningService>();
		}
		else
		{
			this.PlayerControllerRepositoryService = null;
			if (this.nodeBoundaryObject != null)
			{
				MeshFilter component = this.nodeBoundaryObject.GetComponent<MeshFilter>();
				if (component != null)
				{
					Mesh mesh = component.mesh;
					component.mesh = null;
					UnityEngine.Object.DestroyImmediate(mesh, true);
				}
				UnityEngine.Object.DestroyImmediate(this.nodeBoundaryObject);
				this.nodeBoundaryObject = null;
			}
			if (this.worldEntityHelperContent != null)
			{
				this.worldEntityHelperContent.Clear();
				this.worldEntityHelperContent = null;
			}
			if (this.Node != null)
			{
				this.Node = null;
			}
		}
	}

	protected override void OnCursorActivate(bool activate, params object[] parameters)
	{
		base.OnCursorActivate(activate, new object[0]);
		if (activate)
		{
			this.OnCursorActivate(parameters);
		}
		else
		{
			this.OnCursorDeactivate();
		}
	}

	protected void OnCursorActivate(params object[] parameters)
	{
		Diagnostics.Assert(this.PlayerControllerRepositoryService != null);
		if (this.Node != null)
		{
			this.Node = null;
		}
		if (parameters != null)
		{
			this.Node = (parameters.FirstOrDefault((object iterator) => iterator != null && iterator.GetType() == typeof(CreepingNode)) as CreepingNode);
		}
		global::Empire empire = (global::Empire)this.PlayerControllerRepositoryService.ActivePlayerController.Empire;
		if (!CreepingNodeWorldCursor.CheckWhetherCanSelectCreepingNode(empire, this.Node))
		{
			Diagnostics.LogWarning("Cannot select the creeping node...");
			return;
		}
		base.Focus(this.Node, false);
		bool flag = false;
		if (this.Node.Empire.Index == empire.Index)
		{
			flag = true;
		}
		if (flag)
		{
			if (this.ResourceRendererService != null)
			{
				this.ResourceRendererService.Clear();
				if (this.WorldPositionningService != null)
				{
					int fidsiextractionRange = this.Node.NodeDefinition.FIDSIExtractionRange;
					int index = this.Node.PointOfInterest.Region.Index;
					for (int i = 0; i < this.Node.ExploitedTiles.Count; i++)
					{
						if (this.Node.ExploitedTiles[i].IsValid && (int)this.WorldPositionningService.GetRegionIndex(this.Node.ExploitedTiles[i]) == this.Node.PointOfInterest.Region.Index && this.WorldPositionningService.GetDistrict(this.Node.ExploitedTiles[i]) == null && !this.WorldPositionningService.HasRidge(this.Node.ExploitedTiles[i]))
						{
							this.ResourceRendererService.Add(this.Node.ExploitedTiles[i]);
						}
					}
					this.WorldArea = this.ResourceRendererService.FromPositionsToWorldArea();
					if (this.WorldArea.WorldPositions.Count == 0)
					{
						this.WorldArea = null;
						flag = false;
					}
				}
			}
		}
		else
		{
			if (this.ResourceRendererService != null)
			{
				this.ResourceRendererService.Clear();
			}
			this.InvalidateFIDS(true);
		}
		if (this.WorldArea == null)
		{
			this.WorldArea = new WorldArea(new WorldPosition[]
			{
				this.Node.WorldPosition
			});
		}
		this.CreateNodeBoundaryIFN();
		if (flag)
		{
			this.CreateCreepingNodeHexFidsIFN();
		}
	}

	protected void OnCursorDeactivate()
	{
		if (this.nodeBoundaryObject != null)
		{
			MeshFilter component = this.nodeBoundaryObject.GetComponent<MeshFilter>();
			if (component != null)
			{
				Mesh mesh = component.mesh;
				component.mesh = null;
				UnityEngine.Object.DestroyImmediate(mesh, true);
			}
			UnityEngine.Object.DestroyImmediate(this.nodeBoundaryObject);
			this.nodeBoundaryObject = null;
		}
		if (this.worldEntityHelperContent != null)
		{
			this.worldEntityHelperContent.Clear();
			this.worldEntityHelperContent = null;
		}
		if (this.ResourceRendererService != null)
		{
			this.ResourceRendererService.Clear();
		}
		if (this.Node != null)
		{
			this.InvalidateFIDS(false);
			this.Node = null;
		}
		this.WorldArea = null;
	}

	protected override void WorldViewTechniqueChange(WorldViewTechnique technique)
	{
		base.WorldViewTechniqueChange(technique);
		if (technique != null)
		{
			this.ResourceRendererService = technique.Services.GetService<IResourceRendererService>();
			this.VisibilityRendererService = technique.Services.GetService<IVisibilityRendererService>();
			this.WorldEntityFactoryService = technique.Services.GetService<IWorldEntityFactoryService>();
		}
		else
		{
			this.ResourceRendererService = null;
			this.VisibilityRendererService = null;
			this.WorldEntityFactoryService = null;
		}
	}

	private void CreateNodeBoundaryIFN()
	{
		Diagnostics.Assert(this.Node != null);
		Diagnostics.Assert(this.WorldArea != null);
		if (this.WorldEntityFactoryService == null)
		{
			return;
		}
		if (this.nodeBoundaryObject == null)
		{
			UnityEngine.Object @object = Resources.Load("Prefabs/Cities/VillageBoundary");
			if (@object != null)
			{
				this.nodeBoundaryObject = (UnityEngine.Object.Instantiate(@object) as GameObject);
				if (this.nodeBoundaryObject != null)
				{
					Diagnostics.Assert(base.GlobalPositionningService != null);
					Diagnostics.Assert(this.WorldPositionningService != null);
					GameObject gameObject = null;
					if (this.WorldEntityFactoryService.TryGetValue(this.Node.PointOfInterest.GUID, out gameObject))
					{
						this.nodeBoundaryObject.transform.parent = gameObject.transform;
						this.nodeBoundaryObject.transform.localPosition = new Vector3(0f, 0.03f, 0f);
						WorldAreaHexagonRenderer component = this.nodeBoundaryObject.GetComponent<WorldAreaHexagonRenderer>();
						Diagnostics.Assert(component != null);
						if (component == null)
						{
							return;
						}
						Color color = (this.Node.Empire == null) ? Color.grey : this.Node.Empire.Color;
						component.SetColor(color);
						component.SetWorldArea(this.WorldArea.WorldPositions, this.Node.WorldPosition, base.GlobalPositionningService);
						component.SetMaterialSelectionStatus(false, true, true);
						component.SetOnDestroyLaunchAutokillAnimation(0.5f);
					}
				}
			}
		}
	}

	private void CreateCreepingNodeHexFidsIFN()
	{
		if (this.worldEntityHelperContent != null)
		{
			return;
		}
		WorldView worldView = base.ViewService.CurrentView as WorldView;
		DefaultWorldViewTechnique defaultWorldViewTechnique = worldView.CurrentWorldViewTechnique as DefaultWorldViewTechnique;
		if (defaultWorldViewTechnique == null)
		{
			return;
		}
		GameObject gameObject = null;
		if (!this.WorldEntityFactoryService.TryGetValue(this.Node.PointOfInterest.GUID, out gameObject))
		{
			return;
		}
		WorldPatch worldPatch = null;
		Transform parent = gameObject.transform.parent;
		while (worldPatch == null && parent != null)
		{
			worldPatch = parent.GetComponent<WorldPatch>();
			parent = parent.parent;
		}
		if (worldPatch == null)
		{
			return;
		}
		AbstractTerrainPatchRenderer abstractTerrainPatchRenderer = null;
		bool flag = worldPatch.TryGetPatchRenderer<AbstractTerrainPatchRenderer>(out abstractTerrainPatchRenderer);
		if (!flag || abstractTerrainPatchRenderer == null)
		{
			return;
		}
		this.worldEntityHelperContent = new WorldEntityHelper.WorldEntityHelperContent();
		InstanciedMeshBlock orCreateInstanciedMeshBlock = this.worldEntityHelperContent.GetOrCreateInstanciedMeshBlock(defaultWorldViewTechnique.HxTechniqueGraphicData.InstanciedMeshHolders, PrimitiveLayerMask.Fids, InstanciedMeshHelpers.PositionForwardScaleZPixelsPerInstance);
		InstanciedMeshBlock orCreateInstanciedMeshBlock2 = this.worldEntityHelperContent.GetOrCreateInstanciedMeshBlock(defaultWorldViewTechnique.HxTechniqueGraphicData.InstanciedMeshHolders, PrimitiveLayerMask.Fids, InstanciedMeshHelpers.PositionTexCoordWorldScaleTextureScalePixelsPerInstance);
		for (int i = 0; i < this.WorldArea.WorldPositions.Count; i++)
		{
			Vector3 absoluteWorldPosition2D = abstractTerrainPatchRenderer.GlobalPositionningService.GetAbsoluteWorldPosition2D(this.WorldArea.WorldPositions[i]);
			absoluteWorldPosition2D.x -= abstractTerrainPatchRenderer.OffsetX;
			absoluteWorldPosition2D.z -= abstractTerrainPatchRenderer.OffsetZ;
			InstancingHelper.SpawnOnePrefab(abstractTerrainPatchRenderer, defaultWorldViewTechnique, defaultWorldViewTechnique.HxTechniqueGraphicData.AllResourceFidsGraphicDatas.CityExploitationHexaData, absoluteWorldPosition2D, (int)this.WorldArea.WorldPositions[i].Row, (int)this.WorldArea.WorldPositions[i].Column, null, PrimitiveLayerMask.Fids, ref orCreateInstanciedMeshBlock, PrimitiveLayerMask.Fids, ref orCreateInstanciedMeshBlock2, false, 0, 0);
		}
		this.worldEntityHelperContent.CloseInstanciedMeshBlockIFN(abstractTerrainPatchRenderer);
		this.worldEntityHelperContent.Show();
	}

	private void InvalidateFIDS(bool active)
	{
		WorldPositionning.NonConvertedVillagesArentExploitable = active;
		IWorldPositionSimulationEvaluatorService service = base.GameService.Game.Services.GetService<IWorldPositionSimulationEvaluatorService>();
		service.SetSomethingChangedOnRegion((short)this.Node.PointOfInterest.Region.Index);
	}

	private GameObject nodeBoundaryObject;

	private WorldEntityHelper.WorldEntityHelperContent worldEntityHelperContent;
}
