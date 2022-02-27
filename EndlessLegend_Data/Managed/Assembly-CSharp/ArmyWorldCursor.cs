using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.View;
using UnityEngine;

public class ArmyWorldCursor : GarrisonWorldCursor
{
	public ArmyWorldCursor()
	{
		this.IsAbleToBackOnLeftClick = true;
		this.IsAbleToBackOnRightClick = false;
		this.DeploymentAreaWidth = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Battle/DeploymentAreaWidth", 3);
		this.DeploymentAreaDepth = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Battle/DeploymentAreaDepth", 2);
	}

	public Army Army
	{
		get
		{
			if (this.WorldArmy != null)
			{
				return this.WorldArmy.Army;
			}
			return null;
		}
	}

	public override IGarrison Garrison
	{
		get
		{
			return this.Army;
		}
	}

	[Ancillary]
	private protected IPathRendererService PathRendererService { get; private set; }

	[Ancillary]
	private protected IPlayerControllerRepositoryService PlayerControllerRepositoryService { get; private set; }

	protected override WorldPosition GarrisonPosition
	{
		get
		{
			if (this.Army == null)
			{
				return WorldPosition.Invalid;
			}
			return this.Army.WorldPosition;
		}
	}

	private WorldPosition LastHighlightedWorldPosition { get; set; }

	private WorldPath TemporaryWorldPath { get; set; }

	private WorldArmy WorldArmy { get; set; }

	private WorldPath WorldPath { get; set; }

	public override bool HandleCancelRequest()
	{
		bool flag = base.HandleCancelRequest();
		if (flag)
		{
			WorldCursor.SelectedRegion = null;
		}
		return flag;
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
	}

	public void Dispose()
	{
	}

