using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Amplitude.WorldGenerator.Tmx;
using Amplitude.WorldGenerator.World;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class RetrieveSettings : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Execute(context);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(WorldGeneratorSettings));
			XmlDocument xmlDocument = new XmlDocument();
			using (Stream stream = new FileStream(base.Context.Configuration.SettingsPath, FileMode.Open, FileAccess.Read))
			{
				xmlDocument.Load(stream);
			}
			WorldGeneratorSettings worldGeneratorSettings;
			using (Stream stream2 = new FileStream(base.Context.Configuration.SettingsPath, FileMode.Open, FileAccess.Read))
			{
				worldGeneratorSettings = (xmlSerializer.Deserialize(stream2) as WorldGeneratorSettings);
			}
			if (worldGeneratorSettings == null)
			{
				Diagnostics.LogError("[WorldGenerator] <" + base.ShortName + "> Failed to deserialize settings!");
			}
			else
			{
				base.Context.Settings = worldGeneratorSettings;
				base.Context.Settings.Document = xmlDocument;
				base.Context.Configuration.ApplyProperties(worldGeneratorSettings);
				base.Context.Configuration.OverridePOITemplates(worldGeneratorSettings);
				base.Context.Settings.Scenario = base.Context.Configuration.Scenario;
			}
			if (base.Context.Configuration.Scenario != null && !string.IsNullOrEmpty(base.Context.Configuration.Scenario.DirectoryName) && !string.IsNullOrEmpty(base.Context.Configuration.Scenario.TmxMapName))
			{
				xmlSerializer = new XmlSerializer(typeof(Map));
				string path = base.Context.Configuration.Scenario.DirectoryName + "\\" + base.Context.Configuration.Scenario.TmxMapName;
				if (!File.Exists(path))
				{
					base.ReportTmx("?InvalidTmxPath");
					throw new TmxImportException();
				}
				Map map;
				using (Stream stream3 = File.OpenRead(path))
				{
					map = (xmlSerializer.Deserialize(stream3) as Map);
				}
				if (map == null)
				{
					Diagnostics.LogError("[WorldGenerator] <" + base.ShortName + "> Failed to deserialize Tmx map!");
				}
				base.Context.Settings.TmxMap = map;
				base.Context.Settings.Width = map.Width;
				base.Context.Settings.Height = map.Height;
				if (map.Orientation != "hexagonal")
				{
					base.ReportTmx("?NotHexagonalMap");
				}
				if (map.Width % 2 != 0)
				{
					base.ReportTmx("?NoWrapOnOddColumns");
				}
				if (map.StaggerAxis != "y")
				{
					base.ReportTmx("?WrongStaggerAxis");
				}
				if (map.StaggerIndex != "even")
				{
					base.ReportTmx("?WrongStaggerIndex");
				}
				bool flag = false;
				int index2;
				int index;
				for (index = 0; index < Map.LayerNames.Length; index = index2 + 1)
				{
					if (map.Layers.Find((Layer layer) => layer.Name == Map.LayerNames[index]) == null)
					{
						base.ReportTmx("?MissingLayer&$MissingLayer=" + Map.LayerNames[index]);
						flag = true;
					}
					if (map.Tilesets.Find((Tileset tilset) => tilset.Name == Map.LayerNames[index]) == null)
					{
						base.ReportTmx("?MissingTileset&$MissingTileset=" + Map.LayerNames[index]);
					}
					index2 = index;
				}
				if (flag)
				{
					throw new TmxImportException();
				}
			}
			if (base.Context.Settings.Seed == 0)
			{
				Random random = new Random();
				base.Context.Settings.Seed = random.Next();
				Diagnostics.Log("[WorldGenerator] Using the random seed " + base.Context.Settings.Seed + "...");
			}
		}
	}
}
