using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class CreepingNodeImprovementLabelLayerPanel : LabelLayerPanel<CreepingNodeImprovementLayer>
{
	[Ancillary]
	private IGlobalPositionningService GlobalPositionningService { get; set; }

	private IEndTurnService EndTurnService
	{
		get
		{
			return this.endTurnService;
		}
		set
		{
			if (this.endTurnService != null)
			{
				this.endTurnService.GameClientStateChange -= this.EndTurnService_GameClientStateChange;
			}
			this.endTurnService = value;
			if (this.endTurnService != null)
			{
				this.endTurnService.GameClientStateChange += this.EndTurnService_GameClientStateChange;
			}
		}
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.validPointsOfInterest.Clear();
		this.validImprovements.Clear();
		this.LabelsTable.Enable = this.interactionsAllowed;
		this.UnbindAndHideLabels(this.validPointsOfInterest.Count);
	}

	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.departmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfCreepingNodes = base.Empire.GetAgency<DepartmentOfCreepingNodes>();
		this.departmentOfIndustry = base.Empire.GetAgency<DepartmentOfIndustry>();
		this.departmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (this.departmentOfCreepingNodes != null)
		{
			this.departmentOfCreepingNodes.CollectionChanged += this.ConstructionQueue_CollectionChanged;
		}
	}

	public override void Unbind()
	{
		this.departmentOfScience = null;
		this.departmentOfIndustry = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfTheInterior = null;
		if (this.departmentOfCreepingNodes != null)
		{
			this.departmentOfCreepingNodes.CollectionChanged -= this.ConstructionQueue_CollectionChanged;
			this.departmentOfCreepingNodes = null;
		}
		base.Unbind();
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.gameService = Services.GetService<IGameService>();
		global::Game game = this.gameService.Game as global::Game;
		Diagnostics.Assert(game != null, "Failed to retrieve game reference");
		this.world = game.World;
		Diagnostics.Assert(this.world != null, "Failed to retrieve game world");
		this.validPointsOfInterest = new List<PointOfInterest>();
		this.validImprovements = new List<CreepingNodeImprovementDefinition>();
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.interactionsAllowed = base.PlayerController.CanSendOrders();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		yield return base.OnHide(instant);
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		if (this.validPointsOfInterest != null)
		{
			this.validPointsOfInterest.Clear();
			this.validPointsOfInterest = null;
		}
		if (this.validImprovements != null)
		{
			this.validImprovements.Clear();
			this.validImprovements = null;
		}
		this.EndTurnService = null;
		this.gameService = null;
		this.world = null;
		base.OnUnloadGame(game);
	}

	protected override void UnbindAndHideLabels(int startIndex = 0)
	{
		for (int i = startIndex; i < this.LabelsTable.GetChildren().Count; i++)
		{
			this.LabelsTable.GetChildren()[i].GetComponent<CreepingNodeImprovementLabel>().Unbind();
			this.LabelsTable.GetChildren()[i].Visible = false;
		}
	}

	protected override void WorldView_WorldViewTechniqueChanged(object sender, WorldViewTechniqueChangeEventArgs e)
	{
		base.WorldView_WorldViewTechniqueChanged(sender, e);
		if (this.GlobalPositionningService != null)
		{
			this.GlobalPositionningService = null;
		}
		base.WorldViewTechnique = e.WorldViewTechnique;
		if (e.WorldViewTechnique == null || e.Action == WorldViewTechniqueChangeAction.Releasing)
		{
			return;
		}
		this.GlobalPositionningService = base.WorldViewTechnique.GetService<IGlobalPositionningService>();
	}

	protected override void RefreshLayerLabel()
	{
		if (!base.IsVisible)
		{
			return;
		}
		if (base.Empire == null || this.departmentOfTheInterior.MainCity == null)
		{
			return;
		}
		if (this.departmentOfTheInterior == null || this.departmentOfTheInterior.MainCity == null)
		{
			return;
		}
		this.validPointsOfInterest.Clear();
		this.validImprovements.Clear();
		if (this.departmentOfTheInterior.MainCity != null && base.Empire.SimulationObject.Tags.Contains("FactionTraitMimics1"))
		{
			List<StaticString> list = new List<StaticString>();
			IDatabase<CreepingNodeImprovementDefinition> database = Databases.GetDatabase<CreepingNodeImprovementDefinition>(true);
			CreepingNodeImprovementDefinition[] values = database.GetValues();
			if (this.entities == null)
			{
				this.entities = new List<IWorldEntityWithCulling>();
			}
			else
			{
				this.entities.Clear();
			}
			ReadOnlyCollection<IWorldEntityWithCulling> collection;
			if (base.WorldEntityCullingService.TryGetVisibleEntities<WorldPointOfInterest_QuestLocation>(out collection))
			{
				this.entities.AddRange(collection);
			}
			ReadOnlyCollection<IWorldEntityWithCulling> collection2;
			if (base.WorldEntityCullingService.TryGetVisibleEntities<WorldPointOfInterest>(out collection2))
			{
				this.entities.AddRange(collection2);
			}
			for (int i = 0; i < this.entities.Count; i++)
			{
				WorldPointOfInterest worldPointOfInterest = this.entities[i] as WorldPointOfInterest;
				PointOfInterest pointOfInterest = worldPointOfInterest.PointOfInterest;
				Region region = base.WorldPositionningService.GetRegion(pointOfInterest.WorldPosition);
				bool flag = region.IsRegionColonized();
				bool flag2 = region.Kaiju != null;
				bool flag3 = region.BelongToEmpire(base.Empire);
				bool flag4 = false;
				if (flag2)
				{
					flag4 = (region.Kaiju.IsWild() || region.Kaiju.OwnerEmpireIndex == base.Empire.Index);
				}
				if (!flag || flag3 || (flag2 && flag4))
				{
					bool flag5 = this.IsPoiUnlocked(pointOfInterest);
					if (pointOfInterest.CreepingNodeImprovement == null)
					{
						if (!(pointOfInterest.Type != "Village") || pointOfInterest.PointOfInterestImprovement == null)
						{
							if ((base.WorldPositionningService.GetExplorationBits(pointOfInterest.WorldPosition) & base.Empire.Bits) > 0 && flag5)
							{
								List<CreepingNodeImprovementDefinition> list2 = new List<CreepingNodeImprovementDefinition>();
								foreach (CreepingNodeImprovementDefinition creepingNodeImprovementDefinition in values)
								{
									if (creepingNodeImprovementDefinition.PointOfInterestTemplateName == pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName)
									{
										if (!(pointOfInterest.Type == "Village") || (pointOfInterest.SimulationObject.Tags.Contains(Village.PacifiedVillage) && !pointOfInterest.SimulationObject.Tags.Contains(Village.ConvertedVillage)))
										{
											list.Clear();
											DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.departmentOfTheInterior.MainCity, creepingNodeImprovementDefinition, ref list, new string[]
											{
												ConstructionFlags.Prerequisite
											});
											if (!list.Contains(ConstructionFlags.Discard) && this.departmentOfTheTreasury.CheckConstructibleInstantCosts(this.departmentOfTheInterior.MainCity, creepingNodeImprovementDefinition))
											{
												CreepingNodeImprovementDefinition bestCreepingNodeDefinition = this.departmentOfCreepingNodes.GetBestCreepingNodeDefinition(this.departmentOfTheInterior.MainCity, pointOfInterest, creepingNodeImprovementDefinition, list);
												if (!list2.Contains(bestCreepingNodeDefinition))
												{
													list2.Add(bestCreepingNodeDefinition);
												}
											}
										}
									}
								}
								if (list2.Count > 0)
								{
									for (int k = 0; k < list2.Count; k++)
									{
										this.validPointsOfInterest.Add(pointOfInterest);
										this.validImprovements.Add(list2[k]);
									}
								}
							}
						}
					}
				}
			}
		}
		this.LabelsTable.ReserveChildren(this.validPointsOfInterest.Count, this.LabelPrefab, "ConstructibleLabel");
		this.LabelsTable.RefreshChildrenIList<PointOfInterest>(this.validPointsOfInterest, new AgeTransform.RefreshTableItem<PointOfInterest>(this.RefreshPointOfInterest), true, false);
		Transform transform = this.cameraController.Camera.transform;
		List<CreepingNodeImprovementLabel> children = this.LabelsTable.GetChildren<CreepingNodeImprovementLabel>(true);
		for (int l = 0; l < children.Count; l++)
		{
			CreepingNodeImprovementLabel creepingNodeImprovementLabel = children[l];
			PointOfInterest pointOfInterest2 = creepingNodeImprovementLabel.CreepingNodeInfo.PointOfInterest;
			if (pointOfInterest2 != null)
			{
				List<int> list3 = new List<int>();
				for (int m = 0; m < this.validPointsOfInterest.Count; m++)
				{
					if (this.validPointsOfInterest[m] == pointOfInterest2)
					{
						list3.Add(m);
					}
				}
				int num = list3.IndexOf(l);
				Vector3 vector = this.GlobalPositionningService.Get3DPosition(pointOfInterest2.WorldPosition);
				bool flag6 = this.IsInsideCamera(this.cameraController.Camera, vector, creepingNodeImprovementLabel.Width, creepingNodeImprovementLabel.Height, 1f);
				creepingNodeImprovementLabel.AgeTransform.Visible = flag6;
				if (flag6)
				{
					Vector3 lhs = vector - transform.position;
					Vector3 forward = transform.forward;
					float num2 = Vector3.Dot(lhs, forward);
					float num3 = 0.01f;
					if (num2 < num3)
					{
						vector += forward * (num3 - num2);
					}
					Vector3 vector2 = this.cameraController.Camera.WorldToScreenPoint(vector);
					Vector2 vector3 = new Vector3(vector2.x + ((float)num + (float)list3.Count * 0.5f * -1f) * creepingNodeImprovementLabel.Width, (float)Screen.height - vector2.y - this.LabelsTable.Y);
					creepingNodeImprovementLabel.Foreground.TiltAngle = 0f;
					if (vector3.x < 0f)
					{
						vector3.x = 0f;
						vector3.y += ((float)num + (float)list3.Count * 0.5f * -1f) * creepingNodeImprovementLabel.Height;
						creepingNodeImprovementLabel.Foreground.TiltAngle = 270f;
					}
					if (vector3.x > this.LabelsTable.Width - creepingNodeImprovementLabel.Width)
					{
						vector3.x = this.LabelsTable.Width - creepingNodeImprovementLabel.Width;
						vector3.y += ((float)num + (float)list3.Count * 0.5f * -1f) * creepingNodeImprovementLabel.Height;
						creepingNodeImprovementLabel.Foreground.TiltAngle = 90f;
					}
					if (vector3.y < 0f)
					{
						vector3.y = 0f;
						creepingNodeImprovementLabel.Foreground.TiltAngle = 0f;
					}
					if (vector3.y > this.LabelsTable.Height - creepingNodeImprovementLabel.Height)
					{
						vector3.y = this.LabelsTable.Height - creepingNodeImprovementLabel.Height;
						creepingNodeImprovementLabel.Foreground.TiltAngle = 180f;
					}
					creepingNodeImprovementLabel.AgeTransform.X = vector3.x;
					creepingNodeImprovementLabel.AgeTransform.Y = vector3.y;
				}
			}
		}
		if (base.LabelLayer != null)
		{
			base.AgeTransform.Alpha = base.LabelLayer.Opacity;
		}
	}

	protected override void RefreshWorldEntityBinding(AgeTransform tableitem, IWorldEntityWithCulling worldEntity, int index)
	{
		WorldPointOfInterest worldPointOfInterest = worldEntity as WorldPointOfInterest;
		if (worldPointOfInterest != null)
		{
			this.RefreshPointOfInterest(tableitem, worldPointOfInterest.PointOfInterest, index);
		}
	}

	private void ConstructionQueue_CollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		this.RefreshContent();
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.PlayerController != null)
		{
			bool flag = base.PlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				this.LabelsTable.Enable = this.interactionsAllowed;
				if (!this.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void OnSelectCreepingNodeCB(GameObject obj)
	{
		if (base.PlayerController != null)
		{
			AgeControlButton component = obj.GetComponent<AgeControlButton>();
			CreepingNodeImprovementLabel.CreepingNodePair creepingNodePair = component.OnActivateDataObject as CreepingNodeImprovementLabel.CreepingNodePair;
			PointOfInterest pointOfInterest = creepingNodePair.PointOfInterest;
			CreepingNodeImprovementDefinition creepingNodeImprovementDefinition = creepingNodePair.CreepingNodeImprovementDefinition;
			OrderQueueCreepingNode order = new OrderQueueCreepingNode(base.Empire.Index, this.departmentOfTheInterior.MainCity.GUID, pointOfInterest.GUID, creepingNodeImprovementDefinition, pointOfInterest.WorldPosition, false, true);
			base.PlayerController.PostOrder(order);
		}
	}

	private void RefreshPointOfInterest(AgeTransform tableitem, PointOfInterest pointOfInterest, int index)
	{
		CreepingNodeImprovementLabel component = tableitem.GetComponent<CreepingNodeImprovementLabel>();
		component.Bind(pointOfInterest, this.validImprovements[index], base.Empire, base.gameObject, base.GuiService.GuiPanelHelper);
		tableitem.Visible = true;
	}

	private bool IsPoiUnlocked(PointOfInterest pointOfInterest)
	{
		string empty = string.Empty;
		return !pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out empty) || this.departmentOfScience.GetTechnologyState(empty) == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	private bool IsInsideCamera(Camera camera, Vector3 worldPosition, float labelWidth, float labelHeight, float screenFactor = 1f)
	{
		Vector3 vector = camera.WorldToScreenPoint(worldPosition);
		float num = (float)Screen.width;
		float num2 = num * screenFactor;
		float num3 = -(num2 - num);
		float num4 = num2;
		float num5 = labelWidth / 2f;
		if (vector.x + num5 < num3 || vector.x - num5 > num4)
		{
			return false;
		}
		float num6 = (float)Screen.height;
		float num7 = num6 * screenFactor;
		float num8 = -(num7 - num6);
		float num9 = num7;
		float num10 = labelHeight / 2f;
		return vector.y + num10 >= num8 && vector.y - num10 <= num9;
	}

	private List<PointOfInterest> validPointsOfInterest = new List<PointOfInterest>();

	private List<CreepingNodeImprovementDefinition> validImprovements = new List<CreepingNodeImprovementDefinition>();

	private IEndTurnService endTurnService;

	private IGameService gameService;

	private World world;

	private bool interactionsAllowed = true;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfCreepingNodes departmentOfCreepingNodes;

	private DepartmentOfIndustry departmentOfIndustry;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private List<IWorldEntityWithCulling> entities;
}
