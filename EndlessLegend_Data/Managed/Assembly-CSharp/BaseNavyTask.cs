using System;

public abstract class BaseNavyTask : ArmyTask
{
	public abstract NavyTaskEvaluation ComputeFitness(BaseNavyArmy army);

	protected HeuristicValue ComputePowerFitness(float enemyPower, float myPower)
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		enemyPower += 1f;
		myPower += 1f;
		if (enemyPower > myPower)
		{
			float num = myPower / enemyPower;
			if (num < 0.8f)
			{
				heuristicValue.Value = -1f;
				heuristicValue.Log("Enemy really too strong. Ratio My/enemyPower = {0}", new object[]
				{
					num
				});
				return heuristicValue;
			}
			heuristicValue.Add(0.8f, "Max ratio as we are under the enemy power.", new object[0]);
			heuristicValue.Boost(-1f + num, "inverted negative ratio", new object[0]);
		}
		else
		{
			heuristicValue.Add(0.8f, "Max ratio as we are over the enemy power.", new object[0]);
			HeuristicValue heuristicValue2 = new HeuristicValue(0f);
			heuristicValue2.Add(enemyPower, "Enemy power", new object[0]);
			heuristicValue2.Divide(myPower, "My power", new object[0]);
			HeuristicValue heuristicValue3 = new HeuristicValue(0f);
			heuristicValue3.Add(1f, "constant", new object[0]);
			heuristicValue3.Subtract(heuristicValue2, "Enemy/my power ratio", new object[0]);
			heuristicValue3.Clamp(-1f, 1f);
			heuristicValue.Boost(heuristicValue3, "Boost based on power", new object[0]);
		}
		return heuristicValue;
	}

	protected HeuristicValue ComputeDistanceFitness(float numberOfTurnToReach, BaseNavyArmy.ArmyRole armyRole)
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		float num = 5f;
		heuristicValue.Add(numberOfTurnToReach, "Turn to reach", new object[0]);
		float operand = 4f;
		heuristicValue.Divide(operand, "constant", new object[0]);
		heuristicValue.Clamp(0f, num);
		HeuristicValue heuristicValue2 = new HeuristicValue(0f);
		heuristicValue2.Add(num, "constant", new object[0]);
		heuristicValue2.Subtract(heuristicValue, "Turn ratio", new object[0]);
		HeuristicValue heuristicValue3 = new HeuristicValue(0f);
		heuristicValue3.Add(heuristicValue2, "inverted turn ratio", new object[0]);
		heuristicValue3.Divide(num, "constant for normalization", new object[0]);
		return heuristicValue3;
	}
}
