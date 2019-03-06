using System;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class ArmyGoToInstruction : IXmlSerializable
{
	public ArmyGoToInstruction(GameEntityGUID armyGuid)
	{
		if (!armyGuid.IsValid)
		{
			throw new ArgumentException("Army guid is invalid.");
		}
		this.ArmyGUID = armyGuid;
		IGameService service = Services.GetService<IGameService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.pathfindingService != null);
	}

	protected ArmyGoToInstruction()
	{
		IGameService service = Services.GetService<IGameService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.pathfindingService != null);
	}

	public virtual void ReadXml(XmlReader reader)
	{
		reader.ReadStartElement();
		this.ArmyGUID = reader.ReadElementString<ulong>("ArmyGUID");
		this.worldPath = new WorldPath();
		reader.ReadElementSerializable<WorldPath>("WorldPath", ref this.worldPath);
		this.Reset();
		this.Progress = reader.ReadElementString<int>("Progress");
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteElementString<ulong>("ArmyGUID", this.ArmyGUID);
		IXmlSerializable xmlSerializable = this.WorldPath;
		writer.WriteElementSerializable<IXmlSerializable>("WorldPath", ref xmlSerializable);
		writer.WriteElementString<int>("Progress", this.Progress);
	}

	public GameEntityGUID ArmyGUID { get; private set; }

	public ArmyMoveToInstruction ArmyMoveToInstruction { get; private set; }

	public global::Empire Empire
	{
		get
		{
			if (this.Army == null)
			{
				return null;
			}
			return this.Army.Empire;
		}
	}

	public bool IsFinished
	{
		get
		{
			return this.ArmyMoveToInstruction == null && (this.WorldPositions == null || this.WorldPositions.Length == 0 || this.Progress >= this.WorldPositions.Length);
		}
	}

	public bool IsMoveCancelled { get; private set; }

	public bool IsMoveFinished
	{
		get
		{
			return this.Army == null || (!(this.Army.WorldPosition != this.WorldPath.Destination) && this.IsFinished);
		}
	}

	public bool IsMoving { get; set; }

	public int Progress { get; private set; }

	public WorldPath WorldPath
	{
		get
		{
			return this.worldPath;
		}
		private set
		{
			this.worldPath = value;
		}
	}

	public WorldPosition[] WorldPositions { get; private set; }

	protected Army Army
	{
		get
		{
			if (this.army == null)
			{
				IGameService service = Services.GetService<IGameService>();
				Diagnostics.Assert(service != null);
				IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				Diagnostics.Assert(service2 != null);
				IGameEntity gameEntity;
				if (!service2.TryGetValue(this.ArmyGUID, out gameEntity))
				{
					return null;
				}
				Diagnostics.Assert(gameEntity is Army);
				this.army = (gameEntity as Army);
			}
			return this.army;
		}
	}

	public void Cancel(bool silent = false)
	{
		this.IsMoving = false;
		if (!this.IsMoveCancelled)
		{
			this.IsMoveCancelled = true;
			if (!silent)
			{
				OrderCancelMove order = new OrderCancelMove(this.Empire.Index, this.ArmyGUID);
				Diagnostics.Assert(this.Empire.PlayerControllers.Server != null, "Empire player controller (server) is null.");
				this.Empire.PlayerControllers.Server.PostOrder(order);
			}
			if (this.ArmyMoveToInstruction != null)
			{
				this.OnStopMoving();
				this.IsMoving = false;
				this.ArmyMoveToInstruction = null;
			}
		}
	}

	public WorldPosition[] GetRemainingPath()
	{
		WorldPosition[] worldPositions = this.worldPath.WorldPositions;
		if (worldPositions == null)
		{
			return null;
		}
		if (this.Army == null)
		{
			return null;
		}
		int num = 0;
		while (num < worldPositions.Length && worldPositions[num] != this.Army.WorldPosition)
		{
			num++;
		}
		int num2 = worldPositions.Length - num;
		WorldPosition[] array = new WorldPosition[num2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = worldPositions[num + i];
		}
		return array;
	}

	public void Pause()
	{
		this.IsMoving = false;
	}

	public virtual void Reset(WorldPath worldPath)
	{
		this.WorldPath = worldPath;
		this.Reset();
		this.Progress = 0;
		while (this.Progress < this.WorldPositions.Length && this.WorldPositions[this.Progress] != this.Army.WorldPosition)
		{
			this.Progress++;
		}
		this.Progress++;
	}

	public void Resume()
	{
		if (!this.Army.IsLocked && !this.IsFinished)
		{
			this.IsMoving = true;
		}
	}

	public void Tick(GameInterface gameInterface)
	{
		if (this.IsMoveCancelled)
		{
			return;
		}
		while (this.ArmyMoveToInstruction == null || global::Game.Time > this.ArmyMoveToInstruction.EstimatedTimeOfArrival)
		{
			if (!this.army.GUID.IsValid)
			{
				this.Cancel(true);
				return;
			}
			if (this.IsMoving && !this.Army.IsLocked && this.WorldPositions != null && this.Progress < this.WorldPositions.Length && this.Army.GetPropertyValue(SimulationProperties.Movement) > 0f)
			{
				ArmyMoveToInstruction armyMoveToInstruction = new ArmyMoveToInstruction();
				int progress = this.Progress;
				WorldPosition from = (this.ArmyMoveToInstruction == null) ? this.Army.WorldPosition : this.ArmyMoveToInstruction.To;
				WorldPosition worldPosition = this.WorldPositions[this.Progress++];
				while (this.Progress < this.WorldPositions.Length && !this.pathfindingService.IsTileStopable(worldPosition, this.army, PathfindingFlags.IgnoreFogOfWar, null))
				{
					worldPosition = this.WorldPositions[this.Progress++];
				}
				int num = this.Progress - progress;
				Diagnostics.Assert(num >= 1);
				if (num > 1)
				{
					armyMoveToInstruction.IntermediatesPositions = new WorldPosition[num - 1];
					for (int i = 0; i < armyMoveToInstruction.IntermediatesPositions.Length; i++)
					{
						armyMoveToInstruction.IntermediatesPositions[i] = this.WorldPositions[progress + i];
					}
				}
				double num2 = 1.0 * (double)num;
				armyMoveToInstruction.From = from;
				armyMoveToInstruction.To = worldPosition;
				if (this.ArmyMoveToInstruction != null)
				{
					armyMoveToInstruction.EstimatedTimeOfArrival = this.ArmyMoveToInstruction.EstimatedTimeOfArrival + num2;
				}
				else
				{
					armyMoveToInstruction.EstimatedTimeOfArrival = global::Game.Time + num2;
				}
				if (!this.CanMoveTo(gameInterface, armyMoveToInstruction.To))
				{
					this.Cancel(false);
					break;
				}
				if (this.ArmyMoveToInstruction == null)
				{
					this.OnStartMoving();
				}
				this.ArmyMoveToInstruction = armyMoveToInstruction;
				OrderMoveTo order = new OrderMoveTo(this.Empire.Index, this);
				Diagnostics.Assert(this.Empire.PlayerControllers.Server != null, "Empire player controller (server) is null.");
				this.Empire.PlayerControllers.Server.PostOrder(order);
			}
			else
			{
				if (this.ArmyMoveToInstruction != null)
				{
					this.OnStopMoving();
					this.IsMoving = false;
					this.ArmyMoveToInstruction = null;
					break;
				}
				this.IsMoving = false;
				break;
			}
		}
	}

	protected virtual void OnStartMoving()
	{
	}

	protected virtual void OnStopMoving()
	{
	}

	protected virtual bool CanMoveTo(GameInterface gameInterface, WorldPosition destination)
	{
		IWorldPositionningService service = gameInterface.Game.Services.GetService<IWorldPositionningService>();
		Region region = service.GetRegion(this.army.WorldPosition);
		if (region == null || !region.BelongToEmpire(this.army.Empire) || region.City == null || region.City.BesiegingEmpire == null)
		{
			return true;
		}
		bool flag = region.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == this.army.WorldPosition);
		bool flag2 = region.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == destination);
		if (flag)
		{
			return flag2;
		}
		return !flag2;
	}

	private void Reset()
	{
		if (this.worldPath == null)
		{
			return;
		}
		int num = this.worldPath.Length;
		if (this.worldPath.ControlPoints.Length > 0)
		{
			num = (int)(this.worldPath.ControlPoints[0] + 1);
		}
		this.WorldPositions = new WorldPosition[num];
		Array.Copy(this.worldPath.WorldPositions, this.WorldPositions, num);
		this.ArmyMoveToInstruction = null;
	}

	public const double MoveToDuration = 1.0;

	public const double WaitForInterceptionDuration = 0.5;

	private Army army;

	private IPathfindingService pathfindingService;

	private WorldPath worldPath;
}
