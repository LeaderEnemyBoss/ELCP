using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Amplitude;
using Amplitude.Path;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Simulation;

public class PanelFeatureEffects_Terraformation : GuiPanelFeature
{
	private IDatabase<SimulationDescriptor> SimulationDescriptorDatabase { get; set; }

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.worldPositionningService = base.Game.Services.GetService<IWorldPositionningService>();
		this.SimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		SimulationDescriptor classCityDescriptor = this.SimulationDescriptorDatabase.GetValue("ClassCity");
		this.cityProxy = new SimulationObject("PanelFeatureEffects_Terraformation.CityProxy" + PanelFeatureEffects_Terraformation.uniqueId++);
		this.cityProxy.ModifierForward = ModifierForwardType.ChildrenOnly;
		this.cityProxy.AddDescriptor(classCityDescriptor);
		this.districtProxy = new SimulationObject("PanelFeatureEffects_Terraformation.DistrictProxy");
		this.districtProxy.ModifierForward = ModifierForwardType.ChildrenOnly;
		this.stringBuilder = new StringBuilder();
		this.propertyValues = new float[this.properties.Count];
		yield break;
	}

	protected override void DeserializeFeatureDescription(XmlElement featureDescription)
	{
		base.DeserializeFeatureDescription(featureDescription);
		if (featureDescription.Name == "Property")
		{
			string attribute = featureDescription.GetAttribute("Name");
			if (!string.IsNullOrEmpty(attribute))
			{
				this.properties.Add(attribute);
			}
			string attribute2 = featureDescription.GetAttribute("Symbol");
			if (!string.IsNullOrEmpty(attribute2))
			{
				this.symbols.Add(attribute2);
			}
			else
			{
				this.symbols.Add(attribute);
			}
		}
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		IPlayerControllerRepositoryService playerControllerRepositoryService = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.ResetPropertyValues();
		WorldPosition worldPosition = (WorldPosition)this.context;
		this.stringBuilder.Length = 0;
		if (worldPosition.IsValid)
		{
			World world = ((global::Game)base.GameService.Game).World;
			WorldCircle worldCircle = new WorldCircle(worldPosition, 1);
			WorldPosition[] worldPositions = worldCircle.GetWorldPositions(this.worldPositionningService.World.WorldParameters);
			List<WorldPosition> filteredPositions = new List<WorldPosition>(worldPositions);
			for (int positionIndex = filteredPositions.Count - 1; positionIndex >= 0; positionIndex--)
			{
				TerrainTypeMapping terrainTypeMapping = null;
				if (!world.TryGetTerraformMapping(worldPositions[positionIndex], out terrainTypeMapping))
				{
					filteredPositions.RemoveAt(positionIndex);
				}
			}
			for (int index = 0; index < filteredPositions.Count; index++)
			{
				this.FillWorldPositionEffects(playerControllerRepositoryService.ActivePlayerController.Empire, filteredPositions[index], false);
			}
			this.FillOutputString(ref this.stringBuilder);
			this.OldResult.Text = this.stringBuilder.ToString();
			this.ResetPropertyValues();
			this.stringBuilder.Length = 0;
			for (int index2 = 0; index2 < filteredPositions.Count; index2++)
			{
				this.FillWorldPositionEffects(playerControllerRepositoryService.ActivePlayerController.Empire, filteredPositions[index2], true);
			}
			this.FillOutputString(ref this.stringBuilder);
			this.NewResult.Text = this.stringBuilder.ToString();
			this.OldResultNotAffected.Visible = (filteredPositions.Count == 0);
			this.NewResultNotAffected.Visible = (filteredPositions.Count == 0);
		}
		yield return base.OnShow(parameters);
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		if (this.districtProxy != null && this.districtProxy.Parent != null)
		{
			this.districtProxy.Parent.RemoveChild_ModifierForwardType_ChildrenOnly(this.districtProxy);
		}
		if (this.cityProxy != null && this.cityProxy.Parent != null)
		{
			this.cityProxy.Parent.RemoveChild(this.cityProxy);
		}
		yield return base.OnHide(instant);
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.worldPositionningService = null;
		if (this.districtProxy != null)
		{
			this.districtProxy.Dispose();
			this.districtProxy = null;
		}
		if (this.cityProxy != null)
		{
			this.cityProxy.Dispose();
			this.cityProxy = null;
		}
		base.OnUnloadGame(game);
	}

	private void FillWorldPositionEffects(Amplitude.Unity.Game.Empire activeEmpire, WorldPosition worldPosition, bool showTerraformDescriptors = false)
	{
		SimulationObject simulationObject = null;
		if (this.worldPositionningService.IsExploitable(worldPosition, activeEmpire.Bits))
		{
			Region region = this.worldPositionningService.GetRegion(worldPosition);
			bool flag = region != null && region.BelongToEmpire(activeEmpire as global::Empire);
			if (!region.IsOcean && !region.IsWasteland && simulationObject == null)
			{
				this.districtProxy.RemoveAllDescriptors_ModifierForwardType_ChildrenOnly();
				DepartmentOfTheInterior.ApplyDistrictProxyDescriptors(activeEmpire, this.districtProxy, worldPosition, DistrictType.Exploitation, true, showTerraformDescriptors);
				if (flag)
				{
					if (this.districtProxy.Parent != region.City.SimulationObject)
					{
						region.City.SimulationObject.AddChild_ModifierForwardType_ChildrenOnly(this.districtProxy);
					}
					region.City.Refresh(true);
				}
				else
				{
					if (this.districtProxy.Parent != this.cityProxy)
					{
						this.cityProxy.AddChild_ModifierForwardType_ChildrenOnly(this.districtProxy);
					}
					if (this.cityProxy.Parent != activeEmpire.SimulationObject)
					{
						activeEmpire.SimulationObject.AddChild(this.cityProxy);
					}
					this.cityProxy.Refresh();
				}
				simulationObject = this.districtProxy;
				simulationObject.Refresh();
			}
		}
		if (simulationObject != null)
		{
			for (int i = 0; i < this.properties.Count; i++)
			{
				this.propertyValues[i] += simulationObject.GetPropertyValue(this.properties[i]);
			}
		}
	}

	private void ResetPropertyValues()
	{
		for (int i = 0; i < this.properties.Count; i++)
		{
			this.propertyValues[i] = 0f;
		}
	}

	private void FillOutputString(ref StringBuilder stringBuilder)
	{
		for (int i = 0; i < this.properties.Count; i++)
		{
			float num = this.propertyValues[i];
			if (num != 0f)
			{
				string arg;
				if (base.GuiService.TryFormatSymbol(this.symbols[i], out arg, true))
				{
					if (i > 0)
					{
						stringBuilder.Append("  ");
					}
					stringBuilder.AppendFormat("{0} {1}", num, arg);
				}
			}
		}
	}

	public AgePrimitiveLabel OldResult;

	public AgePrimitiveLabel NewResult;

	public AgeTransform OldResultNotAffected;

	public AgeTransform NewResultNotAffected;

	private static int uniqueId;

	private List<StaticString> properties = new List<StaticString>();

	private List<StaticString> symbols = new List<StaticString>();

	private IWorldPositionningService worldPositionningService;

	private SimulationObject cityProxy;

	private SimulationObject districtProxy;

	private StringBuilder stringBuilder;

	private float[] propertyValues;
}
