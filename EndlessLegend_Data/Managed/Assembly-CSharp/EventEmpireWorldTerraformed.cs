using System;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Game;

public class EventEmpireWorldTerraformed : GameEvent
{
	public EventEmpireWorldTerraformed(Amplitude.Unity.Game.Empire terraformingEmpire, WorldPosition[] terraformedTiles) : base(terraformingEmpire, EventEmpireWorldTerraformed.Name, new object[0])
	{
		this.TerraformingEmpire = terraformingEmpire;
		this.TerraformedTiles = terraformedTiles;
	}

	public Amplitude.Unity.Game.Empire TerraformingEmpire { get; private set; }

	public WorldPosition[] TerraformedTiles { get; private set; }

	public EventEmpireWorldTerraformed(Amplitude.Unity.Game.Empire terraformingEmpire, WorldPosition[] terraformedTiles, bool reversible = false) : base(terraformingEmpire, EventEmpireWorldTerraformed.Name, new object[0])
	{
		this.TerraformingEmpire = terraformingEmpire;
		this.TerraformedTiles = terraformedTiles;
		this.Reversible = reversible;
	}

	public bool Reversible { get; private set; }

	public static readonly StaticString Name = new StaticString("EventEmpireWorldTerraformed");
}
