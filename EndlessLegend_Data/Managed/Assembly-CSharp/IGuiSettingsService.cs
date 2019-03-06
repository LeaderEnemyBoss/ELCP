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
}
