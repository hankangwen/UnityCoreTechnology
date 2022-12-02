//
// MeshTree.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using System.Collections.Generic;

namespace FastShadowReceiver {
	public abstract class MeshTree : MeshTreeBase {
		public const int MAX_LIGHTMAP_INDEX = 0xFFFE;
		[System.Serializable]
		protected struct TriangleList
		{
			public static TriangleList CreateFromArray(int[] array)
			{
				TriangleList list = new TriangleList();
				list.triangles = array;
				return list;
			}
			public bool IsEmpty()
			{
				return triangles == null || triangles.Length == 0;
			}
			public int this[int index]
			{
				get { return triangles[index]; }
				set { triangles[index] = value; }
			}
			public int Length
			{
				get { return triangles.Length; }
			}
			public int[] triangles;
		}
		public enum UVIndex {
            None,
            UV2,
            //UV1
        }
		
		[SerializeField][HideInInspector]
		private Object           m_srcMesh;
		[SerializeField][HideInInspector]
		private LayerMask        m_layerMask = -1;
		[SerializeField][HideInInspector]
		private bool             m_excludeIgnoreProjectorMaterial = false; // The default value must be false because the shaders of Universal RP have IgnoreProjector tag.
		[SerializeField][HideInInspector]
		private string[]         m_excludeRenderTypes = {"Transparent"};
		[SerializeField][HideInInspector]
		protected Vector3[]      m_vertices;
		[SerializeField][HideInInspector]
		protected Vector3[]      m_normals;
#if UNITY_2017_1_OR_NEWER
		[SerializeField][HideInInspector]
		protected Vector2[]      m_uvs;
#endif
		[SerializeField][HideInInspector]
		protected int[]          m_indices;
		[SerializeField][HideInInspector]
		protected float          m_scaledOffset = 0.0f;
		[SerializeField][HideInInspector]
		protected float          m_fixedOffset = 0.0f;
		// for debug purpose
		[SerializeField][HideInInspector]
		private Vector3 m_debugVertexSum = Vector3.zero;
		[SerializeField][HideInInspector]
		private Vector3 m_debugSquareVertexSum = Vector3.zero;
#if UNITY_2017_1_OR_NEWER
		public UVIndex m_lightmapUVIndex = UVIndex.None;
		[SerializeField][HideInInspector]
		int[] m_lightmapSegments; // m_indices are sorted by lightmap index. m_lightmapSegments[i+1] indicates the last index + 1 of m_indices for i-th lightmap (m_lightmapSegments[0] is for non lightmapped triangles).
#endif
		public Object srcMesh
		{
			get { return m_srcMesh; }
			set {
				if (m_srcMesh != value) {
					if (value is Mesh || value is GameObject || value == null) {
						Clear();
						m_srcMesh = value;
					}
				}
			}
		}
		public void SetSrcMeshWithoutClear(Object mesh)
		{
			m_srcMesh = mesh;
		}
		public string[] excludeRenderTypes
		{
			get { return m_excludeRenderTypes; }
			set { m_excludeRenderTypes = value; }
		}
		public bool excludeIgnoreProjectorMaterial
		{
			get { return m_excludeIgnoreProjectorMaterial; }
			set { m_excludeIgnoreProjectorMaterial = value; }
		}
		public LayerMask layerMask
		{
			get { return m_layerMask; }
			set { m_layerMask = value; }
		}
		public float scaledOffset {
			get { return m_scaledOffset; }
			set { m_scaledOffset = value; }
		}
		public float fixedOffset {
			get { return m_fixedOffset; }
			set { m_fixedOffset = value; }
		}
		public override bool IsPrebuilt ()
		{
			if ((Debug.isDebugBuild || Application.isEditor) && m_vertices != null && 0 < m_vertices.Length && (m_normals == null || m_normals.Length == 0)) {
				Debug.LogWarning("This mesh tree doesn't have normal vectors. Please rebuild \"" + name + "\" mesh tree.", this);
			}
			return IsBuildFinished();
		}
		public override bool IsReadyToBuild ()
		{
			if (m_srcMesh == null) {
				return false;
			}
			if (m_srcMesh is Mesh) {
				return true;
			}
			if (m_srcMesh is GameObject) {
				return true;
			}
			return false;
		}
#if UNITY_EDITOR
		public override long GetMemoryUsage()
		{
			long size = 0;
			if (m_vertices != null) {
				size += m_vertices.Length * 3*sizeof(float);
			}
			if (m_normals != null) {
				size += m_normals.Length * 3*sizeof(float);
			}
			if (m_indices != null) {
				size += m_indices.Length * sizeof(int);
			}
			return size;
		}
#endif
		protected virtual void Clear()
		{
			WaitForBuild();
			m_vertices = null;
			m_normals = null;
			m_indices = null;
		}
		protected override void PrepareForBuild ()
		{
			if (m_srcMesh is Mesh) {
				Mesh mesh = m_srcMesh as Mesh;
				m_vertices = mesh.vertices;
				m_normals = mesh.normals;
				m_indices = mesh.triangles;
				m_bounds = mesh.bounds;
#if UNITY_2017_1_OR_NEWER
				switch (m_lightmapUVIndex)
				{
					//case UVIndex.UV1:
					//	m_uvs = mesh.uv;
					//	break;
					case UVIndex.UV2:
						m_uvs = mesh.uv2;
						break;
					default:
						m_uvs = null;
						break;
				}
				m_lightmapSegments = null;
#endif
				m_debugVertexSum = Vector3.zero;
				m_debugSquareVertexSum = Vector3.zero;
				foreach (Vector3 v in m_vertices) {
					m_debugVertexSum += v;
					m_debugSquareVertexSum += Vector3.Scale(v, v);
				}
			}
			else if (m_srcMesh is GameObject) {
				CombineMeshes(m_srcMesh as GameObject);
			}
		}
		private void CombineMeshes(GameObject rootObject)
		{
			Transform root = rootObject.transform;
			MeshFilter[] meshes = rootObject.GetComponentsInChildren<MeshFilter>();
            int vertexCount = 0;
			int triangleCount = 0;
			int meshCount = meshes.Length;
			List<int>[] triangleList = new List<int>[meshCount];
#if UNITY_2017_1_OR_NEWER
			int maxLightmapIndex = -1;
			m_lightmapSegments = null;
			if (m_lightmapUVIndex != UVIndex.None) {
				LightmapDataInPrefab lightmapData = rootObject.GetComponent<LightmapDataInPrefab>();
				// sort meshes by lightmap index
                Dictionary<int, List<MeshFilter>> meshMap = new Dictionary<int, List<MeshFilter>>();
				for (int i = 0; i < meshCount; ++i) {
                    MeshFilter mf = meshes[i];
                    Renderer r = mf.GetComponent<Renderer>();
					int lightmapIndex = r.lightmapIndex;
					if (lightmapData != null) {
						lightmapIndex = lightmapData.GetLightmapIndexInPrefab(r);
					}
					if (MAX_LIGHTMAP_INDEX <= lightmapIndex) {
						lightmapIndex = -1;
					}
                    if (maxLightmapIndex < lightmapIndex) {
                        maxLightmapIndex = lightmapIndex;
                    }
                    List<MeshFilter> meshList;
                    if (!meshMap.TryGetValue(lightmapIndex, out meshList)) {
                        meshList = new List<MeshFilter>();
                        meshMap.Add(lightmapIndex, meshList);
                    }
                    meshList.Add(mf);
				}
				int j = 0;
				for (int i = -1; i <= maxLightmapIndex; ++i) {
					List<MeshFilter> meshList;
					if (meshMap.TryGetValue(i, out meshList)) {
						foreach (MeshFilter mf in meshList) {
							meshes[j++] = mf;
						}
					}
				}
				if (0 <= maxLightmapIndex)
				{
					m_lightmapSegments = new int[maxLightmapIndex + 1];
				}
			}
#endif
			for (int i = 0; i < meshCount; ++i) {
				if ((m_layerMask & (1U << meshes[i].gameObject.layer)) != 0U && meshes[i].GetComponent<ReceiverBase>() == null) {
					Mesh mesh = meshes[i].sharedMesh;
					if (mesh == null) {
						continue;
					}
					vertexCount += mesh.vertexCount;
					Material[] materials = meshes[i].GetComponent<Renderer>().sharedMaterials;
					triangleList[i] = new List<int>();
					for (int submesh = 0; submesh < mesh.subMeshCount; ++submesh) {
						bool bExcluded = false;
						if (submesh < materials.Length && materials[submesh] != null) {
							if (m_excludeIgnoreProjectorMaterial) {
								string ignoreProjector = materials [submesh].GetTag ("IgnoreProjector", true);
								if (!string.IsNullOrEmpty (ignoreProjector)) {
									if (string.Compare (ignoreProjector, "True", true) == 0) {
										bExcluded = true;
									}
								}
							}
							if (!bExcluded) {
								string renderType = materials [submesh].GetTag ("RenderType", true);
								for (int j = 0; j < m_excludeRenderTypes.Length; ++j) {
									if (renderType == m_excludeRenderTypes [j]) {
										bExcluded = true;
										break;
									}
								}
							}
						}
						else {
							bExcluded = true;
							if (Debug.isDebugBuild || Application.isEditor) {
								Debug.LogWarning("No material is assigned to submesh[" + submesh.ToString() + "] of " + mesh.name + ".", meshes[i].gameObject);
							}
						}
						if (!bExcluded) {
							int[] subTriangles = mesh.GetTriangles(submesh);
							triangleList[i].AddRange(subTriangles);
							triangleCount += subTriangles.Length;
						}
					}
				}
			}
			if (Debug.isDebugBuild || Application.isEditor)
			{
				if (m_excludeIgnoreProjectorMaterial && triangleCount == 0 && 0 < meshCount) {
					Debug.LogWarning("No triangles found! All of the materials might have IgnoreProjector tag. Please uncheck 'Exclude Ignore Projector Material' of the mesh tree.", this);
				}
			}
			m_vertices = new Vector3[vertexCount];
			m_normals = new Vector3[vertexCount];
			m_indices = new int[triangleCount];
#if UNITY_2017_1_OR_NEWER
			if (m_lightmapUVIndex == UVIndex.None) {
				m_uvs = null;
			}
			else {
				m_uvs = new Vector2[vertexCount];
			}
			int currentLightmapIndex = -1;
#endif
			vertexCount = 0;
			triangleCount = 0;
			m_debugVertexSum = Vector3.zero;
			m_debugSquareVertexSum = Vector3.zero;
			Matrix4x4 worldToRoot = root.worldToLocalMatrix;
			for (int i = 0; i < meshCount; ++i) {
				if ((m_layerMask & (1U << meshes[i].gameObject.layer)) != 0U && meshes[i].GetComponent<ReceiverBase>() == null) {
					Matrix4x4 mat = worldToRoot * meshes[i].transform.localToWorldMatrix;
					Matrix4x4 matIT = mat.transpose.inverse;
					Vector3[] vertices = meshes[i].sharedMesh.vertices;
					Vector3[] normals = meshes[i].sharedMesh.normals;
#if UNITY_2017_1_OR_NEWER
					Vector2[] uvs = null;
					Vector4 lightmapScaleOffset = new Vector4(1, 1, 0, 0);
					if (m_lightmapUVIndex != UVIndex.None) {
						if (m_lightmapUVIndex == UVIndex.UV2) {
							uvs = meshes[i].sharedMesh.uv2;
						}
						if (uvs == null || uvs.Length == 0) {
							uvs = meshes[i].sharedMesh.uv;
						}
						Renderer r = meshes[i].GetComponent<Renderer>();
						lightmapScaleOffset = r.lightmapScaleOffset;
						int lightmapIndex = r.lightmapIndex;
						if (MAX_LIGHTMAP_INDEX <= lightmapIndex)
						{
							lightmapIndex = -1;
						}
						Debug.Assert(currentLightmapIndex <= lightmapIndex); // meshes must be sorted by lightmapIndex
						while (currentLightmapIndex < lightmapIndex)
						{
							m_lightmapSegments[++currentLightmapIndex] = triangleCount;
						}
					}
#endif
					List<int> triangles = triangleList[i];
					int indexOffset = vertexCount;
					for (int j = 0; j < vertices.Length; ++j) {
						Vector3 v = mat.MultiplyPoint(vertices[j]);
						Vector3 n;
						if (normals != null && j < normals.Length) {
							n = matIT.MultiplyVector(normals[j]).normalized;
						}
						else {
							n = Vector3.up;
						}
						m_debugVertexSum += v;
						m_debugSquareVertexSum += Vector3.Scale(v, v);
						float offset = m_fixedOffset + m_scaledOffset * 0.00000025f * v.magnitude;
						v += offset * n;
						m_vertices[vertexCount] = v;
						m_normals[vertexCount] = n;
#if UNITY_2017_1_OR_NEWER
						if (uvs != null && 0 < uvs.Length)
						{
							m_uvs[vertexCount] = new Vector2(lightmapScaleOffset.z + lightmapScaleOffset.x * uvs[j].x, lightmapScaleOffset.w + lightmapScaleOffset.y * uvs[j].y);
						}
						else if (m_uvs != null)
						{
							m_uvs[vertexCount] = Vector2.zero;
						}
#endif
						++vertexCount;
					}
					for (int j = 0; j < triangles.Count; ++j) {
						m_indices[triangleCount++] = triangles[j] + indexOffset;
					}
				}
			}
			if (0 < vertexCount) {
				Vector3 min = m_vertices[0];
				Vector3 max = m_vertices[0];
				for (int i = 1; i < vertexCount; ++i) {
					min.x = Mathf.Min (min.x, m_vertices[i].x);
					min.y = Mathf.Min (min.y, m_vertices[i].y);
					min.z = Mathf.Min (min.z, m_vertices[i].z);
					max.x = Mathf.Max (max.x, m_vertices[i].x);
					max.y = Mathf.Max (max.y, m_vertices[i].y);
					max.z = Mathf.Max (max.z, m_vertices[i].z);
				}
				m_bounds.SetMinMax(min, max);
			}
		}
#if UNITY_2017_1_OR_NEWER
		protected int GetLightmapIndexOfTriangle(int triangleIndex)
		{
			if (m_lightmapSegments == null)
			{
				return -1;
			}
			else
			{
				int i = 0;
				while (i < m_lightmapSegments.Length)
				{
					if (triangleIndex < m_lightmapSegments[i])
					{
						return i - 1;
					}
					++i;
				}
				return i - 1;
			}
		}
		protected int lightmapCount { get { return m_lightmapSegments != null ? m_lightmapSegments.Length : 0; } }
#else
		protected int lightmapCount { get { return 0; } }
#endif
		public override string CheckError(GameObject rootObject)
		{
			if (m_vertices == null || m_vertices.Length == 0) {
				return "Mesh Tree is empty.";
			}
			if (m_debugSquareVertexSum == Vector3.zero) {
				// built by old version. do not check error.
				return null;
			}
			Vector3 vertexSum = Vector3.zero;
			Vector3 vertexSquareSum = Vector3.zero;
			if (m_srcMesh is Mesh) {
				MeshFilter mesh = rootObject.GetComponent<MeshFilter>();
				if (mesh == null || mesh.sharedMesh != m_srcMesh) {
					return rootObject.name + " does not have the mesh from which the mesh tree was built.";
				}
				if (Application.isPlaying && !mesh.sharedMesh.isReadable) {
					// cannot check error
					return null;
				}
				Vector3[] vertices = mesh.sharedMesh.vertices;
				for (int i = 0; i < vertices.Length; ++i) {
					vertexSum += vertices[i];
					vertexSquareSum += Vector3.Scale(vertices[i], vertices[i]);
				}
			}
			else {
				MeshFilter[] meshes = rootObject.GetComponentsInChildren<MeshFilter>();
				int meshCount = meshes.Length;
				Matrix4x4 worldToRoot = rootObject.transform.worldToLocalMatrix;
				for (int i = 0; i < meshCount; ++i) {
					if ((m_layerMask & (1U << meshes[i].gameObject.layer)) != 0U && meshes[i].GetComponent<ReceiverBase>() == null) {
						if (Application.isPlaying && !meshes[i].sharedMesh.isReadable) {
							// cannot check error
							return null;
						}
						Matrix4x4 mat = worldToRoot * meshes[i].transform.localToWorldMatrix;
						Vector3[] vertices = meshes[i].sharedMesh.vertices;
						for (int j = 0; j < vertices.Length; ++j) {
							Vector3 v = mat.MultiplyPoint(vertices[j]);
							vertexSum += v;
							vertexSquareSum += Vector3.Scale(v, v);
						}
					}
				}
			}
			Vector3 diffSum = m_debugVertexSum - vertexSum;
			Vector3 diffSqSum = m_debugSquareVertexSum - vertexSquareSum;
			float epsilon = 0.001f * m_debugVertexSum.magnitude;
			float epsilon2 = 0.001f * m_debugSquareVertexSum.magnitude;
			if (epsilon < Mathf.Abs(diffSum.x) || epsilon < Mathf.Abs(diffSum.y) || epsilon < Mathf.Abs(diffSum.z) ||
			    epsilon2 < Mathf.Abs(diffSqSum.x) || epsilon2 < Mathf.Abs(diffSqSum.y) || epsilon2 < Mathf.Abs(diffSqSum.z)) {
				return rootObject.name + " has been changed since the mesh tree was built, or the mesh tree was not built from " + rootObject.name + ".";
			}
			return null;
		}
	}
}
