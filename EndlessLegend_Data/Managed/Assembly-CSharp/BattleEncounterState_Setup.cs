using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;

[Diagnostics.TagAttribute("Battle")]
public class BattleEncounterState_Setup : BattleEncounterState
{
	public BattleEncounterState_Setup(BattleEncounter battleEncounter) : base(battleEncounter)
	{
	}

	public override void Begin(params object[] parameters)
	{
		base.Begin(parameters);
		Diagnostics.Assert(base.BattleEncounter != null);
		Diagnostics.Assert(base.BattleEncounter.OrderCreateEncounter != null);
		Diagnostics.Assert(base.BattleEncounter.OrderCreateEncounter.ContenderGUIDs != null);
		Diagnostics.Assert(base.BattleEncounter.OrderCreateEncounter.ContenderGUIDs.Length == 2);
		this.setupDuration = -1.0;
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		this.useTimedEncounters = service.Session.GetLobbyData<bool>("TimedEncounter", false);
		if (this.useTimedEncounters)
		{
			this.setupDuration = service.Session.GetLobbyData<double>("EncounterNotificationTimer", -1.0);
		}
		PlayerController playerController = base.BattleEncounter.PlayerController;
		if (playerController != null)
		{
			for (int i = 0; i < base.BattleEncounter.OrderCreateEncounter.ContenderGUIDs.Length; i++)
			{
				base.BattleEncounter.IncommingJoinContendersCount++;
				OrderJoinEncounter order = new OrderJoinEncounter(base.BattleEncounter.EncounterGUID, (byte)i, base.BattleEncounter.OrderCreateEncounter.ContenderGUIDs[i], false, -1);
				playerController.PostOrder(order);
			}
			if (base.BattleEncounter is BattleCityAssaultEncounter)
			{
				BattleCityAssaultEncounter battleCityAssaultEncounter = base.BattleEncounter as BattleCityAssaultEncounter;
				if (battleCityAssaultEncounter.MilitiaGuid.IsValid && base.BattleEncounter.OrderCreateEncounter.ContenderGUIDs[1] != battleCityAssaultEncounter.CityGuid)
				{
					base.BattleEncounter.IncommingJoinContendersCount++;
					OrderJoinEncounter order2 = new OrderJoinEncounter(base.BattleEncounter.EncounterGUID, 1, battleCityAssaultEncounter.CityGuid, true, 0);
					playerController.PostOrder(order2);
				}
			}
		}
		this.coroutine = Coroutine.StartCoroutine(this.RunAsync(), null);
	}

	public override void Run()
	{
		base.Run();
		if (!this.coroutine.IsFinished)
		{
			this.coroutine.Run();
			return;
		}
		bool flag = true;
		bool flag2 = true;
		bool flag3 = true;
		for (int i = 0; i < base.BattleEncounter.BattleContenders.Count; i++)
		{
			if (base.BattleEncounter.BattleContenders[i].ContenderState != ContenderState.ReadyForDeployment)
			{
				flag = false;
				break;
			}
			flag2 &= (base.BattleEncounter.BattleContenders[i].ContenderEncounterOptionChoice == EncounterOptionChoice.Simulated);
			flag3 &= (base.BattleEncounter.BattleContenders[i].ContenderEncounterOptionChoice > EncounterOptionChoice.Manual);
		}
		if (flag)
		{
			if (base.BattleEncounter.OrderCreateEncounter.EncounterMode != EncounterOptionChoice.Simulated)
			{
				if (base.BattleEncounter.Retreat)
				{
					base.BattleEncounter.OrderCreateEncounter.EncounterMode = EncounterOptionChoice.Simulated;
				}
				else if (base.BattleEncounter.OrderCreateEncounter.Instant)
				{
					base.BattleEncounter.OrderCreateEncounter.EncounterMode = EncounterOptionChoice.Simulated;
				}
				else if (flag2)
				{
					base.BattleEncounter.OrderCreateEncounter.EncounterMode = EncounterOptionChoice.Simulated;
				}
				else if (flag3)
				{
					base.BattleEncounter.OrderCreateEncounter.EncounterMode = EncounterOptionChoice.Spectator;
				}
				else if (ELCPUtilities.UseXumukMPBattleRules)
				{
					PlayerController playerController = base.BattleEncounter.PlayerController;
					if (playerController != null)
					{
						for (int j = 0; j < base.BattleEncounter.BattleContenders.Count; j++)
						{
							if (!base.BattleEncounter.BattleContenders[j].Garrison.Empire.IsControlledByAI)
							{
								OrderChangeContenderEncounterOption order = new OrderChangeContenderEncounterOption(base.BattleEncounter.EncounterGUID, base.BattleEncounter.BattleContenders[j].GUID, EncounterOptionChoice.Manual);
								playerController.PostOrder(order);
							}
						}
					}
				}
			}
			PlayerController playerController2 = base.BattleEncounter.PlayerController;
			if (playerController2 != null)
			{
				OrderBeginEncounter order2 = new OrderBeginEncounter(base.BattleEncounter.EncounterGUID, base.BattleEncounter.OrderCreateEncounter.Instant, base.BattleEncounter.OrderCreateEncounter.EncounterMode);
				playerController2.PostOrder(order2);
			}
			base.BattleEncounter.PostStateChange(typeof(BattleEncounterState_Setup_WaitForContendersAcknowledge), new object[0]);
			return;
		}
		if (this.setupEndTime > 0.0 && this.setupEndTime <= Game.Time)
		{
			this.setupEndTime = -1.0;
			PlayerController playerController3 = base.BattleEncounter.PlayerController;
			if (playerController3 != null)
			{
				for (int k = 0; k < base.BattleEncounter.BattleContenders.Count; k++)
				{
					OrderReadyForDeployment order3 = new OrderReadyForDeployment(base.BattleEncounter.EncounterGUID, base.BattleEncounter.BattleContenders[k].GUID, base.BattleEncounter.BattleContenders[k].ContenderEncounterOptionChoice, false);
					playerController3.PostOrder(order3);
				}
			}
		}
	}

