//
// ProjectionReceiverRenderer.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;

namespace FastShadowReceiver {
	/// <summary>
	/// The objective of this component is to setup projection matrix to renderer.material.
	/// The material should have one of "FastShadowReceiver/Projector/XXXXX" shaders.
	/// </summary>
	//[ExecuteInEditMode] // this component cannot be executable in Edit Mode, because this component needs to set a shader keyword to non-shared renderer.material.
	public class ProjectionReceiverRenderer : MonoBehaviour {
		[SerializeField][HideInInspector]
		private Component m_projectorComponent;
		public Projector unityProjector
		{
			get { return m_projectorComponent as Projector; }
			set {
				if (m_projectorComponent != value) {
					m_projectorComponent = value;
					if (value != null) {
						if (m_projector is UnityProjector) {
							(m_projector as UnityProjector).projector = value;
						}
						else {
							m_projector = new UnityProjector(value);
						}
					}
					else {
						m_projector = null;
					}
				}
			}
		}
		public ProjectorBase customProjector
		{
			get { return m_projectorComponent as ProjectorBase; }
			set {
				if (m_projectorComponent != value) {
					m_projectorComponent = value;
					m_projector = value;
				}
			}
		}
		private Material m_material;
		private IProjector m_projector;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
		private Transform m_transform;
#endif
		private Renderer m_renderer;
		private MaterialPropertyBlock m_propertyBlock;
		void Awake()
		{
			m_renderer = GetComponent<Renderer>();
			if (m_renderer == null) {
				if (Debug.isDebugBuild || Application.isEditor) {
					Debug.LogError("No renderer was found!", this);
				}
				return;
			}
			if (!m_renderer.sharedMaterial.IsKeywordEnabled("FSR_RECEIVER")) {
				// if shader keyword "FSR_RECEIVER" is not enabled, create a new instance of the material and enable it. (do not change the shared material)
				m_renderer.material.EnableKeyword ("FSR_RECEIVER");
			}
			if (m_projectorComponent != null) {
				if (m_projectorComponent is IProjector) {
					m_projector = m_projectorComponent as IProjector;
				}
				else if (m_projectorComponent is Projector) {
					m_projector = new UnityProjector(m_projectorComponent as Projector);
				}
			}
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
			m_transform = transform;
#endif
			if (m_propertyBlock == null)
			{
				m_propertyBlock = new MaterialPropertyBlock();
			}
#if UNITY_2017_1_OR_NEWER
			UpdateShadowMaskProperties(m_renderer);
#endif
		}
		void Start()
		{
			if (m_projector == null) {
				ReceiverBase receiver = GetComponent<ReceiverBase>();
				if (receiver != null) {
					m_projector = receiver.projectorInterface;
				}
				else if (Debug.isDebugBuild || Application.isEditor) {
					Debug.LogError("No projector was found!", this);
				}
			}
		}
		
		void OnWillRenderObject()
		{
			if (m_projector == null) {
				return;
			}
			Matrix4x4 m = m_renderer.localToWorldMatrix;
			if (IsVertexPrescaled()) {
				m.SetColumn(0, m.GetColumn(0).normalized);
				m.SetColumn(1, m.GetColumn(1).normalized);
				m.SetColumn(2, m.GetColumn(2).normalized);
			}
			Vector4 dir = m.inverse.MultiplyVector(m_projector.direction).normalized;
			m = m_projector.uvProjectionMatrix * m;
			m_renderer.GetPropertyBlock(m_propertyBlock);
			m_propertyBlock.SetMatrix("_FSRProjector", m);
			m_propertyBlock.SetVector("_FSRProjectDir", dir);
			m_renderer.SetPropertyBlock(m_propertyBlock);
		}
		bool IsVertexPrescaled()
		{
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
			Vector3 scale = m_transform.localScale;
			float max = Mathf.Max (scale.x, Mathf.Max (scale.y, scale.z));
			float min = Mathf.Min (scale.x, Mathf.Min (scale.y, scale.z));
			return 0.00001f * max < (max - min);
#else
			return false;
#endif
		}
#if UNITY_2017_1_OR_NEWER
		const string LIGHTMAP_TEXTURE_NAME = "unity_Lightmap";
		const string SHADOWMASK_TEXTURE_NAME = "unity_ShadowMask";
		const string LIGHTMAP_ST_NAME = "unity_LightmapST";
		public void UpdateShadowMaskProperties(Renderer renderer)
		{
			int i = renderer.lightmapIndex;
			LightmapData[] lightmaps = LightmapSettingsCache.lightmaps;
			if (lightmaps != null && 0 <= i && i < lightmaps.Length) {
				if (m_propertyBlock == null) {
					m_propertyBlock = new MaterialPropertyBlock ();
				}
				LightmapData lightmapData = lightmaps[i];
				renderer.GetPropertyBlock (m_propertyBlock);
				if (lightmapData.lightmapColor != null) {
					m_propertyBlock.SetTexture (LIGHTMAP_TEXTURE_NAME, lightmapData.lightmapColor);
				}
				if (lightmapData.shadowMask != null) {
					m_propertyBlock.SetTexture (SHADOWMASK_TEXTURE_NAME, lightmapData.shadowMask);
				}
				m_propertyBlock.SetVector (LIGHTMAP_ST_NAME, renderer.lightmapScaleOffset);
				renderer.SetPropertyBlock (m_propertyBlock);
			}
		}
#endif
	}
}
