using System;
using Amplitude.WorldGenerator.World;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class CreateEmpires : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?CreateEmpires");
			base.Execute(context);
			base.ExecuteSubTask(new SelectSpawnRegions());
		}
	}
}
