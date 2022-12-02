//
// MeshShadowReceiver.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using FastShadowReceiver;

namespace FastShadowReceiver {
	public class MeshShadowReceiver : ReceiverBase {
		private enum MaterialPropertyType {
			Texture,
			TextureScaleOffset,
			Color,
			Float,
			Vector,
			Matrix
		}
		[System.Serializable]
		private struct MaterialPropertyInfo {
			public MaterialPropertyType Type;
			public string Name;
			private int NameID;
			public MaterialPropertyInfo(MaterialPropertyType type, string name)
			{
				Type = type;
				Name = name;
				NameID = -1;
			}
			public void InitializeNameID()
			{
				if (Type != MaterialPropertyType.TextureScaleOffset) {
					NameID = Shader.PropertyToID (Name);
				} else {
					NameID = Shader.PropertyToID (Name + "_ST");
				}
			}
			public void Copy(MaterialPropertyBlock dst, Material src)
			{
#if UNITY_EDITOR
				if (!src.HasProperty(NameID)) {
					if (src.HasProperty(Name)) {
						InitializeNameID();
					}
					else if (!Application.isPlaying) {
						// if playing, go through for showing errors.
						// if not playing, return silenyly as it might be being edited.
						return;
					}
				}
				else if (!src.HasProperty(Name)) {
					// Maybe Name was changed. Reset NameID.
					NameID = -1;
					return;
				}
#endif
				switch (Type) {
				case MaterialPropertyType.Texture:
					{
						Texture tex = src.GetTexture (NameID);
						dst.SetTexture (NameID, tex);
					}
					break;
				case MaterialPropertyType.TextureScaleOffset:
					{
						Vector2 scale = src.GetTextureScale (Name);
						Vector2 offset = src.GetTextureOffset (Name);
						Vector4 scaleOffset = new Vector4 (scale.x, scale.y, offset.x, offset.y);
						dst.SetVector (NameID, scaleOffset);
					}
					break;
				case MaterialPropertyType.Color:
					{
						Color color = src.GetColor (NameID);
						dst.SetColor (NameID, color);
					}
					break;
				case MaterialPropertyType.Float:
					{
						float v = src.GetFloat (NameID);
						dst.SetFloat (NameID, v);
					}
					break;
				case MaterialPropertyType.Vector:
					{
						Vector4 v = src.GetVector (NameID);
						dst.SetVector (NameID, v);
					}
					break;
				case MaterialPropertyType.Matrix:
					{
						Matrix4x4 m = src.GetMatrix (NameID);
						dst.SetMatrix (NameID, m);
					}
					break;
				}
			}
		}
		[SerializeField]
		private bool m_addToProjectorManagerWhenAwake = false;
		[SerializeField]
		private MeshTreeBase m_meshTree;
		[SerializeField]
		private Transform    m_meshTransform;
		[SerializeField]
		private bool         m_hasNormals = true;
		[SerializeField]
		private bool         m_dontUseLightmap = false; // If true, never use lightmaps. If false, it depends on meshtree.
		[SerializeField]
		private bool         m_cullBackFace;
		[SerializeField]
		private bool         m_scissor = true;
		[SerializeField]
		private float        m_scissorMargin = 0.0f; // applicable if m_meshTree is BinaryMeshTree
		[SerializeField][HideInInspector]
		private Material     m_projectorMaterialForLightmappedMesh = null;
		[SerializeField][HideInInspector]
		private MaterialPropertyInfo[] m_staticMaterialPropertiesToCopyFromProjector = new MaterialPropertyInfo[] { new MaterialPropertyInfo(MaterialPropertyType.Texture, "_ShadowTex") };
		[SerializeField][HideInInspector]
		private MaterialPropertyInfo[] m_dynamicMaterialPropertiesToCopyFromProjector = null;
		[SerializeField][HideInInspector]
		private bool         m_enablePrediction;
		[SerializeField][HideInInspector]
		private Component    m_predictor;
		[SerializeField][HideInInspector]
		private bool         m_updateOnlyWhenProjectorMoved = false;
		[SerializeField][HideInInspector]
		private float        m_margin = 0.0f;

