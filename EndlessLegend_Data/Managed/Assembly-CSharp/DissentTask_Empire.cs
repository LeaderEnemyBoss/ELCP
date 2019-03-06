using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class DissentTask_Empire : DissentTask
{
	public DissentTask_Empire(global::Empire owner, global::Empire other) : base(owner)
	{
		this.OtherEmpire = other;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.visibilityService = service.Game.Services.GetService<IVisibilityService>();
	}

	public global::Empire OtherEmpire { get; set; }

	public override void Behave()
	{
		float villageCost = this.GetVillageCost();
		float num;
		base.Owner.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.Owner.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, false);
		bool flag = base.Owner.GetAgency<DepartmentOfForeignAffairs>().IsAtWarWith(this.OtherEmpire);
		if (base.AssociateRequest == null || (base.AssociateRequest.EvaluationState != EvaluableMessage.EvaluableMessageState.Validate && (!flag || villageCost * 3f > num)))
		{
			base.State = TickableState.NoTick;
			return;
		}
		if (this.villagePOI.Count == 0)
		{
			base.State = TickableState.NoTick;
			return;
		}
		float value = this.villagePOI[0].Score.Value;
		int num2 = 0;
		while (num2 < this.villagePOI.Count && value <= this.villagePOI[num2].Score.Value * 2f)
		{
			if (this.visibilityService.IsWorldPositionVisibleFor(this.villagePOI[num2].Village.WorldPosition, base.Owner))
			{
				OrderDissentVillage order = new OrderDissentVillage(base.Owner.Index, this.villagePOI[num2].Village.WorldPosition);
				Ticket ticket;
				base.Owner.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.DissentOrder_TicketRaised));
				if (base.AssociateRequest.ChosenBuyEvaluation != null)
				{
					base.AssociateRequest.SetBeingObtained(this.villagePOI[num2].Village.GUID);
				}
				base.State = TickableState.NoTick;
				if (!flag || base.AssociateRequest.EvaluationState != EvaluableMessage.EvaluableMessageState.Validate)
				{
					break;
				}
			}
			num2++;
		}
		if (base.State == TickableState.NeedTick)
		{
			base.State = TickableState.Optional;
		}
	}

	public override void DisplayDebug()
	{
		string text = "No request";
		if (base.AssociateRequest != null)
		{
			text = base.AssociateRequest.EvaluationState.ToString();
		}
		string text2 = string.Format("Dissent_Empire {0}({1}) - RequestState: {2} - State: {3}", new object[]
		{
			this.OtherEmpire.LocalizedName,
			this.OtherEmpire.Index,
			text,
			base.State
		});
		this.overallFoldout = GUILayout.Toggle(this.overallFoldout, text2, new GUILayoutOption[0]);
		if (this.overallFoldout)
		{
			for (int i = 0; i < this.villagePOI.Count; i++)
			{
				this.villagePOI[i].Score.Display("Village {0}({1}) - Score = {2}", new object[]
				{
					this.villagePOI[i].Village.LocalizedName,
					this.villagePOI[i].Village.GUID,
					this.villagePOI[i].Score.Value
				});
			}
		}
	}

	public override void NewTurn(AILayer_Dissent dissentLayer)
	{
		this.ComputeVillageScoring();
		if (this.villagePOI.Count == 0)
		{
			base.State = TickableState.NoTick;
			return;
		}
		if (base.AssociateRequest != null && !base.AssociateRequest.IsValidForNewTurn())
		{
			base.AssociateRequest.Cancel();
			base.AssociateRequest = null;
		}
		if (base.AssociateRequest == null)
		{
			base.AssociateRequest = new DissentRequest();
			dissentLayer.AIEntity.AIPlayer.Blackboard.AddMessage(base.AssociateRequest);
		}
		HeuristicValue localOpportunity = new HeuristicValue(0f);
		base.AssociateRequest.SetInterest(dissentLayer.GlobalPriority, localOpportunity);
		base.AssociateRequest.TimeOut = 1;
		float villageCost = this.GetVillageCost();
		base.AssociateRequest.UpdateBuyEvaluation("DissentVillage", 0UL, villageCost, (int)BuyEvaluation.MaxTurnGain, 0f, 0UL);
	}

	private void DissentOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result == PostOrderResponse.Processed)
		{
			base.AssociateRequest.SetObtained();
			return;
		}
		base.AssociateRequest.SetFailedToObtain();
	}

	private float GetVillageCost()
	{
		ConstructionCost[] dissentionCost = DepartmentOfTheInterior.GetDissentionCost(base.Owner, this.villagePOI[0].Village);
		if (dissentionCost.Length >= 1)
		{
			return dissentionCost[0].GetValue(base.Owner);
		}
		return 0f;
	}

	private void ComputeVillageScoring()
	{
		this.villagePOI.Clear();
		DepartmentOfTheInterior agency = this.OtherEmpire.GetAgency<DepartmentOfTheInterior>();
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			MinorEmpire minorEmpire = agency.Cities[i].Region.MinorEmpire;
			if (minorEmpire != null)
			{
				BarbarianCouncil council = minorEmpire.GetAgency<BarbarianCouncil>();
				if (council != null)
				{
					bool flag = agency.IsAssimilated(minorEmpire.MinorFaction);
					int villageIndex2;
					int villageIndex;
					Predicate<DissentTask_Empire.VillageScoring> <>9__1;
					for (villageIndex = 0; villageIndex < council.Villages.Count; villageIndex = villageIndex2 + 1)
					{
						if (council.Villages[villageIndex].HasBeenPacified && !council.Villages[villageIndex].HasBeenConverted)
						{
							List<DissentTask_Empire.VillageScoring> list = this.villagePOI;
							Predicate<DissentTask_Empire.VillageScoring> match2;
							if ((match2 = <>9__1) == null)
							{
								match2 = (<>9__1 = ((DissentTask_Empire.VillageScoring match) => match.Village.GUID == council.Villages[villageIndex].GUID));
							}
							DissentTask_Empire.VillageScoring villageScoring = list.Find(match2);
							if (villageScoring == null)
							{
								villageScoring = new DissentTask_Empire.VillageScoring(council.Villages[villageIndex]);
								this.villagePOI.Add(villageScoring);
							}
							villageScoring.Reset();
							if (flag)
							{
								villageScoring.Score.Boost(0.2f, "Assimilated", new object[0]);
							}
							if (this.visibilityService.IsWorldPositionVisibleFor(villageScoring.Village.WorldPosition, base.Owner))
							{
								villageScoring.Score.Boost(0.2f, "Visible", new object[0]);
							}
						}
						villageIndex2 = villageIndex;
					}
				}
			}
		}
		this.villagePOI.Sort((DissentTask_Empire.VillageScoring left, DissentTask_Empire.VillageScoring right) => -1 * left.Score.CompareTo(right.Score));
	}

	private List<DissentTask_Empire.VillageScoring> villagePOI = new List<DissentTask_Empire.VillageScoring>();

	private IVisibilityService visibilityService;

	private bool overallFoldout;

	private class VillageScoring
	{
		public VillageScoring(Village village)
		{
			this.Village = village;
			this.Score = new HeuristicValue(0f);
		}

		public Village Village { get; set; }

		public HeuristicValue Score { get; set; }

		public void Reset()
		{
			this.Score.Reset();
			this.Score.Boost(0.1f, "Pacified", new object[0]);
			if (this.Village.PointOfInterest.PointOfInterestImprovement != null)
			{
				this.Score.Boost(0.2f, "Rebuilt", new object[0]);
			}
		}
	}
}
