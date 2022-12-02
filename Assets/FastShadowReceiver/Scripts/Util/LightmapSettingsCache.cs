//
// LightmapSettingsCache.cs
//
// Fast Shadow Receiver
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using UnityEngine;

namespace FastShadowReceiver
{
    // accessing array properties in Unity components generates garbage.
    // to avoid freaquent invocations of garbage collection, cache the lightmaps data of LightmapSettings.
    public static class LightmapSettingsCache
    {
        public static LightmapData[] lightmaps
        {
            get
            {
                if (s_lightmapDataCache == null)
                {
                    CacheLightmapData();
                }
                return s_lightmapDataCache;
            }
        }
        public static LightmapsMode lightmapsMode { get { return LightmapSettings.lightmapsMode; } }
        public static LightProbes lightProbes { get { return LightmapSettings.lightProbes; } }

        static LightmapData[] s_lightmapDataCache = null;
        static bool s_lightmapDataCached = false;
        static public  void CacheLightmapData()
        {
            if (!s_lightmapDataCached)
            {
                s_lightmapDataCached = true; // use this flag in case that LightmapSettings.lightmaps can be null.
                s_lightmapDataCache = LightmapSettings.lightmaps;
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged += OnActiveSceneChanged;
                UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += OnSceneUnloaded;
#if UNITY_2019_2_OR_NEWER
    			UnityEditor.Lightmapping.bakeCompleted += UncacheLightmapData;
#else
                UnityEditor.Lightmapping.completed += UncacheLightmapData;
#endif
#endif
            }
        }
        static public void UncacheLightmapData()
        {
            if (s_lightmapDataCached)
            {
                s_lightmapDataCached = false;
                s_lightmapDataCache = null;
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= OnActiveSceneChanged;
                UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= OnSceneUnloaded;
#if UNITY_2019_2_OR_NEWER
    			UnityEditor.Lightmapping.bakeCompleted += UncacheLightmapData;
#else
                UnityEditor.Lightmapping.completed += UncacheLightmapData;
#endif
#endif
            }
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            UncacheLightmapData();
        }
        static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            UncacheLightmapData();
        }
        static void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene scene1, UnityEngine.SceneManagement.Scene scene2)
        {
            UncacheLightmapData();
        }
    }
}
