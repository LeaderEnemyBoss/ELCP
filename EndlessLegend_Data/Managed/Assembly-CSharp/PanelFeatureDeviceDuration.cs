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
			TerraformDevice device = this.context as TerraformDevice;
			bool dismantling = device.DismantlingArmyGUID.IsValid;
			int duration = (!dismantling) ? device.TurnsToActivate() : device.TurnsToDeactivate();
			string format = "$(Input)";
			if (duration == 1)
			{
				format = AgeLocalizer.Instance.LocalizeString((!dismantling) ? "%FeatureDeviceChargingDurationSingle" : "%FeatureDeviceDismantlingDurationSingle");
			}
			else if (duration % 10 == 0)
			{
				format = AgeLocalizer.Instance.LocalizeString((!dismantling) ? "%FeatureDeviceChargingDurationTen" : "%FeatureDeviceDismantlingDurationTen");
			}
			else if (duration > 1999)
			{
				format = AgeLocalizer.Instance.LocalizeString((!dismantling) ? "%FeatureDeviceChargingInfinite" : "%FeatureDeviceDismantlingDurationSingle");
			}
			else
			{
				format = AgeLocalizer.Instance.LocalizeString((!dismantling) ? "%FeatureDeviceChargingDurationPlural" : "%FeatureDeviceDismantlingDurationPlural");
			}
			this.Title.Text = format.Replace("$(Input)", duration.ToString());
			base.AgeTransform.Visible = true;
		}
		yield break;
	}

	public AgePrimitiveLabel Title;
}
