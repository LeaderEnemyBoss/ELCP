using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class QuestInstruction_UpdateStep : QuestInstruction, IXmlSerializable
{
	public QuestInstruction_UpdateStep()
	{
		this.nonPushableGameState.Add(typeof(GameServerState_Turn_End).AssemblyQualifiedName);
		this.nonPushableGameState.Add(typeof(GameServerState_Turn_Ended).AssemblyQualifiedName);
		this.nonPushableGameState.Add(typeof(GameServerState_Autosave).AssemblyQualifiedName);
		this.nonPushableGameState.Add(typeof(GameServerState_Transition).AssemblyQualifiedName);
		base.MustBeDelayed = true;
	}

	public QuestInstruction_UpdateStep(int stepNumber, QuestState state, bool hideRewards) : this()
	{
		this.StepNumber = stepNumber;
		this.State = state;
		this.HideRewards = hideRewards;
		base.MustBeDelayed = (state == QuestState.Completed);
	}

	public QuestState State { get; set; }

	public int StepNumber { get; set; }

	public bool HideRewards { get; set; }

	public override void Deserialize(BinaryReader reader)
	{
		this.StepNumber = reader.ReadInt32();
		this.State = (QuestState)reader.ReadInt32();
		this.HideRewards = reader.ReadBoolean();
	}

	public override bool Execute(Quest quest)
	{
		Diagnostics.Assert(quest != null);
		if (this.StepNumber >= 0 && this.StepNumber < quest.StepStates.Length)
		{
			quest.StepStates[this.StepNumber] = this.State;
			QuestState state = this.State;
			if (state != QuestState.InProgress)
			{
				if (state == QuestState.Completed)
				{
					if (quest.QuestRewards != null)
					{
						IGameService service = Services.GetService<IGameService>();
						if (service != null)
						{
							if (service.Game.Services.GetService<IGameEntityRepositoryService>() != null)
							{
								if (service.Game.Services.GetService<IPlayerControllerRepositoryService>() != null)
								{
									IQuestManagementService service2 = service.Game.Services.GetService<IQuestManagementService>();
									if (service2 != null)
									{
										List<QuestReward> list = new List<QuestReward>(quest.QuestRewards);
										list.RemoveAll((QuestReward reward) => reward.JustForShow);
										if (quest.QuestDefinition.GlobalWinner == GlobalQuestWinner.Participants)
										{
											List<QuestReward> list2 = (from reward in list
											where reward.StepNumber == this.StepNumber && reward.MinimumRank > 0
											select reward).ToList<QuestReward>();
											if (list2.Count > 0)
											{
												int num = 0;
												int rank = service2.GetGlobalQuestRank(quest, ref num, quest.QuestDefinition.Steps[this.StepNumber].Name) + 1;
												list2.RemoveAll((QuestReward reward) => reward.MinimumRank >= rank);
												for (int i = 0; i < list2.Count; i++)
												{
													list.Remove(list2[i]);
												}
											}
										}
										service2.AddStepRewards(quest.QuestDefinition, this.StepNumber, list, null, quest);
										Dictionary<StaticString, DroppableResource> dictionary = new Dictionary<StaticString, DroppableResource>();
										for (int j = list.Count - 1; j >= 0; j--)
										{
											QuestReward questReward = list[j];
											if (!questReward.Hidden && questReward.StepNumber == this.StepNumber && questReward.Droppables != null)
											{
												List<IDroppable> list3 = new List<IDroppable>(questReward.Droppables);
												for (int k = list3.Count - 1; k >= 0; k--)
												{
													DroppableResource droppableResource = list3[k] as DroppableResource;
													if (droppableResource != null)
													{
														if (dictionary.ContainsKey(droppableResource.ResourceName))
														{
															dictionary[droppableResource.ResourceName].Quantity += droppableResource.Quantity;
														}
														else
														{
															dictionary.Add(droppableResource.ResourceName, new DroppableResource(droppableResource.ResourceName, droppableResource.Quantity));
														}
														list3.RemoveAt(k);
														if (list3.Count == 0 && string.IsNullOrEmpty(questReward.LocalizationKey))
														{
															list.RemoveAt(j);
															break;
														}
													}
												}
												questReward.Droppables = list3.ToArray();
											}
										}
										list.Add(new QuestReward(this.StepNumber, false, (from kvp in dictionary
										select kvp.Value).ToArray<DroppableResource>(), string.Empty, false, -1));
										quest.QuestRewards = list.ToArray();
										IEnumerable<QuestReward> enumerable = from reward in quest.QuestRewards
										where reward.StepNumber == this.StepNumber
										select reward;
										foreach (QuestReward questReward2 in enumerable)
										{
											if (questReward2.Droppables == null)
											{
												Diagnostics.LogWarning("Quest reward has no droppables.");
											}
											else
											{
												for (int l = 0; l < questReward2.Droppables.Length; l++)
												{
													if (questReward2.Droppables[l] != null)
													{
														IDroppableWithRewardAllocation droppableWithRewardAllocation = questReward2.Droppables[l] as IDroppableWithRewardAllocation;
														if (droppableWithRewardAllocation != null)
														{
															for (int m = 0; m < 32; m++)
															{
																if ((quest.EmpireBits & 1 << m) != 0)
																{
																	try
																	{
																		global::Empire empire = (service.Game as global::Game).Empires[m];
																		droppableWithRewardAllocation.AllocateRewardTo(empire, new object[0]);
																	}
																	catch
																	{
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			else if (quest.QuestRewards != null)
			{
				IGameService service3 = Services.GetService<IGameService>();
				if (service3 != null)
				{
					IQuestManagementService service4 = service3.Game.Services.GetService<IQuestManagementService>();
					if (service4 != null)
					{
						global::Empire empire2 = null;
						for (int n = 0; n < 32; n++)
						{
							if ((quest.EmpireBits & 1 << n) != 0)
							{
								try
								{
									empire2 = (service3.Game as global::Game).Empires[n];
									break;
								}
								catch
								{
								}
							}
						}
						if (empire2 != null)
						{
							bool flag = false;
							List<QuestReward> list4 = new List<QuestReward>(quest.QuestRewards);
							IEnumerable<QuestReward> enumerable2 = from reward in quest.QuestRewards
							where reward.StepNumber == this.StepNumber
							select reward;
							foreach (QuestReward questReward3 in enumerable2)
							{
								questReward3.Hidden = this.HideRewards;
								if (questReward3.Droppables != null)
								{
									foreach (IDroppable droppable in questReward3.Droppables)
									{
										if (droppable != null && droppable is DroppableReferenceTechnology)
										{
											DroppableReferenceTechnology droppableReferenceTechnology = droppable as DroppableReferenceTechnology;
											TechnologyDefinition technologyDefinition = droppableReferenceTechnology.ConstructibleElement as TechnologyDefinition;
											if (technologyDefinition != null && empire2.GetAgency<DepartmentOfScience>().GetTechnologyState(technologyDefinition.Name) == DepartmentOfScience.ConstructibleElement.State.Researched)
											{
												list4.Remove(questReward3);
												flag = true;
											}
										}
									}
								}
							}
							if (flag)
							{
								List<QuestReward> list5 = service4.ComputeRewards(quest, quest.QuestDefinition, this.StepNumber, null);
								foreach (QuestReward item in list5)
								{
									if (!list4.Contains(item))
									{
										list4.Add(item);
									}
								}
								quest.QuestRewards = list4.ToArray();
							}
						}
					}
				}
			}
			return true;
		}
		return false;
	}

	public override void Serialize(BinaryWriter writer)
	{
		writer.Write(this.StepNumber);
		writer.Write((int)this.State);
		writer.Write(this.HideRewards);
	}

	public void ReadXml(XmlReader reader)
	{
		this.StepNumber = reader.GetAttribute<int>("StepNumber");
		this.State = (QuestState)reader.GetAttribute<int>("State");
		this.HideRewards = reader.GetAttribute<bool>("HideRewards");
		reader.ReadStartElement("UpdateStep");
		reader.ReadEndElement("UpdateStep");
	}

	public void WriteXml(XmlWriter writer)
	{
		writer.WriteStartElement("UpdateStep");
		writer.WriteAttributeString<int>("StepNumber", this.StepNumber);
		writer.WriteAttributeString<int>("State", (int)this.State);
		writer.WriteAttributeString<bool>("HideRewards", this.HideRewards);
		writer.WriteEndElement();
	}
}
