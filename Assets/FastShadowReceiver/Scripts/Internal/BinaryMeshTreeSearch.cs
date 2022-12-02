//
// BinaryMeshTreeSearch.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using System.Collections.Generic;

namespace FastShadowReceiver {
	public class BinaryMeshTreeSearch : MeshTreeSearch {
		private List<int[]>[] m_triangleList;
		private List<int[]>[] m_triangleListToScissor;
		
		internal new void Initialize(int lightmapCountPlusOne)
		{
			if (!m_bOutputUVs) lightmapCountPlusOne = 1;
			base.Initialize(lightmapCountPlusOne);
			if (m_triangleList == null || m_triangleList.Length < lightmapCountPlusOne) {
				m_triangleList = new List<int[]>[lightmapCountPlusOne];
				for (int i = 0; i < lightmapCountPlusOne; ++i)
				{
					m_triangleList[i] = new List<int[]>();
				}
			}
			for (int i = 0; i < m_triangleList.Length; ++i)
			{
				m_triangleList[i].Clear();
			}
			if (m_bScissor) {
				if (m_triangleListToScissor == null || m_triangleListToScissor.Length < lightmapCountPlusOne) {
					m_triangleListToScissor = new List<int[]>[lightmapCountPlusOne];
					for (int i = 0; i < lightmapCountPlusOne; ++i)
					{
						m_triangleListToScissor[i] = new List<int[]>();
					}
				}
				for (int i = 0; i < m_triangleListToScissor.Length; ++i)
				{
					m_triangleListToScissor[i].Clear();
				}
			}
		}

