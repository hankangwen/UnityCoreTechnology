//
// MeshTreeEditor.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using UnityEditor;

namespace FastShadowReceiver.Editor {
	public class MeshTreeEditor : EditorBase {
		protected static T CreateMeshTree<T>(string fileName) where T : ScriptableObject
		{
			string folderPath = GetSelectedFolderPath();
			string path = folderPath + fileName;
			path = AssetDatabase.GenerateUniqueAssetPath(path);
			AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
			return AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
		}
		private long m_memoryUsage;
		private bool m_isBuilding;
		private bool m_showBuiltData;
		void OnEnable()
		{
			MeshTreeBase meshTree = target as MeshTreeBase;
			m_isBuilding = meshTree.IsBuilding();
			if (!m_isBuilding) {
				m_memoryUsage = meshTree.GetMemoryUsage();
			}
			m_showBuiltData = true;
		}
		private int m_editingRenderType = -1;
		private string m_editingString;
		private void ApplyEditingRenderType()
		{
			if (0 <= m_editingRenderType) {
				Undo.RegisterCompleteObjectUndo(target, "Inspector");
				SerializedProperty excludeRenderTypes = serializedObject.FindProperty("m_excludeRenderTypes");
				if (m_editingRenderType < excludeRenderTypes.arraySize) {
					SerializedProperty property = excludeRenderTypes.GetArrayElementAtIndex(m_editingRenderType);
					if (m_editingString != property.stringValue) {
						if (string.IsNullOrEmpty(m_editingString)) {
							excludeRenderTypes.DeleteArrayElementAtIndex(m_editingRenderType);
						}
						else if (m_editingString != property.stringValue) {
							property.stringValue = m_editingString;
						}
					}
				}
				else {
					int index = excludeRenderTypes.arraySize;
					excludeRenderTypes.InsertArrayElementAtIndex(index);
					excludeRenderTypes.GetArrayElementAtIndex(index).stringValue = m_editingString;
				}
				EditorUtility.SetDirty(target);
				m_editingRenderType = -1;
				m_editingString = "";
			}
		}
		private void CancelEditingRenderType()
		{
			m_editingRenderType = -1;
			m_editingString = "";
		}
		GUIStyle m_errorStyle = null;
		GUIStyle errorStyle
		{
			get {
				if (m_errorStyle == null)
				{
					m_errorStyle = new GUIStyle();
					m_errorStyle.richText = true;
					m_errorStyle.wordWrap = true;
				}
				return m_errorStyle;
			}
		}
		public override void OnInspectorGUI ()
		{
			MeshTreeBase meshTreeBase = target as MeshTreeBase;
			if (meshTreeBase.IsBuilding()) {
				GUI.enabled = false;
			}
			DrawDefaultInspector();
			if (meshTreeBase is MeshTree) {
				MeshTree meshTree = meshTreeBase as MeshTree;
				Object meshObject = EditorGUILayout.ObjectField(meshTree.srcMesh is Mesh ? "Mesh" : "Root Object", meshTree.srcMesh, typeof(Object), true);
				if (meshObject != meshTree.srcMesh && meshObject is GameObject && Event.current.command) {
					MeshFilter meshFilter = ((GameObject)meshObject).GetComponent<MeshFilter>();
					if (meshFilter != null && meshFilter.sharedMesh != null)  {
						meshObject = meshFilter.sharedMesh;
					}
				}
				if (meshObject != meshTree.srcMesh) {
					Undo.RegisterCompleteObjectUndo(meshTree, "Inspector");
#if UNITY_2018_3_OR_NEWER
					// This code block converts a GameObject in Prefab Mode to a prefab asset.
					if (meshObject is GameObject)
					{
#if UNITY_2021_2_OR_NEWER
						UnityEditor.SceneManagement.PrefabStage stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
						UnityEditor.Experimental.SceneManagement.PrefabStage stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif
						if (stage != null && stage.IsPartOfPrefabContents(meshObject as GameObject))
						{
							// the next line doesn't work because meshObject may not be a prefab instance. 
							// meshObject = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(meshObject, stage.assetPath);
							if (stage.scene.isDirty)
							{
								Debug.LogError("Please save the prefab before setting Root Object in Prefab Mode.");
								meshObject = meshTree.srcMesh;
							}
							else
							{
#if UNITY_2020_1_OR_NEWER
								GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(stage.assetPath);
#else
								GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(stage.prefabAssetPath);
#endif
								GameObject rootObject = stage.prefabContentsRoot;
								Transform parent = rootObject.transform;
								Transform targetTransform = ((GameObject)meshObject).transform;
								while (parent != targetTransform)
								{
									Transform child = targetTransform;
									while (child.parent != parent)
									{
										child = child.parent;
									}
									for (int i = 0; i < parent.childCount; ++i)
									{
										if (parent.GetChild(i) == child)
										{
											prefab = prefab.transform.GetChild(i).gameObject;
											break;
										}
									}
									parent = child;
								}
								meshObject = prefab;
							}
						}
					}
#endif
					meshTree.srcMesh = meshObject;
					EditorUtility.SetDirty(meshTree);
				}
				if (meshObject != null && meshObject is GameObject) {
#if UNITY_2018_3_OR_NEWER
					if (!PrefabUtility.IsPartOfPrefabAsset(meshObject))
#else
					if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(meshObject)))
#endif
					{
						GUILayout.TextArea("<color=red>A reference to a scene object will not be serialized in Asset data. You will need to set the root object again when you rebuild the tree. Use a prefab instead.</color>", errorStyle);
					}
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_layerMask"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_excludeIgnoreProjectorMaterial"));
					SerializedProperty excludeRenderTypes = serializedObject.FindProperty("m_excludeRenderTypes");
					excludeRenderTypes.isExpanded = EditorGUILayout.Foldout(excludeRenderTypes.isExpanded, "Exclude Render Types");
					if (excludeRenderTypes.isExpanded) {
						for (int i = 0; i < excludeRenderTypes.arraySize + 1; ++i) {
							string renderType;
							if (m_editingRenderType == i) {
								renderType = m_editingString;
							}
							else if (i < excludeRenderTypes.arraySize) {
								renderType = excludeRenderTypes.GetArrayElementAtIndex(i).stringValue;
							}
							else {
								renderType = "";
							}
							string controlName = "RenderType" + i.ToString();
							GUI.SetNextControlName(controlName);
							string newRenderType = EditorGUILayout.TextField(renderType);
							string focusedControl = GUI.GetNameOfFocusedControl();
							if (m_editingRenderType == i) {
								m_editingString = newRenderType;
								if (focusedControl != controlName || (/*Event.current.isKey &&*/ (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))) {
									ApplyEditingRenderType();
								}
								else if (/*Event.current.isKey &&*/ Event.current.keyCode == KeyCode.Escape) {
									CancelEditingRenderType();
								}
							}
							else if (renderType != newRenderType) {
								ApplyEditingRenderType();
								m_editingRenderType = i;
								m_editingString = newRenderType;
							}
						}
					}
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_scaledOffset"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_fixedOffset"));
					serializedObject.ApplyModifiedProperties();
				}
			}
			if (meshTreeBase is TerrainMeshTree) {
				TerrainMeshTree terrainMeshTree = meshTreeBase as TerrainMeshTree;
				Object terrainObj = EditorGUILayout.ObjectField("Terrain Data", terrainMeshTree.terrainData, typeof(Object), true);
				if (terrainObj != terrainMeshTree.terrainData) {
					TerrainData terrainData = terrainObj as TerrainData;
					if (terrainData == null && terrainObj is GameObject) {
						Terrain terrain = ((GameObject)terrainObj).GetComponent<Terrain>();
						if (terrain != null) {
							terrainData = terrain.terrainData;
						}
					}
					if (terrainData != null || terrainObj == null) {
						terrainMeshTree.terrainData = terrainData;
					}
				}
			}
			if (meshTreeBase.IsBuilding()) {
				GUI.enabled = true;
				m_isBuilding = true;
				EditorGUILayout.LabelField("Building... " + Mathf.FloorToInt(100*meshTreeBase.GetBuildProgress()).ToString() + "%");
			}
			else {
				GUI.enabled = meshTreeBase.IsReadyToBuild();
				if (m_isBuilding) {
					m_isBuilding = false;
					m_memoryUsage = meshTreeBase.GetMemoryUsage();
					EditorUtility.SetDirty(meshTreeBase);
				}
				if (meshTreeBase.IsPrebuilt()) {
					m_showBuiltData = EditorGUILayout.Foldout(m_showBuiltData, "Built Tree Info");
					if (m_showBuiltData) {
						string memorySize;
						float mb = m_memoryUsage/(1024.0f*1024.0f);
						if (1.0f <= mb) {
							memorySize = mb.ToString("f3") + "MB";
						}
						else {
							float kb = m_memoryUsage/1024.0f;
							memorySize = kb.ToString("f3") + "KB";
						}
						EditorGUILayout.LabelField("Memory", memorySize);
						EditorGUILayout.LabelField("Node Count", meshTreeBase.GetNodeCount().ToString());
					}
				}
				if (GUILayout.Button (meshTreeBase.IsPrebuilt() ? "Rebuild" : "Build")) {
					ApplyEditingRenderType();
					m_isBuilding = true;
					if (!BuildMeshTree(meshTreeBase))
					{
						m_isBuilding = false;
					}
				}
			}
			GUI.enabled = true;
		}
		public static bool BuildMeshTree(MeshTreeBase meshTreeBase)
		{
			MeshTree meshTree = meshTreeBase as MeshTree;
			GameObject prefab = null;
			GameObject tempObject = null;
			if (meshTree != null && meshTree.srcMesh is GameObject) {
#if UNITY_2018_3_OR_NEWER
				if (PrefabUtility.IsPartOfPrefabAsset(meshTree.srcMesh))
#else
				if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(meshTree.srcMesh)))
