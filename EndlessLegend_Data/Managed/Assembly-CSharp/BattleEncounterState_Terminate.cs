using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class BattleEncounterState_Terminate : BattleEncounterState
{
	public BattleEncounterState_Terminate(BattleEncounter battleEncounter) : base(battleEncounter)
	{
	}

	public override void Begin(params object[] parameters)
	{
		base.Begin(parameters);
		bool canceled = false;
		if (parameters.Length >= 1 && parameters[0] is bool)
		{
			canceled = (bool)parameters[0];
		}
		global::PlayerController playerController = base.BattleEncounter.PlayerController;
		if (playerController != null)
		{
			OrderLockEncounterExternalArmies order = new OrderLockEncounterExternalArmies(base.BattleEncounter.EncounterGUID, base.BattleEncounter.ExternalArmies, false);
			playerController.PostOrder(order);
			for (int i = 0; i < base.BattleEncounter.BattleContenders.Count; i++)
			{
				BattleContender battleContender = base.BattleEncounter.BattleContenders[i];
				if (battleContender.DeadParasitesCount > 0)
				{
					BattleContender enemyContenderWithAbilityFromContender = base.BattleEncounter.GetEnemyContenderWithAbilityFromContender(battleContender, UnitAbility.ReadonlyParasite);
					if (enemyContenderWithAbilityFromContender != null)
					{
						enemyContenderWithAbilityFromContender.UndeadUnitsToCreateCount += battleContender.DeadParasitesCount;
					}
				}
			}
			for (int j = 0; j < base.BattleEncounter.BattleContenders.Count; j++)
			{
				BattleContender battleContender2 = base.BattleEncounter.BattleContenders[j];
				if (battleContender2.Garrison != null && battleContender2.UndeadUnitsToCreateCount > 0)
				{
					OrderCreateUndeadUnits order2 = new OrderCreateUndeadUnits(battleContender2.Garrison.Empire.Index, battleContender2.UndeadUnitsToCreateCount, battleContender2.Garrison.GUID, base.BattleEncounter.EncounterGUID);
					playerController.PostOrder(order2);
				}
			}
			OrderEndEncounter orderEndEncounter = new OrderEndEncounter(base.BattleEncounter.EncounterGUID, canceled);
			if (base.BattleEncounter is BattleCityAssaultEncounter)
			{
				orderEndEncounter.DoNotSubtractActionPoints = (base.BattleEncounter as BattleCityAssaultEncounter).IsCityRipeForTheTaking;
			}
			playerController.PostOrder(orderEndEncounter);
			if (base.BattleEncounter is BattleCityAssaultEncounter && base.BattleEncounter.BattleContenders.Count >= 1)
			{
				BattleCityAssaultEncounter battleCityAssaultEncounter = base.BattleEncounter as BattleCityAssaultEncounter;
				IGameService service = Services.GetService<IGameService>();
				Diagnostics.Assert(service != null);
				IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				Diagnostics.Assert(service2 != null);
				IGameEntity gameEntity = null;
				City city = null;
				if (service2.TryGetValue(battleCityAssaultEncounter.CityGuid, out gameEntity) && gameEntity is City)
				{
					city = (gameEntity as City);
				}
				bool flag = false;
				bool flag2 = false;
				for (int k = 0; k < base.BattleEncounter.BattleContenders.Count; k++)
				{
					BattleContender battleContender3 = base.BattleEncounter.BattleContenders[k];
					if (battleContender3.IsTakingPartInBattle && !battleContender3.IsDead)
					{
						if (battleContender3.Garrison.Empire == city.Empire)
						{
							flag = true;
						}
						else
						{
							flag2 = true;
						}
					}
					else if (!battleContender3.IsTakingPartInBattle && battleContender3.Garrison.GUID == city.GUID)
					{
						flag = true;
					}
				}
				flag |= !flag2;
				OrderCityEncounterEnd order3 = new OrderCityEncounterEnd(base.BattleEncounter.BattleContenders[0].Garrison.Empire.Index, base.BattleEncounter.BattleContenders[0].Garrison.GUID, battleCityAssaultEncounter.CityGuid, flag);
				playerController.PostOrder(order3);
			}
		}
	}
}
