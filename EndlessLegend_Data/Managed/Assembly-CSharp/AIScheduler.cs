using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Amplitude;
using Amplitude.IO;
using Amplitude.Threading;
using Amplitude.Unity.Framework;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

[Diagnostics.TagAttribute("AI")]
public class AIScheduler : IServiceContainer, Amplitude.Unity.Framework.IServiceProvider, IXmlSerializable
{
	public AIScheduler()
	{
		this.aiHelpers = new Dictionary<Type, AIHelper>();
		this.aiServices = new Dictionary<Type, IService>();
		base..ctor();
		this.Released = false;
		this.Initialized = false;
	}

	public static Amplitude.Unity.Framework.IServiceProvider Services
	{
		get
		{
			return AIScheduler.instance;
		}
	}

	public void AddService(Type type, IService service)
	{
		this.aiServices.Add(type, service);
	}

	public void AddService<T>(T service) where T : IService
	{
		this.aiServices.Add(typeof(T), service);
	}

	public IService GetService(Type type)
	{
		if (this.aiServices.ContainsKey(type))
		{
			return this.aiServices[type];
		}
		return null;
	}

	public T GetService<T>() where T : class, IService
	{
		if (this.aiServices.ContainsKey(typeof(T)))
		{
			return this.aiServices[typeof(T)] as T;
		}
		return (T)((object)null);
	}

	private T GetAIHelper<T>() where T : AIHelper
	{
		if (this.aiHelpers.ContainsKey(typeof(T)))
		{
			return this.aiHelpers[typeof(T)] as T;
		}
		return (T)((object)null);
	}

	private IEnumerator InitializeAIHelpers(Game game)
	{
		this.RegisterAIHelpers();
		Coroutine[] coroutines = new Coroutine[this.aiHelpers.Count];
		int index = 0;
		foreach (KeyValuePair<Type, AIHelper> kvp in this.aiHelpers)
		{
			coroutines[index] = Coroutine.StartCoroutine(kvp.Value.Initialize(this, game), null);
			index++;
		}
		bool finish = false;
		while (!finish)
		{
			finish = true;
			for (index = 0; index < coroutines.Length; index++)
			{
				if (!coroutines[index].IsFinished)
				{
					finish = false;
					coroutines[index].Run();
				}
			}
			yield return null;
		}
		AIScheduler.instance = this;
		yield break;
	}

	private void Register<T>(T aiHelper) where T : AIHelper
	{
		if (this.aiHelpers.ContainsKey(typeof(T)))
		{
			return;
		}
		this.aiHelpers.Add(typeof(T), aiHelper);
	}

	private void RegisterAIHelpers()
	{
		this.Register<PersonalityAIHelper>(new PersonalityAIHelper());
		this.Register<ConstructibleElementEvaluation>(new ConstructibleElementEvaluation());
		this.Register<WorldPositionEvaluation>(new WorldPositionEvaluation());
		this.Register<WorldAtlasHelper>(new WorldAtlasHelper());
		this.Register<Intelligence>(new Intelligence());
		this.Register<EntityInfoAIHelper>(new EntityInfoAIHelper());
		this.Register<TickableRepository>(new TickableRepository());
		this.Register<SynchronousJobRepository>(new SynchronousJobRepository());
		this.Register<AIDataRepositoryAIHelper>(new AIDataRepositoryAIHelper());
		this.Register<UnitDesignAIEvaluationHelper>(new UnitDesignAIEvaluationHelper());
		this.Register<UnitPatternAIEvaluationHelper>(new UnitPatternAIEvaluationHelper());
		this.Register<ConstructibleElementAIHelper>(new ConstructibleElementAIHelper());
		this.Register<RandomNumberGeneratorAIHelper>(new RandomNumberGeneratorAIHelper());
		this.Register<AIEntityGUIDAIHelper>(new AIEntityGUIDAIHelper());
		this.Register<CommunicationAIHelper>(new CommunicationAIHelper());
		this.Register<AIUnitDesignRepository>(new AIUnitDesignRepository());
		this.Register<AIEmpireDataAIHelper>(new AIEmpireDataAIHelper());
		this.Register<AIItemDataRepository>(new AIItemDataRepository());
		this.Register<AITradeDataRepository>(new AITradeDataRepository());
		this.Register<OrbAIHelper>(new OrbAIHelper());
	}

