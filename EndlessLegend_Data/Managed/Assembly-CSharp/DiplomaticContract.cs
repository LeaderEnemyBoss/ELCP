using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class DiplomaticContract : IDisposable, IXmlSerializable, IDiplomaticContract, IDiplomaticContractManagement, IGameEntity
{
	public DiplomaticContract()
	{
		this.GUID = GameEntityGUID.Zero;
		this.EmpireWhichInitiated = null;
		this.EmpireWhichProposes = null;
		this.EmpireWhichReceives = null;
		this.terms = new List<DiplomaticTerm>();
		this.Terms = this.terms.AsReadOnly();
		this.TurnAtTheBeginningOfTheState = -1;
	}

	public DiplomaticContract(GameEntityGUID contractGUID, global::Empire empireWhichInitiated, global::Empire empireWhichReceives) : this()
	{
		this.GUID = contractGUID;
		this.State = DiplomaticContractState.Negotiation;
		this.EmpireWhichInitiated = empireWhichInitiated;
		this.EmpireWhichProposes = empireWhichInitiated;
		this.EmpireWhichReceives = empireWhichReceives;
	}

	void IDiplomaticContractManagement.ApplyDiplomaticTermChange(DiplomaticTermChange diplomaticTermChange)
	{
		if (diplomaticTermChange == null)
		{
			throw new ArgumentNullException("diplomaticTermChange");
		}
		Diagnostics.Assert(this.terms != null);
		switch (diplomaticTermChange.Action)
		{
		case CollectionChangeAction.Add:
			Diagnostics.Assert(diplomaticTermChange.Term != null);
			diplomaticTermChange.Term.Index = this.diplomaticTermNextIndex;
			this.terms.Add(diplomaticTermChange.Term);
			this.diplomaticTermNextIndex++;
			break;
		case CollectionChangeAction.Remove:
			Diagnostics.Assert(diplomaticTermChange.Index >= 0);
			this.terms.RemoveAll((DiplomaticTerm term) => term.Index == diplomaticTermChange.Index);
			break;
		case CollectionChangeAction.Refresh:
		{
			Diagnostics.Assert(diplomaticTermChange.Term != null);
			Diagnostics.Assert(diplomaticTermChange.Index >= 0);
			int num = this.terms.FindIndex(0, (DiplomaticTerm term) => term.Index == diplomaticTermChange.Index);
			if (num < 0)
			{
				Diagnostics.LogError("Can't find index for term {0}.", new object[]
				{
					diplomaticTermChange.Index
				});
			}
			diplomaticTermChange.Term.Index = diplomaticTermChange.Index;
			this.terms[num] = diplomaticTermChange.Term;
			break;
		}
		}
		bool flag = true;
		while (flag)
		{
			flag = false;
			for (int i = 0; i < this.terms.Count; i++)
			{
				DiplomaticTerm diplomaticTerm = this.terms[i];
				if (!diplomaticTerm.CanApply(this, new string[0]))
				{
					this.terms.RemoveAt(i);
					flag = true;
					break;
				}
			}
		}
		this.OnDiplomaticContractChange();
	}

	void IDiplomaticContractManagement.SetDiplomaticState(DiplomaticContractState destinationState)
	{
		DiplomaticContractState state = this.State;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		this.TurnAtTheBeginningOfTheState = game.Turn;
		if (destinationState == DiplomaticContractState.Proposed && this.EmpireWhichProposes.IsControlledByAI && !this.EmpireWhichReceives.IsControlledByAI)
		{
			for (int i = this.terms.Count - 1; i >= 0; i--)
			{
				DiplomaticTermResourceExchange diplomaticTermResourceExchange = this.terms[i] as DiplomaticTermResourceExchange;
				if (diplomaticTermResourceExchange != null && diplomaticTermResourceExchange.EmpireWhichProvides.Index == this.EmpireWhichProposes.Index)
				{
					DepartmentOfTheTreasury agency = diplomaticTermResourceExchange.EmpireWhichProvides.GetAgency<DepartmentOfTheTreasury>();
					if (agency.TryTransferResources(diplomaticTermResourceExchange.EmpireWhichProvides, diplomaticTermResourceExchange.ResourceName, -diplomaticTermResourceExchange.Amount))
					{
						float num;
						agency.TryGetResourceStockValue(diplomaticTermResourceExchange.EmpireWhichProvides, diplomaticTermResourceExchange.ResourceName, out num, false);
						Diagnostics.Log("ELCP {0} with {1} Buffering Resource {2} {3}, providerstock2: {4}", new object[]
						{
							diplomaticTermResourceExchange.EmpireWhichProvides,
							diplomaticTermResourceExchange.EmpireWhichReceives,
							diplomaticTermResourceExchange.ResourceName,
							diplomaticTermResourceExchange.Amount,
							num
						});
						diplomaticTermResourceExchange.BufferedAmount = diplomaticTermResourceExchange.Amount;
					}
				}
			}
		}
		if (destinationState == DiplomaticContractState.Negotiation || destinationState == DiplomaticContractState.Refused || destinationState == DiplomaticContractState.Ignored)
		{
			for (int j = this.terms.Count - 1; j >= 0; j--)
			{
				DiplomaticTermResourceExchange diplomaticTermResourceExchange2 = this.terms[j] as DiplomaticTermResourceExchange;
				if (diplomaticTermResourceExchange2 != null && diplomaticTermResourceExchange2.BufferedAmount > 0f)
				{
					DepartmentOfTheTreasury agency2 = diplomaticTermResourceExchange2.EmpireWhichProvides.GetAgency<DepartmentOfTheTreasury>();
					agency2.TryTransferResources(diplomaticTermResourceExchange2.EmpireWhichProvides, diplomaticTermResourceExchange2.ResourceName, diplomaticTermResourceExchange2.BufferedAmount);
					float num2;
					agency2.TryGetResourceStockValue(diplomaticTermResourceExchange2.EmpireWhichProvides, diplomaticTermResourceExchange2.ResourceName, out num2, false);
					Diagnostics.Log("ELCP {0} with {1} UnBuffering Resource {2} {3} {4}, providerstock2: {5}", new object[]
					{
						diplomaticTermResourceExchange2.EmpireWhichProvides,
						diplomaticTermResourceExchange2.EmpireWhichReceives,
						diplomaticTermResourceExchange2.ResourceName,
						diplomaticTermResourceExchange2.BufferedAmount,
						diplomaticTermResourceExchange2.Amount,
						num2
					});
					diplomaticTermResourceExchange2.BufferedAmount = 0f;
				}
			}
		}
		if (state == DiplomaticContractState.Proposed && destinationState == DiplomaticContractState.Negotiation)
		{
			global::Empire empireWhichProposes = this.EmpireWhichProposes;
			this.EmpireWhichProposes = this.EmpireWhichReceives;
			this.EmpireWhichReceives = empireWhichProposes;
			float empireWhichProposesEmpirePointInvestment = this.EmpireWhichProposesEmpirePointInvestment;
			this.EmpireWhichProposesEmpirePointInvestment = this.EmpireWhichReceivesEmpirePointInvestment;
			this.EmpireWhichReceivesEmpirePointInvestment = empireWhichProposesEmpirePointInvestment;
			for (int k = this.terms.Count - 1; k >= 0; k--)
			{
				DiplomaticTerm diplomaticTerm = this.terms[k];
				Diagnostics.Assert(diplomaticTerm != null);
				if (!diplomaticTerm.CanApply(this, new string[0]))
				{
					this.terms.RemoveAt(k);
				}
			}
			int contractRevisionNumber = this.ContractRevisionNumber;
			this.ContractRevisionNumber = contractRevisionNumber + 1;
		}
		if (destinationState == DiplomaticContractState.Signed)
		{
			this.ApplyTerms();
		}
		Diagnostics.Log("Contract {0} pass from state {1} to state {2}.", new object[]
		{
			this.GUID,
			this.State,
			destinationState
		});
		this.State = destinationState;
		this.OnDiplomaticContractChange();
	}

	private void OnDiplomaticContractChange()
	{
	}

	private void ApplyTerms()
	{
		for (int i = 0; i < this.terms.Count; i++)
		{
			Diagnostics.Assert(this.terms[i].CanApply(this, new string[0]));
			IDiplomaticTermWithSignaturePostprocessorEffect diplomaticTermWithSignaturePostprocessorEffect = this.terms[i] as IDiplomaticTermWithSignaturePostprocessorEffect;
			if (diplomaticTermWithSignaturePostprocessorEffect != null)
			{
				diplomaticTermWithSignaturePostprocessorEffect.ApplySignaturePostprocessorEffects();
			}
		}
		for (int j = 0; j < this.terms.Count; j++)
		{
			DiplomaticTerm diplomaticTerm = this.terms[j];
			Diagnostics.Assert(diplomaticTerm.CanApply(this, new string[0]));
			IDiplomaticTermManagement diplomaticTermManagement = diplomaticTerm;
			Diagnostics.Assert(diplomaticTermManagement != null);
			diplomaticTermManagement.ApplyEffects();
		}
	}

	public void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		this.GUID = reader.GetAttribute<ulong>("GUID");
		reader.ReadStartElement();
		this.ContractRevisionNumber = reader.ReadElementString<int>("ContractRevisionNumber");
		string value = reader.ReadElementString<string>("DiplomaticContractState");
		Diagnostics.Assert(!string.IsNullOrEmpty(value));
		this.State = (DiplomaticContractState)((int)Enum.Parse(typeof(DiplomaticContractState), value));
		this.diplomaticTermNextIndex = reader.ReadElementString<int>("DiplomaticTermNextIndex");
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		global::Empire[] empires = game.Empires;
		Diagnostics.Assert(empires != null);
		int num2 = reader.ReadElementString<int>("EmpireWhichInitiatedIndex");
		int num3 = reader.ReadElementString<int>("EmpireWhichProposesIndex");
		int num4 = reader.ReadElementString<int>("EmpireWhichReceivesIndex");
		this.EmpireWhichInitiated = empires[num2];
		this.EmpireWhichProposes = empires[num3];
		this.EmpireWhichReceives = empires[num4];
		Diagnostics.Assert(this.EmpireWhichInitiated != null && this.EmpireWhichProposes != null && this.EmpireWhichReceives != null);
		if (num >= 2)
		{
			this.EmpireWhichProposesEmpirePointInvestment = reader.ReadElementString<float>("EmpireWhichProposesEmpirePointInvestment");
			this.EmpireWhichReceivesEmpirePointInvestment = reader.ReadElementString<float>("EmpireWhichReceivesEmpirePointInvestment");
		}
		if (num >= 3)
		{
			this.TurnAtTheBeginningOfTheState = reader.ReadElementString<int>("TurnAtTheBeginningOfTheState");
		}
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Terms");
		for (int i = 0; i < attribute; i++)
		{
			string attribute2 = reader.GetAttribute<string>("AssemblyQualifiedName");
			if (string.IsNullOrEmpty(attribute2))
			{
				Diagnostics.LogError("Can't retrieve assembly qualified type name.");
			}
			else
			{
				Type type = Type.GetType(attribute2);
				if (type == null)
				{
					Diagnostics.LogError("Can't retrieve type {0}.", new object[]
					{
						attribute2
					});
				}
				else
				{
					DiplomaticTerm diplomaticTerm = (DiplomaticTerm)FormatterServices.GetUninitializedObject(type);
					if (diplomaticTerm == null)
					{
						Diagnostics.LogError("Can't instantiate type {0}.", new object[]
						{
							attribute2
						});
					}
					else
					{
						diplomaticTerm.ReadXml(reader);
						Diagnostics.Assert(this.terms != null);
						this.terms.Add(diplomaticTerm);
						reader.ReadEndElement("DiplomaticTerm");
					}
				}
			}
		}
		reader.ReadEndElement("Terms");
	}

	public void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(3);
		writer.WriteAttributeString<ulong>("GUID", this.GUID);
		writer.WriteElementString<int>("ContractRevisionNumber", this.ContractRevisionNumber);
		writer.WriteElementString<string>("DiplomaticContractState", this.State.ToString());
		writer.WriteElementString<int>("DiplomaticTermNextIndex", this.diplomaticTermNextIndex);
		Diagnostics.Assert(this.EmpireWhichInitiated != null && this.EmpireWhichProposes != null && this.EmpireWhichReceives != null);
		writer.WriteElementString<int>("EmpireWhichInitiatedIndex", this.EmpireWhichInitiated.Index);
		writer.WriteElementString<int>("EmpireWhichProposesIndex", this.EmpireWhichProposes.Index);
		writer.WriteElementString<int>("EmpireWhichReceivesIndex", this.EmpireWhichReceives.Index);
		if (num >= 2)
		{
			writer.WriteElementString<float>("EmpireWhichProposesEmpirePointInvestment", this.EmpireWhichProposesEmpirePointInvestment);
			writer.WriteElementString<float>("EmpireWhichReceivesEmpirePointInvestment", this.EmpireWhichReceivesEmpirePointInvestment);
		}
		if (num >= 3)
		{
			writer.WriteElementString<int>("TurnAtTheBeginningOfTheState", this.TurnAtTheBeginningOfTheState);
		}
		writer.WriteStartElement("Terms");
		Diagnostics.Assert(this.terms != null);
		writer.WriteAttributeString<int>("Count", this.terms.Count);
		for (int i = 0; i < this.terms.Count; i++)
		{
			DiplomaticTerm diplomaticTerm = this.terms[i];
			Diagnostics.Assert(diplomaticTerm != null);
			writer.WriteStartElement("DiplomaticTerm");
			writer.WriteAttributeString<string>("AssemblyQualifiedName", diplomaticTerm.GetType().AssemblyQualifiedName);
			diplomaticTerm.WriteXml(writer);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	public int ContractRevisionNumber { get; private set; }

	public global::Empire EmpireWhichInitiated { get; private set; }

	public global::Empire EmpireWhichProposes { get; private set; }

	public float EmpireWhichProposesEmpirePointInvestment { get; set; }

	public float EmpireWhichProposesPeacePointGain { get; set; }

	public global::Empire EmpireWhichReceives { get; private set; }

	public float EmpireWhichReceivesEmpirePointInvestment { get; set; }

	public float EmpireWhichReceivesPeacePointGain { get; set; }

	public GameEntityGUID GUID { get; private set; }

	public DiplomaticContractState State { get; private set; }

	public ReadOnlyCollection<DiplomaticTerm> Terms { get; private set; }

	public int TurnAtTheBeginningOfTheState { get; private set; }

	public bool CheckContractDataValidity()
	{
		if (this.EmpireWhichInitiated == null || this.EmpireWhichProposes == null || this.EmpireWhichReceives == null)
		{
			return false;
		}
		Diagnostics.Assert(this.terms != null);
		for (int i = 0; i < this.terms.Count; i++)
		{
			DiplomaticTerm diplomaticTerm = this.terms[i];
			if (diplomaticTerm == null || !diplomaticTerm.CheckTermDataValidity())
			{
				return false;
			}
		}
		return true;
	}

	public void Dispose()
	{
		this.EmpireWhichInitiated = null;
		this.EmpireWhichProposes = null;
		this.EmpireWhichReceives = null;
	}

	public DiplomaticTerm.PropositionMethod GetPropositionMethod()
	{
		Diagnostics.Assert(this.terms != null);
		if (this.terms.Count == 0)
		{
			return DiplomaticTerm.PropositionMethod.Negotiation;
		}
		bool flag = true;
		for (int i = 0; i < this.terms.Count; i++)
		{
			DiplomaticTerm diplomaticTerm = this.terms[i];
			Diagnostics.Assert(diplomaticTerm != null && diplomaticTerm.Definition != null);
			flag &= (diplomaticTerm.Definition.PropositionMethod == DiplomaticTerm.PropositionMethod.Declaration);
		}
		return (!flag) ? DiplomaticTerm.PropositionMethod.Negotiation : DiplomaticTerm.PropositionMethod.Declaration;
	}

	public bool IsTransitionPossible(DiplomaticContractState destinationState)
	{
		bool result = false;
		switch (this.State)
		{
		case DiplomaticContractState.Negotiation:
		{
			DiplomaticTerm.PropositionMethod propositionMethod = this.GetPropositionMethod();
			if (destinationState == DiplomaticContractState.Proposed && propositionMethod == DiplomaticTerm.PropositionMethod.Negotiation)
			{
				result = this.IsValid();
			}
			if (destinationState == DiplomaticContractState.Signed && propositionMethod == DiplomaticTerm.PropositionMethod.Declaration)
			{
				result = this.IsValid();
			}
			if ((destinationState == DiplomaticContractState.Refused || destinationState == DiplomaticContractState.Ignored) && propositionMethod == DiplomaticTerm.PropositionMethod.Negotiation)
			{
				result = true;
			}
			break;
		}
		case DiplomaticContractState.Proposed:
			if (destinationState == DiplomaticContractState.Signed)
			{
				result = this.IsValid();
			}
			if (destinationState == DiplomaticContractState.Refused)
			{
				result = true;
			}
			if (destinationState == DiplomaticContractState.Negotiation)
			{
				result = true;
			}
			if (destinationState == DiplomaticContractState.Ignored)
			{
				for (int i = 0; i < this.terms.Count; i++)
				{
					DiplomaticTerm diplomaticTerm = this.terms[i];
					Diagnostics.Assert(diplomaticTerm != null);
					if (diplomaticTerm.EmpireWhichProvides.Index == this.EmpireWhichProposes.Index && !diplomaticTerm.CanApply(this, new string[0]))
					{
						result = true;
					}
				}
			}
			break;
		}
		return result;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < this.terms.Count; i++)
		{
			DiplomaticTerm value = this.terms[i];
			stringBuilder.Append("    ");
			stringBuilder.Append(value);
			if (i < this.terms.Count - 1)
			{
				stringBuilder.Append("\n");
			}
		}
		return string.Format("DiplomaticContract {4} EmpireWhichInitiated: {0} EmpireWhichProposes: {1} EmpireWhichReceives: {2} State: {3} Revision: {5}\n{6}", new object[]
		{
			this.EmpireWhichInitiated,
			this.EmpireWhichProposes,
			this.EmpireWhichReceives,
			this.State,
			this.GUID,
			this.ContractRevisionNumber,
			stringBuilder
		});
	}

	private bool IsValid()
	{
		for (int i = 0; i < this.terms.Count; i++)
		{
			DiplomaticTerm diplomaticTerm = this.terms[i];
			Diagnostics.Assert(diplomaticTerm != null);
			if (!diplomaticTerm.CanApply(this, new string[0]))
			{
				return false;
			}
		}
		return true;
	}

	private int diplomaticTermNextIndex;

	private List<DiplomaticTerm> terms;

	[CompilerGenerated]
	private sealed class ApplyDiplomaticTermChange>c__AnonStorey87B
	{
		internal bool <>m__283(DiplomaticTerm term)
		{
			return term.Index == this.diplomaticTermChange.Index;
		}

		internal bool <>m__284(DiplomaticTerm term)
		{
			return term.Index == this.diplomaticTermChange.Index;
		}

		internal DiplomaticTermChange diplomaticTermChange;
	}
}
