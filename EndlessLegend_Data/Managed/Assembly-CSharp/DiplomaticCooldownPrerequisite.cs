using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class DiplomaticCooldownPrerequisite : DiplomaticPrerequisite
{
	[XmlIgnore]
	public string[] DiplomaticTermNames { get; set; }

	[XmlText]
	public string XmlSerializableDiplomaticTermName
	{
		get
		{
			if (this.DiplomaticTermNames == null || this.DiplomaticTermNames.Length == 0)
			{
				return string.Empty;
			}
			if (this.DiplomaticTermNames.Length == 1)
			{
				return this.DiplomaticTermNames[0];
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < this.DiplomaticTermNames.Length; i++)
			{
				stringBuilder.Append(this.DiplomaticTermNames[i]);
				if (i < this.DiplomaticTermNames.Length - 1)
				{
					stringBuilder.Append(Amplitude.String.Separators[0]);
				}
			}
			return stringBuilder.ToString();
		}
		set
		{
			this.DiplomaticTermNames = ((!string.IsNullOrEmpty(value)) ? value.Split(Amplitude.String.Separators) : new string[0]);
		}
	}

	[XmlAttribute]
	public bool OnlyWhenProposer { get; private set; }

	[XmlAttribute]
	public int Cooldown { get; private set; }

	public override bool Check(IDiplomaticContract diplomaticContract, global::Empire empireWhichProvides, global::Empire empireWhichReceives)
	{
		global::Game game = Services.GetService<IGameService>().Game as global::Game;
		IDiplomaticContractRepositoryService service = game.Services.GetService<IDiplomaticContractRepositoryService>();
		Predicate<DiplomaticContract> match = (DiplomaticContract contract) => (contract.EmpireWhichProposes == empireWhichProvides && contract.EmpireWhichReceives == empireWhichReceives) || (contract.EmpireWhichProposes == empireWhichReceives && contract.EmpireWhichReceives == empireWhichProvides);
		int num = -100;
		bool flag = false;
		foreach (DiplomaticContract diplomaticContract2 in service.FindAll(match))
		{
			if (diplomaticContract2.State == DiplomaticContractState.Signed)
			{
				string[] diplomaticTermNames = this.DiplomaticTermNames;
				for (int i = 0; i < diplomaticTermNames.Length; i++)
				{
					string termname = diplomaticTermNames[i];
					if (termname.Contains("ToWar"))
					{
						flag = true;
					}
					if (!this.OnlyWhenProposer)
					{
						if (diplomaticContract2.Terms.Any((DiplomaticTerm term) => term.Definition.Name.ToString() == termname))
						{
							num = Mathf.Max(num, diplomaticContract2.TurnAtTheBeginningOfTheState);
						}
					}
					else if (diplomaticContract2.Terms.Any((DiplomaticTerm term) => term.Definition.Name.ToString() == termname && term.EmpireWhichProvides == empireWhichProvides))
					{
						num = Mathf.Max(num, diplomaticContract2.TurnAtTheBeginningOfTheState);
					}
				}
			}
		}
		if (flag)
		{
			match = ((DiplomaticContract contract) => contract.EmpireWhichProposes == empireWhichProvides || contract.EmpireWhichReceives == empireWhichProvides);
			Func<DiplomaticTerm, bool> <>9__4;
			foreach (DiplomaticContract diplomaticContract3 in service.FindAll(match))
			{
				if (diplomaticContract3.State == DiplomaticContractState.Signed)
				{
					IEnumerable<DiplomaticTerm> terms = diplomaticContract3.Terms;
					Func<DiplomaticTerm, bool> predicate;
					if ((predicate = <>9__4) == null)
					{
						predicate = (<>9__4 = ((DiplomaticTerm term) => term is DiplomaticTermProposal && term.EmpireWhichProvides == empireWhichProvides && (term as DiplomaticTermProposal).ChosenEmpire == empireWhichReceives && term.Definition.Name == "AskToDeclareWar"));
					}
					if (terms.Any(predicate))
					{
						num = Mathf.Max(num, diplomaticContract3.TurnAtTheBeginningOfTheState);
					}
				}
			}
		}
		return base.Inverted ^ game.Turn - num >= this.Cooldown;
	}
}
