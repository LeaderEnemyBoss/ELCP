using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class GeoConstructibleLabelLayerPanel : LabelLayerPanel<GeoConstructibleLabelLayer>
{
	public City City
	{
		get
		{
			return this.city;
		}
		private set
		{
			if (this.city != null)
			{
				DepartmentOfIndustry agency = this.city.Empire.GetAgency<DepartmentOfIndustry>();
				ConstructionQueue constructionQueue = agency.GetConstructionQueue(this.city);
				constructionQueue.CollectionChanged -= this.ConstructionQueue_CollectionChanged;
			}
			this.city = value;
			this.UnbindAndHideLabels(0);
			if (this.city != null)
			{
				DepartmentOfIndustry agency2 = this.city.Empire.GetAgency<DepartmentOfIndustry>();
				ConstructionQueue constructionQueue2 = agency2.GetConstructionQueue(this.city);
				constructionQueue2.CollectionChanged += this.ConstructionQueue_CollectionChanged;
			}
		}
	}

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

	public void SetContent(City city)
	{
		this.City = city;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.validPointsOfInterest.Clear();
		this.validImprovements.Clear();
		if (this.City != null && this.City.BesiegingEmpire == null && base.Empire == this.City.Empire && !this.City.IsInfected)
		{
			DepartmentOfIndustry agency = this.City.Empire.GetAgency<DepartmentOfIndustry>();
			DepartmentOfTheInterior agency2 = this.City.Empire.GetAgency<DepartmentOfTheInterior>();
			DepartmentOfTheTreasury agency3 = this.City.Empire.GetAgency<DepartmentOfTheTreasury>();
			ConstructibleElement[] availableConstructibleElements = agency.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[0]);
			List<StaticString> list = new List<StaticString>();
			for (int i = 0; i < this.City.Region.PointOfInterests.Length; i++)
			{
				PointOfInterest pointOfInterest = this.City.Region.PointOfInterests[i];
				if (pointOfInterest.PointOfInterestImprovement == null && pointOfInterest.CreepingNodeImprovement == null && (base.WorldPositionningService.GetExplorationBits(pointOfInterest.WorldPosition) & this.City.Empire.Bits) > 0)
				{
					List<PointOfInterestImprovementDefinition> list2 = new List<PointOfInterestImprovementDefinition>();
					List<ConstructibleDistrictDefinition> list3 = new List<ConstructibleDistrictDefinition>();
					for (int j = 0; j < availableConstructibleElements.Length; j++)
					{
						if (availableConstructibleElements[j] is PointOfInterestImprovementDefinition)
						{
							PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = availableConstructibleElements[j] as PointOfInterestImprovementDefinition;
							if (pointOfInterestImprovementDefinition.PointOfInterestTemplateName == pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName)
							{
								list.Clear();
								DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.City, pointOfInterestImprovementDefinition, ref list, new string[]
								{
									ConstructionFlags.Prerequisite
								});
								if (!list.Contains(ConstructionFlags.Discard) && agency3.CheckConstructibleInstantCosts(this.City, pointOfInterestImprovementDefinition))
								{
									list2.Add(agency2.GetBestImprovementDefinition(this.City, pointOfInterest, pointOfInterestImprovementDefinition, list));
								}
							}
						}
						else if (availableConstructibleElements[j] is ConstructibleDistrictDefinition)
						{
							ConstructibleDistrictDefinition constructibleDistrictDefinition = availableConstructibleElements[j] as ConstructibleDistrictDefinition;
							if (constructibleDistrictDefinition != null)
							{
								list3.Add(constructibleDistrictDefinition);
							}
						}
					}
					if (list2.Count > 0)
					{
						ConstructionQueue constructionQueue = agency.GetConstructionQueue(this.City);
						bool flag = false;
						for (int k = 0; k < list2.Count; k++)
						{
							for (int l = 0; l < constructionQueue.Length; l++)
							{
								Construction construction = constructionQueue.PeekAt(l);
								if (construction.ConstructibleElement == list2[k] && construction.WorldPosition == pointOfInterest.WorldPosition)
								{
									flag = true;
									break;
								}
							}
						}
						if (!flag)
						{
							for (int m = 0; m < list3.Count; m++)
							{
								for (int n = 0; n < constructionQueue.Length; n++)
								{
									Construction construction2 = constructionQueue.PeekAt(n);
									if (construction2.ConstructibleElement == list3[m] && construction2.WorldPosition == pointOfInterest.WorldPosition)
									{
										flag = true;
										break;
									}
								}
							}
						}
						if (!flag)
						{
							for (int num = 0; num < list2.Count; num++)
							{
								this.validPointsOfInterest.Add(pointOfInterest);
								this.validImprovements.Add(list2[num]);
							}
						}
					}
				}
			}
		}
		this.LabelsTable.ReserveChildren(this.validPointsOfInterest.Count, this.LabelPrefab, "ConstructibleLabel");
		this.LabelsTable.RefreshChildrenIList<PointOfInterest>(this.validPointsOfInterest, new AgeTransform.RefreshTableItem<PointOfInterest>(this.RefreshPointOfInterest), true, false);
		this.LabelsTable.Enable = this.interactionsAllowed;
		this.UnbindAndHideLabels(this.validPointsOfInterest.Count);
	}

	public override void Unbind()
	{
		this.City = null;
		base.Unbind();
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.validPointsOfInterest = new List<PointOfInterest>();
		this.validImprovements = new List<PointOfInterestImprovementDefinition>();
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
		base.OnUnloadGame(game);
	}

	protected override void UnbindAndHideLabels(int startIndex = 0)
	{
		for (int i = startIndex; i < this.LabelsTable.GetChildren().Count; i++)
		{
			this.LabelsTable.GetChildren()[i].GetComponent<GeoConstructibleLabel>().Unbind();
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
		Transform transform = this.cameraController.Camera.transform;
		List<GeoConstructibleLabel> children = this.LabelsTable.GetChildren<GeoConstructibleLabel>(true);
		for (int i = 0; i < children.Count; i++)
		{
			GeoConstructibleLabel geoConstructibleLabel = children[i];
			PointOfInterest pointOfInterest = geoConstructibleLabel.PointOfInterestInfo.PointOfInterest;
			if (pointOfInterest != null)
			{
				List<int> list = new List<int>();
				for (int j = 0; j < this.validPointsOfInterest.Count; j++)
				{
					if (this.validPointsOfInterest[j] == pointOfInterest)
					{
						list.Add(j);
					}
				}
				int num = list.IndexOf(i);
				Vector3 vector = this.GlobalPositionningService.Get3DPosition(pointOfInterest.WorldPosition);
				Vector3 lhs = vector - transform.position;
				Vector3 forward = transform.forward;
				float num2 = Vector3.Dot(lhs, forward);
				float num3 = 0.01f;
				if (num2 < num3)
				{
					vector += forward * (num3 - num2);
				}
				Vector3 vector2 = this.cameraController.Camera.WorldToScreenPoint(vector);
				Vector2 vector3 = new Vector3(vector2.x + ((float)num + (float)list.Count * 0.5f * -1f) * geoConstructibleLabel.Width, (float)Screen.height - vector2.y - this.LabelsTable.Y);
				geoConstructibleLabel.Foreground.TiltAngle = 0f;
				if (vector3.x < 0f)
				{
					vector3.x = 0f;
					vector3.y += ((float)num + (float)list.Count * 0.5f * -1f) * geoConstructibleLabel.Height;
					geoConstructibleLabel.Foreground.TiltAngle = 270f;
				}
				if (vector3.x > this.LabelsTable.Width - geoConstructibleLabel.Width)
				{
					vector3.x = this.LabelsTable.Width - geoConstructibleLabel.Width;
					vector3.y += ((float)num + (float)list.Count * 0.5f * -1f) * geoConstructibleLabel.Height;
					geoConstructibleLabel.Foreground.TiltAngle = 90f;
				}
				if (vector3.y < 0f)
				{
					vector3.y = 0f;
					geoConstructibleLabel.Foreground.TiltAngle = 0f;
				}
				if (vector3.y > this.LabelsTable.Height - geoConstructibleLabel.Height)
				{
					vector3.y = this.LabelsTable.Height - geoConstructibleLabel.Height;
					geoConstructibleLabel.Foreground.TiltAngle = 180f;
				}
				geoConstructibleLabel.AgeTransform.X = vector3.x;
				geoConstructibleLabel.AgeTransform.Y = vector3.y;
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

	private void OnSelectGeoConstructibleCB(GameObject obj)
	{
		if (base.PlayerController != null)
		{
			AgeControlButton component = obj.GetComponent<AgeControlButton>();
			GeoConstructibleLabel.PointOfInterestPair pointOfInterestPair = component.OnActivateDataObject as GeoConstructibleLabel.PointOfInterestPair;
			PointOfInterest pointOfInterest = pointOfInterestPair.PointOfInterest;
			PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = pointOfInterestPair.PointOfInterestImprovementDefinition;
			OrderQueueConstruction order = new OrderQueueConstruction(this.City.Empire.Index, this.City.GUID, pointOfInterestImprovementDefinition, pointOfInterest.WorldPosition, string.Empty);
			base.PlayerController.PostOrder(order);
		}
	}

	private void RefreshPointOfInterest(AgeTransform tableitem, PointOfInterest pointOfInterest, int index)
	{
		GeoConstructibleLabel component = tableitem.GetComponent<GeoConstructibleLabel>();
		component.Bind(pointOfInterest, this.validImprovements[index], this.City, base.gameObject, base.GuiService.GuiPanelHelper);
		tableitem.Visible = true;
	}

	private List<PointOfInterest> validPointsOfInterest = new List<PointOfInterest>();

	private List<PointOfInterestImprovementDefinition> validImprovements = new List<PointOfInterestImprovementDefinition>();

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;

	private City city;
}
