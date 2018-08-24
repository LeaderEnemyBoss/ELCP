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
}