	private IEnumerator RunAsync()
	{
		while (base.BattleEncounter.IncommingJoinContendersCount != 0)
		{
			yield return null;
		}
		base.BattleEncounter.AskNearbyArmiesToJoin();
		while (base.BattleEncounter.IncommingJoinContendersCount != 0)
		{
			yield return null;
		}
		int groupFlags = 0;
		Dictionary<byte, int> unitsCountByGroup = new Dictionary<byte, int>();
		for (int index = 0; index < base.BattleEncounter.BattleContenders.Count; index++)
		{
			groupFlags |= 1 << (int)base.BattleEncounter.BattleContenders[index].Group;
			if (!unitsCountByGroup.ContainsKey(base.BattleEncounter.BattleContenders[index].Group))
			{
				unitsCountByGroup.Add(base.BattleEncounter.BattleContenders[index].Group, 0);
			}
			Dictionary<byte, int> dictionary2;
			Dictionary<byte, int> dictionary = dictionary2 = unitsCountByGroup;
			byte group;
			byte key = group = base.BattleEncounter.BattleContenders[index].Group;
			int num = dictionary2[group];
			dictionary[key] = num + base.BattleEncounter.BattleContenders[index].Garrison.UnitsCount;
		}
		if (groupFlags <= 2)
		{
			base.BattleEncounter.PostStateChange(typeof(BattleEncounterState_Terminate), new object[]
			{
				true
			});
			yield break;
		}
		foreach (KeyValuePair<byte, int> kvp in unitsCountByGroup)
		{
			if (kvp.Value == 0)
			{
				base.BattleEncounter.OrderCreateEncounter.Instant = true;
				if (base.BattleEncounter is BattleCityAssaultEncounter && kvp.Key == base.BattleEncounter.BattleContenders[1].Group)
				{
					(base.BattleEncounter as BattleCityAssaultEncounter).IsCityRipeForTheTaking = true;
				}
			}
		}
		PlayerController playerController = base.BattleEncounter.PlayerController;
		if (playerController != null)
		{
			this.setupEndTime = ((this.setupDuration <= 0.0) ? -1.0 : (Game.Time + this.setupDuration));
			OrderNotifyEncounter orderNotifyEncounter = new OrderNotifyEncounter(base.BattleEncounter.EncounterGUID, this.setupEndTime, this.setupDuration, base.BattleEncounter.OrderCreateEncounter.Instant);
			playerController.PostOrder(orderNotifyEncounter);
			if (base.BattleEncounter.OrderCreateEncounter.Instant)
			{
				for (int index2 = 0; index2 < base.BattleEncounter.BattleContenders.Count; index2++)
				{
					if (!base.BattleEncounter.BattleContenders[index2].IsMainContender)
					{
						OrderIncludeContenderInEncounter orderIncludeContenderInEncounter = new OrderIncludeContenderInEncounter(base.BattleEncounter.EncounterGUID, base.BattleEncounter.BattleContenders[index2].GUID, false);
						playerController.PostOrder(orderIncludeContenderInEncounter);
					}
					OrderReadyForDeployment order = new OrderReadyForDeployment(base.BattleEncounter.EncounterGUID, base.BattleEncounter.BattleContenders[index2].GUID, EncounterOptionChoice.Simulated, false);
					playerController.PostOrder(order);
				}
			}
		}
		yield break;
	}

	private bool useTimedEncounters;

	private double setupDuration = -1.0;

	private double setupEndTime = -1.0;

	private Coroutine coroutine;
}
