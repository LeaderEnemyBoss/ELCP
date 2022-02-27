using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using UnityEngine;

public class CreepingNodeContructionCompletedLine : MonoBehaviour
{
	public WorldPosition CreepingNodeWorldPosition { get; private set; }

	public void RefreshContent(CreepingNode creepingNode, GameObject client)
	{
		if (creepingNode != null)
		{
			this.selectionClient = client;
			this.CreepingNodeWorldPosition = creepingNode.WorldPosition;
			this.DestroyedArmyIcon.Visible = false;
			this.title.AgeTransform.PixelMarginLeft = this.DestroyedArmyIcon.PixelMarginRight;
			string text = creepingNode.PointOfInterest.CreepingNodeImprovement.Name;
			GuiElement guiElement;
			if (Services.GetService<global::IGuiService>().GuiPanelHelper.TryGetGuiElement(creepingNode.PointOfInterest.CreepingNodeImprovement.Name, out guiElement))
			{
				text = guiElement.Title;
				if (guiElement is ExtendedGuiElement)
				{
					ExtendedGuiElement extendedGuiElement = guiElement as ExtendedGuiElement;
					if (extendedGuiElement.TooltipElement != null)
					{
						text = extendedGuiElement.Title;
					}
				}
			}
			this.title.Text = text;
			this.ShowLocationAttackerButton.Visible = false;
			this.ShowLocationAttackerButton.Enable = false;
		}
	}

	private void OnShowLocationArmyCB()
	{
		this.selectionClient.SendMessage("OnShowLocation", this.CreepingNodeWorldPosition);
	}

	public AgePrimitiveLabel title;

	public AgeTransform ShowLocationAttackerButton;

	public AgeTransform DestroyedArmyIcon;

	private GameObject selectionClient;
}
