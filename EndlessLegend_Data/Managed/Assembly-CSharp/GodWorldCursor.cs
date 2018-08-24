using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.View;
using UnityEngine;

public class GodWorldCursor : WorldCursor
{
	public GodWorldCursor()
	{
		base.Click += this.UnitSpawnWorldCursor_Click;
	}

	public static StaticString[] EditorSelectedUnitDesigns { get; set; }

	public static global::Empire EditorSelectedEmpire { get; set; }

	private void UnitSpawnWorldCursor_Click(object sender, CursorTargetMouseEventArgs e)
	{
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl))
		{
			this.SpawnEnnemyArmy();
			return;
		}
		if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftShift))
		{
			IGameService service = Services.GetService<IGameService>();
			if (service.Game == null || GodWorldCursor.EditorSelectedEmpire == null || GodWorldCursor.EditorSelectedUnitDesigns == null || GodWorldCursor.EditorSelectedUnitDesigns.Length == 0)
			{
				return;
			}
			if (WorldCursor.HighlightedWorldPosition.IsValid)
			{
				OrderSpawnArmy orderSpawnArmy = new OrderSpawnArmy(GodWorldCursor.EditorSelectedEmpire.Index, WorldCursor.HighlightedWorldPosition, GodWorldCursor.EditorSelectedUnitDesigns);
				service.Game.Services.GetService<IPlayerControllerRepositoryService>().ActivePlayerController.PostOrder(orderSpawnArmy);
				Diagnostics.Log("Posting order: {0}.", new object[]
				{
					orderSpawnArmy.ToString()
				});
				return;
			}
		}
		else
		{
			if (Input.GetKey(KeyCode.RightControl))
			{
				this.SpawnWildlingArmy();
				return;
			}
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt))
			{
				this.SpawnMinorArmy();
				return;
			}
			if (Input.GetKey(KeyCode.LeftShift))
			{
				this.SpawnArmy();
				return;
			}
			if (Input.GetKey(KeyCode.LeftAlt))
			{
				this.SpawnCity();
				return;
			}
			if (Input.GetKey(KeyCode.RightShift))
			{
				this.SpawnCamp();
				return;
			}
			if (Input.GetKey(KeyCode.C))
			{
				Amplitude.Unity.View.IViewService service2 = Services.GetService<Amplitude.Unity.View.IViewService>();
				if (service2.CurrentView != null && service2.CurrentView.CameraController is IWorldViewCameraController)
				{
					(service2.CurrentView.CameraController as IWorldViewCameraController).FocusCameraAt(WorldCursor.HighlightedWorldPosition, false, true);
				}
			}
		}
	}

	private void SpawnArmy()
	{
		IGameService service = Services.GetService<IGameService>();
		if (service.Game == null)
		{
			return;
		}
		if (WorldCursor.HighlightedWorldPosition.IsValid)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Debug/GodCursor/SpawnArmy", string.Empty);
			int index = service2.ActivePlayerController.Empire.Index;
			if (string.IsNullOrEmpty(value))
			{
				return;
			}
			string[] array = value.Split(new char[]
			{
				','
			});
			if (array.Length == 0)
			{
				return;
			}
			StaticString[] unitDesignsByName = Array.ConvertAll<string, StaticString>(array, (string input) => input);
			OrderSpawnArmy orderSpawnArmy = new OrderSpawnArmy(index, WorldCursor.HighlightedWorldPosition, unitDesignsByName);
			service2.ActivePlayerController.PostOrder(orderSpawnArmy);
			Diagnostics.Log("Posting order: {0}.", new object[]
			{
				orderSpawnArmy.ToString()
			});
		}
	}

	private void SpawnEnnemyArmy()
	{
		IGameService service = Services.GetService<IGameService>();
		if (service.Game == null)
		{
			return;
		}
		if (WorldCursor.HighlightedWorldPosition.IsValid)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Debug/GodCursor/SpawnEnnemyArmy", string.Empty);
			if (string.IsNullOrEmpty(value))
			{
				return;
			}
			string[] array = value.Split(new char[]
			{
				','
			});
			if (array.Length == 0)
			{
				return;
			}
			StaticString[] unitDesignsByName = Array.ConvertAll<string, StaticString>(array, (string input) => input);
			OrderSpawnArmy orderSpawnArmy = new OrderSpawnArmy(1, WorldCursor.HighlightedWorldPosition, unitDesignsByName);
			service2.ActivePlayerController.PostOrder(orderSpawnArmy);
			Diagnostics.Log("Posting order: {0}.", new object[]
			{
				orderSpawnArmy.ToString()
			});
		}
	}

	private void SpawnMinorArmy()
	{
		IGameService service = Services.GetService<IGameService>();
		if (service.Game == null)
		{
			return;
		}
		if (WorldCursor.HighlightedWorldPosition.IsValid && WorldCursor.HighlightedRegion.MinorEmpire != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			DepartmentOfDefense agency = WorldCursor.HighlightedRegion.MinorEmpire.GetAgency<DepartmentOfDefense>();
			UnitDesign unitDesign = agency.UnitDesignDatabase.UserDefinedUnitDesigns[0];
			List<StaticString> list = new List<StaticString>();
			for (int i = 0; i < 3; i++)
			{
				list.Add(unitDesign.Name);
			}
			OrderSpawnArmy orderSpawnArmy = new OrderSpawnArmy(WorldCursor.HighlightedRegion.MinorEmpire.Index, WorldCursor.HighlightedWorldPosition, list.ToArray());
			service2.ActivePlayerController.PostOrder(orderSpawnArmy);
			Diagnostics.Log("Posting order: {0}.", new object[]
			{
				orderSpawnArmy.ToString()
			});
		}
	}

	private void SpawnWildlingArmy()
	{
		IGameService service = Services.GetService<IGameService>();
		if (service.Game == null)
		{
			return;
		}
		if (WorldCursor.HighlightedWorldPosition.IsValid)
		{
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Debug/GodCursor/SpawnWildlingArmy", string.Empty);
			if (string.IsNullOrEmpty(value))
			{
				return;
			}
			string[] array = value.Split(new char[]
			{
				','
			});
			if (array.Length == 0)
			{
				return;
			}
			StaticString[] unitDesignsByName = Array.ConvertAll<string, StaticString>(array, (string unitDesign) => unitDesign);
			IWildlingsService service2 = service.Game.Services.GetService<IWildlingsService>();
			if (service2 == null)
			{
				return;
			}
			List<StaticString> list = new List<StaticString>();
			list.Add(new StaticString("GodCursorWildlingArmy"));
			List<StaticString> list2 = new List<StaticString>();
			list2.Add(new StaticString("GodCursorWildlingUnit"));
			service2.SpawnArmies(new WorldPosition[]
			{
				WorldCursor.HighlightedWorldPosition
			}, unitDesignsByName, list, list2, 0, QuestArmyObjective.QuestBehaviourType.Roaming);
		}
	}

	private void SpawnCamp()
	{
		IGameService service = Services.GetService<IGameService>();
		if (service.Game == null)
		{
			return;
		}
		if (WorldCursor.HighlightedWorldPosition.IsValid)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			int index = service2.ActivePlayerController.Empire.Index;
			OrderCreateCamp orderCreateCamp = new OrderCreateCamp(index, WorldCursor.HighlightedWorldPosition, true);
			service2.ActivePlayerController.PostOrder(orderCreateCamp);
			Diagnostics.Log("Posting order: {0}.", new object[]
			{
				orderCreateCamp.ToString()
			});
		}
	}

	private void SpawnCity()
	{
		IGameService service = Services.GetService<IGameService>();
		if (service.Game == null)
		{
			return;
		}
		if (WorldCursor.HighlightedWorldPosition.IsValid)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			int index = service2.ActivePlayerController.Empire.Index;
			OrderCreateCity orderCreateCity = new OrderCreateCity(index, WorldCursor.HighlightedWorldPosition);
			service2.ActivePlayerController.PostOrder(orderCreateCity);
			Diagnostics.Log("Posting order: {0}.", new object[]
			{
				orderCreateCity.ToString()
			});
		}
	}

	protected override void ShowTooltip(WorldPosition worldPosition)
	{
		if (this.guiTooltipService == null)
		{
			return;
		}
		if (AgeManager.IsMouseCovered)
		{
			return;
		}
		IWorldPositionningService service = base.GameService.Game.Services.GetService<IWorldPositionningService>();
		if (worldPosition.IsValid)
		{
			if (service != null)
			{
				if (service == null)
				{
					this.guiTooltipService.HideTooltip();
					return;
				}
				if ((service.GetExplorationBits(worldPosition) & base.EmpireBits) == 0)
				{
					this.guiTooltipService.HideTooltip();
					return;
				}
				AgeTransform cursorTooltipAnchor = this.guiTooltipService.GetCursorTooltipAnchor();
				AgeTooltipAnchorMode anchorMode = AgeTooltipAnchorMode.FREE;
				global::CursorTarget cursorTarget = null;
				if (base.CursorTargetService.HighlightedCursorTargets.Count > 0)
				{
					for (int i = 0; i < base.CursorTargetService.HighlightedCursorTargets.Count; i++)
					{
						Diagnostics.Log("Cursortarget {0} at {1} is {2} and has tooltipclass {3}", new object[]
						{
							i,
							worldPosition,
							base.CursorTargetService.HighlightedCursorTargets[i].GetType(),
							(base.CursorTargetService.HighlightedCursorTargets[i] as global::CursorTarget).TooltipClass
						});
						Diagnostics.Log("Conten: {0}", new object[]
						{
							(base.CursorTargetService.HighlightedCursorTargets[i] as global::CursorTarget).TooltipContent
						});
						Diagnostics.Log("Context: {0}", new object[]
						{
							(base.CursorTargetService.HighlightedCursorTargets[i] as global::CursorTarget).TooltipContext.GetType()
						});
						if (cursorTarget == null)
						{
							cursorTarget = (base.CursorTargetService.HighlightedCursorTargets[i] as global::CursorTarget);
						}
						else if (StaticString.IsNullOrEmpty(cursorTarget.TooltipClass))
						{
							cursorTarget = (base.CursorTargetService.HighlightedCursorTargets[i] as global::CursorTarget);
						}
					}
				}
				string text = string.Format("Worldposition: {0} {1}", worldPosition.Row, worldPosition.Column);
				PointOfInterest pointOfInterest = service.GetPointOfInterest(worldPosition);
				if (pointOfInterest != null)
				{
					text += "\nhas POI!";
					IQuestManagementService service2 = base.GameService.Game.Services.GetService<IQuestManagementService>();
					IQuestRepositoryService service3 = base.GameService.Game.Services.GetService<IQuestRepositoryService>();
					global::Empire empire = base.GameService.Game.Services.GetService<IPlayerControllerRepositoryService>().ActivePlayerController.Empire as global::Empire;
					if (empire != null)
					{
						foreach (QuestMarker questMarker in service2.GetMarkersByBoundTargetGUID(pointOfInterest.GUID))
						{
							Quest quest;
							if (service3.TryGetValue(questMarker.QuestGUID, out quest))
							{
								text = text + "\nhas Questmarker for quest" + quest.QuestDefinition.Name;
								QuestBehaviour questBehaviour = service3.GetQuestBehaviour(quest.Name, empire.Index);
								if (questBehaviour != null)
								{
									QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount questBehaviourTreeNode_ConditionCheck_HasResourceAmount;
									if (quest.QuestDefinition.Variables.First((QuestVariableDefinition p) => p.VarName == "$NameOfStrategicResourceToGather1") != null && this.TryGetFirstNodeOfType<QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_ConditionCheck_HasResourceAmount))
									{
										string resourceName = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.ResourceName;
										int wantedAmount = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.WantedAmount;
										text = text + "\nResource: " + resourceName;
										text = text + "\nAmount: " + wantedAmount;
										break;
									}
								}
							}
						}
					}
				}
				if (!(cursorTarget != null) || StaticString.IsNullOrEmpty(cursorTarget.TooltipClass))
				{
					if (base.TooltipFilters != null)
					{
						if (!Array.Exists<StaticString>(base.TooltipFilters, (StaticString match) => match == "Terrain"))
						{
							this.guiTooltipService.HideTooltip();
							return;
						}
					}
					this.guiTooltipService.ShowTooltip(string.Empty, text, worldPosition, cursorTooltipAnchor, anchorMode, 0f, false);
					return;
				}
				if (base.TooltipFilters == null || Array.Exists<StaticString>(base.TooltipFilters, (StaticString match) => match == cursorTarget.TooltipClass))
				{
					this.guiTooltipService.ShowTooltip(string.Empty, text, worldPosition, cursorTooltipAnchor, anchorMode, 0f, false);
					return;
				}
				this.guiTooltipService.HideTooltip();
				return;
			}
		}
		else
		{
			this.guiTooltipService.HideTooltip();
		}
	}

	private bool TryGetFirstNodeOfType<T>(BehaviourTreeNodeController controller, out T Node)
	{
		foreach (BehaviourTreeNode behaviourTreeNode in controller.Children)
		{
			if (behaviourTreeNode is T)
			{
				Node = (T)((object)behaviourTreeNode);
				return true;
			}
			if (behaviourTreeNode is BehaviourTreeNodeController)
			{
				T t = default(T);
				if (this.TryGetFirstNodeOfType<T>(behaviourTreeNode as BehaviourTreeNodeController, out t))
				{
					Node = t;
					return true;
				}
			}
			if (behaviourTreeNode is QuestBehaviourTreeNode_Decorator_InteractWith)
			{
				foreach (QuestBehaviourTreeNode_ConditionCheck questBehaviourTreeNode_ConditionCheck in (behaviourTreeNode as QuestBehaviourTreeNode_Decorator_InteractWith).ConditionChecks)
				{
					if (questBehaviourTreeNode_ConditionCheck is T)
					{
						Node = (T)((object)questBehaviourTreeNode_ConditionCheck);
						return true;
					}
				}
			}
		}
		Node = default(T);
		return false;
	}
}
