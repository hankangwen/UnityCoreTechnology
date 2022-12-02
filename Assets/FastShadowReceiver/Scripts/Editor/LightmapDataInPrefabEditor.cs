//
// LightmapDataInPrefabEditor.cs
//
// Fast Shadow Receiver
//
// Copyright 2019 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace FastShadowReceiver.Editor {
	[CustomEditor(typeof(LightmapDataInPrefab))]
	public class LightmapDataInPrefabEditor : UnityEditor.Editor {
		override public void OnInspectorGUI()
		{
			LightmapDataInPrefab lightmapData = (LightmapDataInPrefab)target;
			LightmapDataInPrefab.AutoMode autoMode = lightmapData.autoMode;
			DrawDefaultInspector ();
			if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(lightmapData.gameObject)))
			{
				// scene object
				if (autoMode != lightmapData.autoMode)
				{
					if (autoMode == LightmapDataInPrefab.AutoMode.AutoCollect || autoMode == LightmapDataInPrefab.AutoMode.AutoCollectAndSave)
					{
						lightmapData.DisableAutoCollect();
					}
					if (lightmapData.autoMode == LightmapDataInPrefab.AutoMode.AutoApply)
					{
						lightmapData.ApplyLightmaps();
					}
					else if (lightmapData.autoMode == LightmapDataInPrefab.AutoMode.AutoCollect || lightmapData.autoMode == LightmapDataInPrefab.AutoMode.AutoCollectAndSave)
					{
						lightmapData.EnableAutoCollect();
					}
				}
				if (GUILayout.Button("Collect Lightmaps"))
				{
					lightmapData.CollectLightmaps();
				}
				if (GUILayout.Button("Apply Lightmaps"))
				{
					lightmapData.ApplyLightmaps();
				}
				if (GUILayout.Button("Save Prefab"))
				{
					lightmapData.SavePrefab();
				}
				if (lightmapData.autoMode != LightmapDataInPrefab.AutoMode.AutoApply && lightmapData.autoMode != LightmapDataInPrefab.AutoMode.None)
				{
					// check prefab's auto mode in case that all modifications are applied to the prefab.
					LightmapDataInPrefab lightmapDataPrefab = null;
#if UNITY_2018_2_OR_NEWER
					lightmapDataPrefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(lightmapData);
#else
					GameObject prefab = UnityEditor.PrefabUtility.GetPrefabParent(lightmapData.gameObject) as GameObject;
					if (prefab != null) {
						lightmapDataPrefab = prefab.GetComponent<LightmapDataInPrefab>();
					}
#endif
					if (lightmapDataPrefab != null && lightmapData.autoMode == lightmapDataPrefab.autoMode)
					{
						lightmapDataPrefab.ValidateAutoModeOfPrefabAsset();
					}
				}
			}
			else
			{
				// prefab asset
				lightmapData.ValidateAutoModeOfPrefabAsset();
			}
		}
	}
}
