using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class DiplomaticRelationsViewport : MonoBehaviour
{
	public Camera Camera { get; private set; }

	public global::Empire CurrentInspectedEmpire
	{
		get
		{
			return this.currentInspectedEmpire;
		}
	}

	public IEnumerator OnHide(bool instant, float duration)
	{
		this.alphaStatusDestination = 0f;
		if (instant || duration == 0f)
		{
			this.currentAlphaStatus = 0f;
			this.RefreshAlphaStatus();
			base.gameObject.SetActive(false);
		}
		yield break;
	}

	public IEnumerator OnLoad()
	{
		Diagnostics.Assert(this.notBoundObjects == null);
		Diagnostics.Assert(this.positionInfos == null);
		base.gameObject.SetActive(false);
		this.diplomacyLayerIndex = LayerMask.NameToLayer("Diplomacy");
		this.diplomacyLayerMask = 1 << this.diplomacyLayerIndex;
		Camera[] cameras = base.GetComponentsInChildren<Camera>(true);
		if (cameras != null && cameras.Length > 0)
		{
			this.Camera = cameras[0];
		}
		else
		{
			Diagnostics.LogError("Can't find no camera.");
		}
		this.notBoundObjects = new List<GameObject>();
		this.positionInfos = new DiplomaticRelationsViewport.PositionInfos(base.gameObject, this.notBoundObjects);
		if (this.backDropObject == null)
		{
			Transform backDropChild = base.transform.FindChild("BackDrop");
			if (backDropChild != null)
			{
				this.backDropObject = backDropChild.gameObject;
				Renderer renderer = this.backDropObject.GetComponent<Renderer>();
				Diagnostics.Assert(renderer != null);
			}
		}
		yield break;
	}

	public IEnumerator OnLoadGame()
	{
		this.InitIFN();
		yield break;
	}

	public void OnUnloadGame(IGame game)
	{
		if (this.players != null)
		{
			for (int i = 0; i < this.players.Length; i++)
			{
				this.players[i].Unload();
			}
			this.players = null;
		}
		this.loaded = false;
	}

	public IEnumerator OnShow(float duration)
	{
		Diagnostics.Assert(duration >= 0f);
		this.alphaAnimationDuration = duration;
		base.gameObject.SetActive(true);
		this.alphaStatusDestination = 1f;
		if (duration == 0f)
		{
			this.currentAlphaStatus = this.alphaAnimationDuration;
		}
		if (this.players != null)
		{
			for (int i = 0; i < this.players.Length; i++)
			{
				this.players[i].SetHighligthed(false, false);
			}
		}
		this.RefreshAlphaStatus();
		yield break;
	}

	public void GetCurrentHighlightedEmpire(ref global::Empire ambassadorEmpire, ref global::Empire inspectedEmpire)
	{
		for (int i = 0; i < this.players.Length; i++)
		{
			if (this.players[i].AmbassadorHighlighted)
			{
				ambassadorEmpire = this.players[i].Empire;
			}
			if (this.players[i].PlayerHighlighted)
			{
				inspectedEmpire = this.players[i].Empire;
			}
		}
	}

	public Bounds GetIconBound(string diplomaticRelationName)
	{
		for (int i = 0; i < this.positionInfos.StatePositionInfos.Length; i++)
		{
			if (this.positionInfos.StatePositionInfos[i].DiplomaticStateName == diplomaticRelationName)
			{
				return this.positionInfos.StatePositionInfos[i].GetIconScreenBound(this.Camera);
			}
		}
		return default(Bounds);
	}

	public Vector3 GetAmbassadorCenter(int empireIndex)
	{
		for (int i = 0; i < this.players.Length; i++)
		{
			if (this.players[i].Empire.Index == empireIndex)
			{
				return this.players[i].GetScreenPosition(this.Camera);
			}
		}
		Diagnostics.Assert(false);
		return new Vector3(-1f, -1f, -1f);
	}

	public Bounds GetAmbassadorBound(int empireIndex)
	{
		for (int i = 0; i < this.players.Length; i++)
		{
			if (this.players[i].Empire.Index == empireIndex)
			{
				return this.players[i].GetScreenBound(this.Camera);
			}
		}
		return default(Bounds);
	}

	public void Update()
	{
		if (DiplomaticRelationsViewport.animatorHighlightTriggerId == -1)
		{
			DiplomaticRelationsViewport.animatorHighlightTriggerId = Animator.StringToHash(DiplomaticRelationsViewport.animatorHighlightTriggerName);
		}
		if (this.players == null)
		{
			return;
		}
		if (this.currentAlphaStatus != this.alphaStatusDestination)
		{
			float num = Time.deltaTime / Math.Max(0.0001f, this.alphaAnimationDuration);
			float val = Math.Max(this.currentAlphaStatus - num, Math.Min(this.currentAlphaStatus + num, this.alphaStatusDestination));
			this.currentAlphaStatus = Math.Min(1f, Math.Max(0f, val));
			if (this.alphaStatusDestination == 0f && this.currentAlphaStatus == 0f)
			{
				base.gameObject.SetActive(false);
				return;
			}
			this.RefreshAlphaStatus();
		}
		for (int i = 0; i < this.players.Length; i++)
		{
			this.players[i].Update(this.currentAlphaStatus);
		}
		Diagnostics.Assert(this.Camera != null);
		Ray ray = this.Camera.ScreenPointToRay(Input.mousePosition);
		RaycastHit[] array = Physics.RaycastAll(ray, float.PositiveInfinity, this.diplomacyLayerMask);
		int num2 = -1;
		int num3 = -1;
		if (array != null && array.Length > 0)
		{
			float num4 = float.MaxValue;
			foreach (RaycastHit raycastHit in array)
			{
				if (raycastHit.distance <= num4)
				{
					string name = raycastHit.collider.transform.parent.name;
					if (name.IndexOf(DiplomaticRelationsViewport.Player.PlayerGameObjectName) == 0)
					{
						string value = name.Substring(DiplomaticRelationsViewport.Player.PlayerGameObjectName.Length);
						try
						{
							int num5 = Convert.ToInt32(value);
							if (this.players[num5].IsCurrentInspectedPlayer && this.players[num5].Empire != this.currentPlayerEmpire)
							{
								num3 = num5;
								num4 = raycastHit.distance;
							}
						}
						catch (FormatException)
						{
							Diagnostics.LogError("Invalid tag name {0}", new object[]
							{
								raycastHit.collider.tag
							});
						}
					}
					if (name.IndexOf(DiplomaticRelationsViewport.Player.AmbassadorGameObjectName) == 0)
					{
						string value2 = name.Substring(DiplomaticRelationsViewport.Player.AmbassadorGameObjectName.Length);
						try
						{
							num2 = Convert.ToInt32(value2);
							num4 = raycastHit.distance;
						}
						catch (FormatException)
						{
							Diagnostics.LogError("Invalid tag name {0}", new object[]
							{
								raycastHit.collider.tag
							});
						}
					}
				}
			}
		}
		for (int k = 0; k < this.players.Length; k++)
		{
			bool flag = !this.players[k].Unknown && !this.players[k].Dead && !this.players[k].Moving;
			bool playerHighlighted = k == num3 && flag;
			bool ambassadorHighlighted = k == num2 && flag;
			this.players[k].SetHighligthed(playerHighlighted, ambassadorHighlighted);
		}
	}

	public void SetPlayerEmpire(global::Empire empire)
	{
		this.InitIFN();
		this.currentPlayerEmpire = empire;
		this.currentInspectedEmpire = empire;
		this.Refresh();
	}

	public void SetInspectedEmpire(global::Empire empire)
	{
		this.currentInspectedEmpire = empire;
		this.Refresh();
	}

	public void Refresh()
	{
		Diagnostics.Assert(this.Camera != null);
		for (int i = 0; i < this.players.Length; i++)
		{
			DiplomaticRelationsViewport.Player player = this.players[i];
			bool asCurrentInspectedPlayer = player.Empire == this.currentInspectedEmpire;
			player.SetAsCurrentInspectedPlayer(asCurrentInspectedPlayer);
		}
		this.RefreshDiplomaticRelationState(this.currentPlayerEmpire, this.currentInspectedEmpire);
	}

	public void SetBackDropVisibility(bool visible, Texture2D texture = null)
	{
		if (this.backDropObject == null)
		{
			Transform transform = base.transform.FindChild("BackDrop");
			if (transform != null)
			{
				this.backDropObject = transform.gameObject;
			}
			else
			{
				Diagnostics.LogError("Unable to find BackDrop gameObject");
			}
		}
		if (this.backDropObject != null)
		{
			this.backDropObject.SetActive(visible);
			if (visible)
			{
				Renderer component = this.backDropObject.GetComponent<Renderer>();
				Diagnostics.Assert(component != null);
				component.material.SetTexture("_MainTex", texture);
				DiplomaticViewportUtilities.SetStatusMaterialProperty(this.backDropObject, new Vector4(this.currentAlphaStatus, 0f, 0f, 0f));
			}
		}
	}

	protected virtual void Start()
	{
	}

	private static void ExtractBoundingBox(GameObject gameObject, ref Vector3 bboxMin, ref Vector3 bboxMax)
	{
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
		bool flag = false;
		Vector3 vector = bboxMin;
		Vector3 vector2 = bboxMax;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			MeshCollider component = componentsInChildren[i].transform.GetComponent<MeshCollider>();
			SkinnedMeshRenderer component2 = componentsInChildren[i].transform.GetComponent<SkinnedMeshRenderer>();
			MeshFilter component3 = componentsInChildren[i].transform.GetComponent<MeshFilter>();
			Mesh mesh = null;
			if (component != null)
			{
				mesh = component.sharedMesh;
				if (!flag)
				{
					bboxMin = vector;
					bboxMax = vector2;
					flag = true;
				}
			}
			else if (component2 != null && !flag)
			{
				mesh = component2.sharedMesh;
			}
			else if (component3 != null && !flag)
			{
				mesh = component3.sharedMesh;
			}
			if (!(mesh == null))
			{
				Vector3[] vertices = mesh.vertices;
				Matrix4x4 localToWorldMatrix = componentsInChildren[i].transform.localToWorldMatrix;
				for (int j = 0; j < vertices.Length; j++)
				{
					Vector3 lhs = localToWorldMatrix.MultiplyPoint(vertices[j]);
					bboxMin = Vector3.Min(lhs, bboxMin);
					bboxMax = Vector3.Max(lhs, bboxMax);
				}
			}
		}
	}

	private static Bounds GetScreenBound(Camera camera, Renderer[] renderers)
	{
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		bool flag = false;
		Vector3 vector3 = vector;
		Vector3 vector4 = vector2;
		for (int i = 0; i < renderers.Length; i++)
		{
			Transform transform = renderers[i].transform;
			MeshCollider component = renderers[i].transform.GetComponent<MeshCollider>();
			SkinnedMeshRenderer component2 = renderers[i].transform.GetComponent<SkinnedMeshRenderer>();
			MeshFilter component3 = renderers[i].transform.GetComponent<MeshFilter>();
			Mesh mesh = null;
			if (component != null)
			{
				mesh = component.sharedMesh;
				if (!flag)
				{
					vector = vector3;
					vector2 = vector4;
					flag = true;
				}
			}
			else if (component2 != null && !flag)
			{
				mesh = component2.sharedMesh;
			}
			else if (component3 != null && !flag)
			{
				mesh = component3.sharedMesh;
			}
			if (!(mesh == null))
			{
				Vector3[] vertices = mesh.vertices;
				Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;
				for (int j = 0; j < vertices.Length; j++)
				{
					Vector3 position = localToWorldMatrix.MultiplyPoint(vertices[j]);
					Vector3 lhs = camera.WorldToScreenPoint(position);
					vector = Vector3.Min(lhs, vector);
					vector2 = Vector3.Max(lhs, vector2);
				}
			}
		}
		return new Bounds((vector + vector2) * 0.5f, vector2 - vector);
	}

	private static Bounds GetScreenBound(Camera camera, Vector3 position, Bounds bounds)
	{
		Vector3 vector = camera.WorldToScreenPoint(position);
		Vector3 vector2 = vector;
		Vector3 a = position + bounds.center - 0.5f * bounds.size;
		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				for (int k = 0; k <= 1; k++)
				{
					Vector3 position2 = a + new Vector3((float)k * bounds.size.x, (float)j * bounds.size.y, (float)i * bounds.size.z);
					Vector3 lhs = camera.WorldToScreenPoint(position2);
					vector = Vector3.Min(lhs, vector);
					vector2 = Vector3.Max(lhs, vector2);
				}
			}
		}
		return new Bounds((vector + vector2) * 0.5f, vector2 - vector);
	}

	private void RefreshAlphaStatus()
	{
		Vector4 status = new Vector4(this.currentAlphaStatus, 0f, 0f, 0f);
		for (int i = 0; i < this.notBoundObjects.Count; i++)
		{
			DiplomaticViewportUtilities.SetStatusMaterialProperty(this.notBoundObjects[i], status);
		}
	}

	private void RefreshDiplomaticRelationState(global::Empire currentPlayerEmpire, global::Empire currentInspectedEmpire)
	{
		Diagnostics.Assert(currentPlayerEmpire != null);
		Diagnostics.Assert(currentInspectedEmpire != null);
		DepartmentOfForeignAffairs agency = currentPlayerEmpire.GetAgency<DepartmentOfForeignAffairs>();
		DepartmentOfForeignAffairs agency2 = currentInspectedEmpire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency2 != null);
		Diagnostics.Assert(agency != null);
		Diagnostics.Assert(agency2.DiplomaticRelations != null);
		Diagnostics.Assert(agency.DiplomaticRelations != null);
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		IDiplomaticContractRepositoryService service2 = game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(service2 != null);
		foreach (DiplomaticRelation diplomaticRelation in agency.DiplomaticRelations)
		{
			int otherEmpireIndex = diplomaticRelation.OtherEmpireIndex;
			int playerIndex = this.GetPlayerIndex(otherEmpireIndex);
			if (playerIndex != -1)
			{
				DiplomaticRelationState state = diplomaticRelation.State;
				bool unknown = (state == null || state.Name == DiplomaticRelationState.Names.Unknown) && otherEmpireIndex != this.currentPlayerEmpire.Index;
				bool dead = (state == null || state.Name == DiplomaticRelationState.Names.Dead) && otherEmpireIndex != this.currentPlayerEmpire.Index;
				this.players[playerIndex].Unknown = unknown;
				this.players[playerIndex].Dead = dead;
			}
		}
		foreach (DiplomaticRelation diplomaticRelation2 in agency2.DiplomaticRelations)
		{
			int otherEmpireIndex2 = diplomaticRelation2.OtherEmpireIndex;
			int playerIndex2 = this.GetPlayerIndex(otherEmpireIndex2);
			if (playerIndex2 != -1)
			{
				DiplomaticRelationState state2 = diplomaticRelation2.State;
				if (state2 != null)
				{
					Bounds screenBound = this.players[playerIndex2].GetScreenBound(this.Camera);
					this.players[playerIndex2].SetDiplomaticState(this.Camera, screenBound, state2, this.positionInfos, this.players.Length);
				}
			}
		}
	}

	private void InitIFN()
	{
		if (this.loaded)
		{
			return;
		}
		this.CreatePlayers();
		this.loaded = true;
	}

	private void CreatePlayers()
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		global::Game game = service.Game as global::Game;
		List<DiplomaticRelationsViewport.Player> list = new List<DiplomaticRelationsViewport.Player>();
		int num = 0;
		Vector3 position = this.Camera.transform.position;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			global::Empire empire = game.Empires[i];
			MajorEmpire majorEmpire = empire as MajorEmpire;
			if (majorEmpire != null)
			{
				list.Add(new DiplomaticRelationsViewport.Player(majorEmpire, true, num, this.positionInfos.DummyCenter, position));
				num++;
			}
		}
		this.players = list.ToArray();
	}

	private int GetPlayerIndex(int empireIndex)
	{
		for (int i = 0; i < this.players.Length; i++)
		{
			if (this.players[i].Empire.Index == empireIndex)
			{
				return i;
			}
		}
		return -1;
	}

	private static string animatorHighlightTriggerName = "HighlightTrigger";

	private static int animatorHighlightTriggerId = -1;

	private bool loaded;

	private int diplomacyLayerIndex;

	private int diplomacyLayerMask;

	private DiplomaticRelationsViewport.PositionInfos positionInfos;

	private global::Empire currentPlayerEmpire;

	private global::Empire currentInspectedEmpire;

	private DiplomaticRelationsViewport.Player[] players;

	private GameObject backDropObject;

	private List<GameObject> notBoundObjects;

	private float currentAlphaStatus;

	private float alphaStatusDestination;

	private float alphaAnimationDuration;

	[SerializeField]
	private bool debugMode;

	private class PositionInfos
	{
		public PositionInfos(GameObject gameObject, List<GameObject> notBoundObjects)
		{
			this.ExtractDummyInformation(gameObject, notBoundObjects);
		}

		public GameObject DummyCenter
		{
			get
			{
				return this.dummyCenter;
			}
		}

		public DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos[] StatePositionInfos
		{
			get
			{
				return this.statePositionInfos;
			}
		}

		private void ExtractDummyInformation(GameObject gameObject, List<GameObject> notBoundObjects)
		{
			Diagnostics.Assert(notBoundObjects != null);
			Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>(true);
			string b = "Dummy_Center";
			this.statePositionInfos = new DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos[7];
			this.statePositionInfos[0] = new DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos("Alliance", DiplomaticRelationState.Names.Alliance);
			this.statePositionInfos[1] = new DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos("Peace", DiplomaticRelationState.Names.Peace);
			this.statePositionInfos[2] = new DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos("ColdWar", DiplomaticRelationState.Names.ColdWar);
			this.statePositionInfos[3] = new DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos("ColdWar", DiplomaticRelationState.Names.Truce);
			this.statePositionInfos[4] = new DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos("War", DiplomaticRelationState.Names.War);
			this.statePositionInfos[5] = new DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos("Unknown", DiplomaticRelationState.Names.Unknown);
			this.statePositionInfos[6] = new DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos("Dead", DiplomaticRelationState.Names.Dead);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				bool flag = false;
				Transform transform = componentsInChildren[i];
				GameObject gameObject2 = transform.gameObject;
				string name = gameObject2.name;
				if (name == b)
				{
					this.dummyCenter = transform.gameObject;
				}
				for (int j = 0; j < this.statePositionInfos.Length; j++)
				{
					bool flag2 = this.statePositionInfos[j].AddDummyIFN(name, gameObject2);
					flag = (flag || flag2);
				}
				if (!flag)
				{
					notBoundObjects.Add(transform.gameObject);
				}
			}
			for (int k = 0; k < this.statePositionInfos.Length; k++)
			{
				this.statePositionInfos[k].CheckConsistency();
			}
		}

		private GameObject dummyCenter;

		private DiplomaticRelationsViewport.PositionInfos.DiplomaticStatePositionInfos[] statePositionInfos;

		public class DiplomaticStatePositionInfos
		{
			public DiplomaticStatePositionInfos(string baseName, string diplomaticStateName)
			{
				this.dummyBaseName = "Dummy_" + baseName;
				this.iconName = "OBJ_GUI_Diplo_Icon_" + baseName;
				this.DiplomaticStateName = diplomaticStateName;
				this.Dummies = new List<GameObject>();
			}

			public void FillPositionInfo(Camera camera, Bounds currentScreenBounds, int playerIndex, int playerCount, Vector3 centerPos, ref Vector3 position, ref Quaternion rotation)
			{
				Diagnostics.Assert(this.Dummies.Count > 0);
				if (this.Dummies.Count == 0)
				{
					position = Vector3.zero;
					rotation = Quaternion.identity;
					return;
				}
				int num = this.Dummies.Count / 2;
				float num2 = (float)playerIndex / ((float)playerCount - 1f);
				int num3 = Math.Min(num - 1, (int)Math.Floor((double)(num2 * (float)num)));
				float num4 = num2 * (float)num - (float)num3;
				Vector3 a;
				Vector3 vector;
				this.FillMinPosAndMaxPos(camera, currentScreenBounds, num3, centerPos, out a, out vector);
				float magnitude = (a - centerPos).magnitude;
				float magnitude2 = (vector - centerPos).magnitude;
				Vector3 a2 = Vector3.Lerp(a, vector, num4);
				float d = (1f - num4) * magnitude + magnitude2 * num4;
				position = a2 * d / a2.magnitude;
				Quaternion rotation2 = this.Dummies[num3 * 2].transform.rotation;
				Quaternion rotation3 = this.Dummies[num3 * 2 + 1].transform.rotation;
				rotation = Quaternion.Lerp(rotation2, rotation3, num4);
			}

			public Bounds GetIconScreenBound(Camera camera)
			{
				Diagnostics.Assert(this.Icon != null);
				if (this.Icon != null)
				{
					return DiplomaticRelationsViewport.GetScreenBound(camera, this.Icon.transform.position, this.IconBounds);
				}
				return default(Bounds);
			}

			public bool AddDummyIFN(string gameObjectName, GameObject gameObject)
			{
				if (gameObjectName == this.iconName)
				{
					this.Icon = gameObject;
					Vector3 position = this.Icon.transform.position;
					Vector3 vector = position;
					Vector3 vector2 = vector;
					DiplomaticRelationsViewport.ExtractBoundingBox(this.Icon, ref vector, ref vector2);
					this.IconBounds = new Bounds((vector + vector2) * 0.5f - position, vector2 - vector);
					return false;
				}
				if (gameObjectName.IndexOf(this.dummyBaseName) == 0)
				{
					string text = this.dummyBaseName + "_Min_";
					string value = this.dummyBaseName + "_Max_";
					bool flag = gameObjectName.IndexOf(text) == 0;
					bool flag2 = gameObjectName.IndexOf(value) == 0;
					if (flag || flag2)
					{
						string text2 = gameObjectName.Substring(text.Length);
						try
						{
							int num = Convert.ToInt32(text2);
							if (num >= 0)
							{
								int num2 = num * 2 + ((!flag2) ? 0 : 1);
								while (this.Dummies.Count <= num2)
								{
									this.Dummies.Add(null);
								}
								this.Dummies[num2] = gameObject;
								return true;
							}
							Diagnostics.LogError("Invalid object name {0}", new object[]
							{
								gameObjectName
							});
						}
						catch (FormatException)
						{
							Diagnostics.LogError("Invalid object name {0} conversion error {1}", new object[]
							{
								gameObjectName,
								text2
							});
						}
						return false;
					}
				}
				return false;
			}

			public void CheckConsistency()
			{
				if (this.Dummies.Count == 0)
				{
					Diagnostics.LogError("Missing Dummy objects for status {0}", new object[]
					{
						this.DiplomaticStateName
					});
				}
				else if (this.Dummies.Count % 2 == 1)
				{
					Diagnostics.LogError("Invalid Dummy object count {0} for status {1}", new object[]
					{
						this.Dummies.Count,
						this.DiplomaticStateName
					});
				}
				else
				{
					for (int i = 0; i < this.Dummies.Count; i++)
					{
						if (this.Dummies[i] == null)
						{
							Diagnostics.LogError("Missing Dummy objects {0} for status {1}", new object[]
							{
								i,
								this.DiplomaticStateName
							});
						}
					}
				}
				if (this.Icon == null)
				{
					Diagnostics.LogWarning("Missing icon {0} objects for status {1}", new object[]
					{
						this.iconName,
						this.DiplomaticStateName
					});
				}
			}

			private void FillMinPosAndMaxPos(Camera camera, Bounds currentScreenBounds, int partIndex, Vector3 centerPos, out Vector3 minPos, out Vector3 maxPos)
			{
				Vector3 position = this.Dummies[partIndex * 2].transform.position;
				Vector3 position2 = this.Dummies[partIndex * 2 + 1].transform.position;
				float magnitude = (position - centerPos).magnitude;
				float magnitude2 = (position2 - centerPos).magnitude;
				minPos = position;
				maxPos = position2;
				int num = 20;
				for (int i = 0; i < num; i++)
				{
					float num2 = (float)i / (float)(num - 1);
					Vector3 vector = Vector3.Lerp(position, position2, num2);
					float d = (1f - num2) * magnitude + magnitude2 * num2;
					vector = vector * d / vector.magnitude;
					Vector3 vector2 = camera.WorldToScreenPoint(vector);
					if (vector2.x > 0.5f * currentScreenBounds.size.x && vector2.x + 0.5f * currentScreenBounds.size.x < (float)camera.pixelWidth)
					{
						minPos = vector;
						break;
					}
				}
				for (int j = 0; j < num; j++)
				{
					float num3 = (float)j / (float)(num - 1);
					Vector3 vector3 = Vector3.Lerp(position2, position, num3);
					float d2 = (1f - num3) * magnitude2 + magnitude * num3;
					vector3 = vector3 * d2 / vector3.magnitude;
					Vector3 vector4 = camera.WorldToScreenPoint(vector3);
					if (vector4.x > 0.5f * currentScreenBounds.size.x && vector4.x + 0.5f * currentScreenBounds.size.x < (float)camera.pixelWidth)
					{
						maxPos = vector3;
						break;
					}
				}
			}

			public string DiplomaticStateName;

			public List<GameObject> Dummies;

			public GameObject Icon;

			public Bounds IconBounds;

			private string dummyBaseName;

			private string iconName;
		}
	}

	private class Player
	{
		public Player(MajorEmpire empire, bool unknown, int diplomaticViewPlayerIndex, GameObject dummyCenter, Vector3 cameraPos)
		{
			this.empire = empire;
			this.unknown = unknown;
			this.currentUnknownIconStatus = 0f;
			this.currentAmbassadorIconStatus = 0f;
			this.currentPlayerIconStatus = 0f;
			this.diplomaticViewPlayerIndex = diplomaticViewPlayerIndex;
			this.isCurrentInspectedPlayer = false;
			this.CreatePlayerIcon(dummyCenter, this.isCurrentInspectedPlayer);
			this.position = dummyCenter.transform.position;
			this.currentPosition = this.position;
			this.rotation = Quaternion.identity;
			this.currentRotation = this.rotation;
			this.playerHighlightStatus = 0f;
			this.ambassadorHighlightStatus = 0f;
			this.currentAmbassadorHighlightStatus = 0.001f;
			this.currentPlayerHighlightStatus = 0.001f;
			this.ambassadorHolder = new GameObject();
			this.ambassadorHolder.name = string.Format("AmbassadorHolder{0}", diplomaticViewPlayerIndex);
			this.ambassadorHolder.transform.parent = dummyCenter.transform;
			this.ambassadorHolder.transform.localPosition = this.position;
			this.ambassadorHolder.SetActive(true);
			this.CreateAmbassadorIcon(cameraPos, this.ambassadorHolder, Vector3.zero);
			this.CreateStateIcon(this.ambassadorHolder, this.position);
			this.CreatePlayerIcon(dummyCenter, false);
		}

		public MajorEmpire Empire
		{
			get
			{
				return this.empire;
			}
		}

		public bool Unknown
		{
			get
			{
				return this.unknown;
			}
			set
			{
				this.unknown = value;
			}
		}

		public bool Dead
		{
			get
			{
				return this.dead;
			}
			set
			{
				this.dead = value;
			}
		}

		public bool HasContract
		{
			get
			{
				return this.hasContract;
			}
			set
			{
				this.hasContract = value;
			}
		}

		public bool AmbassadorHighlighted
		{
			get
			{
				return this.ambassadorHighlightStatus > 0.5f;
			}
		}

		public bool PlayerHighlighted
		{
			get
			{
				return this.playerHighlightStatus > 0.5f;
			}
		}

		public bool IsCurrentInspectedPlayer
		{
			get
			{
				return this.isCurrentInspectedPlayer;
			}
		}

		public Vector3 CurrentPosition
		{
			get
			{
				return this.currentPosition;
			}
		}

		public Vector3 Position
		{
			get
			{
				return this.position;
			}
			set
			{
				this.position = value;
			}
		}

		public Quaternion Rotation
		{
			get
			{
				return this.rotation;
			}
			set
			{
				this.rotation = value;
			}
		}

		public bool Visible
		{
			get
			{
				return !this.isCurrentInspectedPlayer;
			}
		}

		public bool Moving
		{
			get
			{
				return (this.CurrentPosition - this.Position).magnitude > 0.1f;
			}
		}

		public void Unload()
		{
			this.empire = null;
			if (this.playerIcon != null)
			{
				UnityEngine.Object.DestroyImmediate(this.playerIcon);
				this.playerIconRenderers = null;
				this.playerIcon = null;
			}
			if (this.unknownIcon != null)
			{
				UnityEngine.Object.DestroyImmediate(this.unknownIcon);
				this.unknownIconRenderers = null;
				this.unknownIcon = null;
			}
			if (this.ambassadorIcon != null)
			{
				UnityEngine.Object.DestroyImmediate(this.ambassadorIcon);
				this.playerIconRenderers = null;
				this.ambassadorIcon = null;
				this.ambassadorAnimators = null;
				DiplomaticViewportUtilities.ReleaseSortedMesh(ref this.ambassadorDuplicatedMeshes);
			}
			if (this.exclamationIcon != null)
			{
				UnityEngine.Object.DestroyImmediate(this.exclamationIcon);
				this.exclamationIconRenderers = null;
				this.exclamationIcon = null;
			}
			if (this.deadIcon != null)
			{
				UnityEngine.Object.DestroyImmediate(this.deadIcon);
				this.deadIconRenderers = null;
				this.deadIcon = null;
			}
			UnityEngine.Object.DestroyImmediate(this.ambassadorHolder);
			this.ambassadorHolder = null;
		}

		public void SetAsCurrentInspectedPlayer(bool currentPlayer)
		{
			Diagnostics.Assert(this.playerIcon != null);
			Diagnostics.Assert(this.ambassadorIcon != null);
			Diagnostics.Assert(this.unknownIcon != null);
			if (this.isCurrentInspectedPlayer != currentPlayer)
			{
				this.isCurrentInspectedPlayer = currentPlayer;
			}
		}

		public void SetHighligthed(bool playerHighlighted, bool ambassadorHighlighted)
		{
			if (this.ambassadorHighlightStatus == 0f && ambassadorHighlighted)
			{
				for (int i = 0; i < this.ambassadorAnimators.Length; i++)
				{
					this.ambassadorAnimators[i].SetTrigger(DiplomaticRelationsViewport.animatorHighlightTriggerId);
				}
			}
			else if (this.ambassadorHighlightStatus == 1f && !ambassadorHighlighted)
			{
				for (int j = 0; j < this.ambassadorAnimators.Length; j++)
				{
					this.ambassadorAnimators[j].ResetTrigger(DiplomaticRelationsViewport.animatorHighlightTriggerId);
				}
			}
			this.playerHighlightStatus = ((!playerHighlighted) ? 0f : 1f);
			this.ambassadorHighlightStatus = ((!ambassadorHighlighted) ? 0f : 1f);
		}

		public Bounds GetScreenBound(Camera camera)
		{
			if (this.unknown)
			{
				return DiplomaticRelationsViewport.GetScreenBound(camera, this.unknownIconRenderers);
			}
			return DiplomaticRelationsViewport.GetScreenBound(camera, this.ambassadorIconRenderers);
		}

		public Vector3 GetScreenPosition(Camera camera)
		{
			return camera.WorldToScreenPoint(this.CurrentPosition);
		}

		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:ArithmeticExpressionsMustDeclarePrecedence", Justification = "Trop de math ici.")]
		public void Update(float globalAlphaStatus)
		{
			float deltaTime = Time.deltaTime;
			float num = 1f - (float)Math.Exp((double)(-3f * deltaTime));
			float num2 = 1f - (float)Math.Exp((double)(-10f * deltaTime));
			Vector3 vector = (!this.isCurrentInspectedPlayer) ? this.position : Vector3.zero;
			Quaternion quaternion = (!this.isCurrentInspectedPlayer) ? this.rotation : Quaternion.identity;
			if (this.currentPosition != vector)
			{
				this.currentPosition = Vector3.Lerp(this.currentPosition, vector, num);
				this.ambassadorHolder.transform.localPosition = this.currentPosition;
			}
			if (this.currentRotation != quaternion)
			{
				this.currentRotation = Quaternion.Lerp(this.currentRotation, quaternion, num);
				this.ambassadorHolder.transform.localRotation = this.currentRotation;
			}
			float num3 = (float)((!this.isCurrentInspectedPlayer) ? 0 : 1);
			float num4 = (float)((!this.unknown) ? 0 : 1) * (1f - num3);
			float num5 = (float)((!this.unknown) ? 1 : 0) * (1f - num3);
			this.currentAmbassadorIconStatus = this.currentAmbassadorIconStatus * (1f - num2) + num5 * num2;
			this.currentUnknownIconStatus = this.currentUnknownIconStatus * (1f - num2) + num4 * num2;
			this.currentPlayerIconStatus = this.currentPlayerIconStatus * (1f - num) + num3 * num;
			this.currentAmbassadorHighlightStatus = this.currentAmbassadorHighlightStatus * (1f - num2) + this.ambassadorHighlightStatus * num2;
			this.currentPlayerHighlightStatus = this.currentPlayerHighlightStatus * (1f - num2) + this.playerHighlightStatus * num2;
			if (this.playerIcon != null)
			{
				Vector4 status = new Vector4(globalAlphaStatus * this.currentPlayerIconStatus, this.currentPlayerHighlightStatus, 0f, 0f);
				DiplomaticViewportUtilities.SetStatusMaterialProperty(this.playerIconRenderers, status);
				this.playerIcon.SetActive(this.currentPlayerIconStatus > 0.01f);
			}
			if (this.unknownIcon != null)
			{
				Vector4 status2 = new Vector4(globalAlphaStatus * this.currentUnknownIconStatus, this.currentAmbassadorHighlightStatus, 0f, 0f);
				DiplomaticViewportUtilities.SetStatusMaterialProperty(this.unknownIconRenderers, status2);
				this.unknownIcon.SetActive(this.currentUnknownIconStatus > 0.01f);
			}
			if (this.ambassadorIcon != null)
			{
				Vector4 status3 = new Vector4(globalAlphaStatus * this.currentAmbassadorIconStatus, this.currentAmbassadorHighlightStatus, 0f, 0f);
				DiplomaticViewportUtilities.SetStatusMaterialProperty(this.ambassadorIconRenderers, status3);
				this.ambassadorIcon.SetActive(this.currentAmbassadorIconStatus > 0.01f);
			}
			if (this.exclamationIcon != null)
			{
				this.exclamationIcon.SetActive(this.HasContract);
				Vector4 status4 = new Vector4(globalAlphaStatus, this.currentAmbassadorHighlightStatus, 0f, 0f);
				DiplomaticViewportUtilities.SetStatusMaterialProperty(this.exclamationIconRenderers, status4);
			}
			if (this.deadIcon != null)
			{
				this.deadIcon.SetActive(this.Dead);
				Vector4 status5 = new Vector4(globalAlphaStatus, this.currentAmbassadorHighlightStatus, 0f, 0f);
				DiplomaticViewportUtilities.SetStatusMaterialProperty(this.deadIconRenderers, status5);
			}
		}

		public void SetDiplomaticState(Camera camera, Bounds currentScreenBounds, DiplomaticRelationState state, DiplomaticRelationsViewport.PositionInfos positionInfos, int playerCount)
		{
			Diagnostics.Assert(state != null);
			Diagnostics.Assert(playerCount > 1);
			string b = (!this.unknown) ? state.Name : DiplomaticRelationState.Names.Unknown;
			for (int i = 0; i < positionInfos.StatePositionInfos.Length; i++)
			{
				if (positionInfos.StatePositionInfos[i].DiplomaticStateName == b)
				{
					Vector3 zero = Vector3.zero;
					Quaternion identity = Quaternion.identity;
					positionInfos.StatePositionInfos[i].FillPositionInfo(camera, currentScreenBounds, this.diplomaticViewPlayerIndex, playerCount, positionInfos.DummyCenter.transform.position, ref zero, ref identity);
					this.Position = zero;
					this.Rotation = identity;
				}
			}
		}

		private void CreatePlayerIcon(GameObject parent, bool currentPlayer)
		{
			string text = string.Format("Prefabs/Diplomacy/Socle_Player_{0}", this.GetCompatibleFactionName(this.Empire));
			GameObject gameObject = Resources.Load(text) as GameObject;
			if (gameObject != null)
			{
				this.playerIcon = UnityEngine.Object.Instantiate<GameObject>(gameObject);
				this.playerIconRenderers = this.playerIcon.GetComponentsInChildren<Renderer>(true);
				this.playerIcon.transform.parent = parent.transform;
				this.playerIcon.transform.localPosition = new Vector3(0f, 0f, 0f);
				this.playerIcon.name = DiplomaticRelationsViewport.Player.PlayerGameObjectName + this.diplomaticViewPlayerIndex.ToString();
				DiplomaticViewportUtilities.SetFactionColorMaterialProperty(this.playerIcon, this.empire.Color);
				this.playerIcon.SetActive(currentPlayer);
			}
			else
			{
				Diagnostics.LogError("Unable to load prefab {0}", new object[]
				{
					text
				});
			}
		}

		private void CreateAmbassadorIcon(Vector3 cameraPos, GameObject parent, Vector3 position)
		{
			string text = string.Format("Prefabs/Diplomacy/Ambassador_{0}_01", this.GetCompatibleFactionName(this.Empire));
			string path = "Prefabs/Diplomacy/Ambassador_Unknown_01";
			Vector3 zero = Vector3.zero;
			Vector3 zero2 = Vector3.zero;
			GameObject gameObject = Resources.Load(text) as GameObject;
			if (gameObject != null)
			{
				this.ambassadorIcon = UnityEngine.Object.Instantiate<GameObject>(gameObject);
				this.ambassadorIcon.transform.parent = parent.transform;
				this.ambassadorIconRenderers = this.ambassadorIcon.GetComponentsInChildren<Renderer>(true);
				DiplomaticViewportUtilities.OffsetMaterialOrder(this.ambassadorIconRenderers, 1);
				this.ambassadorAnimators = this.ambassadorIcon.GetComponentsInChildren<Animator>(true);
				this.ambassadorDuplicatedMeshes = DiplomaticViewportUtilities.SortMeshes(cameraPos, this.ambassadorIcon);
				DiplomaticViewportUtilities.SetFactionColorMaterialProperty(this.ambassadorIcon, this.empire.Color);
				if (this.GetCompatibleFactionName(this.Empire) == "FactionWinterShifters")
				{
					IGameService service = Services.GetService<IGameService>();
					if (service != null && service.Game != null)
					{
						ISeasonService service2 = service.Game.Services.GetService<ISeasonService>();
						Diagnostics.Assert(service2 != null);
						Season currentSeason = service2.GetCurrentSeason();
						if (currentSeason != null && currentSeason.SeasonDefinition.SeasonType == Season.ReadOnlyWinter)
						{
							this.shiftingFormAtDate1 = 1f;
						}
						else
						{
							this.shiftingFormAtDate1 = 0f;
						}
						service2.SeasonChange += this.UpdateAmbassadorShiftingForm_SeasonChange;
					}
					else
					{
						this.shiftingFormAtDate1 = 0f;
					}
					this.shiftingFormAtDate0 = this.shiftingFormAtDate1;
					float time = Time.time;
					this.date0 = time;
					this.date1 = this.date0;
					DiplomaticViewportUtilities.SetShiftingFormMaterialProperty(this.ambassadorIcon, new Vector4(this.shiftingFormAtDate1, this.date1, this.shiftingFormAtDate0, this.date0));
				}
				this.ambassadorIcon.transform.position = Vector3.zero;
				DiplomaticRelationsViewport.ExtractBoundingBox(this.ambassadorIcon, ref zero, ref zero2);
				this.ambassadorIcon.transform.localPosition = position;
				this.ambassadorIcon.name = DiplomaticRelationsViewport.Player.AmbassadorGameObjectName + this.diplomaticViewPlayerIndex.ToString();
				this.ambassadorIcon.SetActive(false);
			}
			else
			{
				Diagnostics.LogError("Unable to load prefab {0}", new object[]
				{
					text
				});
			}
			GameObject gameObject2 = Resources.Load(path) as GameObject;
			if (gameObject2 != null)
			{
				this.unknownIcon = UnityEngine.Object.Instantiate<GameObject>(gameObject2);
				this.unknownIcon.transform.parent = parent.transform;
				this.unknownIconRenderers = this.unknownIcon.GetComponentsInChildren<Renderer>(true);
				DiplomaticViewportUtilities.OffsetMaterialOrder(this.unknownIconRenderers, 1);
				DiplomaticViewportUtilities.SetFactionColorMaterialProperty(this.unknownIcon, this.empire.Color);
				this.unknownIcon.transform.position = Vector3.zero;
				DiplomaticRelationsViewport.ExtractBoundingBox(this.unknownIcon, ref zero, ref zero2);
				this.unknownIcon.transform.localPosition = position;
				this.unknownIcon.name = DiplomaticRelationsViewport.Player.AmbassadorGameObjectName + this.diplomaticViewPlayerIndex.ToString();
				this.unknownIcon.SetActive(false);
			}
			else
			{
				Diagnostics.LogError("Unable to load prefab {0}", new object[]
				{
					gameObject2
				});
			}
			this.ambassadorBounds = new Bounds((zero + zero2) * 0.5f, zero2 - zero);
		}

		private void UpdateAmbassadorShiftingForm_SeasonChange(object sender, SeasonChangeEventArgs e)
		{
			this.shiftingFormAtDate1 = (float)((!(e.NewSeason.SeasonDefinition.SeasonType == Season.ReadOnlyWinter)) ? 0 : 1);
			float time = Time.time;
			this.date0 = time;
			this.date1 = time + DiplomaticViewportUtilities.SeasonChangeShiftingDelay;
			DiplomaticViewportUtilities.SetShiftingFormMaterialProperty(this.ambassadorIcon, new Vector4(this.shiftingFormAtDate1, this.date1, this.shiftingFormAtDate0, this.date0));
			this.shiftingFormAtDate0 = this.shiftingFormAtDate1;
		}

		private void CreateStateIcon(GameObject parent, Vector3 position)
		{
			string text = "Prefabs/Diplomacy/Icon_Dead";
			string path = "Prefabs/Diplomacy/Icon_Exclamation";
			GameObject gameObject = Resources.Load(text) as GameObject;
			if (gameObject != null)
			{
				this.deadIcon = UnityEngine.Object.Instantiate<GameObject>(gameObject);
				this.deadIcon.transform.parent = parent.transform;
				this.deadIconRenderers = this.deadIcon.GetComponentsInChildren<Renderer>(true);
				this.deadIcon.transform.localPosition = position;
				DiplomaticViewportUtilities.OffsetMaterialOrder(this.deadIconRenderers, 3);
				this.deadIcon.SetActive(false);
			}
			else
			{
				Diagnostics.LogError("Unable to load prefab {0}", new object[]
				{
					text
				});
			}
			GameObject gameObject2 = Resources.Load(path) as GameObject;
			if (gameObject2 != null)
			{
				this.exclamationIcon = UnityEngine.Object.Instantiate<GameObject>(gameObject2);
				this.exclamationIcon.transform.parent = parent.transform;
				this.exclamationIconRenderers = this.exclamationIcon.GetComponentsInChildren<Renderer>(true);
				this.exclamationIcon.transform.localPosition = position;
				DiplomaticViewportUtilities.OffsetMaterialOrder(this.exclamationIconRenderers, 3);
				this.exclamationIcon.SetActive(false);
			}
			else
			{
				Diagnostics.LogError("Unable to load prefab {0}", new object[]
				{
					text
				});
			}
		}

		private string GetCompatibleFactionName(global::Empire empire)
		{
			string str = empire.Faction.AffinityMapping.ToString().Substring("AffinityMapping".Length);
			return "Faction" + str;
		}

		public static string AmbassadorGameObjectName = "AmbassadorIcon";

		public static string PlayerGameObjectName = "PlayerIcon";

		private MajorEmpire empire;

		private int diplomaticViewPlayerIndex;

		private GameObject ambassadorHolder;

		private GameObject unknownIcon;

		private Renderer[] unknownIconRenderers;

		private GameObject ambassadorIcon;

		private Renderer[] ambassadorIconRenderers;

		private Animator[] ambassadorAnimators;

		private Mesh[] ambassadorDuplicatedMeshes;

		private GameObject playerIcon;

		private Renderer[] playerIconRenderers;

		private GameObject exclamationIcon;

		private Renderer[] exclamationIconRenderers;

		private GameObject deadIcon;

		private Renderer[] deadIconRenderers;

		private Vector3 currentPosition;

		private Vector3 position;

		private Quaternion rotation;

		private Quaternion currentRotation;

		private float playerHighlightStatus;

		private float ambassadorHighlightStatus;

		private float currentAmbassadorHighlightStatus;

		private float currentPlayerHighlightStatus;

		private float currentUnknownIconStatus;

		private float currentAmbassadorIconStatus;

		private float currentPlayerIconStatus;

		private bool unknown;

		private bool dead;

		private bool hasContract;

		private bool isCurrentInspectedPlayer;

		private Bounds ambassadorBounds;

		private float date0;

		private float date1 = 0.1f;

		private float shiftingFormAtDate0;

		private float shiftingFormAtDate1;
	}
}
