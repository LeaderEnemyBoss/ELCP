using System;
using Amplitude.Unity.Gui;
using UnityEngine;

public class DeviceSublabel : MonoBehaviour
{
	public TerraformDevice Device { get; private set; }

	public float Height
	{
		get
		{
			return this.AgeTransform.Height;
		}
	}

	public float Width
	{
		get
		{
			return this.AgeTransform.Width;
		}
	}

	public float LeftOffset
	{
		get
		{
			return this.Width * 0.5f;
		}
	}

	public void Bind(TerraformDevice device, Amplitude.Unity.Gui.IGuiService guiService)
	{
		if (this.Device != null && this.Device != device)
		{
			this.Unbind();
		}
		this.Device = device;
	}

	public void Unbind()
	{
		this.Device = null;
	}

	public void RefreshContent()
	{
		if (this.Device == null)
		{
			return;
		}
		Color tintColor = Color.gray;
		if (this.Device.Empire != null && !this.Device.PlacedByPrivateers)
		{
			tintColor = this.Device.Empire.Color;
		}
		this.CoverCircle.TintColor = tintColor;
		this.ChargesGauge.TintColor = tintColor;
		float num;
		float num2;
		float num3;
		float num4;
		if (this.Device.DismantlingArmyGUID.IsValid)
		{
			num = this.Device.Charges + this.Device.DismantleDefense;
			num2 = this.Device.ChargesWhenDismantleStarted + this.Device.DismantleDefense;
			if (num2 == 0f)
			{
				num2 = 1f;
			}
			num3 = num;
			num4 = num;
			num3 -= this.Device.DismantlingArmy.GetPropertyValue(SimulationProperties.DeviceDismantlePower);
			if (this.Device.DismantlingArmy.IsPrivateers)
			{
				this.ChargesProgress.TintColor = this.GrayColor;
				this.ChargesIncrement.TintColor = this.GrayColor;
			}
			else
			{
				this.ChargesProgress.TintColor = this.Device.DismantlingArmy.Empire.Color;
				this.ChargesIncrement.TintColor = this.Device.DismantlingArmy.Empire.Color;
			}
			this.ChargingIcon.AgeTransform.Visible = false;
			this.DismantlingIcon.AgeTransform.Visible = true;
			this.DurationLabel.Text = ((this.Device.TurnsToActivate() <= 2000) ? this.Device.TurnsToDeactivate().ToString() : string.Empty) + "\\7708\\";
			this.tooltip = "%DeviceLabelDismantlingDeviceDescription";
		}
		else
		{
			num = this.Device.ChargesToActivate - this.Device.Charges;
			num2 = this.Device.ChargesToActivate;
			if (num2 == 0f)
			{
				num2 = 1f;
			}
			num3 = num;
			num4 = num;
			num3 -= this.Device.ChargesPerTurn;
			this.ChargesProgress.TintColor = this.GrayColor;
			this.ChargesIncrement.TintColor = this.GrayColor;
			this.DismantlingIcon.AgeTransform.Visible = false;
			this.ChargingIcon.AgeTransform.Visible = true;
			this.DurationLabel.Text = ((this.Device.TurnsToActivate() <= 2000) ? this.Device.TurnsToActivate().ToString() : string.Empty) + "\\7708\\";
			this.tooltip = "%DeviceLabelChargingDeviceDescription";
		}
		this.ChargesProgress.MaxAngle = Mathf.Round((1f - Mathf.Clamp(num / num2, 0f, 1f)) * 360f);
		this.ChargesIncrement.MinAngle = Mathf.Round((1f - Mathf.Clamp(num4 / num2, 0f, 1f)) * 360f);
		AgeModifierSector component = this.ChargesIncrement.GetComponent<AgeModifierSector>();
		component.StartMaxAngle = this.ChargesIncrement.MinAngle;
		component.EndMaxAngle = Mathf.Round((1f - Mathf.Clamp(num3 / num2, 0f, 1f)) * 360f);
		component = this.ChargesGauge.GetComponent<AgeModifierSector>();
		component.StartMinAngle = this.ChargesIncrement.MinAngle;
		component.EndMinAngle = Mathf.Round((1f - Mathf.Clamp(num3 / num2, 0f, 1f)) * 360f);
		if (this.AgeTransform.AgeTooltip != null)
		{
			this.AgeTransform.AgeTooltip.Content = this.tooltip;
		}
	}

	public AgeTransform AgeTransform;

	public AgeTransform DurationGroup;

	public AgePrimitiveLabel DurationLabel;

	public AgePrimitiveImage ChargingIcon;

	public AgePrimitiveImage DismantlingIcon;

	public AgePrimitiveSector ChargesGauge;

	public AgePrimitiveSector ChargesProgress;

	public AgePrimitiveSector ChargesIncrement;

	public AgePrimitiveArc CoverCircle;

	public Color ChargingColor = Color.green;

	public Color DismantlingColor = Color.red;

	public Color GrayColor = new Color(178f, 178f, 178f);

	private string tooltip = string.Empty;
}