		public bool addToProjectorManagerWhenAwake
		{
			get { return m_addToProjectorManagerWhenAwake; }
			set { m_addToProjectorManagerWhenAwake = value; }
		}
		public MeshTreeBase meshTree
		{
			get { return m_meshTree; }
			set {
				if (m_meshTree != value) {
					if (m_meshTree != null) {
						if (!m_meshTree.IsBuildFinished()) {
							m_meshTree.WaitForBuild();
						}
						if (m_search != null) {
							SyncAndSwap();
						}
					}
					m_meshTree = value;
					if (value != null) {
						if (!m_meshTree.IsBuildFinished() && !m_meshTree.IsBuilding()) {
							if (m_meshTree.IsPrebuilt()) {
								m_meshTree.BuildFromPrebuiltData();
								if (Debug.isDebugBuild || Application.isEditor) {
									CheckError();
								}
							}
							else {
								m_meshTree.AsyncBuild();
							}
						}
						m_search = value.CreateSearch();
					}
				}
			}
		}

		public Transform meshTransform
		{
			get { return m_meshTransform; }
			set { m_meshTransform = value; }
		}

		public bool hasNormals
		{
			get { return m_hasNormals; }
			set { m_hasNormals = value; }
		}

		public bool dontUseLightmaps
		{
			get { return m_dontUseLightmap; }
			set { m_dontUseLightmap = value; }
		}

		public bool cullBackFace
		{
			get { return m_cullBackFace; }
			set { m_cullBackFace = value; }
		}

		public bool scissorEnabled
		{
			get { return m_scissor; }
			set { m_scissor = value; }
		}
		
		public float scissorMargin
		{
			get { return m_scissorMargin; }
			set { m_scissorMargin = value; }
		}

		public bool predictEnabled
		{
			get { return m_enablePrediction && m_predictor is ITransformPredictor && m_multiThreading; }
			set { m_enablePrediction = value; }
		}

		public ITransformPredictor predictor
		{
			get { return m_predictor as ITransformPredictor; }
			set {
				if (value == null || value is Component) {
					m_predictor = value as Component;
				}
				else if (Debug.isDebugBuild || Application.isEditor) {
					Debug.LogError("predictor must be a Component!");
				}
			}
		}

		public bool updateOnlyWhenProjectorMoved
		{
			get { return m_updateOnlyWhenProjectorMoved; }
			set { m_updateOnlyWhenProjectorMoved = value; }
		}
		public float margin
		{
			get { return m_margin; }
			set { m_margin = value; }
		}
		
		protected override bool IsReady ()
		{
			if (m_meshTransform == null || m_meshTree == null || !base.IsReady()) {
				return false;
			}
			if (m_meshTree.IsBuildFinished() || m_meshTree.IsPrebuilt()) {
				return true;
			}
			return (Application.isPlaying && m_meshTree.IsReadyToBuild());
		}

		private bool           m_multiThreading;
		private MeshTreeSearch m_search;
		private Transform      m_selfTransform;
		private Vector3    m_projectorPositionWhenMeshCreated;
		private Quaternion m_projectorRotationWhenMeshCreated;
		private Plane[]    m_prevClipPlanes;
		private float[]    m_prevClipDistance;
		private enum MeshState {
			Uncreated,
			Creating,
			Created
		}
		private MeshState m_meshState;
		private GameObject m_subMeshRendererFolder = null;
		private Component m_singleLightmapRenderer = null;

		public bool multiThreadEnabled
		{
			get { return m_multiThreading; }
			set {
				if (m_multiThreading != value) {
					if (m_multiThreading && m_search != null) {
						m_search.Wait();
					}
					m_multiThreading = value;
#if !NETFX_CORE
					if (value) {
						Nyahoon.ThreadPool.InitInstance();
					}
#endif
				}
			}
		}

