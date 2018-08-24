using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Session;

public class FeaturePlayerStatusItem : Behaviour
{
	public void RefreshContent(Player player, Empire playerEmpire, IGuiPanelHelper helper)
	{
		GuiEmpire guiEmpire = new GuiEmpire(player.Empire);
		this.EmpireIcon.Image = guiEmpire.GetImageTexture(global::GuiPanel.IconSize.LogoSmall, playerEmpire);
		this.EmpireIcon.TintColor = guiEmpire.Color;
		AgeUtils.TruncateString(player.LocalizedName, this.EmpireLeader, out this.temp, '.');
		this.EmpireLeader.Text = this.temp;
		this.EmpireLeader.TintColor = player.Empire.Color;
		float value = 0f;
		MajorEmpire majorEmpire = player.Empire as MajorEmpire;
		if (majorEmpire != null)
		{
			Diagnostics.Assert(majorEmpire.GameScores != null);
			GameScore gameScore = null;
			if (majorEmpire.GameScores.TryGetValue(GameScores.Names.GlobalScore, out gameScore))
			{
				value = gameScore.Value;
			}
		}
		this.EmpireScore.Text = GuiFormater.FormatGui(value, false, false, false, 1);
		this.EmpireScore.TintColor = player.Empire.Color;
		if (player.Empire == playerEmpire)
		{
			this.EmpireDiplomacy.Text = "-";
			this.EmpireDiplomacy.TintColor = player.Empire.Color;
		}
		else
		{
			DepartmentOfForeignAffairs agency = playerEmpire.GetAgency<DepartmentOfForeignAffairs>();
			if (agency != null)
			{
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(player.Empire);
				GuiElement guiElement;
				if (helper.TryGetGuiElement(diplomaticRelation.State.Name, out guiElement))
				{
					AgeUtils.TruncateString(AgeLocalizer.Instance.LocalizeString(guiElement.Title), this.EmpireDiplomacy, out this.temp, '.');
					this.EmpireDiplomacy.Text = this.temp;
					this.EmpireDiplomacy.TintColor = player.Empire.Color;
				}
				if (player.Empire.IsControlledByAI)
				{
					if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown)
					{
						AgeUtils.TruncateString(AgeLocalizer.Instance.LocalizeString(guiElement.Title), this.EmpireDiplomacy, out this.temp, '.');
						this.EmpireLeader.Text = this.temp;
					}
					else
					{
						AgeUtils.TruncateString(player.Empire.LocalizedName, this.EmpireLeader, out this.temp, '.');
						this.EmpireLeader.Text = this.temp;
					}
				}
			}
		}
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null && service.Session != null);
		string key = string.Empty;
		this.EmpireStatus.Text = string.Empty;
		PlayerHelper.ComputePlayerState(ref player);
		key = string.Format("%PlayerState_{0}", player.State);
		string src = string.Empty;
		if (player.Type == PlayerType.Human && service.Session.SessionMode != SessionMode.Single)
		{
			src = string.Format("{0} ({1:0.}ms)", AgeLocalizer.Instance.LocalizeString(key), player.Latency * 1000.0);
		}
		else
		{
			src = AgeLocalizer.Instance.LocalizeString(key);
		}
		AgeUtils.TruncateString(src, this.EmpireStatus, out this.temp, '.');
		AgePrimitiveLabel empireStatus = this.EmpireStatus;
		empireStatus.Text = empireStatus.Text + this.temp + "\n";
		this.EmpireStatus.Text = this.EmpireStatus.Text.TrimEnd(new char[]
		{
			'\n'
		});
		this.EmpireStatus.TintColor = player.Empire.Color;
	}

	public AgePrimitiveImage EmpireIcon;

	public AgePrimitiveLabel EmpireLeader;

	public AgePrimitiveLabel EmpireScore;

	public AgePrimitiveLabel EmpireDiplomacy;

	public AgePrimitiveLabel EmpireStatus;

	private string temp = string.Empty;
}
