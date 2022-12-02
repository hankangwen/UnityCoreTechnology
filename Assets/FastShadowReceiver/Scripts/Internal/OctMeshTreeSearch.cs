//
// OctMeshTreeSearch.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using System.Collections.Generic;

namespace FastShadowReceiver {
	public class OctMeshTreeSearch : MeshTreeSearch {
		struct ClipFlags {
			public byte all;
			public byte xFlags;
			public byte yFlags;
			public byte zFlags;
		}
		internal const byte FLAGS_LEFT   = (1 << 0) | (1 << 2) | (1 << 4) | (1 << 6);
		internal const byte FLAGS_RIGHT  = (1 << 1) | (1 << 3) | (1 << 5) | (1 << 7);
		internal const byte FLAGS_BOTTOM = (1 << 0) | (1 << 1) | (1 << 4) | (1 << 5);
		internal const byte FLAGS_TOP    = (1 << 2) | (1 << 3) | (1 << 6) | (1 << 7);
		internal const byte FLAGS_FRONT  = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3);
		internal const byte FLAGS_BACK   = (1 << 4) | (1 << 5) | (1 << 6) | (1 << 7);
		private ClipFlags[]    m_clipFlags;
		private ClipFlags[]    m_clipFlags2;

		private HashSet<int> m_triangleSet;
		private List<int>[] m_triangleList;
		
