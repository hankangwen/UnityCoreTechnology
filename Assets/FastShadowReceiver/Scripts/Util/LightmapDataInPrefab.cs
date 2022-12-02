//
// LightmapDataInPrefab.cs
//
// Fast Shadow Receiver
//
// Copyright 2019 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//

using System.Collections.Generic;
using UnityEngine;

namespace FastShadowReceiver {
#if UNITY_2018_3_OR_NEWER
	[ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
	public class LightmapDataInPrefab : MonoBehaviour {
#pragma warning disable 0649
		[System.Serializable]
		private struct RendererData
		{
			public Renderer renderer;
			public int lightmapIndex;
			public Vector4 lightmapScaleOffset;
		}
		[System.Serializable]
		private struct LightmapData
		{
			public Texture2D lightmapColor;
			public Texture2D lightmapDir;
			public Texture2D shadowMask;
		}
		public enum AutoMode {
			None,
			AutoCollect,
			AutoCollectAndSave,
			AutoApply
		}
		[SerializeField]
		private RendererData[] m_renderers;
		[SerializeField]
		private LightmapData[] m_lightmaps;
		[SerializeField]
		private AutoMode m_autoMode = AutoMode.AutoCollectAndSave;
#pragma warning restore 0649

		public AutoMode autoMode { get { return m_autoMode; } }

		public int GetRealLightmapIndexFromIndexInPrefab(int indexInPrefab)
		{
			var currentLightmaps = LightmapSettings.lightmaps;
			for (int i = 0, endi = currentLightmaps.Length; i < endi; ++i) {
				if (currentLightmaps [i].lightmapColor == m_lightmaps [indexInPrefab].lightmapColor
					&& currentLightmaps [i].lightmapDir == m_lightmaps [indexInPrefab].lightmapDir
					&& currentLightmaps [i].shadowMask == m_lightmaps [indexInPrefab].shadowMask) {
					return i;
				}
			}
			return -1;
		}
		public int GetLightmapIndexInPrefab(Renderer r)
		{
			for (int i = 0, endi = m_renderers.Length; i < endi; ++i) {
				if (m_renderers[i].renderer == r) {
					return m_renderers[i].lightmapIndex;
				}
			}
			return r.lightmapIndex;
		}

		public void ApplyLightmaps()
		{
			if (m_renderers == null || m_lightmaps == null || m_renderers.Length == 0 || m_lightmaps.Length == 0) {
				return;
			}
			var currentLightmaps = LightmapSettings.lightmaps;
			int lightmapCountToAdd = 0;
			Dictionary<int, int> indexMap = new Dictionary<int, int>();
			for (int i = 0, endi = m_lightmaps.Length; i < endi; ++i) {
				bool found = false;
				for (int j = 0, endj = currentLightmaps.Length; j < endj; ++j) {
					if (currentLightmaps[j].lightmapColor == m_lightmaps[i].lightmapColor
					    && currentLightmaps[j].lightmapDir == m_lightmaps[i].lightmapDir
					    && currentLightmaps[j].shadowMask == m_lightmaps[i].shadowMask) {
						found = true;
						indexMap[i] = j;
						break;
					}
				}
				if (!found) {
					indexMap[i] = currentLightmaps.Length + lightmapCountToAdd++;
				}
			}
			if (0 < lightmapCountToAdd) {
				var newLightmaps = new UnityEngine.LightmapData[currentLightmaps.Length + lightmapCountToAdd];
				currentLightmaps.CopyTo(newLightmaps, 0);
				for (int i = 0, endi = m_lightmaps.Length; i < endi; ++i) {
					int newIndex = indexMap[i];
					if (currentLightmaps.Length <= newIndex) {
						newLightmaps[newIndex] = new UnityEngine.LightmapData();
						newLightmaps[newIndex].lightmapColor = m_lightmaps[i].lightmapColor;
						newLightmaps[newIndex].lightmapDir = m_lightmaps[i].lightmapDir;
						newLightmaps[newIndex].shadowMask = m_lightmaps[i].shadowMask;
					}
				}
				LightmapSettings.lightmaps = newLightmaps;
				LightmapSettingsCache.UncacheLightmapData();
			}
			foreach (RendererData r in m_renderers) {
				r.renderer.lightmapIndex = indexMap[r.lightmapIndex];
				r.renderer.lightmapScaleOffset = r.lightmapScaleOffset;
			}
		}

		void Awake() {
#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
			if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
			{
				return;
			}
#endif
			if (m_autoMode == AutoMode.AutoApply) {
				ApplyLightmaps ();
			}
		}
#if UNITY_EDITOR
		// cannot call SetPropertyModifications during OnValidate...
		// instead, call ValidateAutoModeOfPrefabAsset from LightmapDataInPrefabEditor.
		//private void OnValidate()
		//{
		//	if (autoMode != LightmapDataInPrefab.AutoMode.AutoApply && autoMode != LightmapDataInPrefab.AutoMode.None && !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(gameObject)))
		//	{
		//		ValidateAutoModeOfPrefabAsset();
		//	}
		//}
		public void ValidateAutoModeOfPrefabAsset()
		{
			if (autoMode != LightmapDataInPrefab.AutoMode.AutoApply && autoMode != LightmapDataInPrefab.AutoMode.None && !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(gameObject))) {
				// prefab asset must be AutoMode.AutoApply or AutoMode.None
				LightmapDataInPrefab[] instances = Object.FindObjectsOfType<LightmapDataInPrefab>();
				foreach (LightmapDataInPrefab instance in instances)
				{
					if (instance != this &&
#if UNITY_2018_2_OR_NEWER
						UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance) == this
#else
						UnityEditor.PrefabUtility.GetPrefabParent(instance.gameObject) == gameObject
#endif
						)
					{
						AddAutoModeModification(instance.gameObject, this, instance.autoMode);
					}
				}
				m_autoMode = AutoMode.AutoApply;
			}
		}
		private static void AddAutoModeModification(GameObject instance, LightmapDataInPrefab prefab, AutoMode autoMode)
		{
			UnityEditor.PropertyModification[] modifications = UnityEditor.PrefabUtility.GetPropertyModifications(instance);
			bool found = false;
			if (modifications != null)
			{
				foreach (var mod in modifications)
				{
					if (mod.target == prefab && mod.propertyPath == "m_autoMode")
					{
						mod.value = ((int)autoMode).ToString();
						found = true;
						break;
					}
				}
			}
			if (!found)
			{
				UnityEditor.PropertyModification[] newModifications = new UnityEditor.PropertyModification[modifications == null ? 1 : modifications.Length + 1];
				int indexToAdd = 0;
				if (modifications != null && 0 < modifications.Length)
				{
					modifications.CopyTo(newModifications, 0);
					indexToAdd = modifications.Length;
				}
				newModifications[indexToAdd] = new UnityEditor.PropertyModification();
				newModifications[indexToAdd].target = prefab;
				newModifications[indexToAdd].propertyPath = "m_autoMode";
				newModifications[indexToAdd].value = ((int)autoMode).ToString();
				modifications = newModifications;
			}
			UnityEditor.PrefabUtility.SetPropertyModifications(instance, modifications);
		}
		public void CollectLightmaps()
		{
			var srcLightmaps = LightmapSettings.lightmaps;
			Renderer[] renderers = GetComponentsInChildren<Renderer>();
			int rendererCount = 0;
			foreach (Renderer r in renderers)
			{
				if (0 <= r.lightmapIndex && r.lightmapIndex < srcLightmaps.Length)
				{
					++rendererCount;
				}
			}
			// Do not modify m_renderers field and m_lightmaps field directly. Use SerializedObject to set propery modifications.
			UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(this);
			UnityEditor.SerializedProperty renderersProperty = serializedObject.FindProperty("m_renderers");
			UnityEditor.SerializedProperty lightmapsProperty = serializedObject.FindProperty("m_lightmaps");
			renderersProperty.arraySize = rendererCount;
			lightmapsProperty.arraySize = srcLightmaps.Length;
			for (int i = 0; i < srcLightmaps.Length; ++i)
			{
				lightmapsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("lightmapColor").objectReferenceValue = srcLightmaps[i].lightmapColor;
				lightmapsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("lightmapDir").objectReferenceValue = srcLightmaps[i].lightmapDir;
				lightmapsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("shadowMask").objectReferenceValue = srcLightmaps[i].shadowMask;
			}
			int count = 0;
			foreach (Renderer r in renderers)
			{
				if (0 <= r.lightmapIndex && r.lightmapIndex < srcLightmaps.Length)
				{
					renderersProperty.GetArrayElementAtIndex(count).FindPropertyRelative("renderer").objectReferenceValue = r;
					renderersProperty.GetArrayElementAtIndex(count).FindPropertyRelative("lightmapIndex").intValue = r.lightmapIndex;
					renderersProperty.GetArrayElementAtIndex(count).FindPropertyRelative("lightmapScaleOffset").vector4Value = r.lightmapScaleOffset;
					++count;
				}
			}
			serializedObject.ApplyModifiedProperties();
			if (m_autoMode == AutoMode.AutoCollectAndSave)
			{
				SavePrefab();
			}
		}
		public void SavePrefab()
		{
			GameObject gameObject = this.gameObject;
#if UNITY_2018_2_OR_NEWER
			GameObject prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject) as GameObject;
#else
			GameObject prefab = UnityEditor.PrefabUtility.GetPrefabParent(gameObject) as GameObject;
#endif
			if (prefab == null) {
				string path = UnityEditor.EditorUtility.SaveFilePanelInProject ("Save Prefab", gameObject.name, "prefab", "");
				if (!string.IsNullOrEmpty (path)) {
#if UNITY_2018_3_OR_NEWER
					prefab = UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path, UnityEditor.InteractionMode.AutomatedAction);
#else
					prefab = UnityEditor.PrefabUtility.CreatePrefab(path, gameObject);
#endif
				}
			}
			if (prefab != null) {
				AutoMode autoMode = m_autoMode;
				LightmapDataInPrefab lightmapData = prefab.GetComponent<LightmapDataInPrefab>();
#if UNITY_2018_3_OR_NEWER
				string prefabPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);
				if (lightmapData == null)
				{
					UnityEditor.PrefabUtility.ApplyAddedComponent(this, prefabPath, UnityEditor.InteractionMode.AutomatedAction);
					lightmapData = prefab.GetComponent<LightmapDataInPrefab>();
				}
				foreach (var r in m_renderers) {
					if (UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(r.renderer.gameObject) == null) {
						Debug.Log("Add new renderer object to the prefab", r.renderer);
						UnityEditor.PrefabUtility.ApplyAddedGameObject(r.renderer.gameObject, prefabPath, UnityEditor.InteractionMode.AutomatedAction);
					}
				}
				UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(this);
				UnityEditor.SerializedProperty propertyRenderers = serializedObject.FindProperty("m_renderers");
				UnityEditor.SerializedProperty propertyLightmaps = serializedObject.FindProperty("m_lightmaps");
				UnityEditor.PrefabUtility.ApplyPropertyOverride(propertyRenderers, prefabPath, UnityEditor.InteractionMode.AutomatedAction);
				UnityEditor.PrefabUtility.ApplyPropertyOverride(propertyLightmaps, prefabPath, UnityEditor.InteractionMode.AutomatedAction);
