using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Options;

public interface IGuiSettingsService : IService
{
	[OptionTypeConstrained]
	[OptionTypeToggle("HighDefinitionUI", Priority = 10, Latent = true)]
	bool HighDefinitionUI { get; set; }

	[OptionTypeEmpireColorPaletteDropList("EmpireColorPalette", Priority = 20, Latent = false)]
	[OptionTypeConstrained]
	string EmpireColorPalette { get; set; }

	[OptionTypeDropList("CapacityColor1", "0,1,2,3,4,5,6,7,8,9,10,11,12", Priority = 25, Latent = true)]
	int CapacityColor1 { get; set; }

	[OptionTypeDropList("CapacityColor2", "0,1,2,3,4,5,6,7,8,9,10,11,12", Priority = 30, Latent = true)]
	int CapacityColor2 { get; set; }

	[OptionTypeDropList("CapacityColor3", "0,1,2,3,4,5,6,7,8,9,10,11,12", Priority = 35, Latent = true)]
	int CapacityColor3 { get; set; }
}
