﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.View;
using UnityEngine;

public class DistrictWorldCursor : GarrisonWorldCursor
{
	public DistrictWorldCursor()
	{
		this.WorldArea = new WorldArea();
		base.IsAbleToTrackFocusedWorldPositionable = false;
		this.nodeBoundryObjects = new Dictionary<GameEntityGUID, GameObject>();
		this.NodesToDraw = new List<CreepingNode>();
	}

	public City City { get; private set; }

	public Camp Camp { get; private set; }

	public District District
	{
		get
		{
			if (this.WorldDistrict != null)
			{
				return this.WorldDistrict.District;
			}
			return null;
		}
	}

	public override IGarrison Garrison
	{
		get
		{
			return this.City;
		}
	}

	[Ancillary]
	private protected IPlayerControllerRepositoryService PlayerControllerRepositoryService { protected get; private set; }

	[Ancillary]
	private protected IResourceRendererService ResourceRendererService { protected get; private set; }

	[Ancillary]
	private protected IVisibilityRendererService VisibilityRendererService { protected get; private set; }

	protected override WorldPosition GarrisonPosition
	{
		get
		{
			return this.City.WorldPosition;
		}
	}

	private WorldArea Districts { get; set; }

	private WorldArea WorldArea { get; set; }

	private WorldDistrict WorldDistrict { get; set; }

	public override bool HandleCancelRequest()
	{
		bool flag = base.HandleCancelRequest();
		if (flag)
		{
			WorldCursor.SelectedRegion = null;
		}
		return flag;
	}

	public override string ToString()
	{
		return string.Format("{0}:{1}", base.ToString(), (this.City == null) ? GameEntityGUID.Zero : this.City.GUID);
	}

	protected override void GameChange(IGame game)
	{
		base.GameChange(game);
		if (game != null)
		{
			this.PlayerControllerRepositoryService = game.Services.GetService<IPlayerControllerRepositoryService>();
			return;
		}
		this.PlayerControllerRepositoryService = null;
		if (this.City != null)
		{
			this.City.CityDistrictCollectionChange -= this.City_CityDistrictCollectionChange;
			this.City.Empire.GetAgency<DepartmentOfTheInterior>().CitiesCollectionChanged -= this.DistrictWorldCursor_CitiesCollectionChanged;
			this.City = null;
		}
		this.WorldDistrict = null;
		this.CheckNodes = false;
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
		Diagnostics.Assert(base.WorldPositionningService != null);
		Diagnostics.Assert(this.PlayerControllerRepositoryService != null);
		Diagnostics.Assert(base.GlobalPositionningService != null);
		if (this.City != null)
		{
			this.City.CityDistrictCollectionChange -= this.City_CityDistrictCollectionChange;
			this.City.Empire.GetAgency<DepartmentOfTheInterior>().CitiesCollectionChanged -= this.DistrictWorldCursor_CitiesCollectionChanged;
			this.City = null;
			this.ResourceRendererService.SetSelectedRegion(-1);
		}
		if (parameters != null)
		{
			this.City = (parameters.FirstOrDefault((object iterator) => iterator != null && iterator.GetType() == typeof(City)) as City);
		}
		if (base.CursorTarget != null)
		{
			this.WorldDistrict = base.CursorTarget.GetComponent<WorldDistrict>();
			if (this.WorldDistrict != null)
			{
				this.City = this.WorldDistrict.District.City;
			}
			else if (this.City == null)
			{
				Diagnostics.LogError("Invalid cursor target (cant find component of type: 'WorldDistrict').");
				return;
			}
		}
		else if (this.City == null)
		{
			Diagnostics.LogError("Cursor target is null.");
			return;
		}
		if (this.City == null)
		{
			base.Back();
			return;
		}
		IAudioEventService service = Services.GetService<IAudioEventService>();
		Diagnostics.Assert(service != null);
		service.Play2DEvent("Gui/Interface/CitySelection");
		if (this.City.Empire != this.PlayerControllerRepositoryService.ActivePlayerController.Empire)
		{
			this.City.CityDistrictCollectionChange += this.City_CityDistrictCollectionChange;
			this.City.Empire.GetAgency<DepartmentOfTheInterior>().CitiesCollectionChanged += this.DistrictWorldCursor_CitiesCollectionChanged;
			return;
		}
		if (this.ResourceRendererService != null)
		{
			this.ResourceRendererService.Clear();
		}
		this.Camp = (parameters.FirstOrDefault((object iterator) => iterator != null && iterator.GetType() == typeof(Camp)) as Camp);
		if (this.Camp != null || (this.District != null && this.City.Camp != null && this.City.Camp.ContainsDistrict(this.District.GUID)))
		{
			if (this.Camp == null)
			{
				this.Camp = this.City.Camp;
			}
			base.Focus(this.City.Camp, true);
			for (int i = 0; i < this.City.Camp.Districts.Count; i++)
			{
				this.ResourceRendererService.Add(this.City.Camp.Districts[i].WorldPosition);
			}
		}
		else
		{
			this.ResourceRendererService.SetSelectedRegion(this.City.Region.Index);
			if (this.ResourceRendererService != null)
			{
				this.City_CityDistrictCollectionChange(this, new CollectionChangeEventArgs(CollectionChangeAction.Refresh, null));
				this.City.CityDistrictCollectionChange += this.City_CityDistrictCollectionChange;
				this.City.Empire.GetAgency<DepartmentOfTheInterior>().CitiesCollectionChanged += this.DistrictWorldCursor_CitiesCollectionChanged;
			}
		}
		this.City.OnCityCampChanged += this.City_OnCityCampChanged;
		this.City.OnCityDisposed += this.City_OnCityDisposed;
		global::Empire empire = this.PlayerControllerRepositoryService.ActivePlayerController.Empire as global::Empire;
		if (this.City.Empire.Index == empire.Index && empire.SimulationObject.Tags.Contains("FactionTraitMimics1"))
		{
			this.departmentOfCreepingNodes = empire.GetAgency<DepartmentOfCreepingNodes>();
			this.CheckNodes = true;
		}
		this.SelectCreepingNodes(10);
	}