		internal new void Initialize(int lightmapCountPlusOne)
		{
			if (!m_bOutputUVs) lightmapCountPlusOne = 1;
			base.Initialize(lightmapCountPlusOne);
			int numClipPlanes = m_clipPlanes.clipPlaneCount;
			if (m_clipMetric == null || m_clipMetric.Length < numClipPlanes) {
				m_clipMetric = new Vector3[numClipPlanes];
			}
			if (m_clipFlags == null || m_clipFlags.Length < numClipPlanes) {
				m_clipFlags = new ClipFlags[numClipPlanes];
			}
			if (m_clipPlanes.twoSideClipping && (m_clipFlags2 == null || m_clipFlags2.Length < numClipPlanes)) {
				m_clipFlags2 = new ClipFlags[numClipPlanes];
			}
			for (int i = 0; i < numClipPlanes; ++i) {
				Plane clipPlane = m_clipPlanes.clipPlanes[i];
				m_clipMetric[i].x = Mathf.Abs(clipPlane.normal.x);
				m_clipMetric[i].y = Mathf.Abs(clipPlane.normal.y);
				m_clipMetric[i].z = Mathf.Abs(clipPlane.normal.z);
				if (clipPlane.normal.x < 0) {
					m_clipFlags[i].xFlags = FLAGS_LEFT;
				}
				else {
					m_clipFlags[i].xFlags = FLAGS_RIGHT;
				}
				if (clipPlane.normal.y < 0) {
					m_clipFlags[i].yFlags = FLAGS_BOTTOM;
				}
				else {
					m_clipFlags[i].yFlags = FLAGS_TOP;
				}
				if (clipPlane.normal.z < 0) {
					m_clipFlags[i].zFlags = FLAGS_FRONT;
				}
				else {
					m_clipFlags[i].zFlags = FLAGS_BACK;
				}
				m_clipFlags[i].all = (byte)(m_clipFlags[i].xFlags|m_clipFlags[i].yFlags|m_clipFlags[i].zFlags);
				if (m_clipPlanes.twoSideClipping) {
					m_clipFlags2[i].xFlags = (byte)~m_clipFlags[i].xFlags;
					m_clipFlags2[i].yFlags = (byte)~m_clipFlags[i].yFlags;
					m_clipFlags2[i].zFlags = (byte)~m_clipFlags[i].zFlags;
					m_clipFlags2[i].all = (byte)(m_clipFlags2[i].xFlags|m_clipFlags2[i].yFlags|m_clipFlags2[i].zFlags);
				}
			}
			if (m_triangleList == null || m_triangleList.Length < lightmapCountPlusOne) {
				int i = m_triangleList == null ? 0 : m_triangleList.Length;
				m_triangleList = new List<int>[lightmapCountPlusOne];
				while (i < lightmapCountPlusOne)
				{
					m_triangleList[i++] = new List<int>();
				}
			}
			if (m_triangleSet == null) {
				m_triangleSet = new HashSet<int>();
			}
			for (int i = 0; i < lightmapCountPlusOne; ++i)
			{
				m_triangleList[i].Clear();
			}
			m_triangleSet.Clear();
		}
		void ScissorTrianglesInList<Vertex, VertexFunc, VertexArray, VertexArrayFunc>(VertexArray vertices, int[] indices, List<int> triangleList, ref ScissoredTriangleCount scissoredTriangleCount)
			where VertexFunc : IScissoredVertexFunctions<Vertex>
			where VertexArrayFunc : IVertexArrayFunction<Vertex, VertexArray>
		{
			int numTriangles = triangleList.Count;
			InitScissorBuffer<Vertex, VertexFunc>(numTriangles);
			if (m_bBackfaceCulling)
			{
				for (int i = 0; i < numTriangles; ++i)
				{
					int index = triangleList[i];
					Vertex v0 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
					Vertex v1 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
					Vertex v2 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
					if (isFrontFaceTriangle(default(VertexFunc).GetVertexPosition(ref v0), default(VertexFunc).GetVertexPosition(ref v1), default(VertexFunc).GetVertexPosition(ref v2)))
					{
						ScissorTriangle<Vertex, VertexFunc>(ref v0, ref v1, ref v2, ref scissoredTriangleCount);
					}
				}
			}
			else
			{
				for (int i = 0; i < numTriangles; ++i)
				{
					int index = triangleList[i];
					Vertex v0 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
					Vertex v1 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
					Vertex v2 = default(VertexArrayFunc).GetVertexFromArray(ref vertices, indices[index++]);
					ScissorTriangle<Vertex, VertexFunc>(ref v0, ref v1, ref v2, ref scissoredTriangleCount);
				}
			}
		}
		void AddTriangleList<Vertex, VertexArray, VertexArrayFunc>(SearchResult result, VertexArray array, int[] indices, List<int> triangleList)
			where VertexArrayFunc : IVertexArrayFunction<Vertex, VertexArray>
		{
			int numTriangles = triangleList.Count;
			int nVertex = result.vertices.Count;
			if (m_bBackfaceCulling)
			{
				for (int i = 0; i < numTriangles; ++i)
				{
					int index = triangleList[i];
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
			else
			{
				for (int i = 0; i < numTriangles; ++i)
				{
					int index = triangleList[i];
					result.indices.Add(nVertex++);
					default(VertexArrayFunc).AddVertex(result, ref array, indices[index++]);
					result.indices.Add(nVertex++);
					default(VertexArrayFunc).AddVertex(result, ref array, indices[index++]);
					result.indices.Add(nVertex++);
					default(VertexArrayFunc).AddVertex(result, ref array, indices[index++]);
				}
			}
		}
		internal void Finalize(Vector3[] vertices, int[] indices, Vector3[] normals, Vector2[] uvs)
		{
			int lightmapCountPlusOne = m_results.Length;
			for (int i = 0; i < lightmapCountPlusOne; ++i)
			{
				Finalize(vertices, indices, normals, uvs, i);
			}
		}
		private void Finalize(Vector3[] vertices, int[] indices, Vector3[] normals, Vector2[] uvs, int lightmapIndexPlusOne)
		{
			int nVertexCount = 0;
			int nIndexCount = 0;
			int scissoredTriangleCount = 0;
			int numTriangles = m_triangleList[lightmapIndexPlusOne].Count;
			m_bOutputNormals = m_bOutputNormals && (normals != null && 0 < normals.Length);
			m_bOutputUVs = m_bOutputUVs && (uvs != null && 0 < uvs.Length);
			if (m_bScissor && 0 < numTriangles) {
				ScissoredTriangleCount scissorCount = new ScissoredTriangleCount();
				if (m_bOutputNormals)
				{
					if (m_bOutputUVs)
					{
						VertexNormalAndUVArray array = new VertexNormalAndUVArray() { v = vertices, n = normals, uv = uvs };
						ScissorTrianglesInList<VertexNormalAndUV, ScissorVertexNormalAndUVFunc, VertexNormalAndUVArray, VertexNormalAndUVArrayFunc>(array, indices, m_triangleList[lightmapIndexPlusOne], ref scissorCount);
						CalculateScissoredVertexBounds<VertexNormalAndUV, ScissorVertexNormalAndUVFunc>(scissorCount.m_nTriangleCount, lightmapIndexPlusOne, true);
					}
					else
					{
						VertexAndNormalArray array = new VertexAndNormalArray() { v = vertices, n = normals };
						ScissorTrianglesInList<VertexAndNormal, ScissorVertexAndNormalFunc, VertexAndNormalArray, VertexAndNormalArrayFunc>(array, indices, m_triangleList[lightmapIndexPlusOne], ref scissorCount);
						CalculateScissoredVertexBounds<VertexAndNormal, ScissorVertexAndNormalFunc>(scissorCount.m_nTriangleCount, lightmapIndexPlusOne, true);
					}
				}
				else if (m_bOutputUVs)
				{
					VertexAndUVArray array = new VertexAndUVArray() { v = vertices, uv = uvs };
					ScissorTrianglesInList<VertexAndUV, ScissorVertexAndUVFunc, VertexAndUVArray, VertexAndUVArrayFunc>(array, indices, m_triangleList[lightmapIndexPlusOne], ref scissorCount);
					CalculateScissoredVertexBounds<VertexAndUV, ScissorVertexAndUVFunc>(scissorCount.m_nTriangleCount, lightmapIndexPlusOne, true);
				}
				else
				{
					ScissorTrianglesInList<Vector3, ScissorVertexFunc, Vector3[], VertexArrayFunc>(vertices, indices, m_triangleList[lightmapIndexPlusOne], ref scissorCount);
					CalculateScissoredVertexBounds<Vector3, ScissorVertexFunc>(scissorCount.m_nTriangleCount, lightmapIndexPlusOne, true);
				}
				nVertexCount = scissorCount.m_nVertexCount;
				nIndexCount = scissorCount.m_nIndexCount;
				scissoredTriangleCount = scissorCount.m_nTriangleCount;
			}
			else {
				nVertexCount = 3 * numTriangles;
				nIndexCount = 3 * numTriangles;
			}
			// create result buffer
			InitResultBuffer(nVertexCount, nIndexCount, m_bOutputNormals, m_bOutputUVs, lightmapIndexPlusOne);
			if (nVertexCount == 0)
			{
				return;
			}
			SearchResult result = m_results[lightmapIndexPlusOne];
			// fill result buffer
			if (m_bOutputNormals)
			{
				if (m_bOutputUVs)
				{
					if (m_bScissor)
					{
						AddScissoredTrianglesToResults<VertexNormalAndUV, ScissorVertexNormalAndUVFunc>(scissoredTriangleCount, lightmapIndexPlusOne);
					}
					else
					{
						VertexNormalAndUVArray array = new VertexNormalAndUVArray() { v = vertices, n = normals, uv = uvs };
						AddTriangleList<VertexNormalAndUV, VertexNormalAndUVArray, VertexNormalAndUVArrayFunc>(result, array, indices, m_triangleList[lightmapIndexPlusOne]);
					}
				}
				else
				{
					if (m_bScissor)
					{
						AddScissoredTrianglesToResults<VertexAndNormal, ScissorVertexAndNormalFunc>(scissoredTriangleCount, lightmapIndexPlusOne);
					}
					else
					{
						VertexAndNormalArray array = new VertexAndNormalArray() { v = vertices, n = normals };
						AddTriangleList<VertexAndNormal, VertexAndNormalArray, VertexAndNormalArrayFunc>(result, array, indices, m_triangleList[lightmapIndexPlusOne]);
					}
				}
			}
			else if (m_bOutputUVs)
			{
				if (m_bScissor)
				{
					AddScissoredTrianglesToResults<VertexAndUV, ScissorVertexAndUVFunc>(scissoredTriangleCount, lightmapIndexPlusOne);
				}
				else
				{
					VertexAndUVArray array = new VertexAndUVArray() { v = vertices, uv = uvs };
					AddTriangleList<VertexAndUV, VertexAndUVArray, VertexAndUVArrayFunc>(result, array, indices, m_triangleList[lightmapIndexPlusOne]);
				}
			}
			else
			{
				if (m_bScissor)
				{
					AddScissoredTrianglesToResults<Vector3, ScissorVertexFunc>(scissoredTriangleCount, lightmapIndexPlusOne);
				}
				else
				{
					AddTriangleList<Vector3, Vector3[], VertexArrayFunc>(result, vertices, indices, m_triangleList[lightmapIndexPlusOne]);
				}
			}
		}
		private static uint UpdateClipFlags(uint flags, ClipFlags clipFlags, float distance, float xExtent, float yExtent, float zExtent)
		{
			flags &= clipFlags.all;
			float d = distance + xExtent;
			if (d < 0) {
				flags &= (uint)(clipFlags.yFlags | clipFlags.zFlags);
				if (d + yExtent < 0) {
					flags &= clipFlags.zFlags;
				}
				if (d + zExtent < 0) {
					flags &= clipFlags.yFlags;
				}
			}
			d = distance + yExtent;
			if (d < 0) {
				flags &= (uint)(clipFlags.zFlags | clipFlags.xFlags);
				if (d + zExtent < 0) {
					flags &= clipFlags.xFlags;
				}
			}
			d = distance + zExtent;
			if (d < 0) {
				flags &= (uint)(clipFlags.xFlags | clipFlags.yFlags);
			}
			return flags;
		}
		internal uint IsInView(Vector3 center, Vector3 extents, out bool isPartial)
		{
			int numClipPlanes = m_clipPlanes.clipPlaneCount;
			uint flags = 0xff;
			isPartial = false;
			for (int i = 0; i < numClipPlanes; ++i) {
				float distance = m_clipPlanes.clipPlanes[i].GetDistanceToPoint(center);
				float xExtent = extents.x * m_clipMetric[i].x;
				float yExtent = extents.y * m_clipMetric[i].y;
				float zExtent = extents.z * m_clipMetric[i].z;
				float extent = xExtent + yExtent + zExtent;
				float maxDistance = distance + extent;
				float minDistance = distance - extent;
				float clipDistance = m_clipPlanes.maxDistance[i];
				if (maxDistance < 0 || clipDistance < minDistance) {
					return 0U;
				}
				if (minDistance < 0) {
					isPartial = true;
					if (distance < 0) {
						flags = UpdateClipFlags(flags, m_clipFlags[i], distance, xExtent, yExtent, zExtent);
					}
				}
				if (m_clipPlanes.twoSideClipping && clipDistance < maxDistance) {
					// in case of orthographic
					isPartial = true;
					distance = clipDistance - distance;
					if (distance < 0) {
						flags = UpdateClipFlags(flags, m_clipFlags2[i], distance, xExtent, yExtent, zExtent);
					}
				}
			}
			return flags;
		}
		
		internal void AddTriangles(int[] trianglesToAdd, Vector3 center, Vector3 extents, int lightmapCountPlusOne)
		{
			if (!m_bOutputUVs)
			{
				lightmapCountPlusOne = 0;
			}
			for (int i = 0; i < trianglesToAdd.Length; ++i) {
				int tri = trianglesToAdd[i];
				if (!m_triangleSet.Contains(tri)) {
					m_triangleList[lightmapCountPlusOne].Add(tri);
					m_triangleSet.Add(tri);
				}
			}
			if (!m_bScissor) {
				Vector3 min = center - extents;
				Vector3 max = center + extents;
				if (m_triangleList[lightmapCountPlusOne].Count == 0) {
					m_results[lightmapCountPlusOne].minBounds = min;
					m_results[lightmapCountPlusOne].maxBounds = max;
				}
				else {
					m_results[lightmapCountPlusOne].minBounds.x = Mathf.Min (m_results[lightmapCountPlusOne].minBounds.x, min.x);
					m_results[lightmapCountPlusOne].minBounds.y = Mathf.Min (m_results[lightmapCountPlusOne].minBounds.y, min.y);
					m_results[lightmapCountPlusOne].minBounds.z = Mathf.Min (m_results[lightmapCountPlusOne].minBounds.z, min.z);
					m_results[lightmapCountPlusOne].maxBounds.x = Mathf.Max (m_results[lightmapCountPlusOne].maxBounds.x, max.x);
					m_results[lightmapCountPlusOne].maxBounds.y = Mathf.Max (m_results[lightmapCountPlusOne].maxBounds.y, max.y);
					m_results[lightmapCountPlusOne].maxBounds.z = Mathf.Max (m_results[lightmapCountPlusOne].maxBounds.z, max.z);
				}
			}
		}

	}
}
