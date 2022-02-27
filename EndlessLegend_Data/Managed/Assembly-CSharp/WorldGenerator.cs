using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Amplitude;
using Amplitude.Threading;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Localization;
using Amplitude.Unity.Session;
using Amplitude.WorldGenerator;
using Amplitude.WorldGenerator.Embedded;
using UnityEngine;

[Amplitude.Diagnostics.TagAttribute("WorldGenerator")]
public class WorldGenerator : IDisposable
{
	public WorldGenerator()
	{
		this.ConfigurationPath = System.IO.Path.Combine(Amplitude.Unity.Framework.Application.TempDirectory, "WorldGeneratorConfiguration.xml");
	}

	private ILocalizationService LocalizationService { get; set; }

	public static void CheckWorldGeneratorOptionsConstraints()
	{
		ISessionService service = Services.GetService<ISessionService>();
		if (service != null && service.Session != null)
		{
			IDatabase<WorldGeneratorOptionDefinition> database = Databases.GetDatabase<WorldGeneratorOptionDefinition>(false);
			if (database != null)
			{
				List<WorldGeneratorOptionDefinition> list = database.GetValues().ToList<WorldGeneratorOptionDefinition>();
				list.Sort(new WorldGenerator.WorldGeneratorOptionDefinitionComparer());
				for (int i = 0; i < list.Count; i++)
				{
					string text = list[i].Name;
					string lobbyData = service.Session.GetLobbyData<string>(text, null);
					if (string.IsNullOrEmpty(lobbyData))
					{
						string text2 = list[i].DefaultName;
						service.Session.SetLobbyData(text, text2, true);
						Amplitude.Diagnostics.LogWarning("Missing value for option (name: '{0}') has been reset to default (value: '{1}').", new object[]
						{
							text,
							text2
						});
					}
					else if (list[i].ItemDefinitions != null)
					{
						bool flag = false;
						for (int j = 0; j < list[i].ItemDefinitions.Length; j++)
						{
							if (list[i].ItemDefinitions[j].Name == lobbyData)
							{
								flag = true;
								OptionDefinition.ItemDefinition itemDefinition = list[i].ItemDefinitions[j];
								if (itemDefinition.OptionDefinitionConstraints != null)
								{
									foreach (OptionDefinitionConstraint optionDefinitionConstraint in from iterator in itemDefinition.OptionDefinitionConstraints
									where iterator.Type == OptionDefinitionConstraintType.Control
									select iterator)
									{
										string text3 = optionDefinitionConstraint.OptionName;
										if (string.IsNullOrEmpty(text3))
										{
											Amplitude.Diagnostics.LogWarning("Invalid null or empty option name for constraint (item: '{0'}', index: '{1}') in option (name: '{2}').", new object[]
											{
												itemDefinition.Name,
												Array.IndexOf<OptionDefinitionConstraint>(itemDefinition.OptionDefinitionConstraints, optionDefinitionConstraint),
												list[i].Name
											});
										}
										else
										{
											string lobbyData2 = service.Session.GetLobbyData<string>(text3, null);
											if (!optionDefinitionConstraint.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(lobbyData2))
											{
												string text4 = optionDefinitionConstraint.Keys[0].Name;
												service.Session.SetLobbyData(text3, text4, true);
												Amplitude.Diagnostics.LogWarning("Option (name: '{0}') has been control-constrained (new value: '{1}').", new object[]
												{
													text3,
													text4
												});
												for (int k = 0; k < list.Count; k++)
												{
													if (list[k].Name == text3)
													{
														if (k < i)
														{
															Amplitude.Diagnostics.Log("Rollback has been triggered.");
														}
														break;
													}
												}
											}
										}
									}
									foreach (OptionDefinitionConstraint optionDefinitionConstraint2 in from iterator in itemDefinition.OptionDefinitionConstraints
									where iterator.Type == OptionDefinitionConstraintType.Conditional
									select iterator)
									{
										string text5 = optionDefinitionConstraint2.OptionName;
										if (!string.IsNullOrEmpty(text5))
										{
											string lobbyData3 = service.Session.GetLobbyData<string>(text5, null);
											if (!optionDefinitionConstraint2.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(lobbyData3))
											{
												bool flag2 = false;
												foreach (OptionDefinition.ItemDefinition itemDefinition2 in list[i].ItemDefinitions)
												{
													if (!(itemDefinition2.Name == lobbyData))
													{
														bool flag3 = false;
														if (itemDefinition2.OptionDefinitionConstraints == null)
														{
															flag3 = true;
														}
														else
														{
															foreach (OptionDefinitionConstraint optionDefinitionConstraint3 in itemDefinition2.OptionDefinitionConstraints.Where((OptionDefinitionConstraint iterator) => iterator.Type == OptionDefinitionConstraintType.Conditional))
															{
																text5 = optionDefinitionConstraint3.OptionName;
																if (!string.IsNullOrEmpty(text5))
																{
																	lobbyData3 = service.Session.GetLobbyData<string>(text5, null);
																	if (optionDefinitionConstraint2.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(lobbyData3))
																	{
																		flag3 = true;
																		break;
																	}
																}
															}
														}
														if (flag3)
														{
															service.Session.SetLobbyData(text, itemDefinition2.Name, true);
															Amplitude.Diagnostics.LogWarning("Option (name: '{0}') has been condition-constrained (new value: '{1}').", new object[]
															{
																text,
																itemDefinition2.Name
															});
															flag2 = true;
															break;
														}
													}
												}
												if (flag2)
												{
													break;
												}
											}
										}
									}
								}
							}
						}
						if (!flag)
						{
							string text6 = list[i].DefaultName;
							service.Session.SetLobbyData(text, text6, true);
							Amplitude.Diagnostics.LogWarning("Invalid value for option (name: '{0}', value: '{1}') has been reset to default (value: '{2}').", new object[]
							{
								text,
								lobbyData,
								text6
							});
						}
					}
				}
			}
		}
	}

