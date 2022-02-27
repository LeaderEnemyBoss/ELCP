using System;
using System.Collections;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AIEntity_Empire : AIEntity, IXmlSerializable, IAIDrawDebugger
{
	public AIEntity_Empire(MajorEmpire majorEmpire) : base(majorEmpire)
	{
	}

	public void DrawDebug(bool activate)
	{
		if (!activate)
		{
			return;
		}
		for (int i = 0; i < base.AILayers.Count; i++)
		{
			IAIDrawDebugger iaidrawDebugger = base.AILayers[i] as IAIDrawDebugger;
			if (iaidrawDebugger != null)
			{
				iaidrawDebugger.DrawDebug(activate);
			}
		}
	}

	public void SetDrawDebugerParameters()
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
	}

	public Blackboard Blackboard
	{
		get
		{
			return base.AIPlayer.Blackboard;
		}
	}

	public override IEnumerator Initialize()
	{
		yield return base.Initialize();
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		global::Game game = gameService.Game as global::Game;
		World world = game.World;
		Diagnostics.Assert(world != null);
		yield break;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		yield break;
	}

	public override void Release()
	{
		base.Release();
	}

	protected override void RegisterContext()
	{
		base.RegisterContext();
		base.Context.RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AIContext/MaximalValueForAIParameters/";
	}

	protected override void RegisterLayers()
	{
		base.RegisterLayers();
		this.AddLayer(new AILayer_AmasEmpire());
		this.AddLayer(new AILayer_Strategy());
		this.AddLayer(new AILayer_War());
		this.AddLayer(new AILayer_AccountManager());
		this.AddLayer(new AILayer_Economy());
		this.AddLayer(new AILayer_Exploration());
		this.AddLayer(new AILayer_Colonization());
		this.AddLayer(new AILayer_Military());
		this.AddLayer(new AILayer_HeroAssignation());
		this.AddLayer(new AILayer_UnitDesigner());
		this.AddLayer(new AILayer_Encounter());
		this.AddLayer(new AILayer_ArmyRecruitment());
		this.AddLayer(new AILayer_UnitRecruitment());
		this.AddLayer(new AILayer_Pacification());
		this.AddLayer(new AILayer_Assimilation());
		this.AddLayer(new AILayer_Attitude());
		this.AddLayer(new AILayer_ResourceManager());
		this.AddLayer(new AILayer_Patrol());
		this.AddLayer(new AILayer_ArmyManagement());
		this.AddLayer(new AILayer_SiegeBreaker());
		this.AddLayer(new AILayer_QuestSolver());
		this.AddLayer(new AILayer_AutoAction());
		this.AddLayer(new AILayer_Trade());
		this.AddLayer(new AILayer_Village());
		this.AddLayer(new AILayer_HealUnits());
		this.AddLayer(new AILayer_Terraformation());
		this.AddLayer(new AILayer_KaijuAdquisition());
		this.AddLayer(new AILayer_Diplomacy());
		this.AddLayer(new AILayer_Research());
		this.AddLayer(new AILayer_EmpirePlan());
		this.AddLayer(new AILayer_Colossus());
		this.AddLayer(new AILayer_Wonder());
		this.AddLayer(new AILayer_Manta());
		this.AddLayer(new AILayer_EmpireAntiSpy());
		this.AddLayer(new AILayer_Altar());
		this.AddLayer(new AILayer_Navy());
		this.AddLayer(new AILayer_Catspaw());
		this.AddLayer(new AILayer_Dissent());
		this.AddLayer(new AILayer_WeatherControl());
		this.AddLayer(new AILayer_KaijuManagement());
		this.AddLayer(new AILayer_Victory());
		this.AddLayer(new AILayer_QuestBTController());
	}
}
