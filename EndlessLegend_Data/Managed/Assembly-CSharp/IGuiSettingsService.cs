using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Options;

public interface IGuiSettingsService : IService
{
	[OptionTypeConstrained]
	[OptionTypeToggle("HighDefinitionUI", Priority = 10, Latent = true)]
	bool HighDefinitionUI { get; set; }

	[OptionTypeConstrained]
	[OptionTypeEmpireColorPaletteDropList("EmpireColorPalette", Priority = 20, Latent = false)]
	string EmpireColorPalette { get; set; }

	[OptionTypeDropList("CapacityColor2", "0,1,2,3,4,5,6,7,8,9,10,11,12", Priority = 30, Latent = true)]
	int CapacityColor2 { get; set; }

	[OptionTypeDropList("CapacityColor1", "0,1,2,3,4,5,6,7,8,9,10,11,12", Priority = 25, Latent = true)]
	int CapacityColor1 { get; set; }

	[OptionTypeDropList("CapacityColor3", "0,1,2,3,4,5,6,7,8,9,10,11,12", Priority = 35, Latent = true)]
	int CapacityColor3 { get; set; }

	[OptionTypeSlider("ZoomRatioDetailsBecomeAbstract", Priority = 50, Latent = true, MinValue = 0.2f, MaxValue = 0.9f, Default = 0.5f, Increment = 0.01f)]
	float ZoomRatioDetailsBecomeAbstract { get; set; }
}