#endif
				{
					prefab = (GameObject)meshTree.srcMesh;
#if UNITY_2017_1_OR_NEWER
					if (meshTree.m_lightmapUVIndex != MeshTree.UVIndex.None && prefab.GetComponent<LightmapDataInPrefab>() == null)
					{
						// prefab object may not have lightmap indices. Try to find the corresponding instance in the scene.
						GameObject instance = GameObject.Find(prefab.name);
						if (instance != null &&
#if UNITY_2018_2_OR_NEWER
							PrefabUtility.GetCorrespondingObjectFromSource(instance) != prefab
#else
							PrefabUtility.GetPrefabParent(instance) != prefab
#endif
						)
						{
							instance = null;
						}
						if (instance == null && !string.IsNullOrEmpty(prefab.tag))
						{
							GameObject[] instances = GameObject.FindGameObjectsWithTag(prefab.tag);
							foreach (GameObject i in instances)
							{
#if UNITY_2018_2_OR_NEWER
								if (PrefabUtility.GetCorrespondingObjectFromSource(i) == prefab)
#else
								if (PrefabUtility.GetPrefabParent(i) == prefab)
#endif
								{
									instance = i;
									break;
								}
							}
						}
						if (instance == null)
						{
							Transform[] instances = FindObjectsOfType<Transform>();
							foreach (Transform i in instances)
							{
#if UNITY_2018_2_OR_NEWER
								if (PrefabUtility.GetCorrespondingObjectFromSource(i.gameObject) == prefab)
#else
								if (PrefabUtility.GetPrefabParent(i.gameObject) == prefab)
#endif
								{
									instance = i.gameObject;
									break;
								}
							}
						}
						if (instance == null)
						{
							if (!EditorUtility.DisplayDialog("Building a mesh tree from a prefab object",
								"A prefab object may not have lightmap indices. To build a lightmapped meshtree, please open a scene that has a GameObject linked with the prefab.",
								"Build Anyway", "Cancel Build"))
							{
								return false;
							}
						}
						meshTree.srcMesh = instance;
					}
					else
					{
						meshTree.srcMesh = null;
					}
#endif
					if (meshTree.srcMesh == null)
					{
#if UNITY_2018_3_OR_NEWER
						// make sure that all the contents in prefab are accessible.
						tempObject = PrefabUtility.LoadPrefabContents(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab));
						meshTree.srcMesh = prefab;
#else
						tempObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
						meshTree.srcMesh = tempObject;
						while (tempObject.transform.parent != null)
						{
							tempObject = tempObject.transform.parent.gameObject;
						}
						tempObject.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
#endif
					}
				}
			}
			meshTreeBase.AsyncBuild();
			if (prefab != null) {
				meshTree.SetSrcMeshWithoutClear(prefab);
				if (tempObject != null)
				{
					try
					{
#if UNITY_2018_3_OR_NEWER
						PrefabUtility.UnloadPrefabContents(tempObject);
#else
						tempObject.SetActive(false);
						DestroyImmediate(tempObject);
#endif
					}
					catch (System.Exception e)
					{
						// Ignore exception, but print the error message.
						Debug.LogException(e);
					}
				}
			}
			EditorUtility.SetDirty(meshTreeBase);
			return true;
		}
		public static string GetSelectedFolderPath()
		{
			Object[] objects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
			if (objects == null || objects.Length == 0) {
				return "Assets/";
			}
			else {
				string path = AssetDatabase.GetAssetPath(objects[0]);
				if (System.IO.File.Exists(path)) {
					path = System.IO.Path.GetDirectoryName(path);
				}
				return path + "/";
			}
		}
	}
}
