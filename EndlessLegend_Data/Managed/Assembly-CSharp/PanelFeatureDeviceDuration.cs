using System;
using System.Collections;
using Amplitude.Unity.Gui;

public class PanelFeatureDeviceDuration : GuiPanelFeature
{
	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		if (this.context == null || !(this.context is TerraformDevice))
		{
			base.AgeTransform.Visible = false;
		}
		else
		{
			TerraformDevice terraformDevice = this.context as TerraformDevice;
			bool isValid = terraformDevice.DismantlingArmyGUID.IsValid;
			int num = (!isValid) ? terraformDevice.TurnsToActivate() : terraformDevice.TurnsToDeactivate();
			string text;
			if (num == 1)
			{
				text = AgeLocalizer.Instance.LocalizeString((!isValid) ? "%FeatureDeviceChargingDurationSingle" : "%FeatureDeviceDismantlingDurationSingle");
			}
			else if (num % 10 == 0)
			{
				text = AgeLocalizer.Instance.LocalizeString((!isValid) ? "%FeatureDeviceChargingDurationTen" : "%FeatureDeviceDismantlingDurationTen");
			}
			else if (num > 1999)
			{
				text = AgeLocalizer.Instance.LocalizeString((!isValid) ? "%FeatureDeviceChargingInfinite" : "%FeatureDeviceDismantlingDurationSingle");
			}
			else
			{
				text = AgeLocalizer.Instance.LocalizeString((!isValid) ? "%FeatureDeviceChargingDurationPlural" : "%FeatureDeviceDismantlingDurationPlural");
			}
			this.Title.Text = text.Replace("$(Input)", num.ToString());
			base.AgeTransform.Visible = true;
		}
		yield break;
	}

	public AgePrimitiveLabel Title;
}