	public override string ToString()
	{
		return string.Format("{0}:{1}", base.ToString(), (this.Army == null) ? GameEntityGUID.Zero : this.Army.GUID);
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
			this.armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			Diagnostics.Assert(this.armyActionDatabase != null);
			this.allArmyActions = new List<ArmyAction>(this.armyActionDatabase.GetValues());
			return;
		}
		this.PlayerControllerRepositoryService = null;
		this.WorldArmy = null;
		this.battleTarget = null;
		this.armyActionDatabase = null;
		this.allArmyActions = null;
	}

	protected override void OnCursorActivate(bool activate, params object[] parameters)
	{
		base.OnCursorActivate(activate, parameters);
		if (!activate)
		{
			if (this.PathRendererService != null)
			{
				if (this.TemporaryWorldPath != null)
				{
					this.PathRendererService.RemovePath(this.TemporaryWorldPath);
					this.TemporaryWorldPath = null;
				}
				if (this.WorldPath != null)
				{
					this.PathRendererService.RemovePath(this.WorldPath);
					this.WorldPath = null;
				}
			}
			if (this.WorldArmy != null)
			{
				this.WorldArmy.Army.WorldPathChange -= this.Army_WorldPathChange;
				this.WorldArmy.Army.WorldPositionChange -= this.Army_WorldPositionChange;
				this.WorldArmy = null;
			}
			this.LastHighlightedWorldPosition = default(WorldPosition);
			this.LastHighlightedWorldPosition = WorldPosition.Invalid;
			this.battleTarget = null;
			this.HideBattleZone();
			return;
		}
		Diagnostics.Assert(base.GameEntityRepositoryService != null);
		Diagnostics.Assert(base.PathfindingService != null);
		Diagnostics.Assert(this.PlayerControllerRepositoryService != null);
		Diagnostics.Assert(base.WorldPositionningService != null);
		Diagnostics.Assert(this.PathRendererService != null);
		if (base.CursorTarget == null)
		{
			Diagnostics.LogError("Cursor target is null.");
			return;
		}
		this.WorldArmy = base.CursorTarget.GetComponent<WorldArmy>();
		if (this.WorldArmy == null)
		{
			Diagnostics.LogError("Invalid cursor target (cant find component of type: 'WorldArmy').");
			return;
		}
		if (this.PlayerControllerRepositoryService == null || this.PlayerControllerRepositoryService.ActivePlayerController == null)
		{
			return;
		}
		Diagnostics.Assert(this.WorldArmy.Army != null);
		if (this.WorldArmy.Army.Empire != this.PlayerControllerRepositoryService.ActivePlayerController.Empire)
		{
			return;
		}
		base.AddMouseCursorKey("PlayerArmyWorldCursor");
		for (int i = 0; i < this.allArmyActions.Count; i++)
		{
			StaticString staticString = this.allArmyActions[i].MouseCursorKey();
			if (staticString != null && this.allArmyActions[i].MayExecuteSomewhereLater(this.Army))
			{
				base.AddMouseCursorKey(staticString);
			}
		}
		base.Focus(this.Army, false);
		IAudioEventService service = Services.GetService<IAudioEventService>();
		Diagnostics.Assert(service != null);
		service.Play2DEvent("Gui/Interface/InGame/SelectUnit");
		if (this.WorldArmy.Army.WorldPath != null && this.WorldArmy.Army.WorldPath.Length > 1)
		{
			this.WorldPath = this.WorldArmy.Army.WorldPath;
			this.WorldPath.Rebuild(this.Army.WorldPosition, this.Army, this.Army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, PathfindingFlags.IgnoreArmies, false);
			this.PathRendererService.RenderPath(this.WorldPath, this.WorldArmy.Army.WorldPosition);
		}
		this.WorldArmy.Army.WorldPathChange += this.Army_WorldPathChange;
		this.WorldArmy.Army.WorldPositionChange += this.Army_WorldPositionChange;
	}

	protected override void OnCursorDown(MouseButton mouseButton, Amplitude.Unity.View.CursorTarget[] cursorTargets)
	{
		base.OnCursorDown(mouseButton, cursorTargets);
		if (mouseButton == MouseButton.Right && this.Army != null)
		{
			if (this.Army.Empire != this.PlayerControllerRepositoryService.ActivePlayerController.Empire)
			{
				return;
			}
			if (this.WorldPath != null)
			{
				this.PathRendererService.RemovePath(this.WorldPath);
			}
			if (this.TemporaryWorldPath != null)
			{
				this.PathRendererService.RemovePath(this.TemporaryWorldPath);
				this.TemporaryWorldPath = null;
			}
			this.TemporaryWorldPath = new WorldPath();
			this.GeneratePath();
		}
	}

	protected override void OnCursorDrag(MouseButton mouseButton, Amplitude.Unity.View.CursorTarget[] cursorTargets)
	{
		base.OnCursorDrag(mouseButton, cursorTargets);
		if (mouseButton == MouseButton.Right)
		{
			if (this.Army == null)
			{
				return;
			}
			if (this.Army.Empire != this.PlayerControllerRepositoryService.ActivePlayerController.Empire)
			{
				return;
			}
			if (base.IsTransferable(WorldCursor.HighlightedWorldPosition))
			{
				if (this.WorldPath != null)
				{
					this.PathRendererService.RemovePath(this.WorldPath);
				}
				if (this.TemporaryWorldPath != null)
				{
					this.PathRendererService.RemovePath(this.TemporaryWorldPath);
					this.TemporaryWorldPath = null;
				}
				return;
			}
			if (WorldCursor.HighlightedWorldPosition.IsValid && !this.LastHighlightedWorldPosition.Equals(WorldCursor.HighlightedWorldPosition) && !this.LastUnpathableWorldPosition.Equals(WorldCursor.HighlightedWorldPosition))
			{
				if (this.TemporaryWorldPath == null)
				{
					this.TemporaryWorldPath = new WorldPath();
				}
				this.GeneratePath();
			}
		}
	}

	protected override void OnCursorUp(MouseButton mouseButton, Amplitude.Unity.View.CursorTarget[] cursorTargets)
	{
		base.OnCursorUp(mouseButton, cursorTargets);
		if (mouseButton == MouseButton.Right && this.Army != null)
		{
			if (this.Army.Empire != this.PlayerControllerRepositoryService.ActivePlayerController.Empire)
			{
				return;
			}
			this.ValidatePath();
		}
	}

	protected override void WorldViewTechniqueChange(WorldViewTechnique technique)
	{
		base.WorldViewTechniqueChange(technique);
		if (technique != null)
		{
			this.PathRendererService = technique.Services.GetService<IPathRendererService>();
			if (this.PathRendererService == null)
			{
				Diagnostics.LogError("Invalid null path renderer service.");
				return;
			}
		}
		else
		{
			this.PathRendererService = null;
		}
	}

	private void Army_WorldPathChange(object sender, ArmyWorldPathChangeEventArgs e)
	{
		if (this.WorldPath != null)
		{
			this.PathRendererService.RemovePath(this.WorldPath);
		}
		this.WorldPath = this.WorldArmy.Army.WorldPath;
		if (this.WorldPath != null)
		{
			this.WorldPath.Rebuild(this.Army.WorldPosition, this.Army, this.Army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, PathfindingFlags.IgnoreArmies, false);
		}
		this.PathRendererService.RenderPath(this.WorldPath, this.WorldArmy.Army.WorldPosition);
	}

	private void Army_WorldPositionChange(object sender, ArmyWorldPositionChangeEventArgs e)
	{
		base.OnGarrisonPositionChanged();
		if (this.TemporaryWorldPath != null)
		{
			this.GeneratePath();
			return;
		}
		if (this.WorldPath != null)
		{
			if (this.WorldArmy.Army.WorldPath != null)
			{
				this.PathRendererService.RefreshCurrentPosition(this.WorldPath, this.WorldArmy.Army.WorldPosition);
				return;
			}
			this.PathRendererService.RemovePath(this.WorldPath);
			this.WorldPath = null;
		}
	}

	private bool BuildWorldPath()
	{
		this.TemporaryWorldPath.Clear();
		if (this.LastHighlightedWorldPosition == WorldPosition.Invalid)
		{
			return false;
		}
		if (base.IsTransferable(WorldCursor.HighlightedWorldPosition))
		{
			return false;
		}
		if ((!base.PathfindingService.IsTileStopable(this.LastHighlightedWorldPosition, base.PathfindingContext, (PathfindingFlags)0, null) || !base.PathfindingService.IsTilePassable(this.LastHighlightedWorldPosition, base.PathfindingContext, (PathfindingFlags)0, null) || this.OtherEmpireCreepingNodeAtPosition(this.Army.Empire, this.LastHighlightedWorldPosition)) && base.PathfindingService.IsTransitionPassable(this.GarrisonPosition, this.LastHighlightedWorldPosition, base.PathfindingContext, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
		{
			return true;
		}
		PathfindingFlags pathfindingFlags = PathfindingFlags.IgnoreArmies;
		District district = base.WorldPositionningService.GetDistrict(this.GarrisonPosition);
		if (district != null && (district.Type == DistrictType.Center || district.Type == DistrictType.Extension) && district.City != null && district.City.BesiegingEmpireIndex != -1)
		{
			pathfindingFlags &= ~PathfindingFlags.IgnoreArmies;
		}
		Army army = null;
		if (this.LastHighlightedWorldPosition != WorldPosition.Invalid)
		{
			army = base.WorldPositionningService.GetArmyAtPosition(this.LastHighlightedWorldPosition);
		}
		bool flag = base.VisibilityService.IsWorldPositionDetectedFor(this.LastHighlightedWorldPosition, this.Army.Empire) || (army != null && !army.IsCamouflaged);
		bool flag2 = base.WorldPositionningService.GetDistance(this.GarrisonPosition, this.LastHighlightedWorldPosition) <= this.Army.LineOfSightVisionRange;
		if (flag && flag2)
		{
			pathfindingFlags &= ~PathfindingFlags.IgnoreArmies;
		}
		PathfindingResult pathfindingResult = base.PathfindingService.FindPath(base.PathfindingContext, this.GarrisonPosition, this.LastHighlightedWorldPosition, PathfindingManager.RequestMode.Default, null, pathfindingFlags, null);
		if (base.WorldPositionningService != null && pathfindingResult != null && this.LastHighlightedWorldPosition != WorldPosition.Invalid)
		{
			if (!base.PathfindingService.IsTileStopable(this.LastHighlightedWorldPosition, base.PathfindingContext, (PathfindingFlags)0, null))
			{
				WorldPosition lastHighlightedWorldPosition = this.LastHighlightedWorldPosition;
				WorldPosition position = pathfindingResult.GetCompletePath().Last<WorldPosition>();
				bool flag3 = base.WorldPositionningService.IsWaterTile(lastHighlightedWorldPosition);
				bool flag4 = base.WorldPositionningService.IsWaterTile(position);
				if (flag3 != flag4)
				{
					World world = (base.GameService.Game as global::Game).World;
					List<WorldPosition> neighbours = lastHighlightedWorldPosition.GetNeighbours(world.WorldParameters);
					List<WorldPosition> list = new List<WorldPosition>();
					for (int i = 0; i < neighbours.Count; i++)
					{
						if (base.WorldPositionningService.IsWaterTile(neighbours[i]) == flag3 && !base.WorldPositionningService.HasRidge(neighbours[i]) && base.PathfindingService.IsTileStopable(neighbours[i], base.PathfindingContext, (PathfindingFlags)0, null))
						{
							list.Add(neighbours[i]);
						}
					}
					if (list.Count > 0)
					{
						PathfindingResult pathfindingResult2 = null;
						float num = float.MaxValue;
						for (int j = 0; j < list.Count; j++)
						{
							PathfindingResult pathfindingResult3 = base.PathfindingService.FindPath(base.PathfindingContext, this.GarrisonPosition, list[j], PathfindingManager.RequestMode.Default, null, pathfindingFlags, null);
							if (pathfindingResult3 != null && pathfindingResult3.GetCost() < num)
							{
								num = pathfindingResult3.GetCost();
								pathfindingResult2 = pathfindingResult3;
							}
						}
						if (pathfindingResult2 != null)
						{
							pathfindingResult = pathfindingResult2;
						}
					}
				}
			}
			else if (this.OtherEmpireCreepingNodeAtPosition(this.Army.Empire, this.LastHighlightedWorldPosition))
			{
				List<WorldPosition> list2 = pathfindingResult.GetCompletePath().ToList<WorldPosition>();
				if (list2.Count > 0)
				{
					list2.RemoveAt(list2.Count - 1);
					PathfindingResult pathfindingResult4 = base.PathfindingService.FindPath(base.PathfindingContext, this.GarrisonPosition, list2.Last<WorldPosition>(), PathfindingManager.RequestMode.Default, null, pathfindingFlags, null);
					if (pathfindingResult4 != null)
					{
						pathfindingResult = pathfindingResult4;
						this.TemporaryWorldPath.Build(pathfindingResult, this.WorldArmy.Army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
						return true;
					}
				}
			}
		}
		if (pathfindingResult == null)
		{
			return base.PathfindingService.IsTransitionPassable(this.GarrisonPosition, this.LastHighlightedWorldPosition, base.PathfindingContext, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null);
		}
		Army army2 = this.WorldArmy.Army;
		this.TemporaryWorldPath.Build(pathfindingResult, army2.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
		return true;
	}

	private void SelectTarget()
	{
		this.battleTarget = null;
		if (this.LastHighlightedWorldPosition == WorldPosition.Invalid)
		{
			return;
		}
		if (!base.VisibilityService.IsWorldPositionVisibleFor(this.LastHighlightedWorldPosition, this.Army.Empire))
		{
			return;
		}
		Region region = base.WorldPositionningService.GetRegion(this.LastHighlightedWorldPosition);
		if (region == null || this.battleTarget != null)
		{
			return;
		}
		PointOfInterest pointOfInterest = region.PointOfInterests.FirstOrDefault((PointOfInterest match) => match.WorldPosition == this.LastHighlightedWorldPosition);
		if (pointOfInterest != null && (pointOfInterest.Type == Fortress.Citadel || pointOfInterest.Type == Fortress.Facility))
		{
			Fortress fortressAt = region.NavalEmpire.GetAgency<PirateCouncil>().GetFortressAt(pointOfInterest.WorldPosition);
			this.battleTarget = fortressAt;
		}
		if (region != null)
		{
			District district = null;
			if (region.City != null && region.City.Empire != this.Army.Empire)
			{
				district = region.City.Districts.FirstOrDefault((District match) => match.WorldPosition == this.LastHighlightedWorldPosition);
			}
			if (district != null && district.Type != DistrictType.Exploitation)
			{
				this.battleTarget = district;
			}
		}
		if (this.battleTarget == null && region != null && region.City != null && region.City.Camp != null && region.City.Camp.WorldPosition == this.LastHighlightedWorldPosition && region.City.Empire != this.Army.Empire)
		{
			this.battleTarget = region.City.Camp;
		}
		if (this.battleTarget == null)
		{
			Army armyAtPosition = base.WorldPositionningService.GetArmyAtPosition(this.LastHighlightedWorldPosition);
			if (armyAtPosition != null && (!armyAtPosition.IsCamouflaged || base.VisibilityService.IsWorldPositionDetectedFor(this.LastHighlightedWorldPosition, this.Army.Empire)))
			{
				this.battleTarget = armyAtPosition;
			}
		}
		if (region != null && this.battleTarget == null)
		{
			pointOfInterest = region.PointOfInterests.FirstOrDefault((PointOfInterest match) => match.WorldPosition == this.LastHighlightedWorldPosition);
			if (pointOfInterest != null)
			{
				if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero)
				{
					CreepingNode creepingNode = null;
					base.GameEntityRepositoryService.TryGetValue<CreepingNode>(pointOfInterest.CreepingNodeGUID, out creepingNode);
					if (creepingNode != null)
					{
						if (creepingNode.Empire.Index != this.Army.Empire.Index)
						{
							if (ELCPUtilities.UseELCPPeacefulCreepingNodes)
							{
								DepartmentOfForeignAffairs agency = this.Army.Empire.GetAgency<DepartmentOfForeignAffairs>();
								if (agency != null && agency.IsFriend(creepingNode.Empire))
								{
									if (pointOfInterest.Type == "QuestLocation")
									{
										this.battleTarget = pointOfInterest;
									}
									else if (pointOfInterest.Type == "Village")
									{
										IQuestManagementService service = base.GameService.Game.Services.GetService<IQuestManagementService>();
										IQuestRepositoryService service2 = base.GameService.Game.Services.GetService<IQuestRepositoryService>();
										IEnumerable<QuestMarker> markersByBoundTargetGUID = service.GetMarkersByBoundTargetGUID(pointOfInterest.GUID);
										bool flag = false;
										foreach (QuestMarker questMarker in markersByBoundTargetGUID)
										{
											Quest quest;
											if (service2.TryGetValue(questMarker.QuestGUID, out quest) && quest.EmpireBits == this.Army.Empire.Bits)
											{
												Village villageAt = region.MinorEmpire.GetAgency<BarbarianCouncil>().GetVillageAt(pointOfInterest.WorldPosition);
												this.battleTarget = villageAt;
												flag = true;
												break;
											}
										}
										if (!flag)
										{
											this.battleTarget = creepingNode;
										}
									}
									else
									{
										this.battleTarget = creepingNode;
									}
								}
							}
							else
							{
								this.battleTarget = creepingNode;
							}
						}
						else if (pointOfInterest.Type == "QuestLocation" || pointOfInterest.Type == "NavalQuestLocation")
						{
							this.battleTarget = pointOfInterest;
						}
					}
				}
				else if (pointOfInterest.Type == "Village")
				{
					Village villageAt2 = region.MinorEmpire.GetAgency<BarbarianCouncil>().GetVillageAt(pointOfInterest.WorldPosition);
					this.battleTarget = villageAt2;
				}
				else if (pointOfInterest.Type == "QuestLocation" || pointOfInterest.Type == "NavalQuestLocation")
				{
					this.battleTarget = pointOfInterest;
				}
			}
		}
		if (region != null && this.battleTarget == null && region.KaijuEmpire != null && region.Kaiju != null)
		{
			KaijuGarrison kaijuGarrison = region.Kaiju.KaijuGarrison;
			if (kaijuGarrison.WorldPosition == this.LastHighlightedWorldPosition)
			{
				this.battleTarget = kaijuGarrison;
			}
		}
		if (this.battleTarget == null)
		{
			TerraformDevice deviceAtPosition = (base.GameService.Game as global::Game).GetService<ITerraformDeviceService>().GetDeviceAtPosition(this.LastHighlightedWorldPosition);
			if (deviceAtPosition != null)
			{
				this.battleTarget = deviceAtPosition;
			}
		}
	}

	private void UpdateBattlegroundVisibility()
	{
		bool flag = false;
		if (this.battleTarget != null)
		{
			if (this.battleTarget is Army)
			{
				if (((Army)this.battleTarget).Empire.Index != this.Army.Empire.Index)
				{
					flag = true;
				}
			}
			else if (this.battleTarget is Fortress)
			{
				if (((Fortress)this.battleTarget).Empire.Index != this.Army.Empire.Index)
				{
					flag = true;
				}
			}
			else if (this.battleTarget is Village)
			{
				if (!((Village)this.battleTarget).HasBeenPacified)
				{
					flag = true;
				}
			}
			else if (this.battleTarget is District)
			{
				if (((District)this.battleTarget).Empire.Index != this.Army.Empire.Index)
				{
					flag = true;
				}
			}
			else if (this.battleTarget is Camp)
			{
				if (((Camp)this.battleTarget).Empire.Index != this.Army.Empire.Index)
				{
					flag = true;
				}
			}
			else if (this.battleTarget is KaijuGarrison && ((KaijuGarrison)this.battleTarget).Empire.Index != this.Army.Empire.Index)
			{
				flag = true;
			}
			if (flag)
			{
				WorldPosition worldPosition = this.battleTarget.WorldPosition;
				WorldPosition worldPosition2 = this.Army.WorldPosition;
				if (this.TemporaryWorldPath != null && this.TemporaryWorldPath.Length > 1)
				{
					worldPosition2 = this.TemporaryWorldPath.WorldPositions[this.TemporaryWorldPath.Length - 1];
					if (worldPosition2 == worldPosition)
					{
						if (this.TemporaryWorldPath.Length > 2)
						{
							worldPosition2 = this.TemporaryWorldPath.WorldPositions[this.TemporaryWorldPath.Length - 2];
						}
						else
						{
							worldPosition2 = this.Army.WorldPosition;
						}
					}
				}
				WorldOrientation orientation = base.WorldPositionningService.GetOrientation(worldPosition2, worldPosition);
				this.GenerateBattleZone(worldPosition2, orientation);
				return;
			}
		}
		this.HideBattleZone();
	}

	private void HideBattleZone()
	{
		if (this.battleZoneVisible)
		{
			this.battleZoneVisible = false;
			if (this.battleZoneGameObject != null)
			{
				this.HideBattleZoneGameObject(this.battleDeploymentAreaAttacker);
				this.HideBattleZoneGameObject(this.battleDeploymentAreaDefender);
				this.HideBattleZoneGameObject(this.battleAreaBoundary);
				this.battleZoneGameObject.SetActive(false);
			}
		}
	}

	private void HideBattleZoneGameObject(GameObject battleZoneSubGameObject)
	{
		if (battleZoneSubGameObject != null)
		{
			WorldAreaHexagonRenderer component = battleZoneSubGameObject.GetComponent<WorldAreaHexagonRenderer>();
			if (component != null)
			{
				component.SetMaterialSelectionStatus(false, false, true);
			}
		}
	}

	private void UpdatePathRenderer()
	{
		if (this.LastHighlightedWorldPosition == WorldPosition.Invalid)
		{
			if (this.TemporaryWorldPath != null)
			{
				this.PathRendererService.RemovePath(this.TemporaryWorldPath);
				this.TemporaryWorldPath.Clear();
				return;
			}
		}
		else if (this.TemporaryWorldPath != null)
		{
			this.PathRendererService.RenderPath(this.TemporaryWorldPath, this.Army.WorldPosition);
		}
	}

	private void GeneratePath()
	{
		this.LastHighlightedWorldPosition = WorldCursor.HighlightedWorldPosition;
		if (this.LastHighlightedWorldPosition == this.WorldArmy.WorldPosition)
		{
			this.LastHighlightedWorldPosition = WorldPosition.Invalid;
		}
		if (!this.BuildWorldPath())
		{
			this.LastUnpathableWorldPosition = WorldCursor.HighlightedWorldPosition;
			this.LastHighlightedWorldPosition = WorldPosition.Invalid;
		}
		this.SelectTarget();
		this.UpdateBattlegroundVisibility();
		this.UpdatePathRenderer();
	}

	private void GenerateBattleZone(WorldPosition attackerPosition, WorldOrientation orientation)
	{
		World world = (base.GameService.Game as global::Game).World;
		IGlobalPositionningService globalPositionningService = null;
		if (globalPositionningService == null)
		{
			WorldView worldView = Services.GetService<Amplitude.Unity.View.IViewService>().CurrentView as WorldView;
			if (worldView != null && worldView.CurrentWorldViewTechnique != null)
			{
				globalPositionningService = worldView.CurrentWorldViewTechnique.Services.GetService<IGlobalPositionningService>();
			}
		}
		if (!attackerPosition.IsValid)
		{
			return;
		}
		WorldPosition worldPosition = attackerPosition;
		DeploymentArea deploymentArea = new DeploymentArea(worldPosition, orientation, world.WorldParameters);
		deploymentArea.Initialize(this.DeploymentAreaWidth, this.DeploymentAreaDepth);
		WorldArea worldArea = new WorldArea(deploymentArea.GetWorldPositions(world.WorldParameters));
		WorldOrientation forward = orientation.Rotate(3);
		DeploymentArea deploymentArea2 = new DeploymentArea(WorldPosition.GetNeighbourTile(worldPosition, orientation, 1), forward, world.WorldParameters);
		deploymentArea2.Initialize(this.DeploymentAreaWidth, this.DeploymentAreaDepth);
		WorldArea worldArea2 = new WorldArea(deploymentArea2.GetWorldPositions(world.WorldParameters));
		WorldArea worldArea3 = new WorldArea(worldArea.Grow(world.WorldParameters));
		worldArea3 = worldArea3.Union(worldArea2.Grow(world.WorldParameters));
		WorldPatch worldPatch = globalPositionningService.GetWorldPatch(attackerPosition);
		this.GenerateGeometry(worldPatch, attackerPosition, worldArea3, worldArea, worldArea2);
	}

	private void GenerateGeometry(WorldPatch patchParent, WorldPosition attackerPosition, WorldArea battleArea, WorldArea attackerDeploymentArea, WorldArea defenderDeploymentArea)
	{
		if (this.battleZoneGameObject == null)
		{
			this.battleZoneGameObject = new GameObject("BattleArea");
			this.battleDeploymentAreaAttacker = this.InstantiateGameObjectFromPrefabNameAndInitMeshFilter("Prefabs/Encounters/ArmyWorldCursorDeploymentArea", this.battleZoneGameObject, false);
			this.battleDeploymentAreaDefender = this.InstantiateGameObjectFromPrefabNameAndInitMeshFilter("Prefabs/Encounters/ArmyWorldCursorDeploymentArea", this.battleZoneGameObject, false);
			this.battleAreaBoundary = this.InstantiateGameObjectFromPrefabNameAndInitMeshFilter("Prefabs/Encounters/ArmyWorldCursorEncounterBoundary", this.battleZoneGameObject, false);
		}
		this.battleZoneGameObject.transform.parent = patchParent.RootedTransform;
		this.battleZoneGameObject.transform.localPosition = new Vector3(0f, 0.005f, 0f);
		if (this.battleAreaBoundary != null)
		{
			this.battleAreaBoundary.transform.localPosition = Vector3.zero;
			WorldAreaHexagonRenderer component = this.battleAreaBoundary.GetComponent<WorldAreaHexagonRenderer>();
			component.SetWorldArea(battleArea, attackerPosition, base.GlobalPositionningService);
			component.SetMaterialSelectionStatus(false, true, true);
		}
		if (this.battleDeploymentAreaAttacker != null)
		{
			this.battleDeploymentAreaAttacker.transform.localPosition = new Vector3(0f, 0.005f, 0f);
			WorldAreaHexagonRenderer component2 = this.battleDeploymentAreaAttacker.GetComponent<WorldAreaHexagonRenderer>();
			component2.SetWorldArea(attackerDeploymentArea, attackerPosition, base.GlobalPositionningService);
			Color color = this.WorldArmy.Army.Empire.Color;
			color.a = this.attackerAlphaValue;
			component2.SetColor(color);
			component2.SetMaterialSelectionStatus(false, true, true);
		}
		if (this.battleDeploymentAreaDefender != null)
		{
			this.battleDeploymentAreaDefender.transform.localPosition = new Vector3(0f, 0.005f, 0f);
			WorldAreaHexagonRenderer component3 = this.battleDeploymentAreaDefender.GetComponent<WorldAreaHexagonRenderer>();
			component3.SetWorldArea(defenderDeploymentArea, attackerPosition, base.GlobalPositionningService);
			Color color2 = new Color(1f, 0f, 0f, this.defenderAlphaValue);
			if (this.battleTarget is Garrison)
			{
				Garrison garrison = this.battleTarget as Garrison;
				if (garrison.Empire != null)
				{
					color2 = garrison.Empire.Color;
					Army army = garrison as Army;
					if (army != null && army.IsPrivateers)
					{
						IPlayerControllerRepositoryService service = Services.GetService<IGameService>().Game.Services.GetService<IPlayerControllerRepositoryService>();
						if (service.ActivePlayerController != null && service.ActivePlayerController.Empire != null && service.ActivePlayerController.Empire.Index != garrison.Empire.Index)
						{
							color2 = global::Game.PrivateersColor;
						}
					}
					color2.a = this.defenderAlphaValue;
				}
			}
			else if (this.battleTarget is District)
			{
				color2 = ((District)this.battleTarget).Empire.Color;
			}
			component3.SetColor(color2);
			component3.SetMaterialSelectionStatus(false, true, true);
		}
		this.battleZoneGameObject.SetActive(true);
		this.battleZoneVisible = true;
	}

	private GameObject InstantiateGameObjectFromPrefabNameAndInitMeshFilter(string prefabName, GameObject parent, bool allowNull)
	{
		UnityEngine.Object @object = Resources.Load(prefabName);
		if (@object == null)
		{
			if (!allowNull)
			{
				Diagnostics.LogError("Unable to load prefab [{0}]", new object[]
				{
					prefabName
				});
			}
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(@object) as GameObject;
		gameObject.transform.parent = parent.transform;
		return gameObject;
	}

	private void ValidatePath()
	{
		IGuiService service = Services.GetService<IGuiService>();
		IAudioEventService service2 = Services.GetService<IAudioEventService>();
		Diagnostics.Assert(service2 != null);
		bool flag = base.SelectedUnits.Length == 0 || base.SelectedUnits.Length == this.Army.StandardUnits.Count || (this.Garrison.UnitsCount == 1 && this.Garrison.Hero != null);
		if (!base.IsTransferable(WorldCursor.HighlightedWorldPosition) && flag)
		{
			global::Empire empire = this.WorldArmy.Army.Empire;
			if (this.battleTarget is Army)
			{
				Army army = this.battleTarget as Army;
				if (army.Empire.Index == this.Army.Empire.Index)
				{
					int num = Mathf.Max(army.MaximumUnitSlot - army.CurrentUnitSlot, 0);
					if (this.Army.HasCatspaw || army.HasCatspaw)
					{
						QuickWarningPanel.Show("%UnitsTransferImpossibleWithCatsPawArmy");
					}
					else if (this.Army.StandardUnits.Count > num)
					{
						QuickWarningPanel.Show(string.Format(AgeLocalizer.Instance.LocalizeString("%UnitsTransferNotEnoughSlotsInTargetArmy"), num));
					}
					else if (this.Army.GetPropertyValue(SimulationProperties.Movement) <= 0f && base.WorldPositionningService.GetDistance(this.Army.WorldPosition, army.WorldPosition) == 1)
					{
						QuickWarningPanel.Show(AgeLocalizer.Instance.LocalizeString("%" + ArmyAction_TransferUnits.NotEnoughMovementToTransfer + "Description"));
					}
					else if (this.TemporaryWorldPath != null && this.TemporaryWorldPath.Length > 1 && !this.Army.HasCatspaw && !army.HasCatspaw)
					{
						OrderGoToAndMerge order = new OrderGoToAndMerge(empire.Index, this.WorldArmy.Army.GUID, this.battleTarget.GUID, (from unit in base.SelectedUnits
						select unit.GUID).ToArray<GameEntityGUID>(), this.TemporaryWorldPath);
						this.PlayerControllerRepositoryService.ActivePlayerController.PostOrder(order);
					}
				}
				else
				{
					DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
					Diagnostics.Assert(agency != null);
					bool flag2 = this.Army.IsNaval == army.IsNaval;
					if ((agency.CanAttack(this.battleTarget) || this.WorldArmy.Army.IsPrivateers) && flag2)
					{
						ArmyAction armyAction = null;
						IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
						float cost = 0f;
						if (database != null && database.TryGetValue(ArmyAction_Attack.ReadOnlyName, out armyAction))
						{
							cost = armyAction.GetCostInActionPoints();
						}
						if (!this.Army.HasEnoughActionPoint(cost))
						{
							QuickWarningPanel.Show(AgeLocalizer.Instance.LocalizeString("%ArmyNotEnoughActionPointsDescription"));
						}
						else if (this.TemporaryWorldPath != null && this.TemporaryWorldPath.Length > 1)
						{
							OrderGoToAndAttack order2 = new OrderGoToAndAttack(empire.Index, this.WorldArmy.Army.GUID, this.battleTarget.GUID, this.TemporaryWorldPath);
							this.PlayerControllerRepositoryService.ActivePlayerController.PostOrder(order2);
						}
						else
						{
							OrderAttack order3 = new OrderAttack(empire.Index, this.WorldArmy.Army.GUID, this.battleTarget.GUID);
							this.PlayerControllerRepositoryService.ActivePlayerController.PostOrder(order3);
						}
						service2.Play2DEvent("Gui/Interface/InGame/OrderAttack");
					}
					else if (army.Empire is MajorEmpire && flag2)
					{
						global::Empire empire2 = army.Empire;
						service.GetGuiPanel<WarDeclarationModalPanel>().Show(new object[]
						{
							empire2,
							"AttackTarget"
						});
					}
				}
			}
			else
			{
				bool flag3 = false;
				if (this.battleTarget != null)
				{
					if (this.TemporaryWorldPath != null && this.TemporaryWorldPath.Length > 1)
					{
						flag3 = (this.TemporaryWorldPath.ControlPoints.Length == 0);
						flag3 |= (this.TemporaryWorldPath.ControlPoints.Length == 1 && (int)this.TemporaryWorldPath.ControlPoints[0] == this.TemporaryWorldPath.Length - 1);
					}
					else
					{
						flag3 = base.PathfindingService.IsTransitionPassable(this.Army.WorldPosition, this.battleTarget.WorldPosition, this.Army, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null);
					}
				}
				if (flag3 || this.battleTarget is Fortress || this.battleTarget is KaijuGarrison)
				{
					flag3 = false;
					bool flag4 = true;
					if (this.battleTarget is KaijuGarrison)
					{
						KaijuGarrison kaijuGarrison = this.battleTarget as KaijuGarrison;
						DepartmentOfForeignAffairs agency2 = empire.GetAgency<DepartmentOfForeignAffairs>();
						Diagnostics.Assert(agency2 != null);
						if (!agency2.CanAttack(this.battleTarget))
						{
							flag4 = false;
							global::Empire empire3 = kaijuGarrison.Empire;
							service.GetGuiPanel<WarDeclarationModalPanel>().Show(new object[]
							{
								empire3,
								"AttackTarget"
							});
						}
					}
					if (flag4)
					{
						ArmyActionModalPanel guiPanel = service.GetGuiPanel<ArmyActionModalPanel>();
						if (guiPanel != null)
						{
							WorldPosition destination = (this.TemporaryWorldPath == null || this.TemporaryWorldPath.Length <= 1) ? WorldPosition.Invalid : this.TemporaryWorldPath.Destination;
							if (guiPanel.CheckForArmyActionsAvailability(this.Army, this.battleTarget, destination).Count > 0)
							{
								flag3 = true;
							}
						}
					}
				}
				if (flag3 && this.battleTarget != null && !this.Army.IsPrivateers)
				{
					MajorEmpire majorEmpire = null;
					if (this.battleTarget is Garrison)
					{
						majorEmpire = ((this.battleTarget as Garrison).Empire as MajorEmpire);
					}
					else if (this.battleTarget is District)
					{
						majorEmpire = ((this.battleTarget as District).Empire as MajorEmpire);
					}
					if (majorEmpire != null && majorEmpire.Index != this.Army.Empire.Index)
					{
						flag3 = this.Army.Empire.GetAgency<DepartmentOfForeignAffairs>().IsSymetricallyDiscovered(majorEmpire);
					}
				}
				if (flag3)
				{
					WorldPosition worldPosition = (this.TemporaryWorldPath == null || this.TemporaryWorldPath.Length <= 1) ? WorldPosition.Invalid : this.TemporaryWorldPath.Destination;
					service.Show(typeof(ArmyActionModalPanel), new object[]
					{
						this.Army,
						this.battleTarget,
						base.SelectedUnits,
						worldPosition
					});
				}
				else if (this.TemporaryWorldPath != null && this.TemporaryWorldPath.Destination != WorldPosition.Invalid)
				{
					OrderGoTo order4 = new OrderGoTo(empire.Index, this.WorldArmy.Army.GUID, this.TemporaryWorldPath.Destination);
					this.PlayerControllerRepositoryService.ActivePlayerController.PostOrder(order4);
					service2.Play2DEvent("Gui/Interface/InGame/OrderGoTo");
				}
				else if (WorldCursor.HighlightedWorldPosition != WorldPosition.Invalid)
				{
					Region region = base.WorldPositionningService.GetRegion(WorldCursor.HighlightedWorldPosition);
					if (region != null && region.City != null && region.City.Empire != null)
					{
						DepartmentOfForeignAffairs agency3 = this.Army.Empire.GetAgency<DepartmentOfForeignAffairs>();
						Diagnostics.Assert(agency3 != null);
						if (!agency3.CanMoveOn(WorldCursor.HighlightedWorldPosition, this.WorldArmy.Army.IsPrivateers, this.WorldArmy.Army.IsCamouflaged))
						{
							District district = base.WorldPositionningService.GetDistrict(WorldCursor.HighlightedWorldPosition);
							if ((district == null || district.Type == DistrictType.Exploitation) && region.City.Empire is MajorEmpire)
							{
								service.GetGuiPanel<WarDeclarationModalPanel>().Show(new object[]
								{
									region.City.Empire,
									"BreakCloseBorder"
								});
							}
						}
					}
				}
			}
		}
		this.LastHighlightedWorldPosition = WorldPosition.Invalid;
		this.LastUnpathableWorldPosition = WorldPosition.Invalid;
		this.SelectTarget();
		this.UpdateBattlegroundVisibility();
		if (this.TemporaryWorldPath != null)
		{
			this.PathRendererService.RemovePath(this.TemporaryWorldPath);
			this.TemporaryWorldPath = null;
		}
	}

	private bool OtherEmpireCreepingNodeAtPosition(global::Empire empire, WorldPosition worldPosition)
	{
		if (!worldPosition.IsValid)
		{
			return false;
		}
		PointOfInterest pointOfInterest = base.WorldPositionningService.GetPointOfInterest(worldPosition);
		if (pointOfInterest == null || pointOfInterest.CreepingNodeGUID == GameEntityGUID.Zero)
		{
			return false;
		}
		CreepingNode creepingNode = null;
		base.GameEntityRepositoryService.TryGetValue<CreepingNode>(pointOfInterest.CreepingNodeGUID, out creepingNode);
		if (creepingNode != null && empire != null && creepingNode.Empire != null && pointOfInterest.Empire.Index != empire.Index && creepingNode.DismantlingArmy == null)
		{
			if (!ELCPUtilities.UseELCPPeacefulCreepingNodes)
			{
				return true;
			}
			if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != empire)
			{
				if (pointOfInterest.Empire == null)
				{
					return true;
				}
				if (!(pointOfInterest.Empire is MajorEmpire))
				{
					return true;
				}
				DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
				if (agency == null)
				{
					return true;
				}
				if (!agency.IsFriend(pointOfInterest.Empire))
				{
					return true;
				}
			}
		}
		return false;
	}

	private WorldPosition LastUnpathableWorldPosition { get; set; }

	public int DeploymentAreaWidth = 3;

	public int DeploymentAreaDepth = 2;

	private IGameEntityWithWorldPosition battleTarget;

	private GameObject battleZoneGameObject;

	private GameObject battleDeploymentAreaAttacker;

	private GameObject battleDeploymentAreaDefender;

	private GameObject battleAreaBoundary;

	private float attackerAlphaValue = 0.8f;

	private float defenderAlphaValue = 0.8f;

	private bool battleZoneVisible;

	private IDatabase<ArmyAction> armyActionDatabase;

	private List<ArmyAction> allArmyActions;
}