		void ScissorTrianglesInList<Vertex, VertexFunc, VertexArray, VertexArrayFunc>(VertexArray vertices, int[] indices, List<int[]> triangleListToScissor, ref ScissoredTriangleCount scissoredTriangleCount)
			where VertexFunc : IScissoredVertexFunctions<Vertex>
			where VertexArrayFunc : IVertexArrayFunction<Vertex, VertexArray>
		{
			int numTrianglesToScissor = 0;
			for (int i = 0; i < triangleListToScissor.Count; ++i)
			{
				numTrianglesToScissor += triangleListToScissor[i].Length;
			}
			InitScissorBuffer<Vertex, VertexFunc>(numTrianglesToScissor);
			if (m_bBackfaceCulling)
			{
				for (int i = 0; i < triangleListToScissor.Count; ++i)
				{
					int[] triangles = triangleListToScissor[i];
					for (int tri = 0; tri < triangles.Length; ++tri)
					{
						int index = triangles[tri];
						Vertex v0 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
						Vertex v1 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
						Vertex v2 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
						if (isFrontFaceTriangle(default(VertexFunc).GetVertexPosition(ref v0), default(VertexFunc).GetVertexPosition(ref v1), default(VertexFunc).GetVertexPosition(ref v2)))
						{
							ScissorTriangle<Vertex, VertexFunc>(ref v0, ref v1, ref v2, ref scissoredTriangleCount);
						}
					}
				}
			}
			else
			{
				for (int i = 0; i < triangleListToScissor.Count; ++i)
				{
					int[] triangles = triangleListToScissor[i];
					for (int tri = 0; tri < triangles.Length; ++tri)
					{
						int index = triangles[tri];
						Vertex v0 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
						Vertex v1 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
						Vertex v2 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
						ScissorTriangle<Vertex, VertexFunc>(ref v0, ref v1, ref v2, ref scissoredTriangleCount);
					}
				}
			}
		}
		void AddTriangleList<Vertex, VertexArray, VertexArrayFunc>(SearchResult result, VertexArray array, int[] indices, List<int[]> triangleList)
			where VertexArrayFunc : IVertexArrayFunction<Vertex, VertexArray>
		{
			int nVertex = result.vertices.Count;
			if (m_bBackfaceCulling)
			{
				for (int i = 0; i < triangleList.Count; ++i)
				{
					int[] triangles = triangleList[i];
					for (int tri = 0; tri < triangles.Length; ++tri)
					{
						int index = triangles[tri];
						int i0 = indices[index++];
						int i1 = indices[index++];
						int i2 = indices[index++];
						Vector3 v0 = default(VertexArrayFunc).GetVertexPosition(ref array, i0);
						Vector3 v1 = default(VertexArrayFunc).GetVertexPosition(ref array, i1);
						Vector3 v2 = default(VertexArrayFunc).GetVertexPosition(ref array, i2);
						if (isFrontFaceTriangle(v0, v1, v2))
						{
							result.indices.Add(nVertex++);
							default(VertexArrayFunc).AddVertex(result, ref array, i0);
							result.indices.Add(nVertex++);
							default(VertexArrayFunc).AddVertex(result, ref array, i1);
							result.indices.Add(nVertex++);
							default(VertexArrayFunc).AddVertex(result, ref array, i2);
						}
					}
				}
			}
			else
			{
				for (int i = 0; i < triangleList.Count; ++i)
				{
					int[] triangles = triangleList[i];
					for (int tri = 0; tri < triangles.Length; ++tri)
					{
						int index = triangles[tri];
						result.indices.Add(nVertex++);
						default(VertexArrayFunc).AddVertex(result, ref array, indices[index++]);
						result.indices.Add(nVertex++);
						default(VertexArrayFunc).AddVertex(result, ref array, indices[index++]);
						result.indices.Add(nVertex++);
						default(VertexArrayFunc).AddVertex(result, ref array, indices[index++]);
					}
				}
			}
		}
		internal void Finalize(Vector3[] vertices, int[] indices, Vector3[] normals, Vector2[] uvs)
		{
			int count = m_results.Length;
			for (int i = 0; i < count; ++i)
			{
				Finalize(vertices, indices, normals, uvs, i);
			}
		}
		private void Finalize(Vector3[] vertices, int[] indices, Vector3[] normals, Vector2[] uvs, int lightmapIndexPlusOne)
		{
			m_bOutputNormals = m_bOutputNormals && (normals != null && 0 < normals.Length);
			m_bOutputUVs = m_bOutputUVs && (uvs != null && 0 < uvs.Length);
			ScissoredTriangleCount scissoredTriangleCount = new ScissoredTriangleCount();
			// scissor triangles if any
			if (m_bScissor) {
				List<int[]> triangleListToScissor = m_triangleListToScissor[lightmapIndexPlusOne];
				if (0 < triangleListToScissor.Count)
				{
					if (m_bOutputNormals)
					{
						if (m_bOutputUVs)
						{
							VertexNormalAndUVArray array = new VertexNormalAndUVArray() { v = vertices, n = normals, uv = uvs };
							ScissorTrianglesInList<VertexNormalAndUV, ScissorVertexNormalAndUVFunc, VertexNormalAndUVArray, VertexNormalAndUVArrayFunc>(array, indices, triangleListToScissor, ref scissoredTriangleCount);
							CalculateScissoredVertexBounds<VertexNormalAndUV, ScissorVertexNormalAndUVFunc>(scissoredTriangleCount.m_nTriangleCount, lightmapIndexPlusOne, m_triangleList[lightmapIndexPlusOne].Count == 0);
						}
						else
						{
							VertexAndNormalArray array = new VertexAndNormalArray() { v = vertices, n = normals };
							ScissorTrianglesInList<VertexAndNormal, ScissorVertexAndNormalFunc, VertexAndNormalArray, VertexAndNormalArrayFunc>(array, indices, triangleListToScissor, ref scissoredTriangleCount);
							CalculateScissoredVertexBounds<VertexAndNormal, ScissorVertexAndNormalFunc>(scissoredTriangleCount.m_nTriangleCount, lightmapIndexPlusOne, m_triangleList[lightmapIndexPlusOne].Count == 0);
						}
					}
					else if (m_bOutputUVs)
					{
						VertexAndUVArray array = new VertexAndUVArray() { v = vertices, uv = uvs };
						ScissorTrianglesInList<VertexAndUV, ScissorVertexAndUVFunc, VertexAndUVArray, VertexAndUVArrayFunc>(array, indices, triangleListToScissor, ref scissoredTriangleCount);
						CalculateScissoredVertexBounds<VertexAndUV, ScissorVertexAndUVFunc>(scissoredTriangleCount.m_nTriangleCount, lightmapIndexPlusOne, m_triangleList[lightmapIndexPlusOne].Count == 0);
					}
					else
					{
						ScissorTrianglesInList<Vector3, ScissorVertexFunc, Vector3[], VertexArrayFunc>(vertices, indices, triangleListToScissor, ref scissoredTriangleCount);
						CalculateScissoredVertexBounds<Vector3, ScissorVertexFunc>(scissoredTriangleCount.m_nTriangleCount, lightmapIndexPlusOne, m_triangleList[lightmapIndexPlusOne].Count == 0);
					}
				}
			}
			// count vertices and indices
			int numVertexCount = 0;
			List<int[]> triangleList = m_triangleList[lightmapIndexPlusOne];
			for (int i = 0; i < triangleList.Count; ++i) {
				numVertexCount += triangleList[i].Length * 3;
			}
			int numIndexCount = numVertexCount;
			numVertexCount += scissoredTriangleCount.m_nVertexCount;
			numIndexCount += scissoredTriangleCount.m_nIndexCount;
			// create result buffer
			InitResultBuffer(numVertexCount, numIndexCount, m_bOutputNormals, m_bOutputUVs, lightmapIndexPlusOne);
			if (numVertexCount == 0)
			{
				return;
			}
			SearchResult result = m_results[lightmapIndexPlusOne];
			// fill result buffer
			if (m_bOutputNormals)
			{
				if (m_bOutputUVs)
				{
					VertexNormalAndUVArray array = new VertexNormalAndUVArray() { v = vertices, n = normals, uv = uvs };
					AddTriangleList<VertexNormalAndUV, VertexNormalAndUVArray, VertexNormalAndUVArrayFunc>(result, array, indices, triangleList);
					AddScissoredTrianglesToResults<VertexNormalAndUV, ScissorVertexNormalAndUVFunc>(scissoredTriangleCount.m_nTriangleCount, lightmapIndexPlusOne);
				}
				else
				{
					VertexAndNormalArray array = new VertexAndNormalArray() { v = vertices, n = normals };
					AddTriangleList<VertexAndNormal, VertexAndNormalArray, VertexAndNormalArrayFunc>(result, array, indices, triangleList);
					AddScissoredTrianglesToResults<VertexAndNormal, ScissorVertexAndNormalFunc>(scissoredTriangleCount.m_nTriangleCount, lightmapIndexPlusOne);
				}
			}
			else if (m_bOutputUVs)
			{
				VertexAndUVArray array = new VertexAndUVArray() { v = vertices, uv = uvs };
				AddTriangleList<VertexAndUV, VertexAndUVArray, VertexAndUVArrayFunc>(result, array, indices, triangleList);
				AddScissoredTrianglesToResults<VertexAndUV, ScissorVertexAndUVFunc>(scissoredTriangleCount.m_nTriangleCount, lightmapIndexPlusOne);
			}
			else
			{
				AddTriangleList<Vector3, Vector3[], VertexArrayFunc>(result, vertices, indices, triangleList);
				AddScissoredTrianglesToResults<Vector3, ScissorVertexFunc>(scissoredTriangleCount.m_nTriangleCount, lightmapIndexPlusOne);
			}
		}

