using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class DiplomaticRelation : IXmlSerializable, IDumpable, IDiplomaticRelationManagment
{
	public DiplomaticRelation()
	{
		this.OwnerEmpireIndex = -1;
		this.OtherEmpireIndex = -1;
		this.TurnAtTheBeginningOfTheState = -1;
		this.State = null;
		this.score = new DiplomaticRelationScore(1f, -100f, 100f);
	}

	public DiplomaticRelation(int ownerEmpireIndex, int otherEmpireIndex, float turnDurationFactor) : this()
	{
		this.OwnerEmpireIndex = ownerEmpireIndex;
		this.OtherEmpireIndex = otherEmpireIndex;
		this.score = new DiplomaticRelationScore(turnDurationFactor, -100f, 100f);
	}

	void IDiplomaticRelationManagment.AddDiplomaticAbility(DiplomaticAbilityDefinition diplomaticAbilityDefinition)
	{
		if (diplomaticAbilityDefinition == null)
		{
			throw new ArgumentNullException("diplomaticAbilityDefinition");
		}
		if (this.HasAbility(diplomaticAbilityDefinition.Name))
		{
			return;
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		DiplomaticAbility diplomaticAbility = new DiplomaticAbility(diplomaticAbilityDefinition, game.Turn);
		Diagnostics.Assert(this.diplomaticAbilities != null);
		this.diplomaticAbilities.Add(diplomaticAbility);
		diplomaticAbility.IsActive = true;
	}

	int IDiplomaticRelationManagment.AddScoreModifier(DiplomaticRelationScoreModifierDefinition definition, float multiplier)
	{
		if (this.State != null && this.State.Name == DiplomaticRelationState.Names.War && definition.Category == DiplomaticRelationScoreModifier.Categories.Affinity && definition.Name != DiplomaticRelationScoreModifier.Names.StateWar)
		{
			return 0;
		}
		Diagnostics.Assert(this.score != null);
		return this.score.AddModifier(definition, multiplier, 1f, null);
	}

	void IDiplomaticRelationManagment.EndTurnUpdate()
	{
		Diagnostics.Assert(this.score != null);
		this.score.EndTurnUpdate();
		IEndTurnService service = Services.GetService<IEndTurnService>();
		this.RelationDuration = (float)(service.Turn - this.TurnAtTheBeginningOfTheState);
	}

	bool IDiplomaticRelationManagment.RemoveDiplomaticAbility(DiplomaticAbilityDefinition diplomaticAbilityDefinition)
	{
		if (diplomaticAbilityDefinition == null)
		{
			throw new ArgumentNullException("diplomaticAbilityDefinition");
		}
		Diagnostics.Assert(this.diplomaticAbilities != null);
		int num = 0;
		for (int i = 0; i < this.diplomaticAbilities.Count; i++)
		{
			if (this.diplomaticAbilities[i].Definition.Name == diplomaticAbilityDefinition.Name)
			{
				this.diplomaticAbilities[i].IsActive = false;
				this.diplomaticAbilities.RemoveAt(i);
				num++;
			}
		}
		if (num > 0)
		{
			Diagnostics.Log("Diplomatic relation between empire {0} and empire {1}: Remove ability {2}", new object[]
			{
				this.OwnerEmpireIndex,
				this.OtherEmpireIndex,
				diplomaticAbilityDefinition.Name
			});
			return true;
		}
		return false;
	}

	int IDiplomaticRelationManagment.RemoveModifiers(Predicate<DiplomaticRelationScoreModifier> match)
	{
		Diagnostics.Assert(this.score != null);
		return this.score.RemoveModifiers(match);
	}

	bool IDiplomaticRelationManagment.RemoveScoreModifier(int modifierId)
	{
		Diagnostics.Assert(this.score != null);
		return this.score.RemoveModifier(modifierId);
	}

	int IDiplomaticRelationManagment.RemoveScoreModifiersByType(StaticString modifierName)
	{
		Diagnostics.Assert(this.score != null);
		return this.score.RemoveModifiers((DiplomaticRelationScoreModifier modifier) => modifier.Definition.Name == modifierName);
	}

	void IDiplomaticRelationManagment.SetDiplomaticRelationState(DiplomaticRelationState diplomaticRelationState)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		this.State = diplomaticRelationState;
		this.TurnAtTheBeginningOfTheState = game.Turn;
	}

	int IDiplomaticRelationManagment.GetNumberOfScoreModifiersOfType(StaticString modifierName)
	{
		Diagnostics.Assert(this.score != null);
		return this.score.CountModifiers((DiplomaticRelationScoreModifier modifier) => modifier.Definition.Name == modifierName);
	}

	public virtual void DumpAsText(StringBuilder content, string indent = "")
	{
		content.AppendFormat("{0}Relation with Empire#{1} is '{2}' since {3:D3}\r\n", new object[]
		{
			indent,
			this.OtherEmpireIndex,
			(this.State == null) ? "NULL" : this.State.Name.ToString(),
			this.TurnAtTheBeginningOfTheState
		});
		if (this.DiplomaticRelationScore != null)
		{
			this.score.DumpAsText(content, indent + " ");
		}
	}

	public virtual byte[] DumpAsBytes()
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			binaryWriter.Write(this.OtherEmpireIndex);
			binaryWriter.Write((this.State == null) ? "NULL" : this.State.Name.ToString());
			binaryWriter.Write(this.TurnAtTheBeginningOfTheState);
		}
		byte[] result = memoryStream.ToArray();
		memoryStream.Close();
		return result;
	}

	public virtual void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		reader.ReadStartElement();
		this.OwnerEmpireIndex = reader.ReadElementString<int>("OwnerEmpireIndex");
		this.OtherEmpireIndex = reader.ReadElementString<int>("OtherEmpireIndex");
		string text = reader.ReadElementString<string>("State");
		if (string.IsNullOrEmpty(text))
		{
			this.State = null;
			Diagnostics.Assert(this.OwnerEmpireIndex == this.OtherEmpireIndex);
		}
		else
		{
			IDatabase<DiplomaticRelationState> database = Databases.GetDatabase<DiplomaticRelationState>(false);
			DiplomaticRelationState state;
			if (!database.TryGetValue(text, out state))
			{
				Diagnostics.LogError("Can't retrieve diplomatic relation state {0} from database.", new object[]
				{
					text
				});
			}
			this.State = state;
		}
		this.TurnAtTheBeginningOfTheState = reader.ReadElementString<int>("TurnAtTheBeginningOfTheState");
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("DiplomaticAbilities");
		if (num >= 3)
		{
			for (int i = 0; i < attribute; i++)
			{
				DiplomaticAbility diplomaticAbility = new DiplomaticAbility();
				reader.ReadElementSerializable<DiplomaticAbility>(ref diplomaticAbility);
				((IDiplomaticRelationManagment)this).AddDiplomaticAbility(diplomaticAbility);
			}
		}
		else
		{
			IDatabase<DiplomaticAbilityDefinition> database2 = Databases.GetDatabase<DiplomaticAbilityDefinition>(false);
			for (int j = 0; j < attribute; j++)
			{
				string text2 = reader.ReadElementString<string>("DiplomaticAbility");
				if (text2 == "PassOverArmies")
				{
					text2 = DiplomaticAbilityDefinition.PassThroughArmies;
				}
				if (text2 == "VisionAndMapExchange")
				{
					text2 = DiplomaticAbilityDefinition.VisionExchange;
				}
				DiplomaticAbilityDefinition diplomaticAbilityDefinition;
				if (!database2.TryGetValue(text2, out diplomaticAbilityDefinition))
				{
					Diagnostics.LogError("Can't retrieve the diplomatic ability {0}.", new object[]
					{
						text2
					});
				}
				((IDiplomaticRelationManagment)this).AddDiplomaticAbility(diplomaticAbilityDefinition);
			}
		}
		reader.ReadEndElement("DiplomaticAbilities");
		if (num >= 2)
		{
			this.score = new DiplomaticRelationScore(1f, -100f, 100f);
			reader.ReadElementSerializable<DiplomaticRelationScore>(ref this.score);
		}
		IEndTurnService service = Services.GetService<IEndTurnService>();
		this.RelationDuration = (float)(service.Turn - this.TurnAtTheBeginningOfTheState);
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(3);
		writer.WriteElementString<int>("OwnerEmpireIndex", this.OwnerEmpireIndex);
		writer.WriteElementString<int>("OtherEmpireIndex", this.OtherEmpireIndex);
		string value = string.Empty;
		if (this.State != null)
		{
			value = this.State.Name;
		}
		writer.WriteElementString<string>("State", value);
		writer.WriteElementString<int>("TurnAtTheBeginningOfTheState", this.TurnAtTheBeginningOfTheState);
		writer.WriteStartElement("DiplomaticAbilities");
		Diagnostics.Assert(this.diplomaticAbilities != null);
		writer.WriteAttributeString<int>("Count", this.diplomaticAbilities.Count);
		for (int i = 0; i < this.diplomaticAbilities.Count; i++)
		{
			Diagnostics.Assert(this.diplomaticAbilities[i] != null);
			IXmlSerializable xmlSerializable = this.diplomaticAbilities[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
		if (num >= 2)
		{
			IXmlSerializable xmlSerializable2 = this.score;
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable2);
		}
	}

	public ReadOnlyCollection<DiplomaticAbility> DiplomaticAbilities
	{
		get
		{
			return this.diplomaticAbilities.AsReadOnly();
		}
	}

	public int OtherEmpireIndex { get; private set; }

	public int OwnerEmpireIndex { get; private set; }

	public float AffinityScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.Affinity);
		}
	}

	public float RelationStateChaosScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.RelationStateChaos);
		}
	}

	public float DiscussionChaosScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.DiscussionChaos);
		}
	}

	public float BordersChaosScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.BordersChaos);
		}
	}

	public float VisionChaosScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.VisionChaos);
		}
	}

	public float CommercialChaosScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.CommercialChaos);
		}
	}

	public float ResearchChaosScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.ResearchChaos);
		}
	}

	public float MarketBanChaosScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.MarketBanChaos);
		}
	}

	public float BlackSpotChaosScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.BlackSpotChaos);
		}
	}

	public float ForceRelationStateChaosScore
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.GetScoreByCategory(DiplomaticRelationScoreModifier.Categories.ForceStatusChaos);
		}
	}

	public float RelationDuration { get; private set; }

	public float ScoreTrend
	{
		get
		{
			Diagnostics.Assert(this.score != null);
			return this.score.Trend;
		}
	}

	public DiplomaticRelationState State { get; private set; }

	public int TurnAtTheBeginningOfTheState { get; private set; }

	public IRelationScoreProvider DiplomaticRelationScore
	{
		get
		{
			return this.score;
		}
	}

	public int GetAbilityActivationTurn(StaticString abilityName)
	{
		if (StaticString.IsNullOrEmpty(abilityName))
		{
			throw new ArgumentNullException("abilityName");
		}
		Diagnostics.Assert(this.diplomaticAbilities != null);
		DiplomaticAbility diplomaticAbility = this.diplomaticAbilities.Find((DiplomaticAbility match) => match.Name == abilityName);
		if (diplomaticAbility == null)
		{
			return -1;
		}
		return diplomaticAbility.ActivationTurn;
	}

	public bool HasAbility(StaticString abilityName)
	{
		if (StaticString.IsNullOrEmpty(abilityName))
		{
			throw new ArgumentNullException("abilityName");
		}
		Diagnostics.Assert(this.diplomaticAbilities != null);
		for (int i = 0; i < this.diplomaticAbilities.Count; i++)
		{
			if (this.diplomaticAbilities[i].Name == abilityName)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasActiveAbility(StaticString abilityName)
	{
		if (StaticString.IsNullOrEmpty(abilityName))
		{
			throw new ArgumentNullException("abilityName");
		}
		Diagnostics.Assert(this.diplomaticAbilities != null);
		int count = this.diplomaticAbilities.Count;
		for (int i = 0; i < count; i++)
		{
			DiplomaticAbility diplomaticAbility = this.diplomaticAbilities[i];
			if (diplomaticAbility.Name == abilityName)
			{
				return diplomaticAbility.IsActive;
			}
		}
		return false;
	}

	public bool HasInactiveAbility(StaticString abilityName)
	{
		if (StaticString.IsNullOrEmpty(abilityName))
		{
			throw new ArgumentNullException("abilityName");
		}
		Diagnostics.Assert(this.diplomaticAbilities != null);
		int count = this.diplomaticAbilities.Count;
		for (int i = 0; i < count; i++)
		{
			DiplomaticAbility diplomaticAbility = this.diplomaticAbilities[i];
			if (diplomaticAbility.Name == abilityName)
			{
				return !diplomaticAbility.IsActive;
			}
		}
		return false;
	}

	public void AddDiplomaticAbility(DiplomaticAbility diplomaticAbility)
	{
		if (diplomaticAbility == null)
		{
			throw new ArgumentNullException("diplomaticAbility");
		}
		if (this.HasAbility(diplomaticAbility.Name))
		{
			return;
		}
		Diagnostics.Assert(this.diplomaticAbilities != null);
		this.diplomaticAbilities.Add(diplomaticAbility);
		diplomaticAbility.IsActive = true;
	}

	public void RefreshDiplomaticAbilities()
	{
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		int count = this.diplomaticAbilities.Count;
		for (int i = 0; i < count; i++)
		{
			DiplomaticAbilityDefinition definition = this.diplomaticAbilities[i].Definition;
			if (definition != null)
			{
				bool flag = definition.CheckDiplomaticAbilityPrerequisites(game.Empires[this.OwnerEmpireIndex], game.Empires[this.OtherEmpireIndex]) && definition.CheckPrerequisites(game.Empires[this.OwnerEmpireIndex]);
				if (flag != this.diplomaticAbilities[i].IsActive)
				{
					if (this.diplomaticAbilities[i].IsActive)
					{
						this.diplomaticAbilities[i].IsActive = false;
						this.UpdateEmpireDiplomaticAbilityDescriptor(definition, game.Empires[this.OwnerEmpireIndex], false);
					}
					else
					{
						this.diplomaticAbilities[i].IsActive = true;
						this.UpdateEmpireDiplomaticAbilityDescriptor(definition, game.Empires[this.OwnerEmpireIndex], true);
					}
				}
			}
		}
	}

	private bool CheckAbilityPrerequisites(DiplomaticAbility diplomaticAbility, params string[] flags)
	{
		if (diplomaticAbility == null || diplomaticAbility.Definition == null)
		{
			throw new ArgumentNullException();
		}
		if (diplomaticAbility.Definition.DiplomaticPrerequisites == null)
		{
			return true;
		}
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		return game == null || game.Empires == null || (diplomaticAbility.Definition.CheckDiplomaticAbilityPrerequisites(game.Empires[this.OwnerEmpireIndex], game.Empires[this.OtherEmpireIndex]) && diplomaticAbility.Definition.CheckPrerequisites(game.Empires[this.OwnerEmpireIndex]));
	}

	private void UpdateEmpireDiplomaticAbilityDescriptor(DiplomaticAbilityDefinition diplomaticAbilityDefinition, global::Empire empire, bool isAddOperation)
	{
		if (diplomaticAbilityDefinition == null || diplomaticAbilityDefinition.Descriptors == null)
		{
			return;
		}
		for (int i = 0; i < diplomaticAbilityDefinition.Descriptors.Length; i++)
		{
			SimulationDescriptor descriptor = diplomaticAbilityDefinition.Descriptors[i];
			if (isAddOperation)
			{
				empire.AddDescriptor(descriptor, false);
			}
			else
			{
				empire.RemoveDescriptor(descriptor);
			}
		}
		empire.Refresh(false);
	}

	private List<DiplomaticAbility> diplomaticAbilities = new List<DiplomaticAbility>();

	private DiplomaticRelationScore score;

	[CompilerGenerated]
	private sealed class GetNumberOfScoreModifiersOfType>c__AnonStorey97C
	{
		internal bool <>m__460(DiplomaticRelationScoreModifier modifier)
		{
			return modifier.Definition.Name == this.modifierName;
		}

		internal StaticString modifierName;
	}

	[CompilerGenerated]
	private sealed class RemoveScoreModifiersByType>c__AnonStorey97D
	{
		internal bool <>m__461(DiplomaticRelationScoreModifier modifier)
		{
			return modifier.Definition.Name == this.modifierName;
		}

		internal StaticString modifierName;
	}
}