	protected void OnCursorDeactivate()
	{
		if (this.City != null)
		{
			this.City.OnCityCampChanged -= this.City_OnCityCampChanged;
			this.City.OnCityDisposed -= this.City_OnCityDisposed;
			this.City.CityDistrictCollectionChange -= this.City_CityDistrictCollectionChange;
			this.City.Empire.GetAgency<DepartmentOfTheInterior>().CitiesCollectionChanged -= this.DistrictWorldCursor_CitiesCollectionChanged;
			this.City = null;
			this.ResourceRendererService.SetSelectedRegion(-1);
		}
		this.WorldDistrict = null;
		this.Camp = null;
		if (this.ResourceRendererService != null)
		{
			this.ResourceRendererService.Clear();
		}
		GameObject[] array = this.nodeBoundryObjects.Values.ToArray<GameObject>();
		for (int i = array.Length - 1; i >= 0; i--)
		{
			MeshFilter component = array[i].GetComponent<MeshFilter>();
			if (component != null)
			{
				Mesh mesh = component.mesh;
				component.mesh = null;
				UnityEngine.Object.DestroyImmediate(mesh, true);
			}
			UnityEngine.Object.DestroyImmediate(array[i]);
			array[i] = null;
		}
		this.nodeBoundryObjects.Clear();
		this.NodesToDraw.Clear();
		if (this.worldEntityHelperContent != null)
		{
			this.worldEntityHelperContent.Clear();
			this.worldEntityHelperContent = null;
		}
		this.departmentOfCreepingNodes = null;
		this.CheckNodes = false;
	}

	protected override void OnCursorUp(MouseButton mouseButton, Amplitude.Unity.View.CursorTarget[] cursorTargets)
	{
	}

