using System;
using Amplitude;
using Amplitude.Xml.Serialization;

public class AICommander_KaijuSupport : AICommander, IXmlSerializable
{
	public AICommander_KaijuSupport() : base(AICommanderMissionDefinition.AICommanderCategory.KaijuSupport)
	{
	}

	public override float GetPriority(AICommanderMission mission)
	{
		return 2f;
	}

	public override void Initialize()
	{
		base.Initialize();
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		AIData_Army aidata_Army;
		if (this.aiDataRepository.TryGetAIData<AIData_Army>(base.ForceArmyGUID, out aidata_Army) && aidata_Army.Army is KaijuArmy)
		{
			this.Kaiju = (aidata_Army.Army as KaijuArmy).Kaiju;
		}
	}

	public override bool IsMissionFinished(bool forceStop)
	{
		return !this.aiDataRepository.IsGUIDValid(base.ForceArmyGUID) || this.Kaiju == null || this.Kaiju.OnGarrisonMode() || !(base.Empire as MajorEmpire).TamedKaijus.Contains(this.Kaiju);
	}

	public override void PopulateMission()
	{
		Diagnostics.Log("ELCP {0} AICommander_KaijuSupport {1}", new object[]
		{
			base.Empire,
			base.Missions.Count
		});
		if (base.Missions.Count == 0 && this.Kaiju != null && this.Kaiju.Empire.Index == base.Empire.Index && this.Kaiju.OnArmyMode())
		{
			Tags tags = new Tags();
			tags.AddTag(base.Category.ToString());
			base.PopulationFirstMissionFromCategory(tags, new object[0]);
		}
	}

	public override void RefreshMission()
	{
		base.RefreshMission();
		if (base.Missions.Count == 0)
		{
			this.PopulateMission();
			this.EvaluateMission();
			base.PromoteMission();
		}
	}

	public override void Release()
	{
		base.Release();
		this.aiDataRepository = null;
		this.Kaiju = null;
	}

	private Kaiju Kaiju;

	private IAIDataRepositoryAIHelper aiDataRepository;
}
