using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Xml;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[Diagnostics.TagAttribute("Agency")]
[Diagnostics.TagAttribute("Agency")]
public class DepartmentOfForeignAffairs : Agency, Amplitude.Xml.Serialization.IXmlSerializable, IForeignAffairsManagment
{
	public DepartmentOfForeignAffairs(global::Empire empire) : base(empire)
	{
	}

	static DepartmentOfForeignAffairs()
	{
		DepartmentOfForeignAffairs.DiplomaticCostReductionFromEmpirePropertyNames = new StaticString[]
		{
			new StaticString("DiplomaticCostReduction0"),
			new StaticString("DiplomaticCostReduction1"),
			new StaticString("DiplomaticCostReduction2"),
			new StaticString("DiplomaticCostReduction3"),
			new StaticString("DiplomaticCostReduction4"),
			new StaticString("DiplomaticCostReduction5"),
			new StaticString("DiplomaticCostReduction6"),
			new StaticString("DiplomaticCostReduction7")
		};
	}

	public event EventHandler<DiplomaticRelationStateChangeEventArgs> DiplomaticRelationStateChange;

	void IForeignAffairsManagment.ApplyDiplomaticRelationAbilityChange(global::Empire empireWithWhichTheAbilityChange, DiplomaticAbilityChange diplomaticAbilityChange)
	{
		if (diplomaticAbilityChange == null)
		{
			throw new ArgumentNullException("diplomaticAbilityChange");
		}
		if (diplomaticAbilityChange.Prerequisites != null)
		{
			for (int i = 0; i < diplomaticAbilityChange.Prerequisites.Length; i++)
			{
				if (!diplomaticAbilityChange.Prerequisites[i].Check(empireWithWhichTheAbilityChange))
				{
					return;
				}
			}
		}
		if (diplomaticAbilityChange.Operation == DiplomaticAbilityChange.AbilityOperation.Add)
		{
			((IForeignAffairsManagment)this).AddDiplomaticRelationAbility(empireWithWhichTheAbilityChange, diplomaticAbilityChange.DiplomaticAbilityReference);
		}
		else if (diplomaticAbilityChange.Operation == DiplomaticAbilityChange.AbilityOperation.Remove)
		{
			((IForeignAffairsManagment)this).RemoveDiplomaticRelationAbility(empireWithWhichTheAbilityChange, diplomaticAbilityChange.DiplomaticAbilityReference);
		}
	}