		internal bool IsInView(Bounds bounds, out float scissorDistance)
		{
			int numClipPlanes = m_clipPlanes.clipPlaneCount;
			scissorDistance = 0.0f;
			for (int i = 0; i < numClipPlanes; ++i) {
				float distance = m_clipPlanes.clipPlanes[i].GetDistanceToPoint(bounds.center);
				float extent = Vector3.Dot(bounds.extents, m_clipMetric[i]);
				float maxDistance = distance + extent;
				float minDistance = distance - extent;
				if (maxDistance < 0 || m_clipPlanes.maxDistance[i] < minDistance) {
					return false;
				}
				scissorDistance = Mathf.Min(Mathf.Min(scissorDistance, minDistance), m_clipPlanes.maxDistance[i] - maxDistance);
			}
			return true;
		}
		
		internal void AddTriangles(int[] trianglesToAdd, Bounds bounds, bool isPartial, int lightmapIndexPlusOne)
		{
			if (trianglesToAdd == null || trianglesToAdd.Length == 0)
			{
				return;
			}
			if (!m_bOutputUVs)
			{
				lightmapIndexPlusOne = 0;
			}
			if (m_bScissor && isPartial) {
				m_triangleListToScissor[lightmapIndexPlusOne].Add(trianglesToAdd);
			}
			else {
				Vector3 min = bounds.min;
				Vector3 max = bounds.max;
				if (m_triangleList[lightmapIndexPlusOne].Count == 0) {
					m_results[lightmapIndexPlusOne].minBounds = min;
					m_results[lightmapIndexPlusOne].maxBounds = max;
				}
				else {
					m_results[lightmapIndexPlusOne].minBounds.x = Mathf.Min (m_results[lightmapIndexPlusOne].minBounds.x, min.x);
					m_results[lightmapIndexPlusOne].minBounds.y = Mathf.Min (m_results[lightmapIndexPlusOne].minBounds.y, min.y);
					m_results[lightmapIndexPlusOne].minBounds.z = Mathf.Min (m_results[lightmapIndexPlusOne].minBounds.z, min.z);
					m_results[lightmapIndexPlusOne].maxBounds.x = Mathf.Max (m_results[lightmapIndexPlusOne].maxBounds.x, max.x);
					m_results[lightmapIndexPlusOne].maxBounds.y = Mathf.Max (m_results[lightmapIndexPlusOne].maxBounds.y, max.y);
					m_results[lightmapIndexPlusOne].maxBounds.z = Mathf.Max (m_results[lightmapIndexPlusOne].maxBounds.z, max.z);
				}
				m_triangleList[lightmapIndexPlusOne].Add(trianglesToAdd);
			}
		}
	}
}
