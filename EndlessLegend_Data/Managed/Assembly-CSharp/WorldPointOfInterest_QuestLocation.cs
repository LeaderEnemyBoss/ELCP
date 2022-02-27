using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;

public class WorldPointOfInterest_QuestLocation : WorldPointOfInterest
{
	public override IEnumerator Ignite(IGameEntity gameEntity, Amplitude.Unity.Framework.IServiceProvider serviceProvider)
	{
		yield return base.Ignite(gameEntity, serviceProvider);
		string text = base.PointOfInterest.Type;
		if (text != null)
		{
			if (WorldPointOfInterest_QuestLocation.<>f__switch$map24 == null)
			{
				WorldPointOfInterest_QuestLocation.<>f__switch$map24 = new Dictionary<string, int>(2)
				{
					{
						"NavalQuestLocation",
						0
					},
					{
						"QuestLocation",
						0
					}
				};
			}
			int num;
			if (WorldPointOfInterest_QuestLocation.<>f__switch$map24.TryGetValue(text, out num))
			{
				if (num == 0)
				{
					int bits = base.PointOfInterest.Interaction.Bits & base.PlayerControllerRepositoryService.ActivePlayerController.Empire.Bits;
					if (bits == 0)
					{
						base.PointOfInterest.Interaction.PointOfInterestInteractionChange += this.Interaction_PointOfInterestInteractionChange;
						this.interactionBitsBound = true;
					}
					if (bits == base.PlayerControllerRepositoryService.ActivePlayerController.Empire.Bits)
					{
						DepartmentOfScience departmentOfScience = base.PlayerControllerRepositoryService.ActivePlayerController.Empire.GetAgency<DepartmentOfScience>();
						Diagnostics.Assert(departmentOfScience != null);
						if (!StaticString.IsNullOrEmpty(departmentOfScience.ArcheologyTechnologyDefinitionName))
						{
							DepartmentOfScience.ConstructibleElement.State state = departmentOfScience.GetTechnologyState(departmentOfScience.ArcheologyTechnologyDefinitionName);
							if (state != DepartmentOfScience.ConstructibleElement.State.Researched)
							{
								base.PointOfInterest.Interaction.PointOfInterestInteractionChange += this.Interaction_PointOfInterestInteractionChange;
								this.interactionBitsBound = true;
							}
						}
					}
					StaticString questLocationType = base.PointOfInterest.SimulationObject.GetDescriptorNameFromType("QuestLocationType");
					if (questLocationType == "QuestLocationTypeTemple")
					{
						this.sensibleToEndlessDay = true;
						DownloadableContent8.EndlessDay.ActivationChange += this.EndlessDay_ActivationChange;
						SeasonManager.DustDepositsToggle += this.DustDepositsToggle;
					}
					if (base.PointOfInterest is IWorldEntityMappingOverride)
					{
						((IWorldEntityMappingOverride)base.PointOfInterest).TryResolve(out this.interpreterContext);
					}
					if (this.interpreterContext == null)
					{
						this.interpreterContext = new InterpreterContext(base.PointOfInterest);
					}
					this.RefreshVFXEffects();
				}
			}
		}
		yield break;
	}

	public override void Release()
	{
		if (this.sensibleToEndlessDay)
		{
			DownloadableContent8.EndlessDay.ActivationChange -= this.EndlessDay_ActivationChange;
			this.sensibleToEndlessDay = false;
		}
		SeasonManager.DustDepositsToggle -= this.EndlessDay_ActivationChange;
		if (base.PointOfInterest != null && base.PointOfInterest.UntappedDustDeposits)
		{
			base.PointOfInterest.UntappedDustDeposits = false;
		}
		if (this.interactionBitsBound)
		{
			if (base.PointOfInterest != null)
			{
				base.PointOfInterest.Interaction.PointOfInterestInteractionChange -= this.Interaction_PointOfInterestInteractionChange;
			}
			this.interactionBitsBound = false;
		}
		base.Release();
	}

	protected override void CreatePointOfInterestIcon()
	{
		if (this.interpreterContext == null)
		{
			return;
		}
		base.CreatePointOfInterestIcon();
	}

	protected override void CreateOrRegeneratePointOfInterestFXTypeIFN()
	{
		if (this.interpreterContext == null)
		{
			return;
		}
		base.CreateOrRegeneratePointOfInterestFXTypeIFN();
	}