	private void ReleaseAIHelpers()
	{
		Diagnostics.Assert(this.aiHelpers != null);
		foreach (AIHelper aihelper in this.aiHelpers.Values)
		{
			Diagnostics.Assert(aihelper != null);
			aihelper.Release();
		}
		this.aiHelpers.Clear();
		AIScheduler.instance = null;
	}

	public virtual void ReadXml(XmlReader reader)
	{
		reader.ReadStartElement("AIScheduler");
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Helpers");
		for (int i = 0; i < attribute; i++)
		{
			string attribute2 = reader.GetAttribute("AssemblyQualifiedName");
			Type type = Type.GetType(attribute2);
			if (type != null)
			{
				if (this.aiHelpers.ContainsKey(type))
				{
					AIHelper aihelper = this.aiHelpers[type];
					reader.ReadElementSerializable<AIHelper>(ref aihelper);
				}
				else
				{
					reader.Skip();
				}
			}
			else
			{
				reader.Skip();
			}
		}
		reader.ReadEndElement("Helpers");
		attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("AIPlayer_MajorEmpires");
		for (int j = 0; j < attribute; j++)
		{
			int empireIndex = reader.GetAttribute<int>("EmpireIndex");
			AIPlayer aiplayer = this.aiPlayerMajorEmpires.Find((AIPlayer_MajorEmpire match) => match.MajorEmpire.Index == empireIndex);
			if (aiplayer != null)
			{
				reader.ReadElementSerializable<AIPlayer>(ref aiplayer);
			}
			else
			{
				reader.Skip();
			}
		}
		reader.ReadEndElement("AIPlayer_MajorEmpires");
		reader.ReadElementSerializable<AIPlayer_MinorEmpire>(ref this.aiPlayerMinorEmpire);
		reader.ReadElementSerializable<AIPlayer_LesserEmpire>(ref this.aiPlayerLesserEmpire);
		if (reader.IsStartElement("AIPlayer_NavalEmpire"))
		{
			if (this.aiPlayerNavalEmpire != null)
			{
				reader.ReadElementSerializable<AIPlayer_NavalEmpire>("AIPlayer_NavalEmpire", ref this.aiPlayerNavalEmpire);
			}
			else
			{
				reader.Skip("AIPlayer_NavalEmpire");
			}
		}
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteStartElement("Helpers");
		writer.WriteAttributeString<int>("Count", this.aiHelpers.Count);
		foreach (KeyValuePair<Type, AIHelper> keyValuePair in this.aiHelpers)
		{
			IXmlSerializable value = keyValuePair.Value;
			writer.WriteElementSerializable<IXmlSerializable>(ref value);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("AIPlayer_MajorEmpires");
		writer.WriteAttributeString<int>("Count", this.aiPlayerMajorEmpires.Count);
		for (int i = 0; i < this.aiPlayerMajorEmpires.Count; i++)
		{
			IXmlSerializable xmlSerializable = this.aiPlayerMajorEmpires[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
		IXmlSerializable xmlSerializable2 = this.aiPlayerMinorEmpire;
		writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable2);
		IXmlSerializable xmlSerializable3 = this.aiPlayerLesserEmpire;
		writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable3);
		IXmlSerializable xmlSerializable4 = this.aiPlayerNavalEmpire;
		writer.WriteElementSerializable<IXmlSerializable>("AIPlayer_NavalEmpire", ref xmlSerializable4);
	}

	private void GameSerializationService_GameSaving(object sender, GameSavingEventArgs e)
	{
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream))
			{
				xmlWriter.Writer.WriteStartDocument();
				xmlWriter.WriteStartElement(base.GetType().ToString());
				this.WriteXml(xmlWriter);
				xmlWriter.WriteEndElement();
			}
			memoryStream.Flush();
			e.Archive.Add(AIScheduler.AIFileName, memoryStream, CompressionMethod.Compressed);
			memoryStream.Close();
		}
	}

	~AIScheduler()
	{
		this.Released = true;
	}

	public bool Released { get; private set; }

	public bool Initialized { get; private set; }

	public bool IsThreaded { get; set; }

	public bool IsProcessing
	{
		get
		{
			if (this.isHelperProcessing)
			{
				return true;
			}
			for (int i = 0; i < this.aiPlayerMajorEmpires.Count; i++)
			{
				if (this.aiPlayerMajorEmpires[i].IsProcessing)
				{
					return true;
				}
			}
			return this.aiPlayerMinorEmpire.IsProcessing || this.aiPlayerNavalEmpire.IsProcessing || this.aiPlayerLesserEmpire.IsProcessing || this.aiPlayerKaijuEmpire.IsProcessing;
		}
	}

	public void Cancel()
	{
		if (this.Released)
		{
			return;
		}
		if (this.helperThread != null)
		{
			this.helperThread.Abort();
		}
		for (int i = 0; i < this.aiPlayerMajorEmpires.Count; i++)
		{
			this.aiPlayerMajorEmpires[i].Cancel();
		}
	}

	public bool CanEndTurn()
	{
		for (int i = 0; i < this.aiPlayerMajorEmpires.Count; i++)
		{
			if (!this.aiPlayerMajorEmpires[i].CanEndTurn())
			{
				return false;
			}
		}
		return (this.aiPlayerMinorEmpire == null || this.aiPlayerMinorEmpire.CanEndTurn()) && (this.aiPlayerNavalEmpire == null || this.aiPlayerNavalEmpire.CanEndTurn()) && (this.aiPlayerLesserEmpire == null || this.aiPlayerLesserEmpire.CanEndTurn()) && (this.aiPlayerKaijuEmpire == null || this.aiPlayerKaijuEmpire.CanEndTurn());
	}

	public bool IsMajorEmpireReady(Empire empire)
	{
		Diagnostics.Assert(empire is MajorEmpire);
		for (int i = 0; i < this.aiPlayerMajorEmpires.Count; i++)
		{
			if (this.aiPlayerMajorEmpires[i].MajorEmpire.Index == empire.Index)
			{
				return this.aiPlayerMajorEmpires[i].CanEndTurn();
			}
		}
		return false;
	}

	public void ChangeMajorEmpireAIState(Empire empire, AIPlayer.PlayerState newState)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		if (this.Released)
		{
			return;
		}
		Diagnostics.Assert(this.aiPlayerMajorEmpires != null);
		AIPlayer aiplayer = this.aiPlayerMajorEmpires.Find((AIPlayer_MajorEmpire ai) => ai.MajorEmpire.Index == empire.Index);
		if (aiplayer != null)
		{
			aiplayer.ChangeAIState(newState);
		}
	}

	public void ChangeMinorEmpireAIState(AIPlayer.PlayerState newState)
	{
		if (this.Released)
		{
			return;
		}
		if (this.aiPlayerMinorEmpire != null)
		{
			this.aiPlayerMinorEmpire.ChangeAIState(newState);
		}
	}

	public void ChangeNavalEmpireAIState(AIPlayer.PlayerState newState)
	{
		if (this.Released)
		{
			return;
		}
		if (this.aiPlayerNavalEmpire != null)
		{
			this.aiPlayerNavalEmpire.ChangeAIState(newState);
		}
	}

	public void ChangeLesserEmpireAIState(AIPlayer.PlayerState newState)
	{
		if (this.Released)
		{
			return;
		}
		if (this.aiPlayerLesserEmpire != null)
		{
			this.aiPlayerLesserEmpire.ChangeAIState(newState);
		}
	}

	public void ChangeKaijuEmpireAIState(AIPlayer.PlayerState newState)
	{
		if (this.Released)
		{
			return;
		}
		if (this.aiPlayerKaijuEmpire != null)
		{
			this.aiPlayerKaijuEmpire.ChangeAIState(newState);
		}
	}

	public bool TryGetMinorEmpireAIEntity(Region region, out AIEntity_MinorEmpire minorEmpire)
	{
		minorEmpire = null;
		if (this.aiPlayerMinorEmpire != null && this.aiPlayerMinorEmpire.AIEntities != null)
		{
			for (int i = 0; i < this.aiPlayerMinorEmpire.AIEntities.Count; i++)
			{
				AIEntity_MinorEmpire aientity_MinorEmpire = this.aiPlayerMinorEmpire.AIEntities[i] as AIEntity_MinorEmpire;
				MinorEmpire minorEmpire2 = aientity_MinorEmpire.Empire as MinorEmpire;
				if (minorEmpire2.Region == region)
				{
					minorEmpire = aientity_MinorEmpire;
					return true;
				}
			}
		}
		return minorEmpire != null;
	}

	public bool TryGetMajorEmpireAIState(Empire empire, out AIPlayer.PlayerState state)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		if (this.Released)
		{
			state = AIPlayer.PlayerState.Deactivated;
			return false;
		}
		Diagnostics.Assert(this.aiPlayerMajorEmpires != null);
		AIPlayer aiplayer = this.aiPlayerMajorEmpires.Find((AIPlayer_MajorEmpire ai) => ai.MajorEmpire.Index == empire.Index);
		if (aiplayer != null)
		{
			state = aiplayer.AIState;
			return true;
		}
		state = AIPlayer.PlayerState.Deactivated;
		return false;
	}

	public bool TryGetNavalEmpireAIPlayer(out AIPlayer_NavalEmpire navalPlayer)
	{
		navalPlayer = this.aiPlayerNavalEmpire;
		return navalPlayer != null;
	}

	public bool TryGetKaijuEmpireAIEntity(Region region, out AIEntity_KaijuEmpire kaijuEmpire)
	{
		kaijuEmpire = null;
		if (this.aiPlayerKaijuEmpire != null && this.aiPlayerKaijuEmpire.AIEntities != null)
		{
			for (int i = 0; i < this.aiPlayerKaijuEmpire.AIEntities.Count; i++)
			{
				AIEntity_KaijuEmpire aientity_KaijuEmpire = this.aiPlayerKaijuEmpire.AIEntities[i] as AIEntity_KaijuEmpire;
				KaijuEmpire kaijuEmpire2 = aientity_KaijuEmpire.Empire as KaijuEmpire;
				if (kaijuEmpire2.Region == region)
				{
					kaijuEmpire = aientity_KaijuEmpire;
					return true;
				}
			}
		}
		return kaijuEmpire != null;
	}

	public IEnumerator LoadGame(Game game)
	{
		Diagnostics.Assert(!this.Initialized);
		this.gameSerializationService = Amplitude.Unity.Framework.Services.GetService<IGameSerializationService>();
		Diagnostics.Assert(this.gameSerializationService != null);
		this.gameSerializationService.GameSaving += this.GameSerializationService_GameSaving;
		yield return this.Initialize(game);
		yield return this.Load(game);
		this.Initialized = true;
		yield break;
	}

	public void Release()
	{
		this.Released = true;
		if (this.gameSerializationService != null)
		{
			this.gameSerializationService.GameSaving -= this.GameSerializationService_GameSaving;
			this.gameSerializationService = null;
		}
		this.Unload();
	}

	public void Start()
	{
		if (this.Released)
		{
			return;
		}
		if (this.IsProcessing)
		{
			return;
		}
		this.isHelperProcessing = true;
		if (this.IsThreaded)
		{
			this.helperEventWaitHandle.Set();
		}
		else
		{
			this.OnRun();
			this.isHelperProcessing = false;
		}
	}

	public bool TryGetMajorEmpireAIPlayer(MajorEmpire majorEmpire, out AIPlayer_MajorEmpire aiPlayerMajorEmpire)
	{
		if (majorEmpire == null)
		{
			throw new ArgumentNullException("majorEmpire");
		}
		aiPlayerMajorEmpire = null;
		Diagnostics.Assert(this.aiPlayerMajorEmpires != null);
		for (int i = 0; i < this.aiPlayerMajorEmpires.Count; i++)
		{
			AIPlayer_MajorEmpire aiplayer_MajorEmpire = this.aiPlayerMajorEmpires[i];
			Diagnostics.Assert(aiplayer_MajorEmpire != null && aiplayer_MajorEmpire.MajorEmpire != null);
			if (aiplayer_MajorEmpire.MajorEmpire.Index == majorEmpire.Index)
			{
				aiPlayerMajorEmpire = aiplayer_MajorEmpire;
				return true;
			}
		}
		return false;
	}

	private IEnumerator Initialize(Game game)
	{
		Diagnostics.Assert(!this.Released);
		yield return this.InitializeAIHelpers(game);
		if (this.IsThreaded)
		{
			this.helperEventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
			this.helperThread = new Amplitude.Threading.Thread("AIScheduler: Helpers", new ThreadStart(this.Run));
			this.helperThread.Start();
		}
		else
		{
			this.helperThread = null;
		}
		yield break;
	}

	private IEnumerator InitializeMajorEmpires(Game game)
	{
		for (int index = 0; index < game.Empires.Length; index++)
		{
			Empire empire = game.Empires[index];
			if (empire is MajorEmpire)
			{
				AIPlayer_MajorEmpire playerMajorEmpire = new AIPlayer_MajorEmpire(empire as MajorEmpire);
				playerMajorEmpire.ChangeAIState(AIPlayer.PlayerState.EmpireControlledByAI);
				this.aiPlayerMajorEmpires.Add(playerMajorEmpire);
			}
		}
		bool isPlayerThreaded = this.IsThreaded;
		isPlayerThreaded = false;
		for (int index2 = 0; index2 < this.aiPlayerMajorEmpires.Count; index2++)
		{
			yield return this.aiPlayerMajorEmpires[index2].Initialize(isPlayerThreaded);
		}
		yield break;
	}

	private IEnumerator InitializeMinorEmpire(Game game)
	{
		this.aiPlayerMinorEmpire = new AIPlayer_MinorEmpire();
		if (global::Application.FantasyPreferences.EnableMinorEmpireAI)
		{
			this.aiPlayerMinorEmpire.ChangeAIState(AIPlayer.PlayerState.EmpireControlledByAI);
		}
		else
		{
			this.aiPlayerMinorEmpire.ChangeAIState(AIPlayer.PlayerState.EmpireControlledByHuman);
		}
		for (int index = 0; index < game.Empires.Length; index++)
		{
			Empire empire = game.Empires[index];
			if (empire is MinorEmpire)
			{
				this.aiPlayerMinorEmpire.RegisterMinorEmpire(empire as MinorEmpire);
			}
		}
		bool isPlayerThreaded = this.IsThreaded;
		isPlayerThreaded = false;
		yield return this.aiPlayerMinorEmpire.Initialize(isPlayerThreaded);
		yield break;
	}

	private IEnumerator InitializeNavalEmpire(Game game)
	{
		this.aiPlayerNavalEmpire = new AIPlayer_NavalEmpire();
		if (global::Application.FantasyPreferences.EnableNavalEmpireAI)
		{
			this.aiPlayerNavalEmpire.ChangeAIState(AIPlayer.PlayerState.EmpireControlledByAI);
		}
		else
		{
			this.aiPlayerNavalEmpire.ChangeAIState(AIPlayer.PlayerState.EmpireControlledByHuman);
		}
		for (int index = 0; index < game.Empires.Length; index++)
		{
			Empire empire = game.Empires[index];
			if (empire is NavalEmpire)
			{
				this.aiPlayerNavalEmpire.RegisterNavalEmpire(empire as NavalEmpire);
			}
		}
		bool isPlayerThreaded = this.IsThreaded;
		isPlayerThreaded = false;
		yield return this.aiPlayerNavalEmpire.Initialize(isPlayerThreaded);
		yield break;
	}

	private IEnumerator InitializeLesserEmpire(Game game)
	{
		List<Empire> empireList = new List<Empire>(game.Empires);
		empireList = empireList.FindAll((Empire match) => match is LesserEmpire);
		if (empireList.Count != 1)
		{
			Diagnostics.LogWarning("There should only be one LesserEmpire, we'll use only the first one");
		}
		this.aiPlayerLesserEmpire = new AIPlayer_LesserEmpire(empireList[0] as LesserEmpire);
		if (global::Application.FantasyPreferences.EnableLesserEmpireAI)
		{
			this.aiPlayerLesserEmpire.ChangeAIState(AIPlayer.PlayerState.EmpireControlledByAI);
		}
		else
		{
			this.aiPlayerLesserEmpire.ChangeAIState(AIPlayer.PlayerState.EmpireControlledByHuman);
		}
		yield return this.aiPlayerLesserEmpire.Initialize(this.IsThreaded);
		yield break;
	}

	private IEnumerator InitializeKaijuEmpire(Game game)
	{
		this.aiPlayerKaijuEmpire = new AIPlayer_KaijuEmpire();
		if (global::Application.FantasyPreferences.EnableKaijuEmpireAI)
		{
			this.aiPlayerKaijuEmpire.ChangeAIState(AIPlayer.PlayerState.EmpireControlledByAI);
		}
		else
		{
			this.aiPlayerKaijuEmpire.ChangeAIState(AIPlayer.PlayerState.EmpireControlledByHuman);
		}
		for (int index = 0; index < game.Empires.Length; index++)
		{
			Empire empire = game.Empires[index];
			if (empire is KaijuEmpire)
			{
				this.aiPlayerKaijuEmpire.RegisterKaijuEmpire(empire as KaijuEmpire);
			}
		}
		bool isPlayerThreaded = this.IsThreaded;
		isPlayerThreaded = false;
		yield return this.aiPlayerKaijuEmpire.Initialize(isPlayerThreaded);
		yield break;
	}

	private void Unload()
	{
		if (this.helperThread != null)
		{
			this.helperThread.Abort();
			this.helperThread.Dispose();
			this.helperThread = null;
		}
		for (int i = 0; i < this.aiPlayerMajorEmpires.Count; i++)
		{
			if (this.aiPlayerMajorEmpires[i] != null)
			{
				this.aiPlayerMajorEmpires[i].Release();
				this.aiPlayerMajorEmpires[i] = null;
			}
		}
		this.aiPlayerMajorEmpires.Clear();
		if (this.aiPlayerMinorEmpire != null)
		{
			this.aiPlayerMinorEmpire.Release();
			this.aiPlayerMinorEmpire = null;
		}
		if (this.aiPlayerNavalEmpire != null)
		{
			this.aiPlayerNavalEmpire.Release();
			this.aiPlayerNavalEmpire = null;
		}
		if (this.aiPlayerLesserEmpire != null)
		{
			this.aiPlayerLesserEmpire.Release();
			this.aiPlayerLesserEmpire = null;
		}
		if (this.aiPlayerKaijuEmpire != null)
		{
			this.aiPlayerKaijuEmpire.Release();
			this.aiPlayerKaijuEmpire = null;
		}
		this.ReleaseAIHelpers();
		this.aiServices.Clear();
	}

	private IEnumerator Load(Game game)
	{
		Diagnostics.Assert(!this.Released);
		yield return this.InitializeMajorEmpires(game);
		yield return this.InitializeMinorEmpire(game);
		yield return this.InitializeNavalEmpire(game);
		yield return this.InitializeLesserEmpire(game);
		yield return this.InitializeKaijuEmpire(game);
		if (this.gameSerializationService != null && this.gameSerializationService.GameSaveDescriptor != null)
		{
			using (Archive archive = Archive.Open(this.gameSerializationService.GameSaveDescriptor.SourceFileName, ArchiveMode.Open))
			{
				MemoryStream stream = null;
				if (archive.TryGet(AIScheduler.AIFileName, out stream))
				{
					using (XmlReader reader = XmlReader.Create(stream))
					{
						reader.Reader.ReadToDescendant("AIScheduler");
						this.ReadXml(reader);
					}
				}
			}
		}
		Diagnostics.Assert(this.aiHelpers != null);
		foreach (AIHelper aiHelper in this.aiHelpers.Values)
		{
			Diagnostics.Assert(aiHelper != null);
			yield return aiHelper.Load(game);
		}
		for (int index = 0; index < this.aiPlayerMajorEmpires.Count; index++)
		{
			yield return this.aiPlayerMajorEmpires[index].Load();
		}
		yield return this.aiPlayerMinorEmpire.Load();
		yield return this.aiPlayerNavalEmpire.Load();
		yield return this.aiPlayerLesserEmpire.Load();
		yield return this.aiPlayerKaijuEmpire.Load();
		yield break;
	}

	private void Run()
	{
		try
		{
			Diagnostics.Assert(!this.Released);
			for (;;)
			{
				if (!this.helperEventWaitHandle.WaitOne())
				{
					this.helperEventWaitHandle.Reset();
				}
				else
				{
					this.helperEventWaitHandle.Reset();
					this.OnRun();
					this.isHelperProcessing = false;
				}
			}
		}
		catch (Exception ex)
		{
			Diagnostics.LogError(string.Concat(new object[]
			{
				"AIScheduler.Run : Caught exception, ",
				ex,
				", ",
				ex.Message
			}));
		}
	}

	private void OnRun()
	{
		Diagnostics.Assert(!this.Released);
		this.isHelperProcessing = true;
		Diagnostics.Assert(this.aiHelpers != null);
		foreach (AIHelper aihelper in this.aiHelpers.Values)
		{
			Diagnostics.Assert(aihelper != null);
			if (!this.IsThreaded)
			{
			}
			aihelper.RunAIThread();
			if (!this.IsThreaded)
			{
			}
		}
		this.isHelperProcessing = false;
		Diagnostics.Assert(this.aiPlayerMajorEmpires != null);
		for (int i = 0; i < this.aiPlayerMajorEmpires.Count; i++)
		{
			AIPlayer aiplayer = this.aiPlayerMajorEmpires[i];
			Diagnostics.Assert(aiplayer != null);
			if (!this.IsThreaded)
			{
			}
			if (aiplayer.AIState != AIPlayer.PlayerState.Dead)
			{
				aiplayer.Start();
			}
			if (!this.IsThreaded)
			{
			}
		}
		if (!this.IsThreaded)
		{
		}
		this.aiPlayerMinorEmpire.Start();
		if (!this.IsThreaded)
		{
		}
		if (!this.IsThreaded)
		{
		}
		this.aiPlayerNavalEmpire.Start();
		if (!this.IsThreaded)
		{
		}
		if (!this.IsThreaded)
		{
		}
		this.aiPlayerLesserEmpire.Start();
		if (!this.IsThreaded)
		{
		}
		if (!this.IsThreaded)
		{
		}
		this.aiPlayerKaijuEmpire.Start();
		if (!this.IsThreaded)
		{
		}
	}

	private static AIScheduler instance;

	private Dictionary<Type, AIHelper> aiHelpers;

	private Dictionary<Type, IService> aiServices;

	public static readonly string AIFileName = "Endless Legend/AI.xml";

	private List<AIPlayer_MajorEmpire> aiPlayerMajorEmpires = new List<AIPlayer_MajorEmpire>();

	private AIPlayer_MinorEmpire aiPlayerMinorEmpire;

	private AIPlayer_NavalEmpire aiPlayerNavalEmpire;

	private AIPlayer_LesserEmpire aiPlayerLesserEmpire;

	private AIPlayer_KaijuEmpire aiPlayerKaijuEmpire;

	private Amplitude.Threading.Thread helperThread;

	private EventWaitHandle helperEventWaitHandle;

	private IGameSerializationService gameSerializationService;

	private bool isHelperProcessing;
}
