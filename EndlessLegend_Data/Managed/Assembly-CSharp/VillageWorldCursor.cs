using System;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class VillageWorldCursor : WorldCursor
{
	public Village Village { get; private set; }

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

	public static bool CheckWhetherCanSelectConvertedVillage(global::Empire empire, Village village)
	{
		return empire != null && village != null && village.HasBeenConverted && village.Converter.Index == empire.Index;
	}

	public static bool CheckWhetherCanSelectVillage(global::Empire empire, Village village)
	{
		if (empire != null && village != null)
		{
			if (village.HasBeenConverted && village.Converter.Index == empire.Index)
			{
				return true;
			}
			if (village.HasBeenInfected)
			{
				return village.Empire == null || village.Empire.Index == empire.Index;
			}
			if (village.HasBeenPacified && empire.SimulationObject.Tags.Contains(DownloadableContent16.FactionTraitDissent))
			{
				return true;
			}
		}
		return false;
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
			if (this.villageBoundaryObject != null)
			{
				MeshFilter component = this.villageBoundaryObject.GetComponent<MeshFilter>();
				if (component != null)
				{
					Mesh mesh = component.mesh;
					component.mesh = null;
					UnityEngine.Object.DestroyImmediate(mesh, true);
				}
				UnityEngine.Object.DestroyImmediate(this.villageBoundaryObject);
				this.villageBoundaryObject = null;
			}
			if (this.worldEntityHelperContent != null)
			{
				this.worldEntityHelperContent.Clear();
				this.worldEntityHelperContent = null;
			}
			if (this.Village != null)
			{
				this.Village = null;
			}
		}
	}

	protected override void OnCursorActivate(bool activate, params object[] parameters)
	{
		base.OnCursorActivate(activate, parameters);
		if (activate)
		{
			Diagnostics.Assert(this.PlayerControllerRepositoryService != null);
			if (this.Village != null)
			{
				this.Village = null;
			}
			if (parameters != null)
			{
				this.Village = (parameters.FirstOrDefault((object iterator) => iterator != null && iterator.GetType() == typeof(Village)) as Village);
			}
			global::Empire empire = (global::Empire)this.PlayerControllerRepositoryService.ActivePlayerController.Empire;
			if (!VillageWorldCursor.CheckWhetherCanSelectVillage(empire, this.Village))
			{
				Diagnostics.LogWarning("Cannot select dat village...");
				return;
			}
			base.Focus(this.Village, false);
			bool flag = false;
			if (this.Village.HasBeenConverted && this.Village.Converter.Index == empire.Index)
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
						this.ResourceRendererService.Add(this.Village.WorldPosition);
						for (int i = 0; i < 6; i++)
						{
							WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(this.Village.WorldPosition, (WorldOrientation)i, 1);
							if (neighbourTile.IsValid && (int)this.WorldPositionningService.GetRegionIndex(neighbourTile) == this.Village.Region.Index)
							{
								this.ResourceRendererService.Add(neighbourTile);
							}
						}
						this.WorldArea = this.ResourceRendererService.FromPositionsToWorldArea();
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
					this.Village.WorldPosition
				});
			}
			this.CreateVillageBoundaryIFN();
			if (flag)
			{
				this.CreateVillageHexFidsIFN();
			}
		}
		else
		{
			if (this.villageBoundaryObject != null)
			{
				MeshFilter component = this.villageBoundaryObject.GetComponent<MeshFilter>();
				if (component != null)
				{
					Mesh mesh = component.mesh;
					component.mesh = null;
					UnityEngine.Object.DestroyImmediate(mesh, true);
				}
				UnityEngine.Object.DestroyImmediate(this.villageBoundaryObject);
				this.villageBoundaryObject = null;
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
			if (this.Village != null)
			{
				this.InvalidateFIDS(false);
				this.Village = null;
			}
			this.WorldArea = null;
		}
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

	private void CreateVillageBoundaryIFN()
	{
		Diagnostics.Assert(this.Village != null);
		Diagnostics.Assert(this.WorldArea != null);
		if (this.WorldEntityFactoryService == null)
		{
			return;
		}
		if (this.villageBoundaryObject == null)
		{
			UnityEngine.Object @object = Resources.Load("Prefabs/Cities/VillageBoundary");
			if (@object != null)
			{
				this.villageBoundaryObject = (UnityEngine.Object.Instantiate(@object) as GameObject);
				if (this.villageBoundaryObject != null)
				{
					Diagnostics.Assert(base.GlobalPositionningService != null);
					Diagnostics.Assert(this.WorldPositionningService != null);
					GameObject gameObject = null;
					if (this.WorldEntityFactoryService.TryGetValue(this.Village.PointOfInterest.GUID, out gameObject))
					{
						this.villageBoundaryObject.transform.parent = gameObject.transform;
						this.villageBoundaryObject.transform.localPosition = new Vector3(0f, 0.03f, 0f);
						WorldAreaHexagonRenderer component = this.villageBoundaryObject.GetComponent<WorldAreaHexagonRenderer>();
						Diagnostics.Assert(component != null);
						if (component == null)
						{
							return;
						}
						Color color = (this.Village.Converter == null) ? Color.grey : this.Village.Converter.Color;
						component.SetColor(color);
						component.SetWorldArea(this.WorldArea.WorldPositions, this.Village.WorldPosition, base.GlobalPositionningService);
						component.SetMaterialSelectionStatus(false, true, true);
						component.SetOnDestroyLaunchAutokillAnimation(0.5f);
					}
				}
			}
		}
	}

	private void CreateVillageHexFidsIFN()
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
		if (!this.WorldEntityFactoryService.TryGetValue(this.Village.PointOfInterest.GUID, out gameObject))
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
		service.SetSomethingChangedOnRegion((short)this.Village.Region.Index);
	}

	private GameObject villageBoundaryObject;

	private WorldEntityHelper.WorldEntityHelperContent worldEntityHelperContent;
}