	public void Dispose()
	{
		if (this.process != null && !this.process.HasExited)
		{
			this.process.Kill();
		}
	}

	public virtual IEnumerator GenerateWorld()
	{
		this.StartProgress(WorldGenerator.GenerateWorldMessage, "%StartingTheWorldGenerationProcess");
		yield return this.StartProcess(new string[0]);
		Amplitude.Diagnostics.Log("The world generation process has finished.");
		yield break;
	}

	public virtual IEnumerator GenerateWorldGeometry()
	{
		this.StartProgress(WorldGenerator.GenerateWorldGeometryMessage, "%StartingTheWorldGeometryGenerationProcess");
		yield return this.StartProcess(new string[]
		{
			"-geometry-only"
		});
		Amplitude.Diagnostics.Log("The world geometry generation process has finished.");
		yield break;
	}

	public string GetOuputPath()
	{
		string fullPath = Amplitude.Unity.Framework.Path.GetFullPath(this.ConfigurationPath);
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			xmlDocument.Load(fullPath);
			XmlNodeList xmlNodeList = xmlDocument.SelectNodes("/WorldGeneratorConfiguration/OutputPath");
			if (xmlNodeList != null)
			{
				Amplitude.Diagnostics.Assert(xmlNodeList.Count == 1, "There can be only one output path.");
				using (IEnumerator enumerator = xmlNodeList.GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						XmlNode xmlNode = (XmlNode)enumerator.Current;
						string innerText = xmlNode.InnerText;
						if (System.IO.Path.IsPathRooted(innerText))
						{
							return innerText;
						}
						return Amplitude.Unity.Framework.Path.GetFullPath(this.WorldGeneratorDirectory + xmlNode.InnerText);
					}
				}
			}
			else
			{
				Amplitude.Diagnostics.LogError("Can't find output path in the configuration file.");
			}
		}
		catch (Exception ex)
		{
			Amplitude.Diagnostics.LogError("Can't load configuration file : " + ex.Message);
		}
		return string.Empty;
	}

	public IEnumerator WriteConfigurationFile()
	{
		global::WorldGeneratorConfiguration configuration = new global::WorldGeneratorConfiguration();
		Amplitude.Unity.Session.Session session = null;
		ISessionService sessionService = Services.GetService<ISessionService>();
		if (sessionService != null)
		{
			session = sessionService.Session;
		}
		IDatabase<WorldGeneratorOptionDefinition> worldGeneratorOptionDefinitionDatabase = Databases.GetDatabase<WorldGeneratorOptionDefinition>(false);
		if (worldGeneratorOptionDefinitionDatabase != null)
		{
			System.Random random = new System.Random();
			string seedChoice = session.GetLobbyData<string>("SeedChoice", null);
			WorldGeneratorOptionDefinition optionDefinitionSeedNumber;
			if (seedChoice == "User" && worldGeneratorOptionDefinitionDatabase.TryGetValue("SeedNumber", out optionDefinitionSeedNumber))
			{
				Amplitude.Diagnostics.Assert(optionDefinitionSeedNumber.ItemDefinitions.Length == 1);
				Amplitude.Diagnostics.Assert(optionDefinitionSeedNumber.ItemDefinitions[0].KeyValuePairs.Length == 1);
				string value = optionDefinitionSeedNumber.ItemDefinitions[0].KeyValuePairs[0].Value;
				int integer;
				if (int.TryParse(value, out integer))
				{
					random = new System.Random(integer);
				}
			}
			Dictionary<OptionDefinition, OptionDefinition.ItemDefinition> optionAndItemCollection = new Dictionary<OptionDefinition, OptionDefinition.ItemDefinition>();
			OptionDefinition[] optionDefinitions = worldGeneratorOptionDefinitionDatabase.GetValues();
			for (int index = 0; index < optionDefinitions.Length; index++)
			{
				if (optionDefinitions[index] is WorldGeneratorOptionDefinition)
				{
					OptionDefinition.ItemDefinition optionItemDefinition = null;
					if (session != null)
					{
						string itemName = session.GetLobbyData<string>(optionDefinitions[index].Name, optionDefinitions[index].DefaultName);
						if (!string.IsNullOrEmpty(itemName))
						{
							optionItemDefinition = Array.Find<OptionDefinition.ItemDefinition>(optionDefinitions[index].ItemDefinitions, (OptionDefinition.ItemDefinition iterator) => iterator.Name == itemName);
							if (itemName == "Random")
							{
								OptionDefinition.ItemDefinition[] itemDefinitions = (from iterator in optionDefinitions[index].ItemDefinitions
								where iterator.Name != "Custom" && iterator.Name != "Random"
								select iterator).ToArray<OptionDefinition.ItemDefinition>();
								if (itemDefinitions.Length > 0)
								{
									List<OptionDefinition.ItemDefinition> authorizedItemDefinitions = new List<OptionDefinition.ItemDefinition>();
									authorizedItemDefinitions.AddRange(itemDefinitions);
									for (int i = itemDefinitions.Length - 1; i >= 0; i--)
									{
										if (itemDefinitions[i].OptionDefinitionConstraints != null)
										{
											bool authorized = true;
											foreach (OptionDefinitionConstraint optionDefinitionConstraint in from iterator in itemDefinitions[i].OptionDefinitionConstraints
											where iterator.Type == OptionDefinitionConstraintType.Conditional
											select iterator)
											{
												if (string.IsNullOrEmpty(optionDefinitionConstraint.OptionName))
												{
													authorized = false;
													break;
												}
												string constrainedValue = session.GetLobbyData<string>(optionDefinitionConstraint.OptionName, null);
												if (string.IsNullOrEmpty(constrainedValue))
												{
													Amplitude.Diagnostics.LogWarning("Unhandled constraint on option '{0}' from option definition '{1}', item '{2}'.", new object[]
													{
														optionDefinitionConstraint.OptionName,
														optionDefinitions[index].Name,
														optionItemDefinition.Name
													});
												}
												else
												{
													if (optionDefinitionConstraint.Keys == null || optionDefinitionConstraint.Keys.Length == 0)
													{
														authorized = false;
														break;
													}
													if (!optionDefinitionConstraint.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(constrainedValue))
													{
														authorized = false;
														break;
													}
												}
											}
											if (!authorized)
											{
												authorizedItemDefinitions.RemoveAt(i);
											}
										}
									}
									int selection = random.Next() % authorizedItemDefinitions.Count;
									optionItemDefinition = authorizedItemDefinitions[selection];
								}
							}
						}
					}
					if (optionItemDefinition == null)
					{
						optionItemDefinition = optionDefinitions[index].Default;
					}
					if (optionItemDefinition != null)
					{
						optionAndItemCollection.Add(optionDefinitions[index], optionItemDefinition);
					}
				}
			}
			for (int jndex = 0; jndex < optionDefinitions.Length; jndex++)
			{
				if (optionDefinitions[jndex] is WorldGeneratorOptionDefinition)
				{
					OptionDefinition.ItemDefinition optionItemDefinition2 = null;
					if (optionAndItemCollection.TryGetValue(optionDefinitions[jndex], out optionItemDefinition2))
					{
						if (optionItemDefinition2.OptionDefinitionConstraints != null)
						{
							foreach (OptionDefinitionConstraint optionDefinitionConstraint2 in from element in optionItemDefinition2.OptionDefinitionConstraints
							where element.Type == OptionDefinitionConstraintType.Control
							select element)
							{
								if (optionDefinitionConstraint2.Keys != null && optionDefinitionConstraint2.Keys.Length != 0)
								{
									KeyValuePair<OptionDefinition, OptionDefinition.ItemDefinition> constrainedOptionAndItem = optionAndItemCollection.FirstOrDefault((KeyValuePair<OptionDefinition, OptionDefinition.ItemDefinition> iterator) => iterator.Key.Name == optionDefinitionConstraint2.OptionName);
									if (constrainedOptionAndItem.Key != null)
									{
										if (!optionDefinitionConstraint2.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(constrainedOptionAndItem.Value.Name))
										{
											OptionDefinition.ItemDefinition constrainedItemOptionDefinition = constrainedOptionAndItem.Key.ItemDefinitions.FirstOrDefault((OptionDefinition.ItemDefinition iterator) => iterator.Name == optionDefinitionConstraint2.Keys[0].Name);
											if (constrainedItemOptionDefinition != null)
											{
												optionAndItemCollection[constrainedOptionAndItem.Key] = constrainedItemOptionDefinition;
												session.SetLobbyData(constrainedOptionAndItem.Key.Name, constrainedItemOptionDefinition.Name.ToString(), true);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			for (int kndex = 0; kndex < optionDefinitions.Length; kndex++)
			{
				if (optionDefinitions[kndex] is WorldGeneratorOptionDefinition)
				{
					OptionDefinition.ItemDefinition optionItemDefinition3 = null;
					if (optionAndItemCollection.TryGetValue(optionDefinitions[kndex], out optionItemDefinition3))
					{
						if (optionItemDefinition3.KeyValuePairs != null)
						{
							if (!optionItemDefinition3.KeyValuePairs.Any((OptionDefinition.ItemDefinition.KeyValuePair iterator) => iterator.Key == optionDefinitions[kndex].Name))
							{
								configuration.Properties.Add(optionDefinitions[kndex].Name, optionItemDefinition3.Name);
							}
							foreach (OptionDefinition.ItemDefinition.KeyValuePair keyValuePair in optionItemDefinition3.KeyValuePairs)
							{
								if (!configuration.Properties.ContainsKey(keyValuePair.Key))
								{
									configuration.Properties.Add(keyValuePair.Key, keyValuePair.Value);
								}
							}
						}
						else
						{
							configuration.Properties.Add(optionDefinitions[kndex].Name, optionItemDefinition3.Name);
						}
					}
				}
			}
			IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
			configuration.Properties.Add("DownloadableContent16", downloadableContentService.IsShared(DownloadableContent16.ReadOnlyName).ToString());
			configuration.Properties.Add("DownloadableContent19", downloadableContentService.IsShared(DownloadableContent19.ReadOnlyName).ToString());
			configuration.Properties.Add("DownloadableContent20", downloadableContentService.IsShared(DownloadableContent20.ReadOnlyName).ToString());
			configuration.Properties.Add("DownloadableContent21", downloadableContentService.IsShared(DownloadableContent21.ReadOnlyName).ToString());
			IAdvancedVideoSettingsService advancedVideoSettingsService = Services.GetService<IAdvancedVideoSettingsService>();
			if (advancedVideoSettingsService != null)
			{
				StaticString worldGeometryTypeName = "Geometry";
				string optionValue = advancedVideoSettingsService.WorldGeometryType;
				string worldGeometryTypeValue = optionValue;
				configuration.Properties[worldGeometryTypeName] = worldGeometryTypeValue;
				WorldGeneratorOptionDefinition worldGeneratorOptionDefinition;
				if (worldGeneratorOptionDefinitionDatabase.TryGetValue("Geometry", out worldGeneratorOptionDefinition) && worldGeneratorOptionDefinition.ItemDefinitions != null)
				{
					int optionIndex = -1;
					for (int itemPosition = 0; itemPosition < worldGeneratorOptionDefinition.ItemDefinitions.Length; itemPosition++)
					{
						if (worldGeneratorOptionDefinition.ItemDefinitions[itemPosition].Name == optionValue)
						{
							optionIndex = itemPosition;
							break;
						}
					}
					OptionDefinition.ItemDefinition itemDefinition = (optionIndex < 0) ? worldGeneratorOptionDefinition.Default : worldGeneratorOptionDefinition.ItemDefinitions[optionIndex];
					for (int keyValuePairIndex = 0; keyValuePairIndex < itemDefinition.KeyValuePairs.Length; keyValuePairIndex++)
					{
						configuration.Properties[itemDefinition.KeyValuePairs[keyValuePairIndex].Key] = itemDefinition.KeyValuePairs[keyValuePairIndex].Value;
					}
				}
			}
			else
			{
				Amplitude.Diagnostics.Assert(false);
			}
		}
		string worldGeneratorConfigurationPath = Amplitude.Unity.Framework.Path.GetFullPath(this.ConfigurationPath);
		string worldGeneratorConfigurationDirectory = System.IO.Path.GetDirectoryName(worldGeneratorConfigurationPath);
		if (!Directory.Exists(worldGeneratorConfigurationDirectory))
		{
			Directory.CreateDirectory(worldGeneratorConfigurationDirectory);
		}
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
		{
			Encoding = Encoding.UTF8,
			Indent = true,
			IndentChars = "  ",
			NewLineChars = "\r\n",
			NewLineHandling = NewLineHandling.Replace,
			OmitXmlDeclaration = false
		};
		using (StreamWriter stream = new StreamWriter(worldGeneratorConfigurationPath, false))
		{
			using (XmlWriter writer = XmlWriter.Create(stream, xmlWriterSettings))
			{
				writer.WriteStartDocument();
				configuration.WriteOuterXml(writer, null);
				writer.WriteEndDocument();
				writer.Flush();
				writer.Close();
			}
		}
		yield break;
	}

	private ProcessStartInfo CreateStartInfo()
	{
		string fullPath = Amplitude.Unity.Framework.Path.GetFullPath(this.GeneratorPath);
		if (!File.Exists(fullPath))
		{
			throw new WorldGeneratorException("The world generator executable file does not exist (path = '{0}').", new string[]
			{
				fullPath
			});
		}
		ProcessStartInfo processStartInfo = new ProcessStartInfo
		{
			CreateNoWindow = true,
			FileName = fullPath,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			UseShellExecute = false,
			WorkingDirectory = Amplitude.Unity.Framework.Path.GetFullPath(this.WorldGeneratorDirectory)
		};
		Amplitude.Diagnostics.Log("startInfo.FileName = " + processStartInfo.FileName);
		Amplitude.Diagnostics.Log("startInfo.WorkingDirectory = " + processStartInfo.WorkingDirectory);
		string fullPath2 = Amplitude.Unity.Framework.Path.GetFullPath(this.ConfigurationPath);
		if (!File.Exists(fullPath2))
		{
			throw new WorldGeneratorException("The world generator configuration file does not exist (path = '{0}').", new string[]
			{
				fullPath2
			});
		}
		processStartInfo.Arguments = string.Format("-config \"{0}\"", fullPath2);
		Amplitude.Diagnostics.Log("startInfo.Arguments = " + processStartInfo.Arguments);
		this.arguments = new string[]
		{
			"-config",
			fullPath2
		};
		return processStartInfo;
	}

	private void Diagnostics_ErrorMessage(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}
		if (message.Equals("[Tmx]"))
		{
			this.tmxImportErrorCatched = true;
			return;
		}
		if (!string.IsNullOrEmpty(this.errorData) && !this.errorData.EndsWith("\n"))
		{
			this.errorData += "\n";
		}
		this.errorData += message;
	}

	private void Diagnostics_Message(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}
		int num = message.IndexOf("[Report");
		if (num != -1)
		{
			this.logData.Add(message);
		}
		else
		{
			UnityEngine.Debug.Log(message);
		}
	}

	private void PrintLogData()
	{
		if (this.logData.Count > 0)
		{
			for (int i = 0; i < this.logData.Count; i++)
			{
				if (!string.IsNullOrEmpty(this.logData[i]))
				{
					int num = this.logData[i].IndexOf("[Report");
					if (num != -1)
					{
						int num2 = this.logData[i].IndexOf("] [Tmx]", num + 7);
						if (num2 != -1)
						{
							int num3 = num + 8;
							int length = num2 - num3;
							this.FormatTmxReport(this.logData[i].Substring(num3, length));
						}
						else
						{
							int num4 = this.logData[i].IndexOf(']', num + 7);
							if (num4 != -1)
							{
								int num5 = num4 - num + 1;
								if (num5 > 8)
								{
									string[] array = this.logData[i].Substring(num, num5).Split(WorldGenerator.Separators, StringSplitOptions.RemoveEmptyEntries);
									string text = "World Generation...";
									string text2 = this.logData[i];
									if (array.Length > 1)
									{
										string text3 = "%WorldGeneratorReport" + array[1];
										if (this.LocalizationService != null)
										{
											text = this.LocalizationService.Localize(WorldGenerator.Caption, text);
											text2 = this.LocalizationService.Localize(text3);
										}
										if (!string.IsNullOrEmpty(text2))
										{
											Amplitude.Diagnostics.Progress.SetProgress(0f, text2, text);
										}
										else
										{
											Amplitude.Diagnostics.LogWarning("Unable to find localization key [{0}] from WorldGenerator", new object[]
											{
												text3
											});
										}
									}
								}
							}
							Amplitude.Diagnostics.Log(this.logData[i]);
						}
					}
				}
			}
			this.logData.Clear();
		}
	}

	private void FormatTmxReport(string report)
	{
		string[] array = report.Split(WorldGenerator.Separators, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length <= 0)
		{
			return;
		}
		string text = this.LocalizationService.Localize(WorldGenerator.TmxErrorLocationPrefix + array[0]);
		if (array.Length == 1)
		{
			if (!string.IsNullOrEmpty(text))
			{
				if (!this.tmxImportReports.Contains(text))
				{
					this.tmxImportReports.Add(text);
				}
			}
			else if (!this.tmxImportReports.Contains(report))
			{
				this.tmxImportReports.Add(report);
			}
			return;
		}
		for (int i = 1; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(new char[]
			{
				'='
			});
			if (array2.Length == 2)
			{
				if (!string.IsNullOrEmpty(array2[0]) && !string.IsNullOrEmpty(array2[1]))
				{
					if (text.Contains(array2[0]))
					{
						text = text.Replace(array2[0], array2[1]);
					}
				}
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			if (!this.tmxImportReports.Contains(text))
			{
				this.tmxImportReports.Add(text);
			}
		}
		else if (!this.tmxImportReports.Contains(report))
		{
			this.tmxImportReports.Add(report);
		}
	}

	private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
	{
		if (e.Data == null)
		{
			return;
		}
		if (e.Data.Contains("[Tmx]"))
		{
			this.tmxImportErrorCatched = true;
			return;
		}
		if (!string.IsNullOrEmpty(this.errorData) && !this.errorData.EndsWith("\n"))
		{
			this.errorData += "\n";
		}
		this.errorData += e.Data;
	}

	private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
	{
		if (string.IsNullOrEmpty(e.Data))
		{
			return;
		}
		int num = e.Data.IndexOf("[Report");
		if (num != -1)
		{
			this.logData.Add(e.Data);
		}
		else
		{
			UnityEngine.Debug.Log(e.Data);
		}
	}

	private IEnumerator StartProcess(params string[] extraArguments)
	{
		this.errorData = string.Empty;
		this.logData.Clear();
		ProcessStartInfo startInfo = this.CreateStartInfo();
		if (extraArguments != null)
		{
			ProcessStartInfo processStartInfo = startInfo;
			processStartInfo.Arguments += " ";
			ProcessStartInfo processStartInfo2 = startInfo;
			processStartInfo2.Arguments += string.Join(" ", extraArguments);
		}
		bool useShellExecute = false;
		bool fileNameContainsSingleQuote = startInfo.FileName.Contains('\'');
		if (fileNameContainsSingleQuote)
		{
			Amplitude.Diagnostics.LogWarning("Using ShellExecute because a single quote was detected in the filename.");
			useShellExecute = true;
		}
		string commandLine = Environment.CommandLine.ToLowerInvariant();
		if (commandLine.Contains("-usewaitforseconds"))
		{
			yield return Amplitude.Coroutine.WaitForSeconds(2f);
		}
		if (commandLine.Contains("-useshellexecute"))
		{
			Amplitude.Diagnostics.LogWarning("Using ShellExecute because the command line contains the \"-useshellexecute\" directive.");
			useShellExecute = true;
		}
		if (useShellExecute)
		{
			startInfo.UseShellExecute = true;
			startInfo.RedirectStandardError = false;
			startInfo.RedirectStandardOutput = false;
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.CreateNoWindow = true;
			startInfo.ErrorDialog = true;
		}
		if (commandLine.Contains("-useembedded"))
		{
			Amplitude.Diagnostics.LogWarning("Using embedded world generation because the command line contains the \"-useembedded\" directive.");
			Amplitude.WorldGenerator.Diagnostics.Message += this.Diagnostics_Message;
			Amplitude.WorldGenerator.Diagnostics.ErrorMessage += this.Diagnostics_ErrorMessage;
			string[] arguments = this.arguments;
			if (extraArguments != null)
			{
				arguments = new string[this.arguments.Length + extraArguments.Length];
				Array.Copy(this.arguments, arguments, this.arguments.Length);
				Array.Copy(extraArguments, 0, arguments, this.arguments.Length, extraArguments.Length);
			}
			Thread thread = new Thread("Amplitude.WorldGenerator.Embedded", delegate()
			{
				Amplitude.WorldGenerator.Embedded.Process.Execute(arguments);
			});
			thread.Start();
			while (thread.IsAlive)
			{
				yield return null;
				this.PrintLogData();
				if (!string.IsNullOrEmpty(this.errorData) || this.tmxImportErrorCatched)
				{
					break;
				}
			}
			thread.Dispose();
			thread = null;
			Amplitude.WorldGenerator.Diagnostics.Message -= this.Diagnostics_Message;
			Amplitude.WorldGenerator.Diagnostics.ErrorMessage -= this.Diagnostics_ErrorMessage;
			Amplitude.Diagnostics.Progress.Clear();
			if (!string.IsNullOrEmpty(this.errorData))
			{
				throw new WorldGeneratorProcessException("The world generator process has encountered an error: {0}", new string[]
				{
					this.errorData
				});
			}
			if (this.tmxImportReports.Count > 0 && (this.tmxImportErrorCatched || global::Application.CommandLineArguments.EnableModdingTools))
			{
				this.tmxImportReports.Reverse();
				IGuiService guiService = Services.GetService<IGuiService>();
				guiService.Show(typeof(WorldGeneratorReportModalPanel), new object[]
				{
					this.tmxImportReports
				});
				if (this.tmxImportErrorCatched)
				{
					throw new WorldGeneratorTmxImportException("The world generator process has encountered error(s) while importing tmx file");
				}
			}
			yield break;
		}
		else
		{
			this.process = new System.Diagnostics.Process
			{
				EnableRaisingEvents = true,
				StartInfo = startInfo
			};
			this.process.ErrorDataReceived += this.Process_ErrorDataReceived;
			this.process.OutputDataReceived += this.Process_OutputDataReceived;
			try
			{
				Amplitude.Diagnostics.Log("Starting the world generator process...");
				bool started = this.process.Start();
				Amplitude.Diagnostics.Log("The world generator process has been started (with return value '{0}').", new object[]
				{
					started
				});
			}
			catch (Win32Exception ex)
			{
				Win32Exception exception = ex;
				Amplitude.Diagnostics.LogWarning("ErrorCode = " + exception.ErrorCode);
				Amplitude.Diagnostics.LogWarning("Message = " + exception.Message);
				Amplitude.Diagnostics.LogWarning("Source = " + exception.Source);
				Amplitude.Diagnostics.LogWarning("ToString() = " + exception.ToString());
				throw;
			}
			if (useShellExecute)
			{
				Amplitude.Diagnostics.LogWarning("Both standard output and error redirections are disabled when using ShellExecute.");
			}
			else
			{
				this.process.BeginOutputReadLine();
				this.process.BeginErrorReadLine();
			}
			while (!this.process.HasExited)
			{
				yield return null;
				this.PrintLogData();
				if (!string.IsNullOrEmpty(this.errorData) || this.tmxImportErrorCatched)
				{
					this.process.WaitForExit(500);
					break;
				}
			}
			if (this.process.HasExited)
			{
				Amplitude.Diagnostics.Log("The world generator process has exited with code '{0}'.", new object[]
				{
					this.process.ExitCode
				});
			}
			if (!string.IsNullOrEmpty(this.errorData))
			{
				Amplitude.Diagnostics.LogWarning("The world generator process has encountered an error: {0}", new object[]
				{
					this.errorData
				});
			}
			Amplitude.Diagnostics.Progress.Clear();
			this.PrintLogData();
			this.process.ErrorDataReceived -= this.Process_ErrorDataReceived;
			this.process.OutputDataReceived -= this.Process_OutputDataReceived;
			this.process.Close();
			this.process.Dispose();
			this.process = null;
			if (!string.IsNullOrEmpty(this.errorData))
			{
				throw new WorldGeneratorProcessException("The world generator process has encountered an error: {0}", new string[]
				{
					this.errorData
				});
			}
			if (this.tmxImportReports.Count > 0 && (this.tmxImportErrorCatched || global::Application.CommandLineArguments.EnableModdingTools))
			{
				this.tmxImportReports.Reverse();
				IGuiService guiService2 = Services.GetService<IGuiService>();
				guiService2.Show(typeof(WorldGeneratorReportModalPanel), new object[]
				{
					this.tmxImportReports
				});
				if (this.tmxImportErrorCatched)
				{
					throw new WorldGeneratorTmxImportException("The world generator process has encountered error(s) while importing tmx file");
				}
			}
			yield break;
		}
	}

	private void StartProgress(string key, string defaultMessage)
	{
		this.LocalizationService = Services.GetService<ILocalizationService>();
		string text = "World Generation...";
		string message = defaultMessage;
		if (this.LocalizationService != null)
		{
			text = this.LocalizationService.Localize(WorldGenerator.Caption, text);
			message = this.LocalizationService.Localize(key, defaultMessage);
		}
		Amplitude.Diagnostics.Progress.SetProgress(0f, message, text);
	}

	public static char[] Separators = new char[]
	{
		'[',
		']',
		'?',
		'&'
	};

	public static StaticString Caption = "%WorldGeneratorCaption";

	public static StaticString GenerateWorldMessage = "%GenerateWorldMessage";

	public static StaticString GenerateWorldGeometryMessage = "%GenerateWorldGeometryMessage";

	public static StaticString TmxErrorLocationPrefix = "%WorldGeneratorTmxReport";

	public readonly string WorldGeneratorDirectory = "Public/WorldGenerator/";

	public readonly string GeneratorPath = "Public/WorldGenerator/Amplitude.WorldGenerator.exe";

	public string ConfigurationPath = "Public/WorldGenerator/WorldGeneratorConfiguration.xml";

	private bool tmxImportErrorCatched;

	private string errorData;

	private List<string> logData = new List<string>();

	private List<string> tmxImportReports = new List<string>();

	private System.Diagnostics.Process process;

	private string[] arguments;

	private class WorldGeneratorOptionDefinitionComparer : Comparer<WorldGeneratorOptionDefinition>
	{
		public override int Compare(WorldGeneratorOptionDefinition left, WorldGeneratorOptionDefinition right)
		{
			if (left.IsAdvanced == right.IsAdvanced)
			{
				return 0;
			}
			if (left.IsAdvanced)
			{
				return 1;
			}
			return -1;
		}
	}
}
