using System;
using System.Collections.Generic;
using Amplitude;
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
		}
		else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftShift))
		{
			IGameService service = Services.GetService<IGameService>();
			if (service.Game == null || GodWorldCursor.EditorSelectedEmpire == null || GodWorldCursor.EditorSelectedUnitDesigns == null || GodWorldCursor.EditorSelectedUnitDesigns.Length == 0)
			{
				return;
			}
			if (WorldCursor.HighlightedWorldPosition.IsValid)
			{
				OrderSpawnArmy orderSpawnArmy = new OrderSpawnArmy(GodWorldCursor.EditorSelectedEmpire.Index, WorldCursor.HighlightedWorldPosition, GodWorldCursor.EditorSelectedUnitDesigns);
				IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
				service2.ActivePlayerController.PostOrder(orderSpawnArmy);
				Diagnostics.Log("Posting order: {0}.", new object[]
				{
					orderSpawnArmy.ToString()
				});
			}
		}
		else if (Input.GetKey(KeyCode.RightControl))
		{
			this.SpawnWildlingArmy();
		}
		else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt))
		{
			this.SpawnMinorArmy();
		}
		else if (Input.GetKey(KeyCode.LeftShift))
		{
			this.SpawnArmy();
		}
		else if (Input.GetKey(KeyCode.LeftAlt))
		{
			this.SpawnCity();
		}
		else if (Input.GetKey(KeyCode.RightShift))
		{
			this.SpawnCamp();
		}
		else if (Input.GetKey(KeyCode.C))
		{
			Amplitude.Unity.View.IViewService service3 = Services.GetService<Amplitude.Unity.View.IViewService>();
			if (service3.CurrentView != null && service3.CurrentView.CameraController is IWorldViewCameraController)
			{
				IWorldViewCameraController worldViewCameraController = service3.CurrentView.CameraController as IWorldViewCameraController;
				worldViewCameraController.FocusCameraAt(WorldCursor.HighlightedWorldPosition, false, true);
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
			OrderSpawnArmy orderSpawnArmy = new OrderSpawnArmy(0, WorldCursor.HighlightedWorldPosition, unitDesignsByName);
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
}
