using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Math;
using Amplitude.Unity.Runtime;
using Amplitude.Utilities.Maps;
using UnityEngine;

public class VisibilityController : GameAncillary, IService, IVisibilityService
{
	public VisibilityController()
	{
		this.hexagonPointAnglesBuffer = new float[6];
		this.hexagonPointsBuffer = new Vector2[6];
		this.potentialPositions = new List<VisibilityController.Position>();
		this.rectVirtualPositions = new List<WorldPosition>();
		this.tempNewArcs = new Arc[3];
		base..ctor();
		this.stealVisionFromEmpireTags = new StaticString[]
		{
			new StaticString("StealVisionFromEmpire0"),
			new StaticString("StealVisionFromEmpire1"),
			new StaticString("StealVisionFromEmpire2"),
			new StaticString("StealVisionFromEmpire3"),
			new StaticString("StealVisionFromEmpire4"),
			new StaticString("StealVisionFromEmpire5"),
			new StaticString("StealVisionFromEmpire6"),
			new StaticString("StealVisionFromEmpire7")
		};
	}

	public event VisibilityRefreshedEventHandler VisibilityRefreshed;

	private void RefreshLineOfSight(ILineOfSightEntity lineOfSightEntity, int needRefreshBits)
	{
		if (!lineOfSightEntity.LineOfSightActive)
		{
			return;
		}
		MajorEmpire majorEmpire = lineOfSightEntity.Empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return;
		}
		short num = (short)(majorEmpire.Bits | lineOfSightEntity.EmpireInfiltrationBits);
		if ((needRefreshBits & (int)num) == 0)
		{
			return;
		}
		lineOfSightEntity.LineOfSightDirty = false;
		if (!lineOfSightEntity.WorldPosition.IsValid)
		{
			return;
		}
		WorldParameters worldParameters = this.world.WorldParameters;
		int lineOfSightVisionRange = lineOfSightEntity.LineOfSightVisionRange;
		int lineOfSightVisionHeight = lineOfSightEntity.LineOfSightVisionHeight;
		int num2 = 0;
		if (lineOfSightEntity.LineOfSightData == null)
		{
			lineOfSightEntity.LineOfSightData = new LineOfSightData();
		}
		LineOfSightData lineOfSightData = lineOfSightEntity.LineOfSightData;
		if (lineOfSightVisionRange < 0)
		{
			return;
		}
		short num3 = (short)lineOfSightVisionRange;
		if (this.tempRect == null)
		{
			this.tempRect = new WorldRect(lineOfSightEntity.WorldPosition, WorldOrientation.East, num3, num3, num3, num3, worldParameters);
		}
		else
		{
			this.tempRect.Update(lineOfSightEntity.WorldPosition, WorldOrientation.East, num3, num3, num3, num3, worldParameters);
		}
		this.rectVirtualPositions.Clear();
		this.tempRect.FillVirtualWorldPositions(ref this.rectVirtualPositions);
		VisibilityController.Position position = new VisibilityController.Position(WorldPosition.Invalid, WorldPosition.Invalid, -1);
		for (int i = 0; i < this.rectVirtualPositions.Count; i++)
		{
			if (lineOfSightEntity.WorldPosition == WorldPosition.GetValidPosition(this.rectVirtualPositions[i], worldParameters))
			{
				position = new VisibilityController.Position(this.rectVirtualPositions[i], lineOfSightEntity.WorldPosition, 0);
				break;
			}
		}
		Diagnostics.Assert(position.ValidWorldPosition.IsValid);
		this.potentialPositions.Clear();
		for (int j = 0; j < this.rectVirtualPositions.Count; j++)
		{
			WorldPosition worldPosition = this.rectVirtualPositions[j];
			WorldPosition validPosition = WorldPosition.GetValidPosition(worldPosition, worldParameters);
			if (validPosition.IsValid)
			{
				int distance = WorldPosition.GetDistance(position.ValidWorldPosition, validPosition, worldParameters.IsCyclicWorld, worldParameters.Columns);
				if (distance <= lineOfSightVisionRange)
				{
					this.potentialPositions.Add(new VisibilityController.Position(worldPosition, validPosition, distance));
				}
			}
		}
		this.potentialPositions.Sort((VisibilityController.Position position1, VisibilityController.Position position2) => position1.Distance.CompareTo(position2.Distance));
		lineOfSightData.Arcs.Clear();
		float radius = ((float)lineOfSightVisionRange + 0.1f) * 2f * Hexagon.One.Radius;
		lineOfSightData.Arcs.AddFirst(new Arc(position.GeometryPosition, 0f, 6.28308535f, radius));
		bool flag = this.EnableDetection;
		if (flag)
		{
			num2 = lineOfSightEntity.LineOfSightDetectionRange;
			flag &= (num2 > 0);
		}
		for (int k = 0; k < this.potentialPositions.Count; k++)
		{
			VisibilityController.Position obstaclePosition = this.potentialPositions[k];
			bool flag2 = obstaclePosition.Distance > lineOfSightVisionRange || this.IsWorldPositionObstructingVision(position.ValidWorldPosition, obstaclePosition.ValidWorldPosition, lineOfSightVisionHeight, lineOfSightEntity.IgnoreFog);
			if (flag && obstaclePosition.Distance > num2)
			{
				flag = false;
			}
			LinkedListNode<Arc> linkedListNode = lineOfSightData.Arcs.First;
			while (linkedListNode != null)
			{
				Arc value = linkedListNode.Value;
				linkedListNode = linkedListNode.Next;
				if (flag2)
				{
					if (Helper.IsHexagonIntersectArc(value, obstaclePosition.GeometryPosition))
					{
						this.SplitArc(lineOfSightData, value, obstaclePosition);
						this.SetWorldPositionAsExplored(obstaclePosition.ValidWorldPosition, majorEmpire, num);
					}
				}
				else if (value.Contains(obstaclePosition.GeometryPosition))
				{
					if (flag)
					{
						this.SetWorldPositionAsDetected(obstaclePosition.ValidWorldPosition, majorEmpire, num, lineOfSightEntity.VisibilityAccessibilityLevel);
						break;
					}
					this.SetWorldPositionAsVisible(obstaclePosition.ValidWorldPosition, majorEmpire, num, lineOfSightEntity.VisibilityAccessibilityLevel);
					break;
				}
			}
		}
	}

	private void RefreshLineOfSight(IVisibilityArea visibilityArea, int needRefreshBits)
	{
		MajorEmpire majorEmpire = visibilityArea.Empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return;
		}
		if ((needRefreshBits & majorEmpire.Bits) != majorEmpire.Bits)
		{
			return;
		}
		visibilityArea.VisibilityDirty = false;
		foreach (WorldPosition worldPosition in visibilityArea.VisibleWorldPositions)
		{
			if (worldPosition.IsValid)
			{
				if (worldPosition.Row < this.world.WorldParameters.Rows && worldPosition.Column < this.world.WorldParameters.Columns)
				{
					this.SetWorldPositionAsVisible(worldPosition, majorEmpire, 0, visibilityArea.VisibilityAccessibilityLevel);
				}
			}
		}
	}

	private void RefreshLineOfSight(IVisibilityProvider visibilityProvider, int needRefreshBits)
	{
		foreach (ILineOfSightEntity lineOfSightEntity in visibilityProvider.LineOfSightEntities)
		{
			this.RefreshLineOfSight(lineOfSightEntity, needRefreshBits);
		}
		foreach (IVisibilityArea visibilityArea in visibilityProvider.VisibleAreas)
		{
			this.RefreshLineOfSight(visibilityArea, needRefreshBits);
		}
		visibilityProvider.VisibilityDirty = false;
	}

	private void RefreshSharedLineOfSight(ISharedSightEntity sharedSightEntity)
	{
		if (!sharedSightEntity.WorldPosition.IsValid || !sharedSightEntity.SharedSightActive || sharedSightEntity.SharedSightBitsMask == 0)
		{
			sharedSightEntity.SharedSightDirty = false;
			return;
		}
		WorldCircle worldCircle = new WorldCircle(sharedSightEntity.WorldPosition, sharedSightEntity.SharedSightRange);
		List<WorldPosition> list = new List<WorldPosition>(worldCircle.GetWorldPositions(base.Game.World.WorldParameters));
		for (int i = list.Count - 1; i >= 0; i--)
		{
			WorldPosition tilePosition = list[i];
			if (!tilePosition.IsValid)
			{
				list.RemoveAt(i);
			}
			else if (this.IsWorldPositionObstructingVision(sharedSightEntity.WorldPosition, tilePosition, sharedSightEntity.SharedSightHeight, true))
			{
				list.RemoveAt(i);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			WorldPosition worldPosition = list[j];
			short value = this.sharedVisibilityMap.GetValue(worldPosition) | sharedSightEntity.SharedSightBitsMask;
			this.sharedVisibilityMap.SetValue(worldPosition, value);
			if (sharedSightEntity.SharedExploration)
			{
				Empire[] majorEmpiresFromBitMask = base.Game.GetMajorEmpiresFromBitMask(sharedSightEntity.SharedSightBitsMask);
				for (int k = 0; k < majorEmpiresFromBitMask.Length; k++)
				{
					if (majorEmpiresFromBitMask[k] is MajorEmpire)
					{
						this.SetWorldPositionAsExplored(worldPosition, majorEmpiresFromBitMask[k], (short)majorEmpiresFromBitMask[k].Bits);
					}
				}
			}
		}
		sharedSightEntity.SharedSightDirty = false;
	}

	private void SplitArc(LineOfSightData data, Arc arc, VisibilityController.Position obstaclePosition)
	{
		float num = float.PositiveInfinity;
		for (int i = 0; i < this.hexagonPointsBuffer.Length; i++)
		{
			this.hexagonPointsBuffer[i] = Hexagon.One.Points[i] + obstaclePosition.GeometryPosition;
			Vector2 vector = new Vector2(this.hexagonPointsBuffer[i].x - arc.Center.x, this.hexagonPointsBuffer[i].y - arc.Center.y);
			this.hexagonPointAnglesBuffer[i] = Helper.GetPositiveAngle(Mathf.Atan2(vector.y, vector.x));
			if (vector.sqrMagnitude < num)
			{
				num = vector.sqrMagnitude;
			}
		}
		float num2;
		float num3;
		Helper.GetAngleExtremities(this.hexagonPointAnglesBuffer, out num2, out num3);
		if (arc.EndAngleInRad <= num2 || arc.StartAngleInRad >= num3)
		{
			return;
		}
		for (int j = 0; j < this.tempNewArcs.Length; j++)
		{
			this.tempNewArcs[j] = null;
		}
		float num4 = Mathf.Sqrt(num);
		bool flag = Helper.IsAngleBewteen(num2, num3, arc.StartAngleInRad);
		bool flag2 = Helper.IsAngleBewteen(num2, num3, arc.EndAngleInRad);
		if (arc.StartAngleInRad < num2)
		{
			this.tempNewArcs[0] = new Arc(arc.Center, arc.StartAngleInRad, num2, (!flag) ? arc.Radius : num4);
			if (arc.EndAngleInRad <= num3)
			{
				this.tempNewArcs[1] = new Arc(arc.Center, num2, arc.EndAngleInRad, (!flag2) ? arc.Radius : num4);
			}
			else if (arc.EndAngleInRad > num3)
			{
				this.tempNewArcs[1] = new Arc(arc.Center, num2, num3, (!flag2) ? num4 : arc.Radius);
				this.tempNewArcs[2] = new Arc(arc.Center, num3, arc.EndAngleInRad, (!flag2) ? arc.Radius : num4);
			}
		}
		else if (arc.StartAngleInRad >= num2)
		{
			if (arc.EndAngleInRad <= num3)
			{
				this.tempNewArcs[0] = new Arc(arc.Center, arc.StartAngleInRad, arc.EndAngleInRad, (!flag2) ? arc.Radius : num4);
			}
			else if (arc.EndAngleInRad > num3)
			{
				this.tempNewArcs[0] = new Arc(arc.Center, arc.StartAngleInRad, num3, (!flag) ? arc.Radius : num4);
				this.tempNewArcs[1] = new Arc(arc.Center, num3, arc.EndAngleInRad, (!flag2) ? arc.Radius : num4);
			}
		}
		LinkedListNode<Arc> node = data.Arcs.Find(arc);
		for (int k = 0; k < this.tempNewArcs.Length; k++)
		{
			Arc arc2 = this.tempNewArcs[k];
			if (arc2 != null && arc2.AngleInRad > 0f)
			{
				data.Arcs.AddBefore(node, arc2);
			}
		}
		data.Arcs.Remove(node);
	}

	public ReadOnlyCollection<IGameEntityWithLineOfSight> LineOfSightEntities
	{
		get
		{
			if (this.readOnlyGameEntities == null)
			{
				this.readOnlyGameEntities = this.gameEntities.AsReadOnly();
			}
			return this.readOnlyGameEntities;
		}
	}

	public int NeedRefreshBits { get; private set; }

	private bool EnableDetection { get; set; }

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		base.SetLastError(0, "Waiting for service dependencies...");
		yield return base.BindService<IGameEntityRepositoryService>(serviceContainer, delegate(IGameEntityRepositoryService service)
		{
			this.gameEntityRepositoryService = service;
		});
		yield return base.BindService<IPathfindingService>(serviceContainer, delegate(IPathfindingService service)
		{
			this.pathfindingService = service;
		});
		yield return base.BindService<IWeatherService>(serviceContainer, delegate(IWeatherService service)
		{
			this.weatherService = service;
		});
		this.gameEntityRepositoryService.GameEntityRepositoryChange += this.GameEntityRepositoryService_GameEntityRepositoryChange;
		this.technologyDefinitionDrakkenEndQuestReward = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/FactionTrait/Drakkens/TechnologyDefinitionDrakkenEndQuestReward");
		this.technologyDefinitionDrakkenVisionDuringFirstTurn = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/FactionTrait/Drakkens/TechnologyDefinitionDrakkenVisionDuringFirstTurn");
		this.ridgeHeight = (sbyte)Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Ancillaries/Visibility/RidgeHeight", 2);
		this.wastelandHeight = (sbyte)Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Ancillaries/Visibility/WastelandHeight", 2);
		serviceContainer.AddService<IVisibilityService>(this);
		yield break;
	}

	public short GetWorldPositionVisibilityBits(WorldPosition worldPosition)
	{
		if (!worldPosition.IsValid)
		{
			return 0;
		}
		return this.explorationMap.GetValue(worldPosition);
	}

	public bool IsWorldPositionDetected(WorldPosition worldPosition, int bits)
	{
		if (!this.EnableDetection)
		{
			return false;
		}
		if (!worldPosition.IsValid)
		{
			return false;
		}
		short num = 0;
		num |= this.publicDetectionMap.GetValue(worldPosition);
		num |= this.privateDetectionMap.GetValue(worldPosition);
		return ((int)num & bits) != 0;
	}

	public bool IsWorldPositionDetectedFor(WorldPosition worldPosition, Empire empire)
	{
		if (!this.EnableDetection)
		{
			return false;
		}
		if (!worldPosition.IsValid)
		{
			return false;
		}
		MajorEmpire majorEmpire = empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return true;
		}
		short num = 0;
		num |= this.publicDetectionMap.GetValue(worldPosition);
		num |= this.privateDetectionMap.GetValue(worldPosition);
		return ((int)num & majorEmpire.Bits) != 0;
	}

	public bool IsWorldPositionExploredFor(WorldPosition worldPosition, Empire empire)
	{
		if (!worldPosition.IsValid)
		{
			return false;
		}
		MajorEmpire majorEmpire = empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return true;
		}
		short value = this.explorationMap.GetValue(worldPosition);
		return ((int)value & majorEmpire.Bits) == majorEmpire.Bits;
	}

	public bool IsWorldPositionExploredFor(WorldPosition worldPosition, int bits)
	{
		if (!worldPosition.IsValid)
		{
			return false;
		}
		short value = this.explorationMap.GetValue(worldPosition);
		return ((int)value & bits) == bits;
	}

	public bool IsWorldPositionObstructingVision(WorldPosition observerPosition, WorldPosition tilePosition, int observerHeight, bool ignoreFog)
	{
		if (!observerPosition.IsValid || !tilePosition.IsValid)
		{
			return true;
		}
		Diagnostics.Assert(this.elevationMap != null);
		if ((int)observerPosition.Row >= this.elevationMap.Height || (int)observerPosition.Column >= this.elevationMap.Width || (int)tilePosition.Row >= this.elevationMap.Height || (int)tilePosition.Column >= this.elevationMap.Width)
		{
			return true;
		}
		sbyte b = this.elevationMap.GetValue(tilePosition);
		PathfindingMovementCapacity tileMovementCapacity = this.pathfindingService.GetTileMovementCapacity(observerPosition, (PathfindingFlags)0);
		bool flag = tileMovementCapacity == PathfindingMovementCapacity.Water || tileMovementCapacity == PathfindingMovementCapacity.FrozenWater;
		sbyte b2 = (!flag) ? this.elevationMap.GetValue(observerPosition) : this.waterHeightMap.GetValue(observerPosition);
		if (this.ridgeMap.GetValue(tilePosition))
		{
			b = (sbyte)((int)b + (int)this.ridgeHeight);
		}
		IWorldPositionningService service = base.Game.GetService<IWorldPositionningService>();
		if (service == null)
		{
			return false;
		}
		Region region = service.GetRegion(tilePosition);
		if (region != null && region.IsWasteland)
		{
			b = (sbyte)((int)b + (int)this.wastelandHeight);
		}
		if ((int)b - (int)b2 > observerHeight)
		{
			return true;
		}
		if (service.GetDistance(tilePosition, observerPosition) <= 1)
		{
			return false;
		}
		if (this.weatherService == null)
		{
			return false;
		}
		if (region != null && region.IsOcean && !ignoreFog)
		{
			WeatherDefinition weatherDefinitionAtPosition = this.weatherService.GetWeatherDefinitionAtPosition(tilePosition);
			if (weatherDefinitionAtPosition != null && weatherDefinitionAtPosition.ObstructVisibility)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsWorldPositionVisibilityShared(WorldPosition worldPosition, int bits)
	{
		return ((int)this.sharedVisibilityMap.GetValue(worldPosition) & bits) != 0;
	}

	public bool IsWorldPositionVisibilitySharedFor(WorldPosition worldPosition, Empire empire)
	{
		return !(empire is MajorEmpire) || ((int)this.sharedVisibilityMap.GetValue(worldPosition) & empire.Bits) == empire.Bits;
	}

	public bool IsWorldPositionVisible(WorldPosition worldPosition, int bits)
	{
		short num = this.sharedVisibilityMap.GetValue(worldPosition);
		num |= this.publicVisibilityMap.GetValue(worldPosition);
		num |= this.privateVisibilityMap.GetValue(worldPosition);
		return ((int)num & bits) != 0;
	}

	public bool IsWorldPositionVisibleFor(WorldPosition worldPosition, Empire empire)
	{
		if (empire is MajorEmpire)
		{
			short num = this.sharedVisibilityMap.GetValue(worldPosition);
			num |= this.publicVisibilityMap.GetValue(worldPosition);
			num |= this.privateVisibilityMap.GetValue(worldPosition);
			return ((int)num & empire.Bits) == empire.Bits;
		}
		return true;
	}

	public override IEnumerator LoadGame(Game game)
	{
		yield return base.LoadGame(game);
		this.majorEmpires = Array.FindAll<Empire>(game.Empires, (Empire empire) => empire is MajorEmpire).Cast<MajorEmpire>().ToArray<MajorEmpire>();
		Diagnostics.Assert(this.majorEmpires != null && this.majorEmpires.Length > 0);
		this.empireExplorationBits = new int[this.majorEmpires.Length];
		this.empireVibilityBits = new int[this.majorEmpires.Length];
		this.lastEmpireExplorationBits = new int[this.majorEmpires.Length];
		this.empireDetectionBits = new int[this.majorEmpires.Length];
		base.enabled = true;
		yield break;
	}

	public override IEnumerator OnWorldLoaded(World loadedWorld)
	{
		yield return base.OnWorldLoaded(loadedWorld);
		this.world = loadedWorld;
		Diagnostics.Assert(loadedWorld != null, "loadedWorld.Atlas != null");
		Diagnostics.Assert(loadedWorld.Atlas != null);
		this.elevationMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.Relief) as GridMap<sbyte>);
		Diagnostics.Assert(this.elevationMap != null);
		this.ridgeMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>);
		Diagnostics.Assert(this.ridgeMap != null);
		this.publicVisibilityMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.PublicVisibility) as GridMap<short>);
		if (this.publicVisibilityMap == null)
		{
			this.publicVisibilityMap = new GridMap<short>(WorldAtlas.Maps.PublicVisibility, (int)loadedWorld.WorldParameters.Columns, (int)loadedWorld.WorldParameters.Rows, null);
			loadedWorld.Atlas.RegisterMapInstance<GridMap<short>>(this.publicVisibilityMap);
		}
		Diagnostics.Assert(this.publicVisibilityMap != null);
		this.privateVisibilityMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.PrivateVisibility) as GridMap<short>);
		if (this.privateVisibilityMap == null)
		{
			this.privateVisibilityMap = new GridMap<short>(WorldAtlas.Maps.PrivateVisibility, (int)loadedWorld.WorldParameters.Columns, (int)loadedWorld.WorldParameters.Rows, null);
			loadedWorld.Atlas.RegisterMapInstance<GridMap<short>>(this.privateVisibilityMap);
		}
		Diagnostics.Assert(this.privateVisibilityMap != null);
		this.sharedVisibilityMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.SharedVisibility) as GridMap<short>);
		if (this.sharedVisibilityMap == null)
		{
			this.sharedVisibilityMap = new GridMap<short>(WorldAtlas.Maps.SharedVisibility, (int)loadedWorld.WorldParameters.Columns, (int)loadedWorld.WorldParameters.Rows, null);
			loadedWorld.Atlas.RegisterMapInstance<GridMap<short>>(this.sharedVisibilityMap);
		}
		Diagnostics.Assert(this.sharedVisibilityMap != null);
		this.explorationMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.Exploration) as GridMap<short>);
		if (this.explorationMap == null)
		{
			this.explorationMap = new GridMap<short>(WorldAtlas.Maps.Exploration, (int)loadedWorld.WorldParameters.Columns, (int)loadedWorld.WorldParameters.Rows, null);
			loadedWorld.Atlas.RegisterMapInstance<GridMap<short>>(this.explorationMap);
		}
		Diagnostics.Assert(this.explorationMap != null);
		this.waterHeightMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.WaterHeight) as GridMap<sbyte>);
		if (this.waterHeightMap == null)
		{
			Diagnostics.LogError("Can't retrieve the water height map.");
		}
		this.EnableDetection = false;
		this.publicDetectionMap = null;
		this.privateDetectionMap = null;
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
		{
			this.EnableDetection = true;
			this.publicDetectionMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.PublicDetection) as GridMap<short>);
			if (this.publicDetectionMap == null)
			{
				this.publicDetectionMap = new GridMap<short>(WorldAtlas.Maps.PublicDetection, (int)loadedWorld.WorldParameters.Columns, (int)loadedWorld.WorldParameters.Rows, null);
				loadedWorld.Atlas.RegisterMapInstance<GridMap<short>>(this.publicDetectionMap);
			}
			Diagnostics.Assert(this.publicDetectionMap != null);
			this.privateDetectionMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.PrivateDetection) as GridMap<short>);
			if (this.privateDetectionMap == null)
			{
				this.privateDetectionMap = new GridMap<short>(WorldAtlas.Maps.PrivateDetection, (int)loadedWorld.WorldParameters.Columns, (int)loadedWorld.WorldParameters.Rows, null);
				loadedWorld.Atlas.RegisterMapInstance<GridMap<short>>(this.privateDetectionMap);
			}
			Diagnostics.Assert(this.privateDetectionMap != null);
		}
		this.NeedRefreshBits = 0;
		yield break;
	}

	public void NotifyVisibilityHasChanged(Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		MajorEmpire majorEmpire = empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return;
		}
		this.NeedRefreshBits |= majorEmpire.Bits;
		this.OnVisibilityRefreshed(empire);
	}

	public void SetWorldPositionAsExplored(WorldPosition worldPosition, Empire empire, short exceptionalExplorationBits)
	{
		Diagnostics.Assert(worldPosition.IsValid);
		Diagnostics.Assert(empire is MajorEmpire);
		short num = this.explorationMap.GetValue((int)worldPosition.Row, (int)worldPosition.Column);
		Diagnostics.Assert(this.empireExplorationBits != null);
		num |= (short)this.empireExplorationBits[empire.Index];
		num |= exceptionalExplorationBits;
		this.explorationMap.SetValue((int)worldPosition.Row, (int)worldPosition.Column, num);
	}

	public void Update()
	{
		if (this.majorEmpires == null)
		{
			return;
		}
		bool flag = this.EvaluateSharedVisibilityRefresh();
		if (flag)
		{
			this.RefreshSharedVisibility();
		}
		bool flag2 = this.EvaluateVisibilityRefresh();
		if (flag2)
		{
			this.RefreshVisibility();
		}
		if (flag || flag2)
		{
			for (int i = 0; i < this.majorEmpires.Length; i++)
			{
				this.OnVisibilityRefreshed(this.majorEmpires[i]);
			}
		}
	}

	public void ShareExplorationMapBetweenEmpire(Empire empireWhichProvides, Empire empireWhichReceives)
	{
		if (empireWhichProvides == null || empireWhichReceives == null || !(empireWhichProvides is MajorEmpire) || !(empireWhichReceives is MajorEmpire))
		{
			return;
		}
		short num = (short)(empireWhichProvides.Bits | empireWhichReceives.Bits);
		short num2 = 0;
		while ((int)num2 < this.explorationMap.Height)
		{
			short num3 = 0;
			while ((int)num3 < this.explorationMap.Width)
			{
				short num4 = this.explorationMap.GetValue((int)num2, (int)num3);
				if ((num4 & num) != 0)
				{
					num4 |= num;
				}
				this.explorationMap.SetValue((int)num2, (int)num3, num4);
				num3 += 1;
			}
			num2 += 1;
		}
		this.NotifyVisibilityHasChanged(empireWhichProvides);
		this.NotifyVisibilityHasChanged(empireWhichReceives);
	}

	protected override void Awake()
	{
		base.Awake();
		base.enabled = false;
	}

	protected override void Releasing()
	{
		base.Releasing();
		if (this.gameEntityRepositoryService != null)
		{
			this.gameEntityRepositoryService.GameEntityRepositoryChange -= this.GameEntityRepositoryService_GameEntityRepositoryChange;
			this.gameEntityRepositoryService = null;
		}
		this.world = null;
		this.majorEmpires = null;
		this.gameEntities.Clear();
		this.visibilityProviders.Clear();
	}

	private bool EvaluateSharedVisibilityRefresh()
	{
		if (this.forceRefreshSharedVibility)
		{
			return true;
		}
		if (this.sharedSightGameEntities.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < this.sharedSightGameEntities.Count; i++)
		{
			if (this.sharedSightGameEntities[i].SharedSightDirty)
			{
				return true;
			}
		}
		return false;
	}

	private bool EvaluateVisibilityRefresh()
	{
		if (this.NeedRefreshBits == 0)
		{
			for (int i = 0; i < this.gameEntities.Count; i++)
			{
				IGameEntityWithLineOfSight gameEntityWithLineOfSight = this.gameEntities[i];
				if (gameEntityWithLineOfSight.LineOfSightActive && gameEntityWithLineOfSight.LineOfSightDirty)
				{
					MajorEmpire majorEmpire = gameEntityWithLineOfSight.Empire as MajorEmpire;
					if (majorEmpire != null)
					{
						this.NeedRefreshBits |= majorEmpire.Bits;
						this.NeedRefreshBits |= gameEntityWithLineOfSight.EmpireInfiltrationBits;
					}
				}
			}
			for (int j = 0; j < this.visibilityProviders.Count; j++)
			{
				if (this.visibilityProviders[j].VisibilityDirty)
				{
					foreach (Empire empire in this.visibilityProviders[j].VisibilityEmpires)
					{
						if (empire is MajorEmpire)
						{
							this.NeedRefreshBits |= empire.Bits;
						}
					}
				}
				else
				{
					foreach (ILineOfSightEntity lineOfSightEntity in this.visibilityProviders[j].LineOfSightEntities)
					{
						if (lineOfSightEntity.LineOfSightActive && lineOfSightEntity.LineOfSightDirty)
						{
							MajorEmpire majorEmpire2 = lineOfSightEntity.Empire as MajorEmpire;
							if (majorEmpire2 != null)
							{
								this.NeedRefreshBits |= majorEmpire2.Bits;
								this.NeedRefreshBits |= lineOfSightEntity.EmpireInfiltrationBits;
							}
						}
					}
					foreach (IVisibilityArea visibilityArea in this.visibilityProviders[j].VisibleAreas)
					{
						if (visibilityArea.VisibilityDirty)
						{
							MajorEmpire majorEmpire3 = visibilityArea.Empire as MajorEmpire;
							if (majorEmpire3 != null)
							{
								this.NeedRefreshBits |= majorEmpire3.Bits;
							}
						}
					}
				}
			}
		}
		int num = (this.majorEmpires == null) ? 0 : ((int)Mathf.Pow(2f, (float)this.majorEmpires.Length) - 1);
		this.NeedRefreshBits &= num;
		return this.NeedRefreshBits != 0;
	}

	private void SetWorldPositionAsDetected(WorldPosition worldPosition, Empire empire, short exceptionalDetectionBits, VisibilityController.VisibilityAccessibility accessibilityLevel)
	{
		if (this.EnableDetection)
		{
			Diagnostics.Assert(worldPosition.IsValid);
			Diagnostics.Assert(empire is MajorEmpire);
			if (accessibilityLevel != VisibilityController.VisibilityAccessibility.Public)
			{
				if (accessibilityLevel == VisibilityController.VisibilityAccessibility.Private)
				{
					short num = this.privateDetectionMap.GetValue(worldPosition);
					num |= (short)empire.Bits;
					this.privateDetectionMap.SetValue(worldPosition, num);
				}
			}
			else
			{
				short num2 = this.publicDetectionMap.GetValue(worldPosition);
				Diagnostics.Assert(this.empireDetectionBits != null);
				num2 |= (short)this.empireDetectionBits[empire.Index];
				num2 |= exceptionalDetectionBits;
				this.publicDetectionMap.SetValue(worldPosition, num2);
			}
		}
		this.SetWorldPositionAsVisible(worldPosition, empire, exceptionalDetectionBits, accessibilityLevel);
	}

	private void SetWorldPositionAsVisible(WorldPosition worldPosition, Empire empire, short exceptionalVisibilityBits, VisibilityController.VisibilityAccessibility accessibilityLevel)
	{
		Diagnostics.Assert(worldPosition.IsValid);
		Diagnostics.Assert(empire is MajorEmpire);
		if (accessibilityLevel != VisibilityController.VisibilityAccessibility.Public)
		{
			if (accessibilityLevel != VisibilityController.VisibilityAccessibility.Private)
			{
				Diagnostics.LogError("Unknown accessibility level {0}.", new object[]
				{
					accessibilityLevel
				});
			}
			else
			{
				short num = this.privateVisibilityMap.GetValue(worldPosition);
				num |= (short)empire.Bits;
				this.privateVisibilityMap.SetValue(worldPosition, num);
				this.SetWorldPositionAsExplored(worldPosition, empire, exceptionalVisibilityBits);
			}
		}
		else
		{
			short num2 = this.publicVisibilityMap.GetValue(worldPosition);
			Diagnostics.Assert(this.empireVibilityBits != null);
			num2 |= (short)this.empireVibilityBits[empire.Index];
			num2 |= exceptionalVisibilityBits;
			this.publicVisibilityMap.SetValue(worldPosition, num2);
			exceptionalVisibilityBits |= (short)this.empireVibilityBits[empire.Index];
			this.SetWorldPositionAsExplored(worldPosition, empire, exceptionalVisibilityBits);
		}
	}

	private void GameEntityRepositoryService_GameEntityRepositoryChange(object sender, GameEntityRepositoryChangeEventArgs e)
	{
		if (e.Action == GameEntityRepositoryChangeAction.Add)
		{
			IGameEntity gameEntity;
			if (this.gameEntityRepositoryService.TryGetValue(e.GameEntityGuid, out gameEntity))
			{
				if (gameEntity is IGameEntityWithLineOfSight)
				{
					Diagnostics.Assert(this.gameEntities != null);
					IGameEntityWithLineOfSight gameEntityWithLineOfSight = gameEntity as IGameEntityWithLineOfSight;
					gameEntityWithLineOfSight.LineOfSightData = new LineOfSightData();
					this.gameEntities.Add(gameEntityWithLineOfSight);
					MajorEmpire majorEmpire = gameEntityWithLineOfSight.Empire as MajorEmpire;
					if (majorEmpire != null)
					{
						this.NeedRefreshBits |= majorEmpire.Bits;
						this.NeedRefreshBits |= gameEntityWithLineOfSight.EmpireInfiltrationBits;
					}
				}
				if (gameEntity is IGameEntityWithSharedSight)
				{
					this.sharedSightGameEntities.Add(gameEntity as IGameEntityWithSharedSight);
					this.forceRefreshSharedVibility = true;
				}
				if (gameEntity is IVisibilityProvider)
				{
					Diagnostics.Assert(this.visibilityProviders != null);
					IVisibilityProvider visibilityProvider = gameEntity as IVisibilityProvider;
					if (visibilityProvider.LineOfSightEntities != null)
					{
						foreach (ILineOfSightEntity lineOfSightEntity in visibilityProvider.LineOfSightEntities)
						{
							lineOfSightEntity.LineOfSightData = new LineOfSightData();
							lineOfSightEntity.LineOfSightDirty = true;
						}
					}
					visibilityProvider.VisibilityDirty = true;
					this.visibilityProviders.Add(visibilityProvider);
				}
			}
		}
		else if (e.Action == GameEntityRepositoryChangeAction.Remove)
		{
			IGameEntityWithLineOfSight gameEntityWithLineOfSight2 = this.gameEntities.Find((IGameEntityWithLineOfSight entity) => entity.GUID == e.GameEntityGuid);
			if (gameEntityWithLineOfSight2 != null)
			{
				Diagnostics.Assert(this.gameEntities.Count((IGameEntityWithLineOfSight entity) => entity.GUID == e.GameEntityGuid) <= 1, "There must be 1 instance max for a guid in the gameEntities list.");
				this.gameEntities.Remove(gameEntityWithLineOfSight2);
				MajorEmpire majorEmpire2 = gameEntityWithLineOfSight2.Empire as MajorEmpire;
				if (majorEmpire2 != null)
				{
					this.NeedRefreshBits |= majorEmpire2.Bits;
					this.NeedRefreshBits |= gameEntityWithLineOfSight2.EmpireInfiltrationBits;
				}
			}
			IGameEntityWithSharedSight item = this.sharedSightGameEntities.Find((IGameEntityWithSharedSight entity) => entity.GUID == e.GameEntityGuid);
			if (gameEntityWithLineOfSight2 != null)
			{
				this.sharedSightGameEntities.Remove(item);
				this.forceRefreshSharedVibility = true;
			}
			if (this.visibilityProviders != null)
			{
				IVisibilityProvider visibilityProvider2 = this.visibilityProviders.Find((IVisibilityProvider entity) => entity.GUID == e.GameEntityGuid);
				if (visibilityProvider2 != null)
				{
					this.visibilityProviders.Remove(visibilityProvider2);
					if (visibilityProvider2.VisibilityEmpires != null)
					{
						foreach (Empire empire in visibilityProvider2.VisibilityEmpires)
						{
							if (empire is MajorEmpire)
							{
								this.NeedRefreshBits |= empire.Bits;
							}
						}
					}
				}
			}
		}
	}

	private void OnVisibilityRefreshed(Empire empire)
	{
		if (this.VisibilityRefreshed != null)
		{
			this.VisibilityRefreshed(this, new VisibilityRefreshedEventArgs(empire));
		}
	}

	private void RefreshSharedVisibility()
	{
		for (int i = 0; i < this.sharedVisibilityMap.Data.Length; i++)
		{
			this.sharedVisibilityMap.Data[i] = 0;
		}
		for (int j = 0; j < this.sharedSightGameEntities.Count; j++)
		{
			this.RefreshSharedLineOfSight(this.sharedSightGameEntities[j]);
		}
		this.forceRefreshSharedVibility = false;
	}

	private void RefreshVisibility()
	{
		Diagnostics.Assert(this.majorEmpires != null);
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			Diagnostics.Assert(this.empireExplorationBits != null && this.empireVibilityBits != null && this.lastEmpireExplorationBits != null);
			Diagnostics.Assert(this.empireDetectionBits != null);
			this.lastEmpireExplorationBits[i] = this.empireExplorationBits[i];
			this.empireExplorationBits[i] = this.majorEmpires[i].Bits;
			this.empireVibilityBits[i] = this.majorEmpires[i].Bits;
			this.empireDetectionBits[i] = this.majorEmpires[i].Bits;
			MajorEmpire majorEmpire = this.majorEmpires[i];
			DepartmentOfForeignAffairs agency = majorEmpire.GetAgency<DepartmentOfForeignAffairs>();
			for (int j = 0; j < this.majorEmpires.Length; j++)
			{
				if (j != i)
				{
					bool flag = false;
					bool flag2 = false;
					bool flag3 = false;
					DepartmentOfScience agency2 = this.majorEmpires[j].GetAgency<DepartmentOfScience>();
					Diagnostics.Assert(agency2 != null);
					if (agency2.GetTechnologyState(this.technologyDefinitionDrakkenEndQuestReward) == DepartmentOfScience.ConstructibleElement.State.Researched)
					{
						flag2 = true;
						flag3 = true;
					}
					else if (agency2.GetTechnologyState(this.technologyDefinitionDrakkenVisionDuringFirstTurn) == DepartmentOfScience.ConstructibleElement.State.Researched && base.Game.Turn == 0)
					{
						flag2 = true;
						flag = true;
						flag3 = true;
					}
					DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(this.majorEmpires[j]);
					if (diplomaticRelation != null)
					{
						flag |= diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.MapExchange);
						flag2 |= diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.VisionExchange);
					}
					if (this.EnableDetection)
					{
						StaticString tag = this.stealVisionFromEmpireTags[i];
						if (this.majorEmpires[j].SimulationObject.Tags.Contains(tag))
						{
							flag2 = true;
							flag3 = true;
						}
					}
					if (flag2)
					{
						this.empireVibilityBits[i] |= this.majorEmpires[j].Bits;
					}
					if (flag)
					{
						this.empireExplorationBits[i] |= this.majorEmpires[j].Bits;
					}
					if (flag3)
					{
						this.empireDetectionBits[i] |= this.majorEmpires[j].Bits;
					}
					if (flag2 || flag || flag3)
					{
						if ((this.NeedRefreshBits & this.majorEmpires[j].Bits) != 0)
						{
							this.NeedRefreshBits |= 1 << i;
						}
						if ((this.NeedRefreshBits & this.majorEmpires[i].Bits) != 0)
						{
							this.NeedRefreshBits |= 1 << j;
						}
					}
				}
			}
		}
		for (int k = 0; k < this.majorEmpires.Length; k++)
		{
			MajorEmpire majorEmpire2 = this.majorEmpires[k];
			Diagnostics.Assert(this.empireExplorationBits != null && this.lastEmpireExplorationBits != null);
			if (this.lastEmpireExplorationBits[k] != this.empireExplorationBits[k])
			{
				Diagnostics.Assert(this.explorationMap != null && this.explorationMap.Data != null);
				for (int l = 0; l < this.explorationMap.Data.Length; l++)
				{
					if ((this.explorationMap.Data[l] & (short)majorEmpire2.Bits) != 0)
					{
						short[] data = this.explorationMap.Data;
						int num = l;
						data[num] |= (short)this.empireExplorationBits[k];
					}
				}
			}
		}
		short num2 = (short)(~(short)this.NeedRefreshBits);
		for (int m = 0; m < this.publicVisibilityMap.Data.Length; m++)
		{
			short[] data2 = this.publicVisibilityMap.Data;
			int num3 = m;
			data2[num3] &= num2;
			short[] data3 = this.privateVisibilityMap.Data;
			int num4 = m;
			data3[num4] &= num2;
			if (this.EnableDetection)
			{
				short[] data4 = this.publicDetectionMap.Data;
				int num5 = m;
				data4[num5] &= num2;
				short[] data5 = this.privateDetectionMap.Data;
				int num6 = m;
				data5[num6] &= num2;
			}
		}
		for (int n = 0; n < this.gameEntities.Count; n++)
		{
			this.RefreshLineOfSight(this.gameEntities[n], this.NeedRefreshBits);
		}
		for (int num7 = 0; num7 < this.visibilityProviders.Count; num7++)
		{
			this.RefreshLineOfSight(this.visibilityProviders[num7], this.NeedRefreshBits);
		}
		this.NeedRefreshBits = 0;
	}

	private readonly float[] hexagonPointAnglesBuffer;

	private readonly Vector2[] hexagonPointsBuffer;

	private readonly List<VisibilityController.Position> potentialPositions;

	private List<WorldPosition> rectVirtualPositions;

	private WorldRect tempRect;

	private Arc[] tempNewArcs;

	private readonly List<IGameEntityWithLineOfSight> gameEntities = new List<IGameEntityWithLineOfSight>();

	private readonly List<IGameEntityWithSharedSight> sharedSightGameEntities = new List<IGameEntityWithSharedSight>();

	private readonly List<IVisibilityProvider> visibilityProviders = new List<IVisibilityProvider>();

	private readonly StaticString[] stealVisionFromEmpireTags;

	private ReadOnlyCollection<IGameEntityWithLineOfSight> readOnlyGameEntities;

	private GridMap<sbyte> elevationMap;

	private GridMap<short> explorationMap;

	private GridMap<bool> ridgeMap;

	private GridMap<short> publicVisibilityMap;

	private GridMap<short> privateVisibilityMap;

	private GridMap<sbyte> waterHeightMap;

	private GridMap<short> publicDetectionMap;

	private GridMap<short> privateDetectionMap;

	private GridMap<short> sharedVisibilityMap;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IPathfindingService pathfindingService;

	private IWeatherService weatherService;

	private World world;

	private MajorEmpire[] majorEmpires;

	private int[] empireExplorationBits;

	private int[] empireVibilityBits;

	private int[] lastEmpireExplorationBits;

	private int[] empireDetectionBits;

	private bool forceRefreshSharedVibility;

	private StaticString technologyDefinitionDrakkenEndQuestReward;

	private StaticString technologyDefinitionDrakkenVisionDuringFirstTurn;

	private sbyte ridgeHeight;

	private sbyte wastelandHeight;

	public struct Position
	{
		public Position(WorldPosition position, WorldPosition validPosition, int distance = -1)
		{
			this.ValidWorldPosition = validPosition;
			this.WorldPosition = position;
			this.Distance = distance;
			this.GeometryPosition = default(Vector2);
			this.GeometryPosition.x = this.GeometryPosition.x + ((float)this.WorldPosition.Column * Hexagon.One.Apothem * 2f + Hexagon.One.Apothem);
			this.GeometryPosition.y = this.GeometryPosition.y + ((float)this.WorldPosition.Row * Hexagon.One.Side + Hexagon.One.Radius);
			if (this.WorldPosition.Row % 2 != 0)
			{
				this.GeometryPosition.x = this.GeometryPosition.x + Hexagon.One.Apothem;
			}
		}

		public readonly int Distance;

		public readonly Vector2 GeometryPosition;

		public readonly WorldPosition WorldPosition;

		public readonly WorldPosition ValidWorldPosition;
	}

	public enum VisibilityAccessibility
	{
		Public,
		Private
	}
}