		protected override void OnAwake ()
		{
			if (addToProjectorManagerWhenAwake)
			{
#if UNITY_EDITOR
				if (Application.isPlaying)
#endif
				{
					if (ProjectorManager.Instance != null) {
						ProjectorManager.Instance.AddReceiver (this);
					} else {
						Debug.LogError ("No ProjectorManager Instance! Could not add this receiver to ProjectorManager.", this);
					}
				}
			}
			m_multiThreading = 1 < SystemInfo.processorCount;
#if !NETFX_CORE
			if (m_multiThreading) {
				Nyahoon.ThreadPool.InitInstance();
			}
#endif
			if (m_subMeshRendererFolder != null)
			{
				DestroyImmediate(m_subMeshRendererFolder);
				m_subMeshRendererFolder = null;
			}
			base.OnAwake ();
			m_selfTransform = transform;
			m_meshState = MeshState.Uncreated;
			m_singleLightmapRenderer = null;
			if (m_meshTree != null) {
				if (!m_meshTree.IsBuildFinished()) {
					if (m_meshTree.IsPrebuilt()) {
						m_meshTree.BuildFromPrebuiltData();
						if (Debug.isDebugBuild || Application.isEditor) {
							CheckError();
						}
					}
					else {
						m_meshTree.AsyncBuild();
					}
				}
				m_search = m_meshTree.CreateSearch();
#if UNITY_2017_1_OR_NEWER
				if (m_meshTree is TerrainMeshTree) {
					Terrain terrain = m_meshTransform.GetComponent<Terrain> ();
					if (terrain != null && 0 <= terrain.lightmapIndex && terrain.lightmapIndex < MeshTree.MAX_LIGHTMAP_INDEX)
					{
						m_singleLightmapRenderer = terrain;
					}
				}
				else if (m_meshTree is MeshTree) {
					MeshTree meshTree = m_meshTree as MeshTree;
					if (meshTree.srcMesh is Mesh && meshTree.m_lightmapUVIndex != MeshTree.UVIndex.None) {
						Renderer renderer = m_meshTransform.GetComponent<Renderer> ();
						if (renderer != null && 0 <= renderer.lightmapIndex && renderer.lightmapIndex < MeshTree.MAX_LIGHTMAP_INDEX)
						{
							m_singleLightmapRenderer = renderer;
						}
					}
				}
#endif
			}
			if (m_staticMaterialPropertiesToCopyFromProjector != null) {
				for (int i = 0; i < m_staticMaterialPropertiesToCopyFromProjector.Length; ++i) {
					m_staticMaterialPropertiesToCopyFromProjector[i].InitializeNameID ();
				}
			}
			if (m_dynamicMaterialPropertiesToCopyFromProjector != null) {
				for (int i = 0; i < m_dynamicMaterialPropertiesToCopyFromProjector.Length; ++i) {
					m_dynamicMaterialPropertiesToCopyFromProjector[i].InitializeNameID ();
				}
			}
		}
		protected override void OnTerminate()
		{
			if (m_subMeshRendererFolder != null)
			{
#if UNITY_EDITOR
				DestroyImmediate(m_subMeshRendererFolder);
#else
				Destroy(m_subMeshRendererFolder);
#endif
				m_subMeshRendererFolder = null;
			}
			base.OnTerminate();
		}
		private void CheckError()
		{
			if (m_meshTree != null && m_meshTransform != null) {
				string error = m_meshTree.CheckError(m_meshTransform.gameObject);
				if (!string.IsNullOrEmpty(error)) {
					Debug.LogError(error, this);
				}
			}
		}
		private bool UpdateSearchCondition(bool bPredict)
		{
			bool bSmallMove = false;
			Vector3 projectorPosition = Vector3.zero;
			Quaternion projectorRotation = Quaternion.identity;
			Plane[] prevClipPlanes = null;
			float[] prevClipDistance = null;
			int prevClipPlaneCount = 0;
			if (m_updateOnlyWhenProjectorMoved) {
				projectorPosition = m_meshTransform.InverseTransformPoint(projector.position);
				projectorRotation = projector.rotation;
				// check how much projector moved since last time mesh created
				if (m_meshState != MeshState.Uncreated) {
					Vector3 move = projectorPosition - m_projectorPositionWhenMeshCreated;
					float sqDistance = move.sqrMagnitude;
					float limit = m_margin*m_margin + Mathf.Epsilon;
					bSmallMove = (sqDistance <= limit);
					if (!bSmallMove && sqDistance <= 3*limit) {
						move = Quaternion.Inverse(m_projectorRotationWhenMeshCreated) * move;
						bSmallMove = (Mathf.Abs(move.x) <= m_margin && Mathf.Abs(move.y) <= m_margin && Mathf.Abs(move.z) <= m_margin);
					}
					if (bSmallMove) {
						if (m_projectorRotationWhenMeshCreated == projectorRotation) {
							// We don't check other properties such as fieldOfView, farClipPlane, aspectRatio, and so on...
							return false;
						}
						Quaternion rot = Quaternion.Inverse(m_projectorRotationWhenMeshCreated) * projectorRotation;
						if (0.9999f < Mathf.Abs(rot.w)) {
							// We don't check other properties such as fieldOfView, farClipPlane, aspectRatio, and so on...
							return false;
						}
						Vector3 z = (rot * Vector3.forward);
						float far = projector.farClipPlane;
						bSmallMove = (far * far * (z.x*z.x + z.y*z.y) <= limit);
						if (bSmallMove && 0.0f < m_margin) {
							// swap clip plane buffers so that we can check intersection later.
							prevClipPlaneCount = m_search.m_clipPlanes.clipPlaneCount;
							prevClipPlanes = m_search.m_clipPlanes.clipPlanes;
							m_search.m_clipPlanes.clipPlanes = m_prevClipPlanes;
							prevClipDistance = m_search.m_clipPlanes.maxDistance;
							m_search.m_clipPlanes.maxDistance = m_prevClipDistance;
						}
					}
				}
			}
			m_search.m_bOutputNormals = m_hasNormals;
			m_search.m_bOutputUVs = !m_dontUseLightmap;
			m_search.m_bBackfaceCulling = m_cullBackFace;
			m_search.m_bScissor = m_scissor;
			m_search.m_scissorMargin = m_scissorMargin;
			m_search.SetProjectionDir(projector.isOrthographic, m_meshTransform.InverseTransformDirection(projector.direction), m_meshTransform.InverseTransformPoint(projector.position));
			if (bPredict) {
				projector.GetClipPlanes(ref m_search.m_clipPlanes, m_meshTransform, predictor);
			}
			else {
				projector.GetClipPlanes(ref m_search.m_clipPlanes, m_meshTransform);
			}
			if (m_margin != 0) {
				if (bSmallMove) {
					// check whether the new projection volume intersects with the previous projection volume or not.
					// this check is not robust, only check far clip plane. if you got a problem whereby the mesh was not updated at a right timing, please don't use m_margin.
					Vector3 z = projector.direction;
					float far = projector.farClipPlane;
					if (m_search.m_clipPlanes.twoSideClipping) {
						far += Vector3.Dot(projectorPosition, z);
						for (int i = 0; i < m_search.m_clipPlanes.scissorPlaneCount - 1 && bSmallMove; ++i) {
							Plane p0 = m_search.m_clipPlanes.clipPlanes[i];
							Plane p1 = m_search.m_clipPlanes.clipPlanes[i+1];
							Vector3 l = Vector3.Cross(p0.normal, p1.normal);
							float ldotz = Vector3.Dot (l, z);
							if (Mathf.Abs(ldotz) < 0.001f) {
								continue;
							}
							Vector3 x0, x1, x2, x3;
							// project arbitrary position onto plane p0 and the other side of p0
							x0 = projectorPosition - p0.GetDistanceToPoint(projectorPosition) * p0.normal;
							x1 = x0 + m_search.m_clipPlanes.maxDistance[i] * p0.normal;
							// project x0, x1 onto plane p1 along p0
							Vector3 n = Vector3.Cross(l, p0.normal);
							float a = 1.0f/Vector3.Dot(n, p1.normal);
							x0 = x0 - a * p1.GetDistanceToPoint(x0) * n;
							x1 = x1 - a * p1.GetDistanceToPoint(x1) * n;
							n = a * m_search.m_clipPlanes.maxDistance[i+1] * n;
							x2 = x0 + n;
							x3 = x1 + n;
							// project x0, x1, x2, x3 onto far clip plane along l.
							a = 1.0f/ldotz;
							x0 = x0 + (a * (far - Vector3.Dot(x0, z))) * l;
							x1 = x1 + (a * (far - Vector3.Dot(x1, z))) * l;
							x2 = x2 + (a * (far - Vector3.Dot(x2, z))) * l;
							x3 = x3 + (a * (far - Vector3.Dot(x3, z))) * l;
							for (int j = 0; j < prevClipPlaneCount; ++j) {
								float d = prevClipPlanes[j].GetDistanceToPoint(x0);
								if (d < 0.0f || prevClipDistance[j] < d) {
									bSmallMove = false;
									break;
								}
								d = prevClipPlanes[j].GetDistanceToPoint(x1);
								if (d < 0.0f || prevClipDistance[j] < d) {
									bSmallMove = false;
									break;
								}
								d = prevClipPlanes[j].GetDistanceToPoint(x2);
								if (d < 0.0f || prevClipDistance[j] < d) {
									bSmallMove = false;
									break;
								}
								d = prevClipPlanes[j].GetDistanceToPoint(x3);
								if (d < 0.0f || prevClipDistance[j] < d) {
									bSmallMove = false;
									break;
								}
							}
						}
					}
					else {
						for (int i = 0; i < m_search.m_clipPlanes.scissorPlaneCount && bSmallMove; ++i) {
							// assuming that projectorPosition is located on each clip plane.
							Plane p0 = m_search.m_clipPlanes.clipPlanes[i];
							float d0 = p0.GetDistanceToPoint(projectorPosition);
							if (0.0001f*far < Mathf.Abs(d0)) {
								continue;
							}
							int j = (i < m_search.m_clipPlanes.scissorPlaneCount-1) ? i + 1 : 0;
							Plane p1 = m_search.m_clipPlanes.clipPlanes[j];
							float d1 = p1.GetDistanceToPoint(projectorPosition);
							if (0.0001f*far < Mathf.Abs(d1)) {
								++i;
								continue;
							}
							Vector3 l = Vector3.Cross(p0.normal, p1.normal);
							float ldotz = Vector3.Dot (l, z);
							if (Mathf.Abs(ldotz) < 0.001f) {
								continue;
							}
							Vector3 x = projectorPosition + (far/ldotz)*l;
							for (int k = 0; k < prevClipPlaneCount; ++k) {
								if (prevClipPlanes[k].GetDistanceToPoint(x) < 0.0f) {
									bSmallMove = false;
									break;
								}
							}
						}
					}
					if (bSmallMove) {
						m_search.m_clipPlanes.clipPlanes = prevClipPlanes;
						m_search.m_clipPlanes.maxDistance = prevClipDistance;
						return false;
					}
					m_prevClipPlanes = prevClipPlanes;
					m_prevClipDistance = prevClipDistance;
				}
				ClipPlanes clipPlanes = m_search.m_clipPlanes;
				float doubleMargin = 2*m_margin;
				for (int i = 0; i < clipPlanes.clipPlaneCount; ++i) {
					clipPlanes.clipPlanes[i].distance += m_margin;
					if (clipPlanes.twoSideClipping) {
						clipPlanes.maxDistance[i] += doubleMargin;
					}
				}
			}
			if (m_updateOnlyWhenProjectorMoved) {
				m_projectorPositionWhenMeshCreated = projectorPosition;
				m_projectorRotationWhenMeshCreated = projectorRotation;
			}
			return true;
		}
		private void HideSubMeshes()
		{
			if (m_subMeshRendererFolder != null)
			{
				m_subMeshRendererFolder.SetActive(false);
			}
		}
		private void ShowSubMeshes(int lightmapCount)
		{
			int childCount = m_subMeshRendererFolder.transform.childCount;
			if (lightmapCount < childCount)
			{
				do
				{
					m_subMeshRendererFolder.transform.GetChild(lightmapCount).gameObject.SetActive(false);
				} while (++lightmapCount < childCount);
			}
			m_subMeshRendererFolder.SetActive(true);
		}
		static readonly System.Type[] subMeshRendererComponents = new System.Type[] { typeof(MeshFilter), typeof(MeshRenderer), typeof(DynamicMesh) };
		MaterialPropertyBlock m_tempPropertyBlock = null;
		MaterialPropertyBlock temporaryMaterialPropertyBlock {
			get {
				if (m_tempPropertyBlock == null) {
					m_tempPropertyBlock = new MaterialPropertyBlock ();
				}
				return m_tempPropertyBlock;
			}
		}
		private DynamicMesh GetSubMesh(int lightmapIndex)
		{
			if (m_subMeshRendererFolder == null) {
				m_subMeshRendererFolder = new GameObject("_fsrSubMeshFolder");
				m_subMeshRendererFolder.hideFlags = HideFlags.HideAndDontSave;
				//m_subMeshRendererFolder.hideFlags = HideFlags.DontSave;
				m_subMeshRendererFolder.transform.parent = transform;
				m_subMeshRendererFolder.transform.localPosition = Vector3.zero;
			}
			int childCount = m_subMeshRendererFolder.transform.childCount;
			if (childCount <= lightmapIndex)
			{
				do {
					GameObject go = new GameObject("_fsrSubMeshRenderer" + childCount, subMeshRendererComponents);
					go.hideFlags = HideFlags.HideAndDontSave;
					//go.hideFlags = HideFlags.DontSave;
					go.layer = gameObject.layer;
					go.transform.parent = m_subMeshRendererFolder.transform;
					go.transform.localPosition = Vector3.zero;
					MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
					if (m_singleLightmapRenderer == null) {
						LightmapDataInPrefab lightmapData = m_meshTransform.GetComponent<LightmapDataInPrefab>();
						if (lightmapData != null) {
							meshRenderer.lightmapIndex = lightmapData.GetRealLightmapIndexFromIndexInPrefab(lightmapIndex);
							meshRenderer.lightmapScaleOffset = new Vector4(1, 1, 0, 0);
						}
						else {
							meshRenderer.lightmapIndex = lightmapIndex;
							meshRenderer.lightmapScaleOffset = new Vector4(1, 1, 0, 0);
						}
					}
					else {
						if (m_singleLightmapRenderer is Terrain)
						{
							Terrain terrain = (Terrain)m_singleLightmapRenderer;
							meshRenderer.lightmapIndex = terrain.lightmapIndex;
							meshRenderer.lightmapScaleOffset = terrain.lightmapScaleOffset;
						}
						else
						{
							Renderer renderer = (Renderer)m_singleLightmapRenderer;
							meshRenderer.lightmapIndex = renderer.lightmapIndex;
							meshRenderer.lightmapScaleOffset = renderer.lightmapScaleOffset;
						}
					}
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
					meshRenderer.castShadows = false;
#else
					meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
					meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
#endif
#if UNITY_5_4_OR_NEWER
					meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
#else
					meshRenderer.useLightProbes = false;
#endif
					if (m_projectorMaterialForLightmappedMesh != null) {
						m_projectorMaterialForLightmappedMesh.EnableKeyword("FSR_RECEIVER");
						meshRenderer.sharedMaterial = m_projectorMaterialForLightmappedMesh;
						Material srcMaterial = projectorMaterial;
						if (m_staticMaterialPropertiesToCopyFromProjector != null && 0 < m_staticMaterialPropertiesToCopyFromProjector.Length) {
							MaterialPropertyBlock properties = temporaryMaterialPropertyBlock;
							meshRenderer.GetPropertyBlock(properties);
							for (int i = 0; i < m_staticMaterialPropertiesToCopyFromProjector.Length; ++i) {
								m_staticMaterialPropertiesToCopyFromProjector[i].Copy(properties, srcMaterial);
							}
							meshRenderer.SetPropertyBlock(properties);
						}
						if (m_dynamicMaterialPropertiesToCopyFromProjector != null && 0 < m_dynamicMaterialPropertiesToCopyFromProjector.Length) {
							MaterialPropertyBlock properties = temporaryMaterialPropertyBlock;
							meshRenderer.GetPropertyBlock(properties);
							for (int i = 0; i < m_dynamicMaterialPropertiesToCopyFromProjector.Length; ++i) {
								m_dynamicMaterialPropertiesToCopyFromProjector[i].Copy(properties, srcMaterial);
							}
							meshRenderer.SetPropertyBlock(properties);
						}
					}
					else
					{
						meshRenderer.sharedMaterial = projectorMaterial;
					}
					meshRenderer.receiveShadows = m_renderer.receiveShadows;
					ProjectionReceiverRenderer receiverRenderer = go.AddComponent<ProjectionReceiverRenderer>();
					if (unityProjector != null)
					{
						receiverRenderer.unityProjector = unityProjector;
					}
					else
					{
						receiverRenderer.customProjector = customProjector;
					}
					go.GetComponent<DynamicMesh>().Awake();
					Debug.Assert(m_subMeshRendererFolder.transform.GetChild(childCount) == go.transform);
				} while (++childCount <= lightmapIndex);
			}
			return m_subMeshRendererFolder.transform.GetChild(lightmapIndex).GetComponent<DynamicMesh>();
		}
		private void SetupSubMesh(DynamicMesh mesh)
		{
			mesh.gameObject.SetActive(true);
			if (m_dynamicMaterialPropertiesToCopyFromProjector != null && 0 < m_dynamicMaterialPropertiesToCopyFromProjector.Length) {
				MaterialPropertyBlock properties = temporaryMaterialPropertyBlock;
				Renderer renderer = GetComponent<Renderer>();
				renderer.GetPropertyBlock(properties);
				Material srcMaterial = projectorMaterial;
				for (int j = 0; j < m_dynamicMaterialPropertiesToCopyFromProjector.Length; ++j) {
					m_dynamicMaterialPropertiesToCopyFromProjector[j].Copy(properties, srcMaterial);
				}
				renderer.SetPropertyBlock(properties);
			}
		}
		public void UpdateMesh()
		{
#if UNITY_EDITOR
			if (m_meshTree == null || m_search == null || m_search.GetType() != m_meshTree.GetSearchType()) {
				if (!IsReady()) {
					return;
				}
				OnAwake();
				OnStart();
			}
			if (m_meshTree == null || m_meshTransform == null) {
				return;
			}
#else
			if (m_meshTree == null || m_meshTransform == null) {
				Hide(true);
				return;
			}
#endif
			if (!m_meshTree.IsBuildFinished() || projector == null) {
				Hide(true);
				return;
			}
			if (m_selfTransform.parent != null) {
				Vector3 axisX = m_meshTransform.TransformVector(Vector3.right);
				Vector3 axisY = m_meshTransform.TransformVector(Vector3.up);
				Vector3 axisZ = m_meshTransform.TransformVector(Vector3.forward);
				axisX = m_selfTransform.parent.InverseTransformVector(axisX);
				axisY = m_selfTransform.parent.InverseTransformVector(axisY);
				axisZ = m_selfTransform.parent.InverseTransformVector(axisZ);
				if (Debug.isDebugBuild || Application.isEditor) {
					// check if the axises are orthogonal.
                    if (!(Mathf.Abs(Vector3.Dot(axisX, axisY)) < 0.001f && Mathf.Abs(Vector3.Dot(axisY, axisZ)) < 0.001f && Mathf.Abs(Vector3.Dot(axisZ, axisX)) < 0.001f)) {
						Debug.LogWarning("Cannot convert mesh transform! The transform matrix from mesh parent to this parent is not orthogonal! Please consider putting this receiver object under another object or the root of the hierarchy.", this);
					}
				}
				m_selfTransform.localScale = new Vector3(axisX.magnitude, axisY.magnitude, axisZ.magnitude);
			}
			else {
				m_selfTransform.localScale = m_meshTransform.lossyScale;
			}
			m_selfTransform.rotation = m_meshTransform.rotation;
			m_selfTransform.position = m_meshTransform.position;
			if (predictEnabled) {
				SyncAndSwap();
				if (!UpdateSearchCondition(true)) {
					return;
				}
				m_meshState = MeshState.Creating;
				m_search.AsyncStart(m_meshTree);
			}
			else if (m_multiThreading) {
				m_search.Wait();
				if (!UpdateSearchCondition(false)) {
					SyncAndSwap();
					return;
				}
				currentMesh.bounds = m_meshTree.bounds;
				m_meshState = MeshState.Creating;
				m_search.AsyncStart(m_meshTree);
				Hide(false);
			}
			else {
				if (!UpdateSearchCondition(false)) {
					SyncAndSwap();
					return;
				}
				m_meshState = MeshState.Creating;
				m_meshTree.Search(m_search);
				SyncAndSwap();
			}
		}
		private Material projectorMaterial {
			get {
				if (unityProjector != null)
				{
					return unityProjector.material;
				}
				else
				{
					return m_renderer.sharedMaterial;
				}
			}
		}
		public void SyncAndSwap()
		{
#if UNITY_EDITOR
			if (m_search == null) {
				return;
			}
#endif
			if (m_meshState == MeshState.Creating) {
				m_search.Wait();
				if (m_singleLightmapRenderer == null || m_dontUseLightmap) {
					if (m_search.HasResult(-1))
					{
						Hide (false);
						SwapMesh ();
						currentMesh.Clear ();
						m_search.FillMesh (currentMesh, -1);
					}
					else
					{
						Hide(true);
					}
#if UNITY_2017_1_OR_NEWER
					int lightmapCount = m_search.lightmapCount;
					if (0 < lightmapCount && m_search.m_bOutputUVs && !m_dontUseLightmap)
					{
						for (int i = 0; i < lightmapCount; ++i)
						{
							DynamicMesh mesh = GetSubMesh(i);
							if (m_search.HasResult(i))
							{
								mesh.SwapMesh();
								mesh.currentMesh.Clear();
								m_search.FillMesh(mesh.currentMesh, i);
								SetupSubMesh(mesh);
							}
							else
							{
								mesh.gameObject.SetActive(false);
							}
						}
						ShowSubMeshes(lightmapCount);
					}
					else
					{
						HideSubMeshes();
					}
#endif
				}
#if UNITY_2017_1_OR_NEWER
				else {
					Hide(true);
					if (m_search.HasResult (-1)) {
						DynamicMesh mesh = GetSubMesh (0);
						mesh.SwapMesh ();
						mesh.currentMesh.Clear ();
						m_search.FillMesh (mesh.currentMesh, -1);
						SetupSubMesh (mesh);
						ShowSubMeshes (1);
					} else {
						HideSubMeshes();
					}
				}
#endif
				m_meshState = MeshState.Created;
			}
		}
		protected override void OnUpdate ()
		{
			UpdateMesh();
		}
		void OnWillRenderObject()
		{
			if (!predictEnabled) {
				SyncAndSwap();
			}
		}
#if UNITY_EDITOR
		public override void DrawMeshesAsGizmos ()
		{
			base.DrawMeshesAsGizmos ();
			if (m_subMeshRendererFolder != null && m_subMeshRendererFolder.activeSelf) {
				for (int i = 0, count = m_subMeshRendererFolder.transform.childCount; i < count; ++i) {
					Transform child = m_subMeshRendererFolder.transform.GetChild (i);
					if (child.gameObject.activeSelf) {
						DynamicMesh mesh = child.GetComponent<DynamicMesh> ();
						if (0 < mesh.currentMesh.vertexCount) {
							Graphics.DrawMeshNow (mesh.currentMesh, child.localToWorldMatrix);
						}
					}
				}
			}
		}
#endif
	}
}