#else
				prefab = UnityEditor.PrefabUtility.ReplacePrefab(gameObject, prefab);
				if (lightmapData == null)
				{
					// If the old prefab didn't have LightmapDataInPrefab component,
					// 'this' component will remain as added component to the prefub.
					// Thus, this gameObject will have two LightmapDataInPrefab components;
					// one is from the prefab, and the other is from added components.
					lightmapData = prefab.GetComponent<LightmapDataInPrefab>();
#if UNITY_2018_2_OR_NEWER
					if (UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(this) != lightmapData)
#else
					Object parent = UnityEditor.PrefabUtility.GetPrefabParent(this);
					if (parent != lightmapData)
#endif
					{
						Object.DestroyImmediate(this);
					}
				}
#endif
				if (lightmapData.m_autoMode != AutoMode.AutoApply && lightmapData.m_autoMode != AutoMode.None) {
					AddAutoModeModification(gameObject, lightmapData, autoMode);
					lightmapData.m_autoMode = AutoMode.AutoApply;
				}
				UnityEditor.EditorUtility.SetDirty (prefab);
			}
		}
		public void EnableAutoCollect()
		{
#if UNITY_2019_2_OR_NEWER
			UnityEditor.Lightmapping.bakeCompleted += CollectLightmaps;
#else
			UnityEditor.Lightmapping.completed += CollectLightmaps;
#endif
		}
		public void DisableAutoCollect()
		{
#if UNITY_2019_2_OR_NEWER
			UnityEditor.Lightmapping.bakeCompleted -= CollectLightmaps;
#else
			UnityEditor.Lightmapping.completed -= CollectLightmaps;
#endif
		}
		void OnEnable()
		{
#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
			if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
			{
				return;
			}
#endif
			if (m_autoMode == AutoMode.AutoCollect || m_autoMode == AutoMode.AutoCollectAndSave) {
				EnableAutoCollect();
			}
		}
		void OnDisable()
		{
#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
			if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
			{
				return;
			}
#endif
			if (m_autoMode == AutoMode.AutoCollect || m_autoMode == AutoMode.AutoCollectAndSave) {
				DisableAutoCollect();
			}
		}
#endif
	}
}
