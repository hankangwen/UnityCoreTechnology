//
// BinaryMeshTree.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using System.Collections.Generic;

namespace FastShadowReceiver {
	public class BinaryMeshTree : MeshTree {
#pragma warning disable 0649
		[System.Serializable]
		private struct TreeNode {
			public Bounds m_bounds;
			public int m_childIndex;
			public int[] m_triangles;
		}
#pragma warning restore 0649
		[System.Serializable]
		private struct TreeNodeVer2
		{
			public Bounds m_bounds;
			public int m_childIndex;
			public int m_triangleListIndex; // -1 means no triangles
		}
		[SerializeField][HideInInspector][System.Obsolete]
		private TreeNode[] m_treeNodes; // no longer used, but remains to support old project data.
		[SerializeField][HideInInspector]
		private TreeNodeVer2[] m_treeNodes2;
		[SerializeField][HideInInspector]
		private TriangleList[] m_triangleListForEachNode;
		private const int LEAFNODE_TRIANGLE_COUNT = 2;

		public void OnEnable()
		{
			UpdateTreeNodes();
		}
		private void UpdateTreeNodes()
		{
#pragma warning disable 0612
			// convert old data to new one
			if (m_treeNodes != null && 0 < m_treeNodes.Length)
			{
				m_treeNodes2 = new TreeNodeVer2[m_treeNodes.Length];
				List<TriangleList> triangleLists = new List<TriangleList>();
				for (int i = 0; i < m_treeNodes.Length; ++i)
				{
					m_treeNodes2[i].m_bounds = m_treeNodes[i].m_bounds;
					m_treeNodes2[i].m_childIndex = m_treeNodes[i].m_childIndex;
					if (m_treeNodes[i].m_triangles != null && 0 < m_treeNodes[i].m_triangles.Length)
					{
						m_treeNodes2[i].m_triangleListIndex = triangleLists.Count;
						triangleLists.Add(TriangleList.CreateFromArray(m_treeNodes[i].m_triangles));
					}
					else
					{
						m_treeNodes2[i].m_triangleListIndex = -1;
					}
				}
				m_triangleListForEachNode = triangleLists.ToArray();
				m_treeNodes = null;
			}
#pragma warning restore 0612
		}
#if UNITY_EDITOR
		public override long GetMemoryUsage ()
		{
			UpdateTreeNodes();
			long size = base.GetMemoryUsage ();
			if (m_treeNodes2 != null) {
				size += m_treeNodes2.Length * (sizeof(float) * 6 + 2 * sizeof(int));
			}
			if (m_triangleListForEachNode != null) {
				size += m_triangleListForEachNode.Length * System.IntPtr.Size;
				for (int i = 0; i < m_triangleListForEachNode.Length; ++i) {
					if (!m_triangleListForEachNode[i].IsEmpty()) {
						size += m_triangleListForEachNode[i].Length * sizeof(int);
					}
				}
			}
			return size;
		}
		public override int GetNodeCount ()
		{
			if (m_treeNodes2 != null && m_treeNodes2.Length != 0) {
				return m_treeNodes2.Length;
			}
			return 0;
		}
		private int m_totalTriangleCount;
		private int m_triangleCountInLeafNodes;
		public override float GetBuildProgress ()
		{
			if (m_treeNodes2 != null) {
				return 1.0f;
			}
			if (m_totalTriangleCount == 0) {
				return 0;
			}
			return (float)m_triangleCountInLeafNodes/(float)m_totalTriangleCount;
		}
#endif
		public override bool IsBuildFinished()
		{
			return m_treeNodes2 != null && 0 < m_treeNodes2.Length;
		}
		public override System.Type GetSearchType ()
		{
			return typeof(BinaryMeshTreeSearch);
		}
		public override MeshTreeSearch CreateSearch()
		{
			return new BinaryMeshTreeSearch();
		}
		public override void Search (MeshTreeSearch search)
		{
			if (!(search is BinaryMeshTreeSearch)) {
				Debug.LogError("Invalid MeshTreeSearch class!");
				return;
			}
			BinaryMeshTreeSearch binSearch = (BinaryMeshTreeSearch)search;
			binSearch.Initialize(lightmapCount + 1);
			Search(binSearch, ref m_treeNodes2[m_treeNodes2.Length - 1]);
#if UNITY_2017_1_OR_NEWER
			binSearch.Finalize(m_vertices, m_indices, m_normals, m_uvs);
#else
			binSearch.Finalize(m_vertices, m_indices, m_normals, null);
#endif
		}
		public override void Raycast(MeshTreeRaycast raycast)
		{
			MeshTreeRaycast.TemporaryParam param = raycast.CreateTemporaryParam();
			Raycast(raycast, ref m_treeNodes2[m_treeNodes2.Length - 1], ref param);
		}
		public override void BuildFromPrebuiltData ()
		{
			// completely serialized. nothing to do here
		}
		private void AddAllTrianglesInChildren(BinaryMeshTreeSearch search, ref TreeNodeVer2 node, bool bScissor)
		{
			int n = node.m_triangleListIndex;
			if (n != -1) {
#if UNITY_2017_1_OR_NEWER
				int count = lightmapCount + 1;
				for (int i = 0; i < count; ++i)
				{
					search.AddTriangles(m_triangleListForEachNode[n++].triangles, node.m_bounds, bScissor, i);
				}
#else
				search.AddTriangles(m_triangleListForEachNode[n].triangles, node.m_bounds, bScissor, 0);
#endif
			}
			else if (0 <= node.m_childIndex) {
				AddAllTrianglesInChildren(search, ref m_treeNodes2[node.m_childIndex], false);
				AddAllTrianglesInChildren(search, ref m_treeNodes2[node.m_childIndex + 1], false);
			}
		}
		private void Search(BinaryMeshTreeSearch search, ref TreeNodeVer2 node)
		{
			int i = node.m_triangleListIndex;
			if (node.m_childIndex < 0 && i == -1) {
				return;
			}
			float scissorDistance;
			if (!search.IsInView(node.m_bounds, out scissorDistance)) {
				return;
			}
			if (scissorDistance < 0 && 0 <= node.m_childIndex) {
				Search(search, ref m_treeNodes2[node.m_childIndex]);
				Search(search, ref m_treeNodes2[node.m_childIndex + 1]);
			}
			else {
				AddAllTrianglesInChildren(search, ref node, scissorDistance < -search.m_scissorMargin);
			}
		}
		private bool Raycast(MeshTreeRaycast raycast, ref TreeNodeVer2 node, ref MeshTreeRaycast.TemporaryParam param)
		{
			if (node.m_childIndex < 0) {
				if (node.m_triangleListIndex == -1) {
					return false;
				}
				bool hit = false;
#if UNITY_2017_1_OR_NEWER
				int triangleListCount = lightmapCount + 1;
#else
				int triangleListCount = 1;
#endif
				int j = node.m_triangleListIndex;
				for (int lightmap = 0; lightmap < triangleListCount; ++lightmap)
				{
					int[] triangles = m_triangleListForEachNode[j++].triangles;
					for (int i = 0; i < triangles.Length; ++i)
					{
						int tri = triangles[i];
						if (raycast.TriangleHitTest(m_vertices[m_indices[tri]], m_vertices[m_indices[tri + 1]], m_vertices[m_indices[tri + 2]]))
						{
							hit = true;
						}
					}
				}
				return hit;
			}
			else {
				float distance1, distance2;
				bool hit1 = raycast.BoundsHitTest(m_treeNodes2[node.m_childIndex].m_bounds.center, m_treeNodes2[node.m_childIndex].m_bounds.extents, param, out distance1);
				bool hit2 = raycast.BoundsHitTest(m_treeNodes2[node.m_childIndex + 1].m_bounds.center, m_treeNodes2[node.m_childIndex + 1].m_bounds.extents, param, out distance2);
				if (hit1) {
					if (hit2) {
						if (distance1 < distance2) {
							if (Raycast(raycast, ref m_treeNodes2[node.m_childIndex], ref param)) {
								// there is a chance that the other node has the nearest hit point, since bounding boxes of nodes in a binary mesh trees are overlapping each other.
								if (distance2 < raycast.hitDistance) {
									Raycast(raycast, ref m_treeNodes2[node.m_childIndex + 1], ref param);
								}
								return true;
							}
							return Raycast(raycast, ref m_treeNodes2[node.m_childIndex + 1], ref param);
						}
						else {
							if (Raycast(raycast, ref m_treeNodes2[node.m_childIndex + 1], ref param)) {
								// there is a chance that the other node has the nearest hit point, since bounding boxes of nodes in a binary mesh trees are overlapping each other.
								if (distance1 < raycast.hitDistance) {
									Raycast(raycast, ref m_treeNodes2[node.m_childIndex], ref param);
								}
								return true;
							}
							return Raycast(raycast, ref m_treeNodes2[node.m_childIndex], ref param);
						}
					}
					else {
						return Raycast(raycast, ref m_treeNodes2[node.m_childIndex], ref param);
					}
				}
				else if (hit2) {
					return Raycast(raycast, ref m_treeNodes2[node.m_childIndex + 1], ref param);
				}
				return false;
			}
		}
		protected override void Clear ()
		{
			base.Clear ();
			m_treeNodes2 = null;
		}
		protected override void PrepareForBuild ()
		{
			base.PrepareForBuild ();
			m_treeNodes2 = null;
#if UNITY_EDITOR
			m_totalTriangleCount = m_indices.Length / 3;
			m_triangleCountInLeafNodes = 0;
#endif
		}
		protected override void Build ()
		{
			int numTriangles = m_indices.Length/3;
			Bounds[] triangleBounds = new Bounds[numTriangles];
			int index = 0;
			List<TreeNodeVer2> nodeList = new List<TreeNodeVer2>();
			TreeNodeVer2 rootNode;
			rootNode.m_bounds = m_bounds;
			rootNode.m_childIndex = -1;
			rootNode.m_triangleListIndex = -1;
			List<int> triangleList = new List<int>();
			List<TriangleList> triangleListForEachNode = new List<TriangleList>();
			for (int i = 0; i < numTriangles; ++i) {
				triangleList.Add(index);
				Vector3 v0 = m_vertices[m_indices[index++]];
				Vector3 v1 = m_vertices[m_indices[index++]];
				Vector3 v2 = m_vertices[m_indices[index++]];
				Vector3 min, max;
				min.x = Mathf.Min(Mathf.Min(v0.x, v1.x), v2.x);
				min.y = Mathf.Min(Mathf.Min(v0.y, v1.y), v2.y);
				min.z = Mathf.Min(Mathf.Min(v0.z, v1.z), v2.z);
				max.x = Mathf.Max(Mathf.Max(v0.x, v1.x), v2.x);
				max.y = Mathf.Max(Mathf.Max(v0.y, v1.y), v2.y);
				max.z = Mathf.Max(Mathf.Max(v0.z, v1.z), v2.z);
				triangleBounds[i].SetMinMax(min, max);
			}
			BuildMeshTree(nodeList, ref rootNode, ref triangleListForEachNode, triangleList, triangleBounds);
			nodeList.Add(rootNode);
			m_treeNodes2 = nodeList.ToArray();
			m_triangleListForEachNode = triangleListForEachNode.ToArray();
		}
		private static void DivideTrianglesByXAxis(List<int> triangleList, List<int> list1, List<int> list2, Vector3 center, Bounds[] triangleBounds)
		{
			int numTriangles = triangleList.Count;
			for (int i = 0; i < numTriangles; ++i) {
				int tri = triangleList[i];
				if (triangleBounds[tri/3].center.x < center.x) {
					list1.Add(tri);
				}
				else {
					list2.Add(tri);
				}
			}
		}
		private static void DivideTrianglesByYAxis(List<int> triangleList, List<int> list1, List<int> list2, Vector3 center, Bounds[] triangleBounds)
		{
			int numTriangles = triangleList.Count;
			for (int i = 0; i < numTriangles; ++i) {
				int tri = triangleList[i];
				if (triangleBounds[tri/3].center.y < center.y) {
					list1.Add(tri);
				}
				else {
					list2.Add(tri);
				}
			}
		}
		private static void DivideTrianglesByZAxis(List<int> triangleList, List<int> list1, List<int> list2, Vector3 center, Bounds[] triangleBounds)
		{
			int numTriangles = triangleList.Count;
			for (int i = 0; i < numTriangles; ++i) {
				int tri = triangleList[i];
				if (triangleBounds[tri/3].center.z < center.z) {
					list1.Add(tri);
				}
				else {
					list2.Add(tri);
				}
			}
		}
		private void AddTriangleList(ref List<TriangleList> triangleListForEachNode, List<int> triangleList)
		{
#if UNITY_2017_1_OR_NEWER
			int triangleListCount = lightmapCount + 1;
			if (triangleListCount == 1)
			{
				triangleListForEachNode.Add(TriangleList.CreateFromArray(triangleList.ToArray()));
			}
			else
			{
				List<int>[] triangleLists = new List<int>[triangleListCount];
				for (int i = 0; i < triangleListCount; ++i)
				{
					triangleLists[i] = new List<int>();
				}
				foreach (int tri in triangleList)
				{
					triangleLists[GetLightmapIndexOfTriangle(tri) + 1].Add(tri);
				}
				for (int i = 0; i < triangleListCount; ++i)
				{
					triangleListForEachNode.Add(TriangleList.CreateFromArray(triangleLists[i].ToArray()));
				}
			}
#else
			triangleListForEachNode.Add(TriangleList.CreateFromArray(triangleList.ToArray()));
#endif
		}
		private void BuildMeshTree(List<TreeNodeVer2> nodeList, ref TreeNodeVer2 parentNode, ref List<TriangleList> triangleListForEachNode, List<int> triangleList, Bounds[] triangleBounds)
		{
			int numTriangles = triangleList.Count;
			if (numTriangles <= 16) {
				// to save memory usage, TreeNode hold triangles only when triangle count <= 16.
				parentNode.m_triangleListIndex = triangleListForEachNode.Count;
				AddTriangleList(ref triangleListForEachNode, triangleList);
			}
			if (numTriangles <= LEAFNODE_TRIANGLE_COUNT) {
#if UNITY_EDITOR
				m_triangleCountInLeafNodes += numTriangles;
#endif
				parentNode.m_childIndex = -1;
				return;
			}
			Vector3 extents = parentNode.m_bounds.extents;
			Vector3 center = parentNode.m_bounds.center;
			List<int> list1 = new List<int>();
			List<int> list2 = new List<int>();
			if (extents.y <= extents.x && extents.z <= extents.x) {
				DivideTrianglesByXAxis(triangleList, list1, list2, center, triangleBounds);
				if (list1.Count == 0 || list2.Count == 0) {
					list1.Clear();
					list2.Clear();
					if (extents.z <= extents.y) {
						DivideTrianglesByYAxis(triangleList, list1, list2, center, triangleBounds);
						if (list1.Count == 0 || list2.Count == 0) {
							list1.Clear();
							list2.Clear();
							DivideTrianglesByZAxis(triangleList, list1, list2, center, triangleBounds);
						}
					}
					else {
						DivideTrianglesByZAxis(triangleList, list1, list2, center, triangleBounds);
						if (list1.Count == 0 || list2.Count == 0) {
							list1.Clear();
							list2.Clear();
							DivideTrianglesByYAxis(triangleList, list1, list2, center, triangleBounds);
						}
					}
				}
			}
			else if (extents.x <= extents.y && extents.z <= extents.y) {
				DivideTrianglesByYAxis(triangleList, list1, list2, center, triangleBounds);
				if (list1.Count == 0 || list2.Count == 0) {
					list1.Clear();
					list2.Clear();
					if (extents.z <= extents.x) {
						DivideTrianglesByXAxis(triangleList, list1, list2, center, triangleBounds);
						if (list1.Count == 0 || list2.Count == 0) {
							list1.Clear();
							list2.Clear();
							DivideTrianglesByZAxis(triangleList, list1, list2, center, triangleBounds);
						}
					}
					else {
						DivideTrianglesByZAxis(triangleList, list1, list2, center, triangleBounds);
						if (list1.Count == 0 || list2.Count == 0) {
							list1.Clear();
							list2.Clear();
							DivideTrianglesByXAxis(triangleList, list1, list2, center, triangleBounds);
						}
					}
				}
			}
			else {
				DivideTrianglesByZAxis(triangleList, list1, list2, center, triangleBounds);
				if (list1.Count == 0 || list2.Count == 0) {
					list1.Clear();
					list2.Clear();
					if (extents.y <= extents.x) {
						DivideTrianglesByXAxis(triangleList, list1, list2, center, triangleBounds);
						if (list1.Count == 0 || list2.Count == 0) {
							list1.Clear();
							list2.Clear();
							DivideTrianglesByYAxis(triangleList, list1, list2, center, triangleBounds);
						}
					}
					else {
						DivideTrianglesByYAxis(triangleList, list1, list2, center, triangleBounds);
						if (list1.Count == 0 || list2.Count == 0) {
							list1.Clear();
							list2.Clear();
							DivideTrianglesByXAxis(triangleList, list1, list2, center, triangleBounds);
						}
					}
				}
			}
			if (list1.Count == 0 || list2.Count == 0) {
				if (parentNode.m_triangleListIndex == -1) {
					parentNode.m_triangleListIndex = triangleListForEachNode.Count;
					AddTriangleList(ref triangleListForEachNode, triangleList);
				}
#if UNITY_EDITOR
				m_triangleCountInLeafNodes += numTriangles;
#endif
				parentNode.m_childIndex = -1;
				return;
			}
			triangleList.Clear();
			triangleList.Capacity = 0;
			TreeNodeVer2 child1 = new TreeNodeVer2();
			TreeNodeVer2 child2 = new TreeNodeVer2();
			child1.m_triangleListIndex = -1;
			child2.m_triangleListIndex = -1;
			CalculateBounds(ref child1, list1, triangleBounds);
			CalculateBounds(ref child2, list2, triangleBounds);
			BuildMeshTree(nodeList, ref child1, ref triangleListForEachNode, list1, triangleBounds);
			BuildMeshTree(nodeList, ref child2, ref triangleListForEachNode, list2, triangleBounds);
			parentNode.m_childIndex = nodeList.Count;
			nodeList.Add(child1);
			nodeList.Add(child2);
		}
		private void CalculateBounds(ref TreeNodeVer2 node, List<int> triangleList, Bounds[] triangleBounds)
		{
			if (triangleList.Count == 0) {
				node.m_bounds = new Bounds(Vector3.zero, Vector3.zero);
				return;
			}
			int first = triangleList[0]/3;
			Vector3 min = triangleBounds[first].min;
			Vector3 max = triangleBounds[first].max;
			for (int i = 1; i < triangleList.Count; ++i) {
				int index = triangleList[i]/3;
				Vector3 m = triangleBounds[index].min;
				Vector3 M = triangleBounds[index].max;
				min.x = Mathf.Min(min.x, m.x);
				min.y = Mathf.Min(min.y, m.y);
				min.z = Mathf.Min(min.z, m.z);
				max.x = Mathf.Max(max.x, M.x);
				max.y = Mathf.Max(max.y, M.y);
				max.z = Mathf.Max(max.z, M.z);
			}
			node.m_bounds.SetMinMax(min, max);
		}
	}
}
