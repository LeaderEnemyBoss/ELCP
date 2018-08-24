using System;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class CreateResources : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?CreateResources");
			base.Execute(context);
			base.ExecuteSubTask(new PrepareOceanicPOIDistribution());
			bool flag = base.Context.Configuration.IsDLCAvailable("NavalPack");
			if (flag)
			{
				base.ExecuteSubTask(new DistributeOceanicCitadels());
				base.ExecuteSubTask(new DistributeUniqueFacilities());
				base.ExecuteSubTask(new DistributeStrategicFacilities());
			}
			base.ExecuteSubTask(new DistributeStrategicResources());
			base.ExecuteSubTask(new DistributeLuxuryResources());
			if (flag)
			{
				base.ExecuteSubTask(new DistributeSunkenRuins());
			}
			foreach (OceanicFortress oceanicFortress in base.Context.OceanicFortresses)
			{
				base.Trace(string.Format("Oceanic Fortress in region {0}", oceanicFortress.OceanRegion.Id));
				foreach (PointOfInterestDefinition pointOfInterestDefinition in oceanicFortress.Facilities)
				{
					base.Trace(string.Format(" - Facility : {0}", pointOfInterestDefinition.TemplateName));
				}
			}
		}
	}
}