	protected override void OnDestroy()
	{
		if (this.sensibleToEndlessDay)
		{
			DownloadableContent8.EndlessDay.ActivationChange -= this.EndlessDay_ActivationChange;
			this.sensibleToEndlessDay = false;
		}
		SeasonManager.DustDepositsToggle -= this.EndlessDay_ActivationChange;
		if (base.PointOfInterest != null && base.PointOfInterest.UntappedDustDeposits)
		{
			base.PointOfInterest.UntappedDustDeposits = false;
		}
		if (this.interactionBitsBound)
		{
			if (base.PointOfInterest != null)
			{
				base.PointOfInterest.Interaction.PointOfInterestInteractionChange -= this.Interaction_PointOfInterestInteractionChange;
			}
			this.interactionBitsBound = false;
		}
		base.OnDestroy();
	}

	protected override void PlayerControllerRepositoryService_ActivePlayerControllerChange(object sender, ActivePlayerControllerChangeEventArgs e)
	{
		base.PlayerControllerRepositoryService_ActivePlayerControllerChange(sender, e);
		this.RefreshVFXEffects();
	}

	private void EndlessDay_ActivationChange(bool isActive)
	{
		this.RefreshVFXEffects();
	}

	private void DustDepositsToggle(bool isActive)
	{
		this.RefreshVFXEffects();
		if (isActive && !this.interactionBitsBound)
		{
			base.PointOfInterest.Interaction.PointOfInterestInteractionChange += this.Interaction_PointOfInterestInteractionChange;
			this.interactionBitsBound = true;
		}
	}

	private void Interaction_PointOfInterestInteractionChange(object sender, PointOfInterestInteractionChangeEventArgs e)
	{
		this.RefreshVFXEffects();
	}

	private void RefreshVFXEffects()
	{
		bool endlessDay = false;
		bool dustDeposits = false;
		if (this.sensibleToEndlessDay)
		{
			endlessDay = SimulationGlobal.GlobalTagsContains(DownloadableContent8.EndlessDay.ReadOnlyTag);
		}
		if (base.PointOfInterest.UntappedDustDeposits)
		{
			dustDeposits = SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag);
		}
		this.RefreshVFXEffects(endlessDay, dustDeposits);
	}

	private void RefreshVFXEffects(bool endlessDay, bool dustDeposits)
	{
		string text = base.PointOfInterest.Type;
		if (text != null)
		{
			if (WorldPointOfInterest_QuestLocation.<>f__switch$map25 == null)
			{
				WorldPointOfInterest_QuestLocation.<>f__switch$map25 = new Dictionary<string, int>(2)
				{
					{
						"NavalQuestLocation",
						0
					},
					{
						"QuestLocation",
						0
					}
				};
			}
			int num;
			if (WorldPointOfInterest_QuestLocation.<>f__switch$map25.TryGetValue(text, out num))
			{
				if (num == 0)
				{
					bool flag = (base.PlayerControllerRepositoryService.ActivePlayerController.Empire.Bits & base.PointOfInterest.Interaction.Bits) != 0;
					int num2 = this.NumberOfSetBits(base.PointOfInterest.Interaction.Bits);
					string text2 = string.Empty;
					if (flag)
					{
						text2 = "Searched";
					}
					else if (num2 == 0)
					{
						text2 = "Blank";
					}
					else
					{
						text2 = "Other";
					}
					if (base.PointOfInterest.UntappedDustDeposits && dustDeposits)
					{
						text2 += "_DustDeposit";
					}
					else if (this.sensibleToEndlessDay && endlessDay)
					{
						text2 += "_EndlessDay";
					}
					this.interpreterContext.Register("Interaction", text2);
					this.DestroyPointOfInterestIcon();
					this.CreatePointOfInterestIcon();
					this.CreateOrRegeneratePointOfInterestFXTypeIFN();
					if (flag && this.interactionBitsBound)
					{
						DepartmentOfScience agency = base.PlayerControllerRepositoryService.ActivePlayerController.Empire.GetAgency<DepartmentOfScience>();
						Diagnostics.Assert(agency != null);
						if (StaticString.IsNullOrEmpty(agency.ArcheologyTechnologyDefinitionName))
						{
							base.PointOfInterest.Interaction.PointOfInterestInteractionChange -= this.Interaction_PointOfInterestInteractionChange;
							this.interactionBitsBound = false;
						}
						else
						{
							DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(agency.ArcheologyTechnologyDefinitionName);
							if (technologyState == DepartmentOfScience.ConstructibleElement.State.Researched)
							{
								base.PointOfInterest.Interaction.PointOfInterestInteractionChange -= this.Interaction_PointOfInterestInteractionChange;
								this.interactionBitsBound = false;
							}
						}
					}
					base.UpdatePointOfInterestVisibility();
				}
			}
		}
	}

	private int NumberOfSetBits(int bitField)
	{
		bitField -= (bitField >> 1 & 1431655765);
		bitField = (bitField & 858993459) + (bitField >> 2 & 858993459);
		return (bitField + (bitField >> 4) & 252645135) * 16843009 >> 24;
	}

	private bool interactionBitsBound;

	private bool sensibleToEndlessDay;
}
