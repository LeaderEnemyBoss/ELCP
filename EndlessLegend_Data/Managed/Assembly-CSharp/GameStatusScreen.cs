using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Session;
using UnityEngine;

public class GameStatusScreen : GuiPlayerControllerScreen
{
	public GameStatusScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
		base.EnableNavigation = true;
	}

	public override void Bind(global::Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		base.Bind(empire);
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	public override bool HandleCancelRequest()
	{
		this.Hide(false);
		base.GuiService.GetGuiPanel<GameEmpireScreen>().Show(new object[0]);
		return true;
	}

	public override void RefreshContent()
	{
		if (base.Empire == null)
		{
			return;
		}
		IVictoryManagementService service = base.Game.Services.GetService<IVictoryManagementService>();
		this.filteredVictoryConditions = new List<VictoryCondition>();
		foreach (VictoryCondition victoryCondition in service.VictoryConditionsFilteredThisGame)
		{
			if (victoryCondition.Progression != null)
			{
				this.filteredVictoryConditions.Add(victoryCondition);
			}
		}
		this.VictoryTable.Width = 0f;
		this.VictoryTable.ReserveChildren(this.filteredVictoryConditions.Count, this.VictoryStatusItemPrefab, "Item");
		this.VictoryTable.RefreshChildrenIList<VictoryCondition>(this.filteredVictoryConditions, new AgeTransform.RefreshTableItem<VictoryCondition>(this.SetupVictoryCondition), true, false);
		this.VictoryTable.ArrangeChildren();
		this.VictoryScrollview.ResetLeft();
		this.scoreGraphsPanel.RefreshContent();
	}

	public override void Unbind()
	{
		base.Unbind();
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.scoreGraphsPanel.Hide(false);
		if (base.PlayerController != null && base.PlayerController.Empire != null)
		{
			DepartmentOfForeignAffairs departmentOfForeignAffairs = base.PlayerController.Empire.GetAgency<DepartmentOfForeignAffairs>();
			if (departmentOfForeignAffairs != null)
			{
				departmentOfForeignAffairs.DiplomaticRelationStateChange -= this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
			}
		}
		IEndTurnService endTurnService = Services.GetService<IEndTurnService>();
		endTurnService.GameClientStateChange -= this.EndTurnService_GameClientStateChange;
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.scoreGraphsPanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<ScoreGraphsPanel>(this.ScoreGroup, this.ScoreGraphsPanelPrefab, null);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.empires = new List<MajorEmpire>();
		foreach (global::Empire empire in (base.Game as global::Game).Empires)
		{
			if (empire is MajorEmpire)
			{
				this.empires.Add(empire as MajorEmpire);
			}
		}
		yield break;
	}

	protected override void PlayerControllerRepository_ActivePlayerControllerChange(object sender, ActivePlayerControllerChangeEventArgs eventArgs)
	{
		base.PlayerControllerRepository_ActivePlayerControllerChange(sender, eventArgs);
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		ISessionService sessionService = Services.GetService<ISessionService>();
		int numberOfMajorFactions = sessionService.Session.GetLobbyData<int>("NumberOfMajorFactions", 0);
		EmpireInfo[] empireInfo = new EmpireInfo[numberOfMajorFactions];
		for (int empireIndex = 0; empireIndex < numberOfMajorFactions; empireIndex++)
		{
			empireInfo[empireIndex] = EmpireInfo.Read(sessionService.Session, empireIndex);
		}
		IEndTurnService endTurnService = Services.GetService<IEndTurnService>();
		endTurnService.GameClientStateChange += this.EndTurnService_GameClientStateChange;
		DepartmentOfForeignAffairs departmentOfForeignAffairs = base.PlayerController.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (departmentOfForeignAffairs != null)
		{
			departmentOfForeignAffairs.DiplomaticRelationStateChange += this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
		}
		this.scoreGraphsPanel.HideUnknownEmpires = true;
		this.scoreGraphsPanel.Show(new object[]
		{
			empireInfo
		});
		this.RefreshContent();
		yield break;
	}

	protected override void OnUnload()
	{
		this.scoreGraphsPanel.Unload();
		UnityEngine.Object.Destroy(this.scoreGraphsPanel);
		base.OnUnload();
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.VictoryTable.DestroyAllChildren();
		this.empires.Clear();
		this.empires = null;
		this.Unbind();
		base.OnUnloadGame(game);
	}

	private void DepartmentOfForeignAffairs_DiplomaticRelationStateChange(object sender, DiplomaticRelationStateChangeEventArgs e)
	{
		this.RefreshContent();
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		this.RefreshContent();
	}

	private void OnAcademyScreenCB(GameObject obj)
	{
		this.Hide(true);
		GameAcademyScreen guiPanel = base.GuiService.GetGuiPanel<GameAcademyScreen>();
		guiPanel.FromMilitaryScreen = true;
		guiPanel.Show(new object[0]);
	}

	private void OnCloseCB(GameObject obj)
	{
		this.HandleCancelRequest();
	}

	private void SetupVictoryCondition(AgeTransform item, VictoryCondition victoryCondition, int index)
	{
		VictoryStatusItem component = item.GetComponent<VictoryStatusItem>();
		component.SetContent(victoryCondition, base.GuiService.GuiPanelHelper, base.PlayerController, index);
		component.UpdateVictoryStatus(this.empires);
	}

	public AgeTransform ScoreGroup;

	public Transform ScoreGraphsPanelPrefab;

	public AgeControlScrollView VictoryScrollview;

	public AgeTransform VictoryTable;

	public Transform VictoryStatusItemPrefab;

	private ScoreGraphsPanel scoreGraphsPanel;

	private List<MajorEmpire> empires;

	private List<VictoryCondition> filteredVictoryConditions;
}