	void IForeignAffairsManagment.AddDiplomaticRelationAbility(global::Empire empireWithWhichTheAbilityChange, StaticString diplomaticAbilityReference)
	{
		if (empireWithWhichTheAbilityChange == null)
		{
			throw new ArgumentNullException("empireWithWhichTheAbilityChange");
		}
		if (StaticString.IsNullOrEmpty(diplomaticAbilityReference))
		{
			throw new ArgumentException("The diplomatic ability reference name must be valid", "diplomaticAbilityReference");
		}
		Diagnostics.Assert(this.diplomaticAbilityStateDatabase != null);
		DiplomaticAbilityDefinition diplomaticAbilityDefinition;
		if (!this.diplomaticAbilityStateDatabase.TryGetValue(diplomaticAbilityReference, out diplomaticAbilityDefinition))
		{
			Diagnostics.LogError("Can't retrieve the element {0}.", new object[]
			{
				diplomaticAbilityReference
			});
			return;
		}
		Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires != null);
		DiplomaticRelation diplomaticRelation = this.diplomaticRelationStateWithOtherMajorEmpires[empireWithWhichTheAbilityChange.Index];
		Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.OwnerEmpireIndex == base.Empire.Index && diplomaticRelation.OtherEmpireIndex == empireWithWhichTheAbilityChange.Index);
		((IDiplomaticRelationManagment)diplomaticRelation).AddDiplomaticAbility(diplomaticAbilityDefinition);
		Diagnostics.Assert(diplomaticAbilityDefinition != null);
		if (diplomaticAbilityDefinition.Descriptors != null)
		{
			for (int i = 0; i < diplomaticAbilityDefinition.Descriptors.Length; i++)
			{
				SimulationDescriptor descriptor = diplomaticAbilityDefinition.Descriptors[i];
				empireWithWhichTheAbilityChange.AddDescriptor(descriptor, false);
			}
		}
		empireWithWhichTheAbilityChange.Refresh(false);
		if (diplomaticAbilityDefinition.NotifyVisibilityHasChanged)
		{
			IVisibilityService service = this.GameService.Game.Services.GetService<IVisibilityService>();
			Diagnostics.Assert(service != null);
			service.NotifyVisibilityHasChanged(base.Empire as global::Empire);
		}
		if (diplomaticAbilityReference == DiplomaticAbilityDefinition.PrestigeTrend)
		{
			this.UpdatePrestigeTrendBonus();
		}
		if (diplomaticAbilityReference == DiplomaticAbilityDefinition.BlackSpot)
		{
			foreach (global::Empire empire in (this.GameService.Game as global::Game).Empires)
			{
				EventBlackSpotUsed eventToNotify = new EventBlackSpotUsed(empire, empireWithWhichTheAbilityChange, base.Empire);
				IEventService service2 = Services.GetService<IEventService>();
				if (service2 != null)
				{
					service2.Notify(eventToNotify);
				}
			}
		}
	}

	void IForeignAffairsManagment.AddDiplomaticRelationScoreModifier(global::Empire empireWithWhichTheAbilityChange, StaticString diplomaticRelationScoreModifierReference)
	{
		if (empireWithWhichTheAbilityChange == null)
		{
			throw new ArgumentNullException("empireWithWhichTheAbilityChange");
		}
		if (StaticString.IsNullOrEmpty(diplomaticRelationScoreModifierReference))
		{
			throw new ArgumentException("The diplomatic relation score reference name must be valid", "diplomaticRelationScoreModifierReference");
		}
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		DiplomaticRelationScoreModifierDefinition definition;
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(diplomaticRelationScoreModifierReference, out definition))
		{
			Diagnostics.LogError("Can't retrieve the element {0}.", new object[]
			{
				diplomaticRelationScoreModifierReference
			});
			return;
		}
		Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires != null);
		DiplomaticRelation diplomaticRelation = this.diplomaticRelationStateWithOtherMajorEmpires[empireWithWhichTheAbilityChange.Index];
		Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.OwnerEmpireIndex == base.Empire.Index && diplomaticRelation.OtherEmpireIndex == empireWithWhichTheAbilityChange.Index);
		((IDiplomaticRelationManagment)diplomaticRelation).AddScoreModifier(definition, 1f);
	}

	void IForeignAffairsManagment.RemoveDiplomaticRelationAbility(global::Empire empireWithWhichTheAbilityChange, StaticString diplomaticAbilityReference)
	{
		if (empireWithWhichTheAbilityChange == null)
		{
			throw new ArgumentNullException("empireWithWhichTheAbilityChange");
		}
		if (StaticString.IsNullOrEmpty(diplomaticAbilityReference))
		{
			throw new ArgumentException("The diplomatic ability reference name must be valid", "diplomaticAbilityReference");
		}
		Diagnostics.Assert(this.diplomaticAbilityStateDatabase != null);
		DiplomaticAbilityDefinition diplomaticAbilityDefinition;
		if (!this.diplomaticAbilityStateDatabase.TryGetValue(diplomaticAbilityReference, out diplomaticAbilityDefinition))
		{
			Diagnostics.LogError("Can't retrieve the element {0}.", new object[]
			{
				diplomaticAbilityReference
			});
			return;
		}
		Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires != null);
		DiplomaticRelation diplomaticRelation = this.diplomaticRelationStateWithOtherMajorEmpires[empireWithWhichTheAbilityChange.Index];
		Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.OwnerEmpireIndex == base.Empire.Index && diplomaticRelation.OtherEmpireIndex == empireWithWhichTheAbilityChange.Index);
		bool flag = ((IDiplomaticRelationManagment)diplomaticRelation).RemoveDiplomaticAbility(diplomaticAbilityDefinition);
		if (flag)
		{
			Diagnostics.Assert(diplomaticAbilityDefinition != null);
			if (diplomaticAbilityDefinition.Descriptors != null)
			{
				for (int i = 0; i < diplomaticAbilityDefinition.Descriptors.Length; i++)
				{
					SimulationDescriptor descriptor = diplomaticAbilityDefinition.Descriptors[i];
					empireWithWhichTheAbilityChange.RemoveDescriptor(descriptor);
				}
			}
			empireWithWhichTheAbilityChange.Refresh(false);
			if (diplomaticAbilityDefinition.NotifyVisibilityHasChanged)
			{
				IVisibilityService service = this.GameService.Game.Services.GetService<IVisibilityService>();
				Diagnostics.Assert(service != null);
				service.NotifyVisibilityHasChanged(empireWithWhichTheAbilityChange);
			}
			if (diplomaticAbilityReference == DiplomaticAbilityDefinition.BlackSpot)
			{
				foreach (global::Empire empire in (this.GameService.Game as global::Game).Empires)
				{
					EventBlackSpotRemoved eventToNotify = new EventBlackSpotRemoved(empire, empireWithWhichTheAbilityChange, base.Empire);
					IEventService service2 = Services.GetService<IEventService>();
					if (service2 != null)
					{
						service2.Notify(eventToNotify);
					}
				}
			}
		}
		if (diplomaticAbilityReference == DiplomaticAbilityDefinition.PrestigeTrend)
		{
			this.UpdatePrestigeTrendBonus();
		}
	}

	void IForeignAffairsManagment.SetDiplomaticRelationState(global::Empire empireWithWhichTheStatusChange, StaticString diplomaticRelationStateName, bool notify)
	{
		if (empireWithWhichTheStatusChange == null)
		{
			throw new ArgumentNullException("empireWithWhichTheStatusChange");
		}
		if (StaticString.IsNullOrEmpty(diplomaticRelationStateName))
		{
			throw new ArgumentException("The diplomatic relation state name must be valid", "diplomaticRelationStateName");
		}
		if (base.Empire.Index == empireWithWhichTheStatusChange.Index)
		{
			Diagnostics.LogError("Can't change the status with yourself.");
			return;
		}
		Diagnostics.Assert(this.diplomaticRelationStateDatabase != null);
		DiplomaticRelationState diplomaticRelationState;
		if (!this.diplomaticRelationStateDatabase.TryGetValue(diplomaticRelationStateName, out diplomaticRelationState))
		{
			Diagnostics.LogError("Can't retrieve the diplomatic status {0}.", new object[]
			{
				diplomaticRelationStateName
			});
			return;
		}
		if (this.diplomaticRelationStateWithOtherMajorEmpires == null)
		{
			Diagnostics.LogError("There is no status between empires.");
			return;
		}
		DiplomaticRelation diplomaticRelation = this.diplomaticRelationStateWithOtherMajorEmpires[empireWithWhichTheStatusChange.Index];
		Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.OwnerEmpireIndex == base.Empire.Index && diplomaticRelation.OtherEmpireIndex == empireWithWhichTheStatusChange.Index);
		DiplomaticRelationState state = diplomaticRelation.State;
		StaticString x = (state == null) ? StaticString.Empty : state.Name;
		if (x == diplomaticRelationStateName)
		{
			return;
		}
		((IDiplomaticRelationManagment)diplomaticRelation).SetDiplomaticRelationState(diplomaticRelationState);
		Diagnostics.Assert(diplomaticRelationState != null);
		if (diplomaticRelationState.DiplomaticAbilityChanges != null)
		{
			for (int i = 0; i < diplomaticRelationState.DiplomaticAbilityChanges.Length; i++)
			{
				DiplomaticAbilityChange diplomaticAbilityChange = diplomaticRelationState.DiplomaticAbilityChanges[i];
				Diagnostics.Assert(diplomaticAbilityChange != null);
				((IForeignAffairsManagment)this).ApplyDiplomaticRelationAbilityChange(empireWithWhichTheStatusChange, diplomaticAbilityChange);
			}
		}
		this.OnDiplomaticRelationStateChange(empireWithWhichTheStatusChange, diplomaticRelationState, state, notify);
	}

	public static float GetEmpirePointCost(DiplomaticTerm diplomaticTerm, global::Empire referenceEmpire)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		Diagnostics.Assert(diplomaticTerm.Definition != null);
		float num = 0f;
		IDiplomaticCost[] diplomaticCosts = diplomaticTerm.DiplomaticCosts;
		if (diplomaticCosts != null)
		{
			foreach (IDiplomaticCost diplomaticCost in diplomaticCosts)
			{
				Diagnostics.Assert(diplomaticCost != null);
				if (!(diplomaticCost.ResourceName != SimulationProperties.EmpirePoint))
				{
					float valueFor = diplomaticCost.GetValueFor(referenceEmpire, diplomaticTerm);
					num += Math.Max(0f, valueFor);
				}
			}
		}
		return num;
	}

	public static float GetEmpirePointCost(DiplomaticContract diplomaticContract, global::Empire referenceEmpire)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		Diagnostics.Assert(diplomaticContract.Terms != null);
		float num = 0f;
		for (int i = 0; i < diplomaticContract.Terms.Count; i++)
		{
			DiplomaticTerm diplomaticTerm = diplomaticContract.Terms[i];
			num += DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTerm, referenceEmpire);
		}
		return num;
	}

	public static bool CheckConstructiblePrerequisites(IDiplomaticContract diplomaticContract, global::Empire empireWhichProvides, global::Empire empireWhichReceives, DepartmentOfForeignAffairs.ConstructibleElement constructibleElement, params string[] flags)
	{
		if (empireWhichProvides == null)
		{
			throw new ArgumentNullException("empireWhichProvides");
		}
		if (empireWhichReceives == null)
		{
			throw new ArgumentNullException("empireWhichReceives");
		}
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		if (constructibleElement.DiplomaticPrerequisites == null)
		{
			return true;
		}
		for (int i = 0; i < constructibleElement.DiplomaticPrerequisites.Length; i++)
		{
			DiplomaticPrerequisite diplomaticPrerequisite = constructibleElement.DiplomaticPrerequisites[i];
			if (diplomaticPrerequisite != null)
			{
				if (flags == null || flags.Length <= 0 || (diplomaticPrerequisite.Flags != null && Array.Exists<string>(diplomaticPrerequisite.Flags, (string prerequisiteFlag) => Array.Exists<string>(flags, (string flag) => flag == prerequisiteFlag))))
				{
					if (!diplomaticPrerequisite.Check(diplomaticContract, empireWhichProvides, empireWhichReceives))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public static bool CheckConstructibleEmpirePrerequisite(global::Empire empireWhichProvides, DepartmentOfForeignAffairs.ConstructibleElement constructibleElement, params string[] flags)
	{
		if (empireWhichProvides == null)
		{
			throw new ArgumentNullException("empireWhichProvides");
		}
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		if (constructibleElement.DiplomaticPrerequisites == null)
		{
			return true;
		}
		for (int i = 0; i < constructibleElement.DiplomaticPrerequisites.Length; i++)
		{
			DiplomaticRelationStateEmpirePrerequisite diplomaticRelationStateEmpirePrerequisite = constructibleElement.DiplomaticPrerequisites[i] as DiplomaticRelationStateEmpirePrerequisite;
			if (diplomaticRelationStateEmpirePrerequisite != null)
			{
				if (flags == null || flags.Length <= 0 || (diplomaticRelationStateEmpirePrerequisite.Flags != null && Array.Exists<string>(diplomaticRelationStateEmpirePrerequisite.Flags, (string prerequisiteFlag) => Array.Exists<string>(flags, (string flag) => flag == prerequisiteFlag))))
				{
					if (!diplomaticRelationStateEmpirePrerequisite.Check(empireWhichProvides))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public static void CheckConstructiblePrerequisites(DiplomaticTermDefinition termDefinition, IDiplomaticContract diplomaticContract, global::Empire empireWhichProvides, global::Empire empireWhichReceives, ref List<StaticString> failureFlags, params string[] flags)
	{
		if (termDefinition == null)
		{
			throw new ArgumentNullException("termDefinition");
		}
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		if (empireWhichProvides == null)
		{
			throw new ArgumentNullException("empireWhichProvides");
		}
		if (empireWhichReceives == null)
		{
			throw new ArgumentNullException("empireWhichReceives");
		}
		if (termDefinition.DiplomaticPrerequisites == null)
		{
			return;
		}
		for (int i = 0; i < termDefinition.DiplomaticPrerequisites.Length; i++)
		{
			DiplomaticPrerequisite diplomaticPrerequisite = termDefinition.DiplomaticPrerequisites[i];
			Diagnostics.Assert(diplomaticPrerequisite != null);
			if (flags == null || flags.Length <= 0 || (diplomaticPrerequisite.Flags != null && Array.Exists<string>(diplomaticPrerequisite.Flags, (string prerequisiteFlag) => Array.Exists<string>(flags, (string flag) => flag == prerequisiteFlag))))
			{
				if (!diplomaticPrerequisite.Check(diplomaticContract, empireWhichProvides, empireWhichReceives))
				{
					if (diplomaticPrerequisite.Flags != null)
					{
						for (int j = 0; j < diplomaticPrerequisite.Flags.Length; j++)
						{
							failureFlags.AddOnce(diplomaticPrerequisite.Flags[j]);
						}
					}
					else
					{
						Diagnostics.LogWarning("Prerequisite <{0}> are false but there is no associated flags to set in the production state.", new object[]
						{
							diplomaticPrerequisite.ToString()
						});
					}
				}
			}
		}
	}

	public static bool CheckDiplomaticTermPrerequisites(DiplomaticTermDefinition termDefinition, IDiplomaticContract diplomaticContract, global::Empire empireWhichProposes, global::Empire empireWhichProvides, global::Empire empireWhichReceives, params string[] flags)
	{
		if (termDefinition == null)
		{
			throw new ArgumentNullException("termDefinition");
		}
		if (empireWhichProvides == null)
		{
			throw new ArgumentNullException("empireWhichProvides");
		}
		if (empireWhichReceives == null)
		{
			throw new ArgumentNullException("empireWhichReceives");
		}
		Diagnostics.Assert(empireWhichProposes != null);
		if (termDefinition.ApplicationMethod == DiplomaticTerm.ApplicationMethod.Symetric && empireWhichProposes.Index != empireWhichProvides.Index)
		{
			if (flags != null)
			{
				if (!Array.Exists<string>(flags, (string flag) => flag == DepartmentOfForeignAffairs.SymetricFailureTag))
				{
					goto IL_8F;
				}
			}
			return false;
		}
		IL_8F:
		if ((termDefinition.ApplicationMethod == DiplomaticTerm.ApplicationMethod.ProviderOnly || termDefinition.ApplicationMethod == DiplomaticTerm.ApplicationMethod.ReceiverOnly) && termDefinition.PropositionMethod == DiplomaticTerm.PropositionMethod.Declaration && empireWhichProposes.Index != empireWhichProvides.Index)
		{
			if (flags != null)
			{
				if (!Array.Exists<string>(flags, (string flag) => flag == DepartmentOfForeignAffairs.EmpireWhichProvidesMustBeEmpireWhichProposesTag))
				{
					goto IL_F4;
				}
			}
			return false;
		}
		IL_F4:
		return DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empireWhichProposes, termDefinition, flags) && DepartmentOfForeignAffairs.CheckConstructiblePrerequisites(diplomaticContract, empireWhichProvides, empireWhichReceives, termDefinition, flags);
	}

	public static void CheckDiplomaticTermPrerequisites(DiplomaticTermDefinition termDefinition, IDiplomaticContract diplomaticContract, global::Empire empireWhichProposes, global::Empire empireWhichProvides, global::Empire empireWhichReceives, ref List<StaticString> failureFlags, params string[] flags)
	{
		if (termDefinition == null)
		{
			throw new ArgumentNullException("termDefinition");
		}
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		if (empireWhichProvides == null)
		{
			throw new ArgumentNullException("empireWhichProvides");
		}
		if (empireWhichReceives == null)
		{
			throw new ArgumentNullException("empireWhichReceives");
		}
		Diagnostics.Assert(empireWhichProposes != null);
		if (termDefinition.ApplicationMethod == DiplomaticTerm.ApplicationMethod.Symetric && empireWhichProposes.Index != empireWhichProvides.Index)
		{
			DepartmentOfForeignAffairs.AddFailureTags(DepartmentOfForeignAffairs.SymetricFailureTag, ref failureFlags, flags);
		}
		if ((termDefinition.ApplicationMethod == DiplomaticTerm.ApplicationMethod.ProviderOnly || termDefinition.ApplicationMethod == DiplomaticTerm.ApplicationMethod.ReceiverOnly) && termDefinition.PropositionMethod == DiplomaticTerm.PropositionMethod.Declaration && diplomaticContract.EmpireWhichProposes.Index != empireWhichProvides.Index)
		{
			DepartmentOfForeignAffairs.AddFailureTags(DepartmentOfForeignAffairs.EmpireWhichProvidesMustBeEmpireWhichProposesTag, ref failureFlags, flags);
		}
		DepartmentOfTheTreasury.CheckConstructiblePrerequisites(diplomaticContract.EmpireWhichProposes, termDefinition, ref failureFlags, flags);
		DepartmentOfForeignAffairs.CheckConstructiblePrerequisites(termDefinition, diplomaticContract, empireWhichProvides, empireWhichReceives, ref failureFlags, flags);
	}

	public static void GetAllAvailableDiplomaticTerm(DiplomaticContract diplomaticContract, global::Empire empireWhichProvides, global::Empire empireWhichReceives, ref List<DiplomaticTerm> diplomaticTerms)
	{
		IDatabase<DepartmentOfForeignAffairs.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfForeignAffairs.ConstructibleElement>(false);
		if (database != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = database.GetValues();
			for (int i = 0; i < values.Length; i++)
			{
				DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
				if (diplomaticTermDefinition != null)
				{
					DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, empireWhichProvides, empireWhichReceives, ref diplomaticTerms);
				}
			}
		}
	}

	public static void GetDiplomaticTerms(DiplomaticTermDefinition termDefinition, IDiplomaticContract diplomaticContract, global::Empire empireWhichProvides, global::Empire empireWhichReceives, ref List<DiplomaticTerm> diplomaticTerms)
	{
		if (termDefinition == null)
		{
			throw new ArgumentNullException("termDefinition");
		}
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		if (empireWhichProvides == null)
		{
			throw new ArgumentNullException("empireWhichProvides");
		}
		if (empireWhichReceives == null)
		{
			throw new ArgumentNullException("empireWhichReceives");
		}
		if (!DepartmentOfForeignAffairs.CheckDiplomaticTermPrerequisites(termDefinition, diplomaticContract, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives, new string[]
		{
			ConstructionFlags.Prerequisite,
			DepartmentOfForeignAffairs.SymetricFailureTag,
			DepartmentOfForeignAffairs.InvalidPropositionMethodTag,
			DepartmentOfForeignAffairs.UnicityFailureTag,
			DepartmentOfForeignAffairs.EmpireWhichProvidesMustBeEmpireWhichProposesTag
		}))
		{
			return;
		}
		if (termDefinition is DiplomaticTermDiplomaticRelationStateDefinition)
		{
			DiplomaticTerm diplomaticTerm = new DiplomaticTermDiplomaticRelationState(termDefinition as DiplomaticTermDiplomaticRelationStateDefinition, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives);
			DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTerm, diplomaticContract, ref diplomaticTerms);
		}
		else if (termDefinition is DiplomaticTermCityExchangeDefinition)
		{
			DiplomaticTermCityExchangeDefinition definition = termDefinition as DiplomaticTermCityExchangeDefinition;
			DepartmentOfTheInterior agency = empireWhichProvides.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(agency != null);
			for (int i = 0; i < agency.Cities.Count; i++)
			{
				City city = agency.Cities[i];
				DiplomaticTerm diplomaticTerm = new DiplomaticTermCityExchange(definition, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives, city.GUID);
				DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTerm, diplomaticContract, ref diplomaticTerms);
			}
		}
		else if (termDefinition is DiplomaticTermFortressExchangeDefinition)
		{
			DiplomaticTermFortressExchangeDefinition definition2 = termDefinition as DiplomaticTermFortressExchangeDefinition;
			DepartmentOfTheInterior agency2 = empireWhichProvides.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(agency2 != null);
			for (int j = 0; j < agency2.OccupiedFortresses.Count; j++)
			{
				Fortress fortress = agency2.OccupiedFortresses[j];
				DiplomaticTerm diplomaticTerm = new DiplomaticTermFortressExchange(definition2, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives, fortress.GUID);
				DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTerm, diplomaticContract, ref diplomaticTerms);
			}
		}
		else if (termDefinition is DiplomaticTermResourceExchangeDefinition)
		{
			DiplomaticTermResourceExchangeDefinition diplomaticTermResourceExchangeDefinition = termDefinition as DiplomaticTermResourceExchangeDefinition;
			DepartmentOfTheTreasury agency3 = empireWhichProvides.GetAgency<DepartmentOfTheTreasury>();
			Diagnostics.Assert(agency3 != null);
			if (diplomaticTermResourceExchangeDefinition.TradableResources != null && diplomaticTermResourceExchangeDefinition.TradableResources.TradableResourceReferences != null)
			{
				foreach (XmlNamedReference xmlNamedReference in diplomaticTermResourceExchangeDefinition.TradableResources.TradableResourceReferences)
				{
					Diagnostics.Assert(xmlNamedReference != null);
					float num;
					if (agency3.TryGetResourceStockValue(empireWhichProvides, xmlNamedReference.Name, out num, true))
					{
						if (num >= 1f)
						{
							DiplomaticTerm diplomaticTerm = new DiplomaticTermResourceExchange(diplomaticTermResourceExchangeDefinition, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives, xmlNamedReference.Name, Mathf.Floor(num));
							DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTerm, diplomaticContract, ref diplomaticTerms);
						}
					}
				}
			}
		}
		else if (termDefinition is DiplomaticTermBoosterExchangeDefinition)
		{
			DiplomaticTermBoosterExchangeDefinition diplomaticTermBoosterExchangeDefinition = termDefinition as DiplomaticTermBoosterExchangeDefinition;
			DepartmentOfEducation agency4 = empireWhichProvides.GetAgency<DepartmentOfEducation>();
			Diagnostics.Assert(agency4 != null);
			if (diplomaticTermBoosterExchangeDefinition.Boosters != null && diplomaticTermBoosterExchangeDefinition.Boosters.BoosterReferences != null)
			{
				XmlNamedReference[] boosterReferences = diplomaticTermBoosterExchangeDefinition.Boosters.BoosterReferences;
				for (int l = 0; l < boosterReferences.Length; l++)
				{
					XmlNamedReference availableBoosterReference = boosterReferences[l];
					Diagnostics.Assert(availableBoosterReference != null);
					if (agency4.Count((VaultItem match) => match.Constructible.Name == availableBoosterReference.Name) >= 1)
					{
						GameEntityGUID guid = agency4.FirstOrDefault((VaultItem match) => match.Constructible.Name == availableBoosterReference.Name).GUID;
						GameEntityGUID[] boosterGUIDs = new GameEntityGUID[]
						{
							guid
						};
						DiplomaticTerm diplomaticTerm = new DiplomaticTermBoosterExchange(diplomaticTermBoosterExchangeDefinition, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives, boosterGUIDs, availableBoosterReference.Name);
						DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTerm, diplomaticContract, ref diplomaticTerms);
					}
				}
			}
		}
		else if (termDefinition is DiplomaticTermTechnologyExchangeDefinition)
		{
			DiplomaticTermTechnologyExchangeDefinition definition3 = termDefinition as DiplomaticTermTechnologyExchangeDefinition;
			DepartmentOfScience agency5 = empireWhichProvides.GetAgency<DepartmentOfScience>();
			DepartmentOfScience agency6 = empireWhichReceives.GetAgency<DepartmentOfScience>();
			Diagnostics.Assert(agency5 != null && agency6 != null);
			IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
			DepartmentOfScience.ConstructibleElement[] values = database.GetValues();
			for (int m = 0; m < values.Length; m++)
			{
				TechnologyDefinition technologyDefinition = values[m] as TechnologyDefinition;
				if (technologyDefinition != null)
				{
					if (technologyDefinition.Visibility == TechnologyDefinitionVisibility.Visible)
					{
						if (agency5.GetTechnologyState(technologyDefinition) == DepartmentOfScience.ConstructibleElement.State.Researched)
						{
							if (agency6.GetTechnologyState(technologyDefinition) != DepartmentOfScience.ConstructibleElement.State.Researched)
							{
								if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empireWhichReceives, technologyDefinition, new string[]
								{
									ConstructionFlags.DiplomaticTradable,
									ConstructionFlags.DiplomaticTradableForEmpireWhichReceives
								}))
								{
									if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empireWhichProvides, technologyDefinition, new string[]
									{
										ConstructionFlags.DiplomaticTradable,
										ConstructionFlags.DiplomaticTradableForEmpireWhichProvides
									}))
									{
										DiplomaticTerm diplomaticTerm = new DiplomaticTermTechnologyExchange(definition3, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives, technologyDefinition);
										DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTerm, diplomaticContract, ref diplomaticTerms);
									}
								}
							}
						}
					}
				}
			}
		}
		else if (termDefinition is DiplomaticTermProposalDefinition)
		{
			DiplomaticTermProposalDefinition proposalDefinition = termDefinition as DiplomaticTermProposalDefinition;
			DiplomaticTermProposal diplomaticTermProposal = new DiplomaticTermProposal(proposalDefinition, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives);
			DepartmentOfForeignAffairs.thirdEmpires.Clear();
			if (diplomaticTermProposal.TryGetValidEmpires(diplomaticContract, ref DepartmentOfForeignAffairs.thirdEmpires) && DepartmentOfForeignAffairs.thirdEmpires.Count > 0)
			{
				diplomaticTermProposal.ChangeEmpire(diplomaticContract, DepartmentOfForeignAffairs.thirdEmpires[0]);
				DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTermProposal, diplomaticContract, ref diplomaticTerms);
			}
		}
		else if (termDefinition is DiplomaticTermPrisonerExchangeDefinition)
		{
			DiplomaticTermPrisonerExchangeDefinition definition4 = termDefinition as DiplomaticTermPrisonerExchangeDefinition;
			DepartmentOfEducation agency7 = empireWhichProvides.GetAgency<DepartmentOfEducation>();
			for (int n = 0; n < agency7.Prisoners.Count; n++)
			{
				if (agency7.Prisoners[n].OwnerEmpireIndex == empireWhichReceives.Index && (agency7.Prisoners[n].CaptureNoticed || diplomaticContract.EmpireWhichProposes.Index == empireWhichProvides.Index))
				{
					DiplomaticTerm diplomaticTerm = new DiplomaticTermPrisonerExchange(definition4, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives, agency7.Prisoners[n].UnitGuid);
					DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTerm, diplomaticContract, ref diplomaticTerms);
				}
			}
		}
		else if (termDefinition is DiplomaticTermMapExchangeDefinition)
		{
			DiplomaticTermMapExchangeDefinition definition5 = termDefinition as DiplomaticTermMapExchangeDefinition;
			DiplomaticTerm diplomaticTerm = new DiplomaticTermMapExchange(definition5, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives);
			DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTerm, diplomaticContract, ref diplomaticTerms);
		}
		else
		{
			DiplomaticTerm diplomaticTerm = new DiplomaticTerm(termDefinition, diplomaticContract.EmpireWhichProposes, empireWhichProvides, empireWhichReceives);
			DepartmentOfForeignAffairs.AddDiplomaticTermToList(diplomaticTerm, diplomaticContract, ref diplomaticTerms);
		}
	}

	private static void AddDiplomaticTermToList(DiplomaticTerm diplomaticTerm, IDiplomaticContract diplomaticContract, ref List<DiplomaticTerm> diplomaticTerms)
	{
		if (diplomaticTerm.CanApply(diplomaticContract, new string[]
		{
			ConstructionFlags.Prerequisite,
			DepartmentOfForeignAffairs.SymetricFailureTag,
			DepartmentOfForeignAffairs.InvalidPropositionMethodTag,
			DepartmentOfForeignAffairs.UnicityFailureTag,
			DepartmentOfForeignAffairs.EmpireWhichProvidesMustBeEmpireWhichProposesTag
		}))
		{
			diplomaticTerms.Add(diplomaticTerm);
		}
	}

	private static void AddFailureTags(StaticString failureTag, ref List<StaticString> failureFlags, params string[] flags)
	{
		if (flags == null || flags.Length == 0 || Array.Exists<string>(flags, (string flag) => flag == failureTag))
		{
			failureFlags.AddOnce(failureTag);
		}
	}

	private void AddDiplomaticRelationScoreModifier(IDiplomaticRelationManagment diplomaticRelationManagment, StaticString diplomaticRelationScoreModifierName)
	{
		DiplomaticRelationScoreModifierDefinition definition;
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(diplomaticRelationScoreModifierName, out definition))
		{
			Diagnostics.LogWarning("Can't retrieve DiplomaticRelationScoreModifier {0} from database.", new object[]
			{
				diplomaticRelationScoreModifierName
			});
			return;
		}
		diplomaticRelationManagment.AddScoreModifier(definition, 1f);
	}

	private void DepartmentOfDefense_OnAttackStart(object sender, AttackStartEventArgs eventArgs)
	{
		if (base.Empire.Index != eventArgs.Attacker.Empire.Index)
		{
			return;
		}
		global::Empire empire = null;
		if (eventArgs.DefenderGameEntity is Army)
		{
			empire = (eventArgs.DefenderGameEntity as Army).Empire;
		}
		else if (eventArgs.DefenderGameEntity is District)
		{
			empire = (eventArgs.DefenderGameEntity as District).Empire;
		}
		else if (eventArgs.DefenderGameEntity is City)
		{
			empire = (eventArgs.DefenderGameEntity as City).Empire;
		}
		else if (eventArgs.DefenderGameEntity is Camp)
		{
			empire = (eventArgs.DefenderGameEntity as Camp).Empire;
		}
		else
		{
			if (eventArgs.DefenderGameEntity is Village)
			{
				return;
			}
			if (eventArgs.DefenderGameEntity is Fortress)
			{
				if (!(eventArgs.DefenderGameEntity as Fortress).IsOccupied)
				{
					return;
				}
				empire = (eventArgs.DefenderGameEntity as Fortress).Occupant;
			}
			else if (eventArgs.DefenderGameEntity is Kaiju)
			{
				empire = (eventArgs.DefenderGameEntity as Kaiju).MajorEmpire;
			}
			else if (eventArgs.DefenderGameEntity is KaijuGarrison)
			{
				empire = (eventArgs.DefenderGameEntity as KaijuGarrison).Kaiju.MajorEmpire;
			}
		}
		if (empire == null)
		{
			Diagnostics.LogWarning("Unknown defender (type: '{0}', guid: {1}).", new object[]
			{
				eventArgs.DefenderGameEntity.GetType(),
				eventArgs.DefenderGameEntity.GUID.ToString()
			});
			return;
		}
		DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(empire);
		if (diplomaticRelation == null)
		{
			return;
		}
		IDiplomaticRelationManagment diplomaticRelationManagment = diplomaticRelation;
		Diagnostics.Assert(diplomaticRelationManagment != null);
		diplomaticRelationManagment.RemoveScoreModifiersByType(DiplomaticRelationScoreModifier.Names.Peaceful);
	}

	private void DepartmentOfDefense_OnSiegeStateChange(object sender, SiegeStateChangedEventArgs eventArgs)
	{
		if (base.Empire.Index != eventArgs.Attacker.Empire.Index)
		{
			return;
		}
		Diagnostics.Assert(eventArgs.City != null);
		DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(eventArgs.City.Empire);
		IDiplomaticRelationManagment diplomaticRelationManagment = diplomaticRelation;
		Diagnostics.Assert(diplomaticRelationManagment != null);
		diplomaticRelationManagment.RemoveScoreModifiersByType(DiplomaticRelationScoreModifier.Names.Peaceful);
	}

	private void DiplomaticRelationScore_DiplomaticRelationStateChange(DiplomaticRelationStateChangeEventArgs eventArgs)
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		Diagnostics.Assert(base.Empire.Index == eventArgs.Empire.Index);
		DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(eventArgs.EmpireWithWhichTheStatusChange);
		IDiplomaticRelationManagment diplomaticRelationManagment = diplomaticRelation;
		if (eventArgs.PreviousDiplomaticRelationState != null)
		{
			if (eventArgs.PreviousDiplomaticRelationState.Name == DiplomaticRelationState.Names.Peace)
			{
				diplomaticRelationManagment.RemoveScoreModifiersByType(DiplomaticRelationScoreModifier.Names.StatePeace);
			}
			else if (eventArgs.PreviousDiplomaticRelationState.Name == DiplomaticRelationState.Names.Alliance)
			{
				diplomaticRelationManagment.RemoveScoreModifiersByType(DiplomaticRelationScoreModifier.Names.StateAlliance);
			}
			else if (eventArgs.PreviousDiplomaticRelationState.Name == DiplomaticRelationState.Names.War)
			{
				diplomaticRelationManagment.RemoveScoreModifiersByType(DiplomaticRelationScoreModifier.Names.StateWar);
				this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.LastWar);
			}
			else if (eventArgs.PreviousDiplomaticRelationState.Name == DiplomaticRelationState.Names.ColdWar)
			{
				diplomaticRelationManagment.RemoveScoreModifiersByType(DiplomaticRelationScoreModifier.Names.Peaceful);
			}
		}
		Diagnostics.Assert(eventArgs.DiplomaticRelationState != null);
		if (eventArgs.DiplomaticRelationState.Name == DiplomaticRelationState.Names.Peace)
		{
			this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.StatePeace);
		}
		else if (eventArgs.DiplomaticRelationState.Name == DiplomaticRelationState.Names.Alliance)
		{
			this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.StateAlliance);
		}
		else if (eventArgs.DiplomaticRelationState.Name == DiplomaticRelationState.Names.War)
		{
			diplomaticRelationManagment.RemoveModifiers((DiplomaticRelationScoreModifier modifier) => modifier.Definition.Category == DiplomaticRelationScoreModifier.Categories.Affinity);
			this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.StateWar);
		}
		if (eventArgs.PreviousDiplomaticRelationState != eventArgs.DiplomaticRelationState && eventArgs.PreviousDiplomaticRelationState != null && eventArgs.PreviousDiplomaticRelationState.Name != DiplomaticRelationState.Names.Unknown)
		{
			this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.RelationStateChangeChaos);
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs eventArgs)
	{
		if (base.Empire == null)
		{
			return;
		}
		EventDiplomaticContractStateChange eventDiplomaticContractStateChange = eventArgs.RaisedEvent as EventDiplomaticContractStateChange;
		if (eventDiplomaticContractStateChange != null)
		{
			DiplomaticContract diplomaticContract = eventDiplomaticContractStateChange.DiplomaticContract;
			Diagnostics.Assert(diplomaticContract != null);
			Diagnostics.Assert(diplomaticContract.EmpireWhichProposes != null);
			Diagnostics.Assert(diplomaticContract.EmpireWhichReceives != null);
			if (diplomaticContract.State == DiplomaticContractState.Signed && diplomaticContract.Terms != null && (diplomaticContract.EmpireWhichProposes.Index == base.Empire.Index || diplomaticContract.EmpireWhichReceives.Index == base.Empire.Index))
			{
				global::Empire empire = (diplomaticContract.EmpireWhichProposes.Index != base.Empire.Index) ? diplomaticContract.EmpireWhichProposes : diplomaticContract.EmpireWhichReceives;
				DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(empire);
				Diagnostics.Assert(diplomaticRelation != null);
				IDiplomaticRelationManagment diplomaticRelationManagment = diplomaticRelation;
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && term.Definition.Name == DiplomaticTermDefinition.Names.Warning))
				{
					diplomaticRelationManagment.RemoveScoreModifiersByType(DiplomaticRelationScoreModifier.Names.Peaceful);
				}
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.Warning || term.Definition.Name == DiplomaticTermDefinition.Names.Gratify)))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.DiscussionChaos);
				}
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.WarToTruceDeclaration || term.Definition.Name == DiplomaticTermDefinition.Names.ColdWarToPeaceDeclaration || term.Definition.Name == DiplomaticTermDefinition.Names.ColdWarToAllianceDeclaration || term.Definition.Name == DiplomaticTermDefinition.Names.PeaceToAllianceDeclaration)))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.ForceStatusChaos);
				}
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.MarketBan || term.Definition.Name == DiplomaticTermDefinition.Names.MarketBanNullification || term.Definition.Name == DiplomaticTermDefinition.Names.MarketBanRemoval)))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.MarketBanChaos);
				}
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.BlackSpot || term.Definition.Name == DiplomaticTermDefinition.Names.BlackSpotNullification || term.Definition.Name == DiplomaticTermDefinition.Names.BlackSpotRemoval)))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.BlackSpotChaos);
				}
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.OpenBorders || term.Definition.Name == DiplomaticTermDefinition.Names.CloseBorders) && term.EmpireWhichProvides.Index == base.Empire.Index))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.BordersChaos);
				}
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.MapEmbargo || term.Definition.Name == DiplomaticTermDefinition.Names.VisionAndMapEmbargo || term.Definition.Name == DiplomaticTermDefinition.Names.VisionEmbargo || term.Definition.Name == DiplomaticTermDefinition.Names.MapExchange || term.Definition.Name == DiplomaticTermDefinition.Names.VisionAndMapExchange)))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.VisionChaos);
				}
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.CommercialAgreement || term.Definition.Name == DiplomaticTermDefinition.Names.CommercialEmbargo)))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.CommercialChaos);
				}
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.ResearchAgreement || term.Definition.Name == DiplomaticTermDefinition.Names.ResearchEmbargo)))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.ResearchChaos);
				}
				if (this.IsCommercialContract(diplomaticContract))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.PositiveContractSigned);
				}
				if (this.IsAgressiveContract(diplomaticContract))
				{
					this.AddDiplomaticRelationScoreModifier(diplomaticRelationManagment, DiplomaticRelationScoreModifier.Names.NegativeContractSigned);
				}
			}
		}
	}

	private IEnumerator GameClientState_Turn_Begin_UpdateDiplomaticRelationsScoreModifiers(string context, string name)
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		DiplomaticRelationScoreModifierDefinition peacefulModifierDefinition;
		if (this.diplomaticRelationScoreModifierDatabase.TryGetValue(DiplomaticRelationScoreModifier.Names.Peaceful, out peacefulModifierDefinition))
		{
			Diagnostics.Assert(this.majorEmpires != null);
			for (int index = 0; index < this.majorEmpires.Length; index++)
			{
				global::Empire majorEmpire = this.majorEmpires[index];
				DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(majorEmpire);
				Diagnostics.Assert(diplomaticRelation != null);
				IDiplomaticRelationManagment diplomaticRelationManagment = diplomaticRelation;
				if (diplomaticRelation.State == null || diplomaticRelation.State.Name != DiplomaticRelationState.Names.ColdWar)
				{
					diplomaticRelationManagment.RemoveScoreModifiersByType(DiplomaticRelationScoreModifier.Names.Peaceful);
				}
				else if (diplomaticRelationManagment.GetNumberOfScoreModifiersOfType(DiplomaticRelationScoreModifier.Names.Peaceful) == 0)
				{
					diplomaticRelationManagment.AddScoreModifier(peacefulModifierDefinition, 1f);
				}
			}
		}
		yield break;
	}

	private bool IsAgressiveContract(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		if (diplomaticContract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Negotiation)
		{
			return false;
		}
		if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term is DiplomaticTermDiplomaticRelationState))
		{
			return false;
		}
		return !diplomaticContract.Terms.Any((DiplomaticTerm term) => term.Definition.Name == DiplomaticTermDefinition.Names.Gratify || term.Definition.Name == DiplomaticTermDefinition.Names.Warning);
	}

	private bool IsCommercialContract(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		if (diplomaticContract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Declaration)
		{
			return false;
		}
		if (diplomaticContract.Terms == null || diplomaticContract.Terms.Count == 0)
		{
			return false;
		}
		if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term is DiplomaticTermDiplomaticRelationState))
		{
			return false;
		}
		return diplomaticContract.Terms.All((DiplomaticTerm term) => term is DiplomaticTermResourceExchange || term is DiplomaticTermTechnologyExchange || term is DiplomaticTermBoosterExchange || term is DiplomaticTermCityExchange || term is DiplomaticTermFortressExchange || term.Definition.Alignment == DiplomaticTermAlignment.Good);
	}

	public bool CanSeeOrbWithOrbHunterTrait
	{
		get
		{
			if (base.Empire.SimulationObject.Tags.Contains(PanelFeatureTerrainOrb.OrbHunterFactionTrait))
			{
				return true;
			}
			for (int i = 0; i < this.DiplomaticRelations.Count; i++)
			{
				if (this.DiplomaticRelations[i] != null)
				{
					DiplomaticRelation diplomaticRelation = this.DiplomaticRelations[i];
					bool flag = diplomaticRelation.HasActiveAbility(PanelFeatureTerrainOrb.MapExchangeDiplomaticAbilityName);
					bool flag2 = this.majorEmpires[diplomaticRelation.OtherEmpireIndex].SimulationObject.Tags.Contains(PanelFeatureTerrainOrb.OrbHunterFactionTrait);
					if (flag && flag2)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public static bool CanAttackMe(Army myArmy, Army opponentArmy)
	{
		if (myArmy == null)
		{
			throw new ArgumentNullException("myArmy");
		}
		if (opponentArmy == null)
		{
			return false;
		}
		if (myArmy.IsPrivateers)
		{
			return true;
		}
		global::Empire empire = opponentArmy.Empire;
		if (empire.Index == myArmy.Empire.Index)
		{
			return false;
		}
		if (empire is MajorEmpire && !opponentArmy.IsPrivateers)
		{
			DepartmentOfForeignAffairs agency = opponentArmy.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency != null);
			return agency.CanAttack(myArmy);
		}
		return true;
	}

	public static void GetDiplomaticTermAmountLimits(DiplomaticTerm diplomaticTerm, out float minimumAmount, out float maximumAmount)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		minimumAmount = float.NaN;
		maximumAmount = float.NaN;
		global::Empire empireWhichProvides = diplomaticTerm.EmpireWhichProvides;
		Diagnostics.Assert(empireWhichProvides != null);
		DiplomaticTermResourceExchange diplomaticTermResourceExchange = diplomaticTerm as DiplomaticTermResourceExchange;
		if (diplomaticTermResourceExchange != null)
		{
			DepartmentOfTheTreasury agency = empireWhichProvides.GetAgency<DepartmentOfTheTreasury>();
			Diagnostics.Assert(agency != null);
			float f;
			if (!agency.TryGetResourceStockValue(empireWhichProvides.SimulationObject, diplomaticTermResourceExchange.ResourceName, out f, false))
			{
				Diagnostics.LogWarning("Could not get the available quantity of the resource {0}.", new object[]
				{
					diplomaticTermResourceExchange.ResourceName
				});
				return;
			}
			minimumAmount = 0f;
			maximumAmount = Mathf.Floor(f);
		}
		DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = diplomaticTerm as DiplomaticTermBoosterExchange;
		if (diplomaticTermBoosterExchange != null)
		{
			DepartmentOfEducation agency2 = empireWhichProvides.GetAgency<DepartmentOfEducation>();
			Diagnostics.Assert(agency2 != null);
			minimumAmount = 0f;
			maximumAmount = (float)agency2.Count((VaultItem match) => match.Constructible.Name == diplomaticTermBoosterExchange.BoosterDefinitionName);
		}
	}

	public static float GetPeacePointGain(DiplomaticTerm diplomaticTerm, global::Empire concernedEmpire)
	{
		float num = 0f;
		IDiplomaticCost[] diplomaticCosts = diplomaticTerm.DiplomaticCosts;
		if (diplomaticCosts != null)
		{
			foreach (IDiplomaticCost diplomaticCost in diplomaticCosts)
			{
				if (!(diplomaticCost.ResourceName != DepartmentOfTheTreasury.Resources.EmpirePoint))
				{
					float valueFor = diplomaticCost.GetValueFor(concernedEmpire, diplomaticTerm);
					num += valueFor;
				}
			}
			num *= concernedEmpire.GetPropertyValue(SimulationProperties.EmpirePointToPeacePointFactor);
		}
		return num;
	}

	public static void GetPeacePointGain(DiplomaticContract diplomaticContract, out float empireWhichProposesPeacePoint, out float empireWhichReceivesPeacePoint)
	{
		empireWhichProposesPeacePoint = 0f;
		empireWhichReceivesPeacePoint = 0f;
		for (int i = 0; i < diplomaticContract.Terms.Count; i++)
		{
			DiplomaticTerm diplomaticTerm = diplomaticContract.Terms[i];
			IDiplomaticCost[] diplomaticCosts = diplomaticTerm.DiplomaticCosts;
			if (diplomaticCosts != null)
			{
				foreach (IDiplomaticCost diplomaticCost in diplomaticCosts)
				{
					if (!(diplomaticCost.ResourceName != DepartmentOfTheTreasury.Resources.EmpirePoint) && diplomaticCost.CanBeConvertedToPeacePoint)
					{
						empireWhichProposesPeacePoint += diplomaticCost.GetValueFor(diplomaticContract.EmpireWhichProposes, diplomaticTerm);
						empireWhichReceivesPeacePoint += diplomaticCost.GetValueFor(diplomaticContract.EmpireWhichReceives, diplomaticTerm);
					}
				}
			}
		}
		empireWhichProposesPeacePoint *= diplomaticContract.EmpireWhichProposes.GetPropertyValue(SimulationProperties.EmpirePointToPeacePointFactor);
		empireWhichReceivesPeacePoint *= diplomaticContract.EmpireWhichReceives.GetPropertyValue(SimulationProperties.EmpirePointToPeacePointFactor);
	}

	public IEnumerable<DiplomaticTermDiplomaticRelationStateDefinition> GetDiplomaticTermDiplomaticRelationStateDefinition(DiplomaticContract diplomaticContract, StaticString targetedDiplomaticRelationState)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(diplomaticContract.EmpireWhichReceives);
		Diagnostics.Assert(diplomaticRelation != null);
		if (diplomaticRelation.State == null)
		{
			yield break;
		}
		Diagnostics.Assert(this.constructibleElementDatabase != null);
		foreach (DepartmentOfForeignAffairs.ConstructibleElement constructibleElement in this.constructibleElementDatabase)
		{
			DiplomaticTermDiplomaticRelationStateDefinition definition = constructibleElement as DiplomaticTermDiplomaticRelationStateDefinition;
			if (definition != null)
			{
				if (!(definition.DiplomaticRelationStateReference != targetedDiplomaticRelationState))
				{
					if (DepartmentOfForeignAffairs.CheckDiplomaticTermPrerequisites(definition, diplomaticContract, diplomaticContract.EmpireWhichProposes, diplomaticContract.EmpireWhichProposes, diplomaticContract.EmpireWhichReceives, new string[0]))
					{
						yield return definition;
					}
				}
			}
		}
		yield break;
	}

	public void GetOrCreateContract(global::Empire empireWhichReceives, Action<DiplomaticContract> onContractRetrievedDelegate)
	{
		if (empireWhichReceives == null)
		{
			throw new ArgumentNullException("empireWhichReceives");
		}
		if (onContractRetrievedDelegate == null)
		{
			throw new ArgumentNullException("onContractRetrievedDelegate");
		}
		global::Empire empire = base.Empire as global::Empire;
		Diagnostics.Assert(empire != null);
		IDiplomacyService service = this.GameService.Game.Services.GetService<IDiplomacyService>();
		Diagnostics.Assert(service != null);
		DiplomaticContract obj;
		if (!service.TryGetActiveDiplomaticContract(empire, empireWhichReceives, out obj))
		{
			OrderCreateDiplomaticContract order = new OrderCreateDiplomaticContract(empire, empireWhichReceives);
			Ticket ticket;
			empire.PlayerControllers.Client.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.DiplomaticContractCreationComplete));
			this.diplomaticContractDelegateByTicket.Add(ticket.Order.TicketNumber, onContractRetrievedDelegate);
		}
		else
		{
			onContractRetrievedDelegate(obj);
		}
	}

	public void PreFillContract(global::Empire empireWhichReceives, Action<DiplomaticContract> preFillContractCompleteDelegate, StaticString wantedRelationState)
	{
		Diagnostics.Assert(this.preFillContractRequests != null);
		this.preFillContractRequests.Add(new DepartmentOfForeignAffairs.PreFillContractRequest(empireWhichReceives, DiplomaticRelationState.Names.War, preFillContractCompleteDelegate));
		this.GetOrCreateContract(empireWhichReceives, new Action<DiplomaticContract>(this.OnContractRetrievedDelegate));
	}

	public bool IsInWarWithSomeone()
	{
		for (int i = 0; i < this.DiplomaticRelations.Count; i++)
		{
			DiplomaticRelation diplomaticRelation = this.DiplomaticRelations[i];
			bool flag = diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War;
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	private void DiplomaticContractCreationComplete(object sender, TicketRaisedEventArgs eventArgs)
	{
		Diagnostics.Assert(this.diplomaticContractDelegateByTicket != null);
		Action<DiplomaticContract> action;
		if (!this.diplomaticContractDelegateByTicket.TryGetValue(eventArgs.Order.TicketNumber, out action))
		{
			Diagnostics.LogError("Can't retrieve the delegate for ticket number {0}.", new object[]
			{
				eventArgs.Order.TicketNumber
			});
		}
		this.diplomaticContractDelegateByTicket.Remove(eventArgs.Order.TicketNumber);
		if (action == null)
		{
			return;
		}
		if (eventArgs.Result != PostOrderResponse.Processed)
		{
			action(null);
			return;
		}
		OrderCreateDiplomaticContract orderCreateDiplomaticContract = eventArgs.Order as OrderCreateDiplomaticContract;
		Diagnostics.Assert(orderCreateDiplomaticContract != null);
		IDiplomaticContractRepositoryService service = this.GameService.Game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(service != null);
		DiplomaticContract obj;
		if (!service.TryGetValue(orderCreateDiplomaticContract.ContractGUID, out obj))
		{
			Diagnostics.LogError("Can't retrieve the created contract {0}.", new object[]
			{
				orderCreateDiplomaticContract.ContractGUID
			});
		}
		action(obj);
	}

	private void OnChangeDiplomaticContractTermsCollectionComplete(object sender, TicketRaisedEventArgs ticketRaisedEventArgs)
	{
		OrderChangeDiplomaticContractTermsCollection order = ticketRaisedEventArgs.Order as OrderChangeDiplomaticContractTermsCollection;
		if (order == null)
		{
			return;
		}
		Diagnostics.Assert(this.preFillContractRequests != null);
		DepartmentOfForeignAffairs.PreFillContractRequest preFillContractRequest = this.preFillContractRequests.Find((DepartmentOfForeignAffairs.PreFillContractRequest request) => request.DiplomaticContract != null && request.DiplomaticContract.GUID == order.ContractGUID);
		if (preFillContractRequest == null)
		{
			Diagnostics.LogError("Can't retrieve the pre-fill contract request assotiated to the contract {0}.", new object[]
			{
				order.ContractGUID
			});
			return;
		}
		if (preFillContractRequest.RequestCompleteDelegate != null)
		{
			if (ticketRaisedEventArgs.Result == PostOrderResponse.Processed)
			{
				preFillContractRequest.RequestCompleteDelegate(preFillContractRequest.DiplomaticContract);
			}
			else
			{
				preFillContractRequest.RequestCompleteDelegate(null);
			}
		}
		this.preFillContractRequests.Remove(preFillContractRequest);
	}

	private void OnContractRetrievedDelegate(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			Diagnostics.LogError("Can't retrieve diplomatic contract.");
			return;
		}
		Diagnostics.Assert(this.preFillContractRequests != null);
		DepartmentOfForeignAffairs.PreFillContractRequest preFillContractRequest = this.preFillContractRequests.Find((DepartmentOfForeignAffairs.PreFillContractRequest request) => request.EmpireWhichReceives.Index == diplomaticContract.EmpireWhichReceives.Index);
		if (preFillContractRequest == null)
		{
			Diagnostics.LogError("Can't retrieve the pre-fill contract request assotiated to the empire {0}.", new object[]
			{
				diplomaticContract.EmpireWhichReceives
			});
			return;
		}
		preFillContractRequest.DiplomaticContract = diplomaticContract;
		DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition = this.GetDiplomaticTermDiplomaticRelationStateDefinition(diplomaticContract, preFillContractRequest.WantedRelationState).FirstOrDefault<DiplomaticTermDiplomaticRelationStateDefinition>();
		if (diplomaticTermDiplomaticRelationStateDefinition == null)
		{
			this.preFillContractRequests.Remove(preFillContractRequest);
			Diagnostics.LogError("Can't retrieve the diplomaticTermDefinition to pass to the relation state {0}.", new object[]
			{
				preFillContractRequest.WantedRelationState
			});
			return;
		}
		DiplomaticTerm term = new DiplomaticTermDiplomaticRelationState(diplomaticTermDiplomaticRelationStateDefinition, diplomaticContract.EmpireWhichInitiated, diplomaticContract.EmpireWhichProposes, diplomaticContract.EmpireWhichReceives);
		DiplomaticTermChange[] array = new DiplomaticTermChange[diplomaticContract.Terms.Count + 1];
		for (int i = 0; i < diplomaticContract.Terms.Count; i++)
		{
			array[i] = DiplomaticTermChange.Remove(diplomaticContract.Terms[i].Index);
		}
		array[array.Length - 1] = DiplomaticTermChange.Add(term);
		OrderChangeDiplomaticContractTermsCollection order = new OrderChangeDiplomaticContractTermsCollection(diplomaticContract, array);
		Ticket ticket;
		diplomaticContract.EmpireWhichInitiated.PlayerControllers.Client.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnChangeDiplomaticContractTermsCollectionComplete));
	}

	public override void DumpAsText(StringBuilder content, string indent = "")
	{
		base.DumpAsText(content, indent);
		for (int i = 0; i < this.diplomaticRelationStateWithOtherMajorEmpires.Length; i++)
		{
			DiplomaticRelation diplomaticRelation = this.diplomaticRelationStateWithOtherMajorEmpires[i];
			if (i != base.Empire.Index && diplomaticRelation != null)
			{
				diplomaticRelation.DumpAsText(content, indent);
			}
		}
	}

	public override byte[] DumpAsBytes()
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			binaryWriter.Write(base.DumpAsBytes());
			for (int i = 0; i < this.diplomaticRelationStateWithOtherMajorEmpires.Length; i++)
			{
				DiplomaticRelation diplomaticRelation = this.diplomaticRelationStateWithOtherMajorEmpires[i];
				if (i != base.Empire.Index && diplomaticRelation != null)
				{
					binaryWriter.Write(diplomaticRelation.DumpAsBytes());
				}
			}
		}
		byte[] result = memoryStream.ToArray();
		memoryStream.Close();
		return result;
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("DiplomaticRelations");
		this.diplomaticRelationStateWithOtherMajorEmpires = new DiplomaticRelation[attribute];
		for (int i = 0; i < this.diplomaticRelationStateWithOtherMajorEmpires.Length; i++)
		{
			this.diplomaticRelationStateWithOtherMajorEmpires[i] = new DiplomaticRelation();
			reader.ReadElementSerializable<DiplomaticRelation>(ref this.diplomaticRelationStateWithOtherMajorEmpires[i]);
			Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires[i].OwnerEmpireIndex == base.Empire.Index);
			Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires[i].OtherEmpireIndex >= 0);
		}
		reader.ReadEndElement("DiplomaticRelations");
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("DiplomaticRelations");
		writer.WriteAttributeString<int>("Count", this.diplomaticRelationStateWithOtherMajorEmpires.Length);
		for (int i = 0; i < this.diplomaticRelationStateWithOtherMajorEmpires.Length; i++)
		{
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = this.diplomaticRelationStateWithOtherMajorEmpires[i];
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	public ReadOnlyCollection<DiplomaticRelation> DiplomaticRelations
	{
		get
		{
			if (this.diplomaticRelations == null)
			{
				this.diplomaticRelations = this.diplomaticRelationStateWithOtherMajorEmpires.ToList<DiplomaticRelation>().AsReadOnly();
			}
			return this.diplomaticRelations;
		}
	}

	[Service]
	private IGameService GameService { get; set; }

	public DiplomaticRelation GetDiplomaticRelation(int index)
	{
		Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires != null);
		if (index >= this.diplomaticRelationStateWithOtherMajorEmpires.Length)
		{
			return null;
		}
		return this.diplomaticRelationStateWithOtherMajorEmpires[index];
	}

	public DiplomaticRelation GetDiplomaticRelation(global::Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires != null);
		if (empire.Index >= this.diplomaticRelationStateWithOtherMajorEmpires.Length)
		{
			return null;
		}
		return this.diplomaticRelationStateWithOtherMajorEmpires[empire.Index];
	}

	public DiplomaticRelationState GetDiplomaticRelationStateFromName(StaticString diplomaticRelationStateName)
	{
		if (StaticString.IsNullOrEmpty(diplomaticRelationStateName))
		{
			throw new ArgumentException("The diplomatic relation state name must be valid", "diplomaticRelationStateName");
		}
		Diagnostics.Assert(this.diplomaticRelationStateDatabase != null);
		DiplomaticRelationState result;
		if (!this.diplomaticRelationStateDatabase.TryGetValue(diplomaticRelationStateName, out result))
		{
			Diagnostics.LogError("Can't retrieve the diplomatic relation state {0}.", new object[]
			{
				diplomaticRelationStateName
			});
			return null;
		}
		return result;
	}

	public bool CanAttack(IGameEntity opponentGameEntity)
	{
		if (opponentGameEntity == null)
		{
			throw new ArgumentNullException("opponentGameEntity");
		}
		Army army = opponentGameEntity as Army;
		if (army != null)
		{
			if (army.IsPrivateers)
			{
				return true;
			}
			global::Empire empire = army.Empire;
			if (!(empire is MajorEmpire))
			{
				return true;
			}
			if (empire.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim))
			{
				return true;
			}
			DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(empire);
			if (diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.AttackArmies))
			{
				return true;
			}
			if (diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.ColdWarAttackArmies) || (diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown))
			{
				Diagnostics.Assert(this.worldPositionningService != null);
				Region region = this.worldPositionningService.GetRegion(army.WorldPosition);
				Diagnostics.Assert(region != null);
				if (!region.BelongToEmpire(empire))
				{
					return true;
				}
			}
			return false;
		}
		else
		{
			City city = opponentGameEntity as City;
			if (city == null)
			{
				District district = opponentGameEntity as District;
				city = ((district == null) ? null : district.City);
			}
			if (city != null)
			{
				global::Empire empire2 = city.Empire;
				if (!(empire2 is MajorEmpire))
				{
					return true;
				}
				if (empire2.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim))
				{
					return true;
				}
				DiplomaticRelation diplomaticRelation2 = this.GetDiplomaticRelation(empire2);
				Diagnostics.Assert(diplomaticRelation2 != null);
				return diplomaticRelation2.HasActiveAbility(DiplomaticAbilityDefinition.AttackCities);
			}
			else
			{
				Camp camp = opponentGameEntity as Camp;
				if (camp != null)
				{
					global::Empire empire3 = camp.Empire;
					if (!(empire3 is MajorEmpire))
					{
						return true;
					}
					if (empire3.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim))
					{
						return true;
					}
					DiplomaticRelation diplomaticRelation3 = this.GetDiplomaticRelation(empire3);
					Diagnostics.Assert(diplomaticRelation3 != null);
					return diplomaticRelation3.HasActiveAbility(DiplomaticAbilityDefinition.AttackCities);
				}
				else
				{
					Village village = opponentGameEntity as Village;
					if (village != null)
					{
						if (!village.HasBeenConverted)
						{
							return true;
						}
						if (base.Empire == village.Converter)
						{
							return false;
						}
						if (village.Converter.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim))
						{
							return true;
						}
						DiplomaticRelation diplomaticRelation4 = this.GetDiplomaticRelation(village.Converter);
						if (diplomaticRelation4.State.Name == DiplomaticRelationState.Names.ColdWar)
						{
							return !village.Region.IsRegionColonized() || village.Region.Owner == base.Empire;
						}
						return diplomaticRelation4.HasActiveAbility(DiplomaticAbilityDefinition.AttackCities);
					}
					else
					{
						Fortress fortress = opponentGameEntity as Fortress;
						if (fortress != null)
						{
							if (!fortress.IsOccupied)
							{
								return true;
							}
							if (base.Empire == fortress.Occupant)
							{
								return false;
							}
							if (fortress.Occupant.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim))
							{
								return true;
							}
							DiplomaticRelation diplomaticRelation5 = this.GetDiplomaticRelation(fortress.Occupant);
							if (fortress.Region != null && fortress.Region.BelongToEmpire(fortress.Occupant))
							{
								return diplomaticRelation5.HasActiveAbility(DiplomaticAbilityDefinition.AttackCities);
							}
							return diplomaticRelation5.HasActiveAbility(DiplomaticAbilityDefinition.AttackCities) || diplomaticRelation5.HasActiveAbility(DiplomaticAbilityDefinition.ColdWarAttackArmies);
						}
						else
						{
							KaijuGarrison kaijuGarrison = opponentGameEntity as KaijuGarrison;
							if (kaijuGarrison != null && !kaijuGarrison.Kaiju.IsStunned())
							{
								global::Empire empire4 = kaijuGarrison.Kaiju.Empire;
								if (!(empire4 is MajorEmpire))
								{
									return true;
								}
								if (empire4.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim))
								{
									return true;
								}
								DiplomaticRelation diplomaticRelation6 = this.GetDiplomaticRelation(empire4);
								Diagnostics.Assert(diplomaticRelation6 != null);
								return diplomaticRelation6.HasActiveAbility(DiplomaticAbilityDefinition.AttackCities);
							}
							else
							{
								KaijuArmy kaijuArmy = opponentGameEntity as KaijuArmy;
								if (kaijuArmy != null && !kaijuArmy.Kaiju.IsStunned())
								{
									global::Empire empire5 = kaijuArmy.Kaiju.Empire;
									if (!(empire5 is MajorEmpire))
									{
										return true;
									}
									if (empire5.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim))
									{
										return true;
									}
									DiplomaticRelation diplomaticRelation7 = this.GetDiplomaticRelation(empire5);
									Diagnostics.Assert(diplomaticRelation7 != null);
									return diplomaticRelation7.HasActiveAbility(DiplomaticAbilityDefinition.AttackArmies);
								}
								else
								{
									Kaiju kaiju = opponentGameEntity as Kaiju;
									if (kaiju != null && !kaiju.IsStunned())
									{
										return true;
									}
									Diagnostics.LogWarning("DepartmentOfForeignAffairs.CanAttack: Unknown game entity {0} of type {1}.", new object[]
									{
										opponentGameEntity.GUID,
										opponentGameEntity.GetType()
									});
									return false;
								}
							}
						}
					}
				}
			}
		}
	}

	public bool CanBesiegeCity(City city)
	{
		Diagnostics.Assert(city != null);
		global::Empire empire = city.Empire as MajorEmpire;
		if (empire != null)
		{
			DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(empire);
			Diagnostics.Assert(diplomaticRelation != null);
			if (diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.SiegeCities))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsBannedFromMarket()
	{
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			if (i != base.Empire.Index)
			{
				DepartmentOfForeignAffairs agency = this.majorEmpires[i].GetAgency<DepartmentOfForeignAffairs>();
				if (agency != null && agency.HasBannedEmpireFromMarket(base.Empire.Index))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsBlackSpotVictim()
	{
		for (int i = 0; i < this.diplomaticRelationStateWithOtherMajorEmpires.Length; i++)
		{
			DiplomaticRelation diplomaticRelation = this.diplomaticRelationStateWithOtherMajorEmpires[i];
			if (diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.BlackSpot))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsBlackSpottedBy(int empireIndex)
	{
		for (int i = 0; i < this.diplomaticRelationStateWithOtherMajorEmpires.Length; i++)
		{
			DiplomaticRelation diplomaticRelation = this.diplomaticRelationStateWithOtherMajorEmpires[i];
			if (diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.BlackSpot) && i == empireIndex)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsEnnemy(global::Empire otherEmpire)
	{
		if (otherEmpire == null)
		{
			return false;
		}
		DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(otherEmpire);
		if (diplomaticRelation != null && diplomaticRelation.State != null)
		{
			if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
			{
				return true;
			}
			if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar)
			{
				return true;
			}
			if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsAtWarWith(global::Empire otherEmpire)
	{
		if (otherEmpire == null)
		{
			return false;
		}
		DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(otherEmpire);
		return diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War;
	}

	public bool IsSymetricallyDiscovered(MajorEmpire otherEmpire)
	{
		if (otherEmpire == null)
		{
			return false;
		}
		if (otherEmpire.Index == base.Empire.Index)
		{
			return false;
		}
		DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(otherEmpire);
		bool flag = diplomaticRelation != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown;
		DepartmentOfForeignAffairs agency = otherEmpire.GetAgency<DepartmentOfForeignAffairs>();
		DiplomaticRelation diplomaticRelation2 = agency.GetDiplomaticRelation((global::Empire)base.Empire);
		bool flag2 = diplomaticRelation2 != null && diplomaticRelation2.State.Name != DiplomaticRelationState.Names.Unknown;
		return flag && flag2;
	}

	public int CountNumberOfWar()
	{
		int num = 0;
		for (int i = 0; i < this.DiplomaticRelations.Count; i++)
		{
			DiplomaticRelation diplomaticRelation = this.DiplomaticRelations[i];
			if (diplomaticRelation != null && diplomaticRelation.State != null)
			{
				if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
				{
					num++;
				}
			}
		}
		return num;
	}

	public bool IsFriend(global::Empire otherEmpire)
	{
		if (otherEmpire == null)
		{
			return false;
		}
		DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(otherEmpire);
		if (diplomaticRelation != null && diplomaticRelation.State != null)
		{
			if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
			{
				return true;
			}
			if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasBannedEmpireFromMarket(int otherEmpireIndex)
	{
		if (this.diplomaticRelationStateWithOtherMajorEmpires != null)
		{
			for (int i = 0; i < this.diplomaticRelationStateWithOtherMajorEmpires.Length; i++)
			{
				DiplomaticRelation diplomaticRelation = this.diplomaticRelationStateWithOtherMajorEmpires[i];
				if (diplomaticRelation.OtherEmpireIndex == otherEmpireIndex && diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.MarketBan))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool CanMoveOn(WorldPosition position, bool isPrivateers = false, bool isCamouflaged = false)
	{
		if (!isPrivateers)
		{
			Diagnostics.Assert(this.worldPositionningService != null);
			Region region = this.worldPositionningService.GetRegion(position);
			Diagnostics.Assert(region != null);
			MajorEmpire majorEmpire = region.Owner as MajorEmpire;
			if (majorEmpire != null && majorEmpire.Index != base.Empire.Index && !region.IsOcean && (!isCamouflaged || this.visibilityService.IsWorldPositionDetectedFor(position, majorEmpire)))
			{
				DepartmentOfForeignAffairs agency = majorEmpire.GetAgency<DepartmentOfForeignAffairs>();
				Diagnostics.Assert(agency != null);
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(base.Empire as global::Empire);
				Diagnostics.Assert(diplomaticRelation != null);
				if (diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.CloseBorders) && !majorEmpire.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim) && (!base.Empire.SimulationObject.Tags.Contains(this.seasonEffectDiplomacy1) || majorEmpire.SimulationObject.Tags.Contains(SeasonEffect.WinterEffectImmunity)))
				{
					return false;
				}
			}
		}
		District district = this.worldPositionningService.GetDistrict(position);
		if (district != null && district.Type != DistrictType.Exploitation)
		{
			global::Empire empire = district.Empire;
			if (empire != null && empire.Index != base.Empire.Index)
			{
				DiplomaticRelation diplomaticRelation2 = this.GetDiplomaticRelation(empire);
				Diagnostics.Assert(diplomaticRelation2 != null);
				if (!diplomaticRelation2.HasActiveAbility(DiplomaticAbilityDefinition.PassThroughCities))
				{
					return false;
				}
			}
		}
		return true;
	}

	public bool CanMoveOn(int regionIndex, bool isPrivateers)
	{
		if (isPrivateers)
		{
			return true;
		}
		Diagnostics.Assert(this.worldPositionningService != null);
		Region region = this.worldPositionningService.GetRegion(regionIndex);
		Diagnostics.Assert(region != null);
		if (region.City != null && region.City.Empire != null && !region.IsOcean)
		{
			global::Empire empire = region.City.Empire;
			if (empire is MajorEmpire && empire.Index != base.Empire.Index)
			{
				DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
				Diagnostics.Assert(agency != null);
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(base.Empire as global::Empire);
				Diagnostics.Assert(diplomaticRelation != null);
				if (diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.CloseBorders) && (!base.Empire.SimulationObject.Tags.Contains(this.seasonEffectDiplomacy1) || empire.SimulationObject.Tags.Contains(SeasonEffect.WinterEffectImmunity)))
				{
					return false;
				}
			}
		}
		return true;
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.GameService = Services.GetService<IGameService>();
		Diagnostics.Assert(this.GameService != null);
		this.constructibleElementDatabase = Databases.GetDatabase<DepartmentOfForeignAffairs.ConstructibleElement>(false);
		Diagnostics.Assert(this.constructibleElementDatabase != null);
		this.diplomaticRelationStateDatabase = Databases.GetDatabase<DiplomaticRelationState>(false);
		Diagnostics.Assert(this.diplomaticRelationStateDatabase != null);
		this.diplomaticAbilityStateDatabase = Databases.GetDatabase<DiplomaticAbilityDefinition>(false);
		Diagnostics.Assert(this.diplomaticAbilityStateDatabase != null);
		this.diplomaticRelationScoreModifierDatabase = Databases.GetDatabase<DiplomaticRelationScoreModifierDefinition>(false);
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		this.eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.eventService != null);
		Diagnostics.Assert(this.GameService != null);
		this.worldPositionningService = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.visibilityService = this.GameService.Game.Services.GetService<IVisibilityService>();
		Diagnostics.Assert(this.visibilityService != null);
		this.diplomaticRelationStateWithOtherMajorEmpires = null;
		Diagnostics.Assert(base.Empire != null);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "UpdateDiplomaticRelationsScoreModifiers", new Agency.Action(this.GameClientState_Turn_Begin_UpdateDiplomaticRelationsScoreModifiers), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "NotifyCurrentNegotiationsAfterLoading", new Agency.Action(this.GameClientState_Turn_Begin_NotifyCurrentNegotiationsAfterLoading), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "BeginTurnUpdateAlliancePrestigeTrendBonus", new Agency.Action(this.GameClientState_Turn_Begin_UpdateAlliancePrestigeTrendBonus), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "UpdateAlliancePrestigeTrendBonus", new Agency.Action(this.GameClientState_Turn_End_UpdateAlliancePrestigeTrendBonus), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "UpdateDiplomaticRelations", new Agency.Action(this.GameClientState_Turn_End_UpdateDiplomaticRelations), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "ComputeTotalDiplomaticRelationScore", new Agency.Action(this.GameClientState_Turn_End_ComputeTotalDiplomaticRelationScore), new string[]
		{
			"UpdateDiplomaticRelations"
		});
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		DepartmentOfDefense departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		departmentOfDefense.OnAttackStart += this.DepartmentOfDefense_OnAttackStart;
		departmentOfDefense.OnSiegeStateChange += this.DepartmentOfDefense_OnSiegeStateChange;
		IEventService eventService = Services.GetService<IEventService>();
		eventService.EventRaise += this.EventService_EventRaise;
		if (this.diplomaticRelationStateWithOtherMajorEmpires != null)
		{
			for (int index = 0; index < this.DiplomaticRelations.Count; index++)
			{
				this.DiplomaticRelations[index].RefreshDiplomaticAbilities();
			}
		}
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		global::Game fantasyGame = game as global::Game;
		Diagnostics.Assert(fantasyGame != null && fantasyGame.Empires != null);
		this.majorEmpires = (from empire in fantasyGame.Empires
		where empire is MajorEmpire
		select empire).ToArray<global::Empire>();
		if (this.diplomaticRelationStateWithOtherMajorEmpires == null)
		{
			this.diplomaticRelationStateWithOtherMajorEmpires = new DiplomaticRelation[this.majorEmpires.Length];
			Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires != null);
			for (int index = 0; index < this.majorEmpires.Length; index++)
			{
				Diagnostics.Assert(fantasyGame.Empires[index] is MajorEmpire);
				float turnDurationFactor = base.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
				this.diplomaticRelationStateWithOtherMajorEmpires[index] = new DiplomaticRelation(base.Empire.Index, index, turnDurationFactor);
				if (base.Empire.Index != index)
				{
					((IForeignAffairsManagment)this).SetDiplomaticRelationState(fantasyGame.Empires[index], DiplomaticRelationState.Names.Unknown, false);
				}
			}
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		if (this.GameService != null)
		{
			this.GameService = null;
		}
		this.diplomaticRelationStateWithOtherMajorEmpires = new DiplomaticRelation[0];
	}

	private IEnumerator GameClientState_Turn_Begin_NotifyCurrentNegotiationsAfterLoading(string context, string name)
	{
		IDiplomaticContractRepositoryService diplomacyService = this.GameService.Game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(diplomacyService != null);
		foreach (DiplomaticContract contract2 in diplomacyService.FindAll((DiplomaticContract contract) => contract.EmpireWhichReceives == base.Empire && contract.State == DiplomaticContractState.Proposed))
		{
			IEventService eventService = Services.GetService<IEventService>();
			eventService.Notify(new EventDiplomaticContractStateChange(contract2, contract2.State));
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_UpdateAlliancePrestigeTrendBonus(string context, string name)
	{
		this.UpdatePrestigeTrendBonus();
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UpdateAlliancePrestigeTrendBonus(string context, string name)
	{
		this.UpdatePrestigeTrendBonus();
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UpdateDiplomaticRelations(string context, string name)
	{
		Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires != null);
		for (int index = 0; index < this.diplomaticRelationStateWithOtherMajorEmpires.Length; index++)
		{
			IDiplomaticRelationManagment diplomaticRelationManagment = this.diplomaticRelationStateWithOtherMajorEmpires[index];
			Diagnostics.Assert(diplomaticRelationManagment != null);
			diplomaticRelationManagment.EndTurnUpdate();
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_ComputeTotalDiplomaticRelationScore(string context, string name)
	{
		float affinityRelationScoreSum = 0f;
		Diagnostics.Assert(this.diplomaticRelationStateWithOtherMajorEmpires != null);
		for (int index = 0; index < this.diplomaticRelationStateWithOtherMajorEmpires.Length; index++)
		{
			affinityRelationScoreSum += this.diplomaticRelationStateWithOtherMajorEmpires[index].AffinityScore;
		}
		base.Empire.SetPropertyBaseValue(SimulationProperties.AffinityRelationScoreSum, affinityRelationScoreSum);
		yield break;
	}

	private void OnDiplomaticRelationStateChange(global::Empire empireWithWhichTheStatusChange, DiplomaticRelationState diplomaticRelationState, DiplomaticRelationState previousRelationState, bool notify)
	{
		if (diplomaticRelationState == null)
		{
			throw new ArgumentNullException("diplomaticRelationState");
		}
		DiplomaticRelationStateChangeEventArgs diplomaticRelationStateChangeEventArgs = new DiplomaticRelationStateChangeEventArgs(base.Empire as global::Empire, empireWithWhichTheStatusChange, diplomaticRelationState, previousRelationState);
		if (this.DiplomaticRelationStateChange != null)
		{
			this.DiplomaticRelationStateChange(this, diplomaticRelationStateChangeEventArgs);
		}
		if (notify)
		{
			Diagnostics.Assert(this.eventService != null);
			this.eventService.Notify(new EventDiplomaticRelationStateChange(base.Empire, empireWithWhichTheStatusChange, diplomaticRelationState.Name, (previousRelationState == null) ? StaticString.Empty : previousRelationState.Name));
		}
		this.DiplomaticRelationScore_DiplomaticRelationStateChange(diplomaticRelationStateChangeEventArgs);
	}

	private void UpdatePrestigeTrendBonus()
	{
		float num = 0f;
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			global::Empire majorEmpire = this.majorEmpires[i];
			num += this.ComputePrestigeTrendBonusForEmpire(majorEmpire);
		}
		base.Empire.SetPropertyBaseValue(SimulationProperties.PrestigeTrendBonus, num);
	}

	private float ComputePrestigeTrendBonusForEmpire(global::Empire majorEmpire)
	{
		if (majorEmpire == null)
		{
			throw new ArgumentNullException("majorEmpire");
		}
		DiplomaticRelation diplomaticRelation = this.GetDiplomaticRelation(majorEmpire);
		Diagnostics.Assert(diplomaticRelation != null);
		int abilityActivationTurn = diplomaticRelation.GetAbilityActivationTurn(DiplomaticAbilityDefinition.PrestigeTrend);
		if (abilityActivationTurn < 0)
		{
			return 0f;
		}
		float propertyValue = base.Empire.GetPropertyValue(SimulationProperties.PrestigeTrendBonusMaximumValue);
		float propertyValue2 = base.Empire.GetPropertyValue(SimulationProperties.PrestigeTrendBonusTrend);
		global::Game game = this.GameService.Game as global::Game;
		Diagnostics.Assert(game != null);
		int turn = game.Turn;
		float value = propertyValue2 * (float)(turn - abilityActivationTurn);
		return Mathf.Clamp(value, 0f, propertyValue);
	}

	public static readonly StaticString[] DiplomaticCostReductionFromEmpirePropertyNames;

	public static StaticString SymetricFailureTag = "SymetricFailure";

	public static StaticString InvalidPropositionMethodTag = "InvalidPropositionMethod";

	public static StaticString UnicityFailureTag = "UnicityFailure";

	public static StaticString CantTradeCityFailureTag = "CantTradeCityFailureTag";

	public static StaticString EmpireWhichProvidesMustBeEmpireWhichProposesTag = "EmpireWhichProvidesMustBeEmpireWhichProposesTag";

	private static List<global::Empire> thirdEmpires = new List<global::Empire>();

	private Dictionary<ulong, Action<DiplomaticContract>> diplomaticContractDelegateByTicket = new Dictionary<ulong, Action<DiplomaticContract>>();

	private List<DepartmentOfForeignAffairs.PreFillContractRequest> preFillContractRequests = new List<DepartmentOfForeignAffairs.PreFillContractRequest>();

	private DiplomaticRelation[] diplomaticRelationStateWithOtherMajorEmpires;

	private IDatabase<DepartmentOfForeignAffairs.ConstructibleElement> constructibleElementDatabase;

	private IDatabase<DiplomaticRelationState> diplomaticRelationStateDatabase;

	private IDatabase<DiplomaticAbilityDefinition> diplomaticAbilityStateDatabase;

	private IDatabase<DiplomaticRelationScoreModifierDefinition> diplomaticRelationScoreModifierDatabase;

	private IEventService eventService;

	private IWorldPositionningService worldPositionningService;

	private ReadOnlyCollection<DiplomaticRelation> diplomaticRelations;

	private global::Empire[] majorEmpires;

	private IVisibilityService visibilityService;

	private StaticString seasonEffectDiplomacy1 = "SeasonEffectDiplomacy1";

	public abstract class ConstructibleElement : global::ConstructibleElement
	{
		protected ConstructibleElement()
		{
			base.TooltipClass = DepartmentOfForeignAffairs.ConstructibleElement.ReadOnlyDefaultTooltipClass;
		}

		[XmlElement(Type = typeof(DiplomaticRelationStatePrerequisite), ElementName = "DiplomaticRelationStatePrerequisite")]
		[XmlElement(Type = typeof(DiplomaticContractContainsTermPrerequisite), ElementName = "DiplomaticContractContainsTermPrerequisite")]
		[XmlElement(Type = typeof(DiplomaticAbilityPrerequisite), ElementName = "DiplomaticAbilityPrerequisite")]
		[XmlElement(Type = typeof(DiplomaticMetaPrerequisite), ElementName = "DiplomaticMetaPrerequisite")]
		[XmlElement(Type = typeof(DiplomaticRelationStateEmpirePrerequisite), ElementName = "DiplomaticRelationStateEmpirePrerequisite")]
		public DiplomaticPrerequisite[] DiplomaticPrerequisites { get; private set; }

		public static readonly string ReadOnlyDefaultTooltipClass = "Constructible";
	}

	private class PreFillContractRequest
	{
		public PreFillContractRequest(global::Empire empireWhichReceives, StaticString wantedRelationState, Action<DiplomaticContract> requestCompleteDelegate)
		{
			this.EmpireWhichReceives = empireWhichReceives;
			this.DiplomaticContract = null;
			this.RequestCompleteDelegate = requestCompleteDelegate;
			this.WantedRelationState = wantedRelationState;
		}

		public DiplomaticContract DiplomaticContract { get; set; }

		public global::Empire EmpireWhichReceives { get; private set; }

		public Action<DiplomaticContract> RequestCompleteDelegate { get; private set; }

		public StaticString WantedRelationState { get; private set; }
	}
}