	protected override void WorldViewTechniqueChange(WorldViewTechnique technique)
	{
		base.WorldViewTechniqueChange(technique);
		if (technique != null)
		{
			this.ResourceRendererService = technique.Services.GetService<IResourceRendererService>();
			this.VisibilityRendererService = technique.Services.GetService<IVisibilityRendererService>();
			this.worldEntityCullingService = technique.GetService<IWorldEntityCullingService>();
			return;
		}
		this.ResourceRendererService = null;
		this.VisibilityRendererService = null;
		this.worldEntityCullingService = null;
	}

	private bool Accept(WorldPosition worldPosition)
	{
		worldPosition = base.GlobalPositionningService.FromRelativeToConstrainedWorldPosition(worldPosition);
		if (!worldPosition.IsValid)
		{
			return false;
		}
		if (base.WorldPositionningService != null)
		{
			int regionIndex = (int)base.WorldPositionningService.GetRegionIndex(worldPosition);
			if (regionIndex != this.City.Region.Index)
			{
				return false;
			}
			IVisibilityService service = base.GameService.Game.Services.GetService<IVisibilityService>();
			if (!service.IsWorldPositionExploredFor(worldPosition, this.City.Empire))
			{
				return false;
			}
		}
		return true;
	}

	private void City_CityDistrictCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		if (this.ResourceRendererService != null)
		{
			this.ResourceRendererService.Clear();
			this.WorldArea = new WorldArea();
			for (int i = 0; i < this.City.Districts.Count; i++)
			{
				District district = this.City.Districts[i];
				this.ResourceRendererService.Add(district.WorldPosition);
			}
			if (this.City.Camp != null)
			{
				for (int j = 0; j < this.City.Camp.Districts.Count; j++)
				{
					this.ResourceRendererService.Add(this.City.Camp.Districts[j].WorldPosition);
				}
			}
			this.Districts = this.ResourceRendererService.FromPositionsToWorldArea();
			this.SelectCreepingNodes(1);
		}
	}

	private void City_OnCityCampChanged()
	{
		if (this.City != null && this.ResourceRendererService != null)
		{
			if (this.City.Camp == null)
			{
				this.ResourceRendererService.Clear();
				for (int i = 0; i < this.City.Districts.Count; i++)
				{
					District district = this.City.Districts[i];
					this.ResourceRendererService.Add(district.WorldPosition);
				}
			}
			else
			{
				for (int j = 0; j < this.City.Camp.Districts.Count; j++)
				{
					this.ResourceRendererService.Add(this.City.Camp.Districts[j].WorldPosition);
				}
			}
			this.SelectCreepingNodes(1);
		}
	}

	private void City_OnCityDisposed()
	{
		if (this.City != null && this.ResourceRendererService != null)
		{
			this.City = null;
			this.ResourceRendererService.SetSelectedRegion(-1);
			this.ResourceRendererService.Clear();
			this.CheckNodes = false;
		}
	}

	private void DistrictWorldCursor_CitiesCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		if (e.Action == CollectionChangeAction.Remove && e.Element == this.City)
		{
			base.Back();
			if (this.City != null)
			{
				this.City.CityDistrictCollectionChange -= this.City_CityDistrictCollectionChange;
				this.City.Empire.GetAgency<DepartmentOfTheInterior>().CitiesCollectionChanged -= this.DistrictWorldCursor_CitiesCollectionChanged;
				this.City = null;
				this.CheckNodes = false;
			}
		}
	}

	private void SelectCreepingNodes()
	{
		global::Empire empire = this.PlayerControllerRepositoryService.ActivePlayerController.Empire as global::Empire;
		if (this.City.Empire.Index == empire.Index && empire.SimulationObject.Tags.Contains("FactionTraitMimics1"))
		{
			DepartmentOfCreepingNodes agency = empire.GetAgency<DepartmentOfCreepingNodes>();
			if (agency.Nodes.Count > 0)
			{
				ReadOnlyCollection<IWorldEntityWithCulling> readOnlyCollection;
				if (this.worldEntityCullingService.TryGetVisibleEntities<WorldCreepingNode>(out readOnlyCollection))
				{
					for (int i = 0; i < readOnlyCollection.Count; i++)
					{
						WorldCreepingNode worldCreepingNode = readOnlyCollection[i] as WorldCreepingNode;
						if (worldCreepingNode != null && worldCreepingNode.CreepingNode != null && worldCreepingNode.CreepingNode.Empire.Index == empire.Index)
						{
							this.ELCPDrawCreepingNode(worldCreepingNode.CreepingNode);
						}
					}
					return;
				}
				for (int j = 0; j < agency.Nodes.Count; j++)
				{
					CreepingNode creepingNode = agency.Nodes[j];
					this.ELCPDrawCreepingNode(creepingNode);
				}
			}
		}
	}

	private void CreateNodeBoundaryIFN(CreepingNode node, WorldArea nodeArea)
	{
		Diagnostics.Assert(node != null);
		Diagnostics.Assert(nodeArea != null);
		if (base.WorldEntityFactoryService == null)
		{
			return;
		}
		if (!this.nodeBoundryObjects.ContainsKey(node.GUID))
		{
			UnityEngine.Object @object = Resources.Load("Prefabs/Cities/VillageBoundary");
			if (@object != null)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(@object) as GameObject;
				if (gameObject != null)
				{
					Diagnostics.Assert(base.GlobalPositionningService != null);
					Diagnostics.Assert(base.WorldPositionningService != null);
					GameObject gameObject2 = null;
					if (base.WorldEntityFactoryService.TryGetValue(node.PointOfInterest.GUID, out gameObject2))
					{
						gameObject.transform.parent = gameObject2.transform;
						gameObject.transform.localPosition = new Vector3(0f, 0.03f, 0f);
						WorldAreaHexagonRenderer component = gameObject.GetComponent<WorldAreaHexagonRenderer>();
						Diagnostics.Assert(component != null);
						if (component == null)
						{
							return;
						}
						Color color = (node.Empire == null) ? Color.grey : node.Empire.Color;
						component.SetColor(color);
						component.SetWorldArea(nodeArea.WorldPositions, node.WorldPosition, base.GlobalPositionningService);
						component.SetMaterialSelectionStatus(false, true, true);
						component.SetOnDestroyLaunchAutokillAnimation(0.5f);
						this.nodeBoundryObjects.Add(node.GUID, gameObject);
					}
				}
			}
		}
	}

	private void CreateCreepingNodeHexFidsIFN(CreepingNode node, WorldArea worldArea)
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
		if (!base.WorldEntityFactoryService.TryGetValue(node.PointOfInterest.GUID, out gameObject))
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
		for (int i = 0; i < worldArea.WorldPositions.Count; i++)
		{
			Vector3 absoluteWorldPosition2D = abstractTerrainPatchRenderer.GlobalPositionningService.GetAbsoluteWorldPosition2D(worldArea.WorldPositions[i]);
			absoluteWorldPosition2D.x -= abstractTerrainPatchRenderer.OffsetX;
			absoluteWorldPosition2D.z -= abstractTerrainPatchRenderer.OffsetZ;
			InstancingHelper.SpawnOnePrefab(abstractTerrainPatchRenderer, defaultWorldViewTechnique, defaultWorldViewTechnique.HxTechniqueGraphicData.AllResourceFidsGraphicDatas.CityExploitationHexaData, absoluteWorldPosition2D, (int)worldArea.WorldPositions[i].Row, (int)worldArea.WorldPositions[i].Column, null, PrimitiveLayerMask.Fids, ref orCreateInstanciedMeshBlock, PrimitiveLayerMask.Fids, ref orCreateInstanciedMeshBlock2, false, 0, 0);
		}
		this.worldEntityHelperContent.CloseInstanciedMeshBlockIFN(abstractTerrainPatchRenderer);
		this.worldEntityHelperContent.Show();
	}

	private void ELCPDrawCreepingNode(CreepingNode creepingNode)
	{
		WorldArea worldArea = new WorldArea();
		if (base.WorldPositionningService != null)
		{
			int fidsiextractionRange = creepingNode.NodeDefinition.FIDSIExtractionRange;
			int index = creepingNode.PointOfInterest.Region.Index;
			for (int i = 0; i < creepingNode.ExploitedTiles.Count; i++)
			{
				if (creepingNode.ExploitedTiles[i].IsValid && (int)base.WorldPositionningService.GetRegionIndex(creepingNode.ExploitedTiles[i]) == creepingNode.PointOfInterest.Region.Index && base.WorldPositionningService.GetDistrict(creepingNode.ExploitedTiles[i]) == null && !base.WorldPositionningService.HasRidge(creepingNode.ExploitedTiles[i]))
				{
					this.ResourceRendererService.Add(creepingNode.ExploitedTiles[i]);
					worldArea.WorldPositions.Add(creepingNode.ExploitedTiles[i]);
				}
			}
			if (worldArea.WorldPositions.Count == 0)
			{
				worldArea.WorldPositions.Add(creepingNode.WorldPosition);
			}
			this.CreateNodeBoundaryIFN(creepingNode, worldArea);
			this.CreateCreepingNodeHexFidsIFN(creepingNode, worldArea);
		}
	}

	private void SelectCreepingNodes(int DrawCount = 1)
	{
		if (this.CheckNodes && this.departmentOfCreepingNodes.Nodes.Count > 0 && this.departmentOfCreepingNodes.Nodes.Count > this.nodeBoundryObjects.Count)
		{
			ReadOnlyCollection<IWorldEntityWithCulling> readOnlyCollection;
			if (this.NodesToDraw.Count == 0 && this.worldEntityCullingService.TryGetVisibleEntities<WorldCreepingNode>(out readOnlyCollection))
			{
				new List<CreepingNode>();
				for (int i = 0; i < readOnlyCollection.Count; i++)
				{
					WorldCreepingNode worldCreepingNode = readOnlyCollection[i] as WorldCreepingNode;
					if (worldCreepingNode != null && worldCreepingNode.CreepingNode != null && worldCreepingNode.CreepingNode.Empire.Index == this.City.Empire.Index && !this.nodeBoundryObjects.ContainsKey(worldCreepingNode.CreepingNode.GUID))
					{
						this.NodesToDraw.Add(worldCreepingNode.CreepingNode);
					}
				}
				int num;
				for (int j = 0; j < this.departmentOfCreepingNodes.Nodes.Count; j = num + 1)
				{
					if (!this.nodeBoundryObjects.ContainsKey(this.departmentOfCreepingNodes.Nodes[j].GUID))
					{
						this.NodesToDraw.Add(this.departmentOfCreepingNodes.Nodes[j]);
					}
					num = j;
				}
			}
			while (this.NodesToDraw.Count > 0 && DrawCount > 0)
			{
				CreepingNode creepingNode = this.NodesToDraw[0];
				this.NodesToDraw.RemoveAt(0);
				if (!this.nodeBoundryObjects.ContainsKey(creepingNode.GUID))
				{
					this.ELCPDrawCreepingNode(creepingNode);
					DrawCount--;
				}
			}
		}
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		if (base.IsActive && this.ResourceRendererService != null)
		{
			this.SelectCreepingNodes(1);
		}
	}

	private Dictionary<GameEntityGUID, GameObject> nodeBoundryObjects;

	private WorldEntityHelper.WorldEntityHelperContent worldEntityHelperContent;

	private IWorldEntityCullingService worldEntityCullingService;

	private List<CreepingNode> NodesToDraw;

	private DepartmentOfCreepingNodes departmentOfCreepingNodes;

	private bool CheckNodes;
}
