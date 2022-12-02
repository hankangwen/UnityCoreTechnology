//
// MeshShadowReceiverEditor.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FastShadowReceiver.Editor {
	[CustomEditor(typeof(MeshShadowReceiver))]
	public class MeshShadowReceiverEditor : ReceiverBaseEditor {
		string m_errorString = null;
		public override void OnInspectorGUI ()
		{
			MeshShadowReceiver receiver = target as MeshShadowReceiver;
			Transform oldMeshTransform = receiver.meshTransform;
			MeshTreeBase oldMeshTree = receiver.meshTree;
			if (!string.IsNullOrEmpty(m_errorString)) {
				EditorGUILayout.TextArea(m_errorString, errorStyle);
			}
			base.OnInspectorGUI();
			SerializedProperty property = serializedObject.FindProperty("m_enablePrediction");
			bool bPredictEnabled = EditorGUILayout.Toggle("Enable Prediction", property.boolValue);
			if (bPredictEnabled != property.boolValue) {
				Undo.RecordObject(target, "Inspector");
				property.boolValue = bPredictEnabled;
				EditorUtility.SetDirty(target);
			}
			if (bPredictEnabled) {
				Component predictor = (Component)EditorGUILayout.ObjectField("Predictor", receiver.predictor as Component, typeof(Component), true);
				if (predictor != receiver.predictor as Component) {
					if (predictor == null) {
						Undo.RecordObject(receiver, "Inspector");
						receiver.predictor = null;
						EditorUtility.SetDirty(receiver);
					}
					else {
						Component[] components = predictor.GetComponents<Component>();
						for (int i = 0; i < components.Length; ++i) {
							if (components[i] is ITransformPredictor) {
								Undo.RecordObject(receiver, "Inspector");
								receiver.predictor = components[i] as ITransformPredictor;
								EditorUtility.SetDirty(receiver);
								break;
							}
						}
					}
				}
			}
			property = serializedObject.FindProperty("m_updateOnlyWhenProjectorMoved");
			bool bCheckMove = EditorGUILayout.Toggle("Update Only When Projector Moved", property.boolValue);
			if (bCheckMove != property.boolValue) {
				Undo.RecordObject(target, "Inspector");
				property.boolValue = bCheckMove;
				EditorUtility.SetDirty(target);
			}
			if (bCheckMove) {
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_margin"));
			}
			if (!receiver.dontUseLightmaps) {
				property = serializedObject.FindProperty("m_projectorMaterialForLightmappedMesh");
				EditorGUILayout.PropertyField (property);
				if (property.objectReferenceValue != null) {
					EditorGUI.indentLevel++;
					DrawMateriaPropertyArrayGUI(serializedObject.FindProperty("m_staticMaterialPropertiesToCopyFromProjector"));
					DrawMateriaPropertyArrayGUI(serializedObject.FindProperty("m_dynamicMaterialPropertiesToCopyFromProjector"));
					EditorGUI.indentLevel--;
				}
			}
			serializedObject.ApplyModifiedProperties();
			if (receiver.meshTree == null && receiver.meshTransform != null) {
				if (receiver.meshTransform != oldMeshTransform) {
					// to make it easier to setup mesh shadow receiver, find a mesh tree from meshTransform.
					MeshTreeBase meshTree = FindMeshTreeFromMeshTransform(receiver.meshTransform);
					if (meshTree != null) {
						receiver.meshTree = meshTree;
						EditorUtility.SetDirty(receiver);
					}
				}
				// if not found, show create button
				if (receiver.meshTree == null) {
					if (GUILayout.Button("Create a new MeshTree")) {
						LayerMask layers = -1;
						if (receiver.unityProjector != null) {
							layers = receiver.unityProjector.ignoreLayers;
						}
						CreateMeshTreeWizard.CreateWizard(receiver.meshTransform, layers, receiver);
					}
				}
			}
			if (receiver.meshTree != null && receiver.meshTransform != null && (receiver.meshTree != oldMeshTree || receiver.meshTransform != oldMeshTransform)) {
				m_errorString = null;
				if (receiver.meshTree.IsPrebuilt() && !receiver.meshTree.IsBuildFinished()) {
					receiver.meshTree.BuildFromPrebuiltData();
				}
				if (receiver.meshTree.IsBuildFinished()) {
					string error = receiver.meshTree.CheckError(receiver.meshTransform.gameObject);
					if (!string.IsNullOrEmpty(error)) {
						Debug.LogError(error, receiver);
						m_errorString = "<color=red>" + error + "</color>";
					}
				}
			}
		}
		private string m_addNewMaterialProperty = null;
		private void DrawMateriaPropertyArrayGUI(SerializedProperty serializedProperty)
		{
			serializedProperty.isExpanded = EditorGUILayout.Foldout(serializedProperty.isExpanded, serializedProperty.displayName);
			if (serializedProperty.isExpanded) {
				EditorGUI.indentLevel++;
				for (int i = 0; i < serializedProperty.arraySize; ++i) {
					SerializedProperty arrayElement = serializedProperty.GetArrayElementAtIndex (i);
					EditorGUILayout.BeginHorizontal ();
					SerializedProperty type = arrayElement.FindPropertyRelative ("Type");
					SerializedProperty name = arrayElement.FindPropertyRelative ("Name");
					string typeControlName = serializedProperty.name + i.ToString () + type.name;
					string nameControlName = serializedProperty.name + i.ToString () + name.name;
					float oldLabelWidth = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 80;
					GUI.SetNextControlName (nameControlName);
					EditorGUILayout.PropertyField (name);
					GUI.SetNextControlName (typeControlName);
					EditorGUILayout.PropertyField (type, GUILayout.MaxWidth(150));
					EditorGUIUtility.labelWidth = oldLabelWidth;
					EditorGUILayout.EndHorizontal ();
					if (m_addNewMaterialProperty == serializedProperty.name && i == serializedProperty.arraySize - 1) {
						m_addNewMaterialProperty = null;
						GUI.FocusControl (nameControlName);
					}
					string focusedControlName = GUI.GetNameOfFocusedControl();
					if (string.IsNullOrEmpty(name.stringValue) && focusedControlName != typeControlName && focusedControlName != nameControlName) {
						serializedProperty.DeleteArrayElementAtIndex (i);
						--i;
					}
				}
				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("Add Property")) {
					int i = serializedProperty.arraySize;
					serializedProperty.InsertArrayElementAtIndex (i);
					SerializedProperty arrayElement = serializedProperty.GetArrayElementAtIndex (i);
					SerializedProperty type = arrayElement.FindPropertyRelative ("Type");
					type.enumValueIndex = 0;
					SerializedProperty name = arrayElement.FindPropertyRelative ("Name");
					name.stringValue = "";
					m_addNewMaterialProperty = serializedProperty.name;
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUI.indentLevel--;
			}
		}
		public static MeshTreeBase FindMeshTreeFromMeshTransform(Transform meshTransform)
		{
			Terrain terrain = meshTransform.GetComponent<Terrain>();
			if (terrain != null && terrain.terrainData != null) {
				string[] trees = AssetDatabase.FindAssets("t:TerrainMeshTree");
				foreach (string guid in trees) {
					TerrainMeshTree meshTree = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(TerrainMeshTree)) as TerrainMeshTree;
					if (meshTree.terrainData == terrain.terrainData) {
						return meshTree;
					}
				}
			}
#if UNITY_2018_2_OR_NEWER
			Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(meshTransform.gameObject);
#else
			Object prefab = PrefabUtility.GetPrefabParent(meshTransform.gameObject);
#endif
			if (prefab != null) {
				string[] trees = AssetDatabase.FindAssets("t:MeshTree");
				foreach (string guid in trees) {
					MeshTree meshTree = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(MeshTree)) as MeshTree;
					if (meshTree.srcMesh == prefab) {
						return meshTree;
					}
				}
			}
			return null;
		}
	}
}
