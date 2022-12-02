//
// TerrainMeshTreeSearch.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using System.Collections.Generic;

namespace FastShadowReceiver {
	public class TerrainMeshTreeSearch : MeshTreeSearch {
		private uint[]    m_clipFlags;
		private int       m_minX, m_maxX;
		private int       m_minZ, m_maxZ;
		
		struct Patch {
			public Patch(int posX, int posZ, int width)
			{
				m_posX = posX; m_posZ = posZ; m_width = width;
			}
			public int m_posX;
			public int m_posZ;
			public int m_width;
		}
		private List<Patch> m_patchList;
		private List<Patch> m_patchListToScissor;
		private int         m_indexCount;

		private const uint CLIP_FLAGS_NXNZ = 7 | (3 << 4) | (5 << 8) | (1 << 12);
		private const uint CLIP_FLAGS_NXPZ = 13 | (12 << 4) | (5 << 8) | (4 << 12);
		private const uint CLIP_FLAGS_PXNZ = 11 | (3 << 4) | (10 << 8) | (2 << 12);
		private const uint CLIP_FLAGS_PXPZ = 14 | (12 << 4) | (10 << 8) | (8 << 12);
		internal void Initialize()
		{
			base.Initialize(1);
			int numClipPlanes = m_clipPlanes.clipPlaneCount;
			if (m_clipMetric == null || m_clipMetric.Length < numClipPlanes) {
				m_clipMetric = new Vector3[numClipPlanes];
			}
			if (m_clipFlags == null || m_clipFlags.Length < numClipPlanes) {
				m_clipFlags = new uint[numClipPlanes];
			}
			for (int i = 0; i < numClipPlanes; ++i) {
				Plane clipPlane = m_clipPlanes.clipPlanes[i];
				m_clipMetric[i].x = Mathf.Abs(clipPlane.normal.x);
				m_clipMetric[i].y = Mathf.Abs(clipPlane.normal.y);
				m_clipMetric[i].z = Mathf.Abs(clipPlane.normal.z);
				if (clipPlane.normal.x < 0) {
					if (clipPlane.normal.z < 0) {
						m_clipFlags[i] = CLIP_FLAGS_NXNZ | (CLIP_FLAGS_PXPZ << 16);
					}
					else {
						m_clipFlags[i] = CLIP_FLAGS_NXPZ | (CLIP_FLAGS_PXNZ << 16);
					}
				}
				else {
					if (clipPlane.normal.z < 0) {
						m_clipFlags[i] = CLIP_FLAGS_PXNZ | (CLIP_FLAGS_NXPZ << 16);
					}
					else {
						m_clipFlags[i] = CLIP_FLAGS_PXPZ | (CLIP_FLAGS_NXNZ << 16);
					}
				}
			}
			
			if (m_patchList == null) {
				m_patchList = new List<Patch>();
			}
			m_patchList.Clear();
			if (m_bScissor) {
				if (m_patchListToScissor == null) {
					m_patchListToScissor = new List<Patch>();
				}
				m_patchListToScissor.Clear();
			}
			m_indexCount = 0;
			m_minX = m_maxX = 0;
			m_minZ = m_maxZ = 0;
		}
#if UNITY_2019_3_OR_NEWER
		internal void Finalize(float[,] heightMap, float scaleX, float scaleZ, int heightmapWidth, bool[,] holeMap)
#else
		internal void Finalize(float[,] heightMap, float scaleX, float scaleZ, int heightmapWidth)
#endif
		{
			m_results[0].minBounds.x = scaleX * m_minX;
			m_results[0].minBounds.z = scaleZ * m_minZ;
			m_results[0].maxBounds.x = scaleX * m_maxX;
			m_results[0].maxBounds.z = scaleZ * m_maxZ;

			ScissoredTriangleCount scissoredTriangleCount = new ScissoredTriangleCount();
			// scissor triangles if any
			if (m_bScissor && 0 < m_patchListToScissor.Count) {
				int numTrianglesToScissor = 0;
				for (int i = 0; i < m_patchListToScissor.Count; ++i) {
					int w = m_patchListToScissor[i].m_width;
					numTrianglesToScissor += 2 * w * w;
				}
				if (m_bOutputUVs) {
					float uvScale = 1.0f / heightmapWidth;
					if (m_bOutputNormals) {
						float invScaleX = 0.5f/scaleX;
						float invScaleZ = 0.5f/scaleZ;
						InitScissorBuffer<VertexNormalAndUV, ScissorVertexNormalAndUVFunc>(numTrianglesToScissor);
						for (int i = 0; i < m_patchListToScissor.Count; ++i) {
							Patch patch = m_patchListToScissor [i];
							for (int z = patch.m_posZ, zEnd = patch.m_posZ + patch.m_width; z < zEnd; ++z) {
								for (int x = patch.m_posX, xEnd = patch.m_posX + patch.m_width; x < xEnd; ++x) {
#if UNITY_2019_3_OR_NEWER
									if (holeMap[z, x])
#endif
									{
										VertexNormalAndUV v0 = new VertexNormalAndUV(new Vector3(scaleX * x, heightMap[z, x], scaleZ * z), CalculateNormal(heightMap, invScaleX, invScaleZ, z, x), new Vector2(uvScale * x, uvScale * z));
										VertexNormalAndUV v1 = new VertexNormalAndUV(new Vector3(scaleX * (x + 1), heightMap[z, x + 1], scaleZ * z), CalculateNormal(heightMap, invScaleX, invScaleZ, z, x + 1), new Vector2(uvScale * (x + 1), uvScale * z));
										VertexNormalAndUV v2 = new VertexNormalAndUV(new Vector3(scaleX * x, heightMap[z + 1, x], scaleZ * (z + 1)), CalculateNormal(heightMap, invScaleX, invScaleZ, z + 1, x), new Vector2(uvScale * x, uvScale * (z + 1)));
										VertexNormalAndUV v3 = new VertexNormalAndUV(new Vector3(scaleX * (x + 1), heightMap[z + 1, x + 1], scaleZ * (z + 1)), CalculateNormal(heightMap, invScaleX, invScaleZ, z + 1, x + 1), new Vector2(uvScale * (x + 1), uvScale * (z + 1)));
										ScissorTriangle<VertexNormalAndUV, ScissorVertexNormalAndUVFunc>(ref v0, ref v2, ref v3, ref scissoredTriangleCount);
										ScissorTriangle<VertexNormalAndUV, ScissorVertexNormalAndUVFunc>(ref v0, ref v3, ref v1, ref scissoredTriangleCount);
									}
								}
							}
						}
						CalculateScissoredVertexBounds<VertexNormalAndUV, ScissorVertexNormalAndUVFunc>(scissoredTriangleCount.m_nTriangleCount, 0, m_patchList.Count == 0);
					} else {
						InitScissorBuffer<VertexAndUV, ScissorVertexAndUVFunc>(numTrianglesToScissor);
						for (int i = 0; i < m_patchListToScissor.Count; ++i) {
							Patch patch = m_patchListToScissor [i];
							for (int z = patch.m_posZ, zEnd = patch.m_posZ + patch.m_width; z < zEnd; ++z) {
								for (int x = patch.m_posX, xEnd = patch.m_posX + patch.m_width; x < xEnd; ++x) {
#if UNITY_2019_3_OR_NEWER
									if (holeMap[z, x])
#endif
									{
										VertexAndUV v0 = new VertexAndUV(new Vector3(scaleX * x, heightMap[z, x], scaleZ * z), new Vector2(uvScale * x, uvScale * z));
										VertexAndUV v1 = new VertexAndUV(new Vector3(scaleX * (x + 1), heightMap[z, x + 1], scaleZ * z), new Vector2(uvScale * (x + 1), uvScale * z));
										VertexAndUV v2 = new VertexAndUV(new Vector3(scaleX * x, heightMap[z + 1, x], scaleZ * (z + 1)), new Vector2(uvScale * x, uvScale * (z + 1)));
										VertexAndUV v3 = new VertexAndUV(new Vector3(scaleX * (x + 1), heightMap[z + 1, x + 1], scaleZ * (z + 1)), new Vector2(uvScale * (x + 1), uvScale * (z + 1)));
										ScissorTriangle<VertexAndUV, ScissorVertexAndUVFunc>(ref v0, ref v2, ref v3, ref scissoredTriangleCount);
										ScissorTriangle<VertexAndUV, ScissorVertexAndUVFunc>(ref v0, ref v3, ref v1, ref scissoredTriangleCount);
									}
								}
							}
						}
						CalculateScissoredVertexBounds<VertexAndUV, ScissorVertexAndUVFunc>(scissoredTriangleCount.m_nTriangleCount, 0, m_patchList.Count == 0);
					}
				} else if (m_bOutputNormals) {
					float invScaleX = 0.5f/scaleX;
					float invScaleZ = 0.5f/scaleZ;
					InitScissorBuffer<VertexAndNormal, ScissorVertexAndNormalFunc>(numTrianglesToScissor);
					for (int i = 0; i < m_patchListToScissor.Count; ++i) {
						Patch patch = m_patchListToScissor[i];
						for (int z = patch.m_posZ, zEnd = patch.m_posZ + patch.m_width; z < zEnd; ++z) {
							for (int x = patch.m_posX, xEnd = patch.m_posX + patch.m_width; x < xEnd; ++x) {
#if UNITY_2019_3_OR_NEWER
								if (holeMap[z, x])
#endif
								{
									VertexAndNormal v0 = new VertexAndNormal(new Vector3(scaleX * x, heightMap[z, x], scaleZ * z), CalculateNormal(heightMap, invScaleX, invScaleZ, z, x));
									VertexAndNormal v1 = new VertexAndNormal(new Vector3(scaleX * (x + 1), heightMap[z, x + 1], scaleZ * z), CalculateNormal(heightMap, invScaleX, invScaleZ, z, x + 1));
									VertexAndNormal v2 = new VertexAndNormal(new Vector3(scaleX * x, heightMap[z + 1, x], scaleZ * (z + 1)), CalculateNormal(heightMap, invScaleX, invScaleZ, z + 1, x));
									VertexAndNormal v3 = new VertexAndNormal(new Vector3(scaleX * (x + 1), heightMap[z + 1, x + 1], scaleZ * (z + 1)), CalculateNormal(heightMap, invScaleX, invScaleZ, z + 1, x + 1));
									ScissorTriangle<VertexAndNormal, ScissorVertexAndNormalFunc>(ref v0, ref v2, ref v3, ref scissoredTriangleCount);
									ScissorTriangle<VertexAndNormal, ScissorVertexAndNormalFunc>(ref v0, ref v3, ref v1, ref scissoredTriangleCount);
								}
							}
						}
					}
					CalculateScissoredVertexBounds<VertexAndNormal, ScissorVertexAndNormalFunc>(scissoredTriangleCount.m_nTriangleCount, 0, m_patchList.Count == 0);
				} else {
					InitScissorBuffer<Vector3, ScissorVertexFunc> (numTrianglesToScissor);
					for (int i = 0; i < m_patchListToScissor.Count; ++i) {
						Patch patch = m_patchListToScissor [i];
						for (int z = patch.m_posZ, zEnd = patch.m_posZ + patch.m_width; z < zEnd; ++z) {
							for (int x = patch.m_posX, xEnd = patch.m_posX + patch.m_width; x < xEnd; ++x) {
#if UNITY_2019_3_OR_NEWER
								if (holeMap[z, x])
#endif
								{
									Vector3 v0 = new Vector3(scaleX * x, heightMap[z, x], scaleZ * z);
									Vector3 v1 = new Vector3(scaleX * (x + 1), heightMap[z, x + 1], scaleZ * z);
									Vector3 v2 = new Vector3(scaleX * x, heightMap[z + 1, x], scaleZ * (z + 1));
									Vector3 v3 = new Vector3(scaleX * (x + 1), heightMap[z + 1, x + 1], scaleZ * (z + 1));
									ScissorTriangle<Vector3, ScissorVertexFunc>(ref v0, ref v2, ref v3, ref scissoredTriangleCount);
									ScissorTriangle<Vector3, ScissorVertexFunc>(ref v0, ref v3, ref v1, ref scissoredTriangleCount);
								}
							}
						}
					}
					CalculateScissoredVertexBounds<Vector3, ScissorVertexFunc> (scissoredTriangleCount.m_nTriangleCount, 0, m_patchList.Count == 0);
				}
			}
			// create result buffer
			int vertexCount = scissoredTriangleCount.m_nVertexCount + (m_maxX - m_minX + 1)*(m_maxZ - m_minZ + 1);
			int indexCount = scissoredTriangleCount.m_nIndexCount + m_indexCount;
			InitResultBuffer(vertexCount, indexCount, m_bOutputNormals, m_bOutputUVs, 0);
			if (vertexCount == 0)
			{
				return;
			}
			// fill result buffer
			SearchResult result = m_results[0];
			if (m_bOutputUVs) {
				float uvScale = 1.0f / heightmapWidth;
				if (m_bOutputNormals) {
					float invScaleX = 0.5f/scaleX;
					float invScaleZ = 0.5f/scaleZ;
					for (int z = m_minZ; z <= m_maxZ; ++z) {
						for (int x = m_minX; x <= m_maxX; ++x) {
							result.uvs.Add(new Vector2(uvScale * x, uvScale * z));
							result.normals.Add(CalculateNormal(heightMap, invScaleX, invScaleZ, z, x));
							result.vertices.Add(new Vector3(scaleX * x, heightMap[z, x], scaleZ * z));
						}
					}
				}
				else {
					for (int z = m_minZ; z <= m_maxZ; ++z) {
						for (int x = m_minX; x <= m_maxX; ++x) {
							result.uvs.Add(new Vector2(uvScale * x, uvScale * z));
							result.vertices.Add(new Vector3(scaleX * x, heightMap[z, x], scaleZ * z));
						}
					}
				}
			}
			else if (m_bOutputNormals) {
				float invScaleX = 0.5f/scaleX;
				float invScaleZ = 0.5f/scaleZ;
				for (int z = m_minZ; z <= m_maxZ; ++z) {
					for (int x = m_minX; x <= m_maxX; ++x) {
						result.normals.Add(CalculateNormal(heightMap, invScaleX, invScaleZ, z, x));
						result.vertices.Add(new Vector3(scaleX * x, heightMap[z, x], scaleZ * z));
					}
				}
			}
			else {
				for (int z = m_minZ; z <= m_maxZ; ++z) {
					for (int x = m_minX; x <= m_maxX; ++x) {
						result.vertices.Add(new Vector3(scaleX * x, heightMap[z, x], scaleZ * z));
					}
				}
			}
			int width = m_maxX - m_minX + 1;
			for (int i = 0; i < m_patchList.Count; ++i) {
				Patch patch = m_patchList[i];
				int offset = (patch.m_posZ - m_minZ) * width + (patch.m_posX - m_minX);
				for (int z = 0; z < patch.m_width; ++z) {
					int vtx = offset;
#if UNITY_2019_3_OR_NEWER
					int zz = z + patch.m_posZ;
#endif
					for (int x = 0; x < patch.m_width; ++x) {
#if UNITY_2019_3_OR_NEWER
						if (holeMap[zz, x + patch.m_posX])
#endif
						{
							result.indices.Add(vtx);
							result.indices.Add(vtx + width);
							result.indices.Add(vtx + width + 1);
							result.indices.Add(vtx);
							result.indices.Add(vtx + width + 1);
							result.indices.Add(vtx + 1);
						}
						++vtx;
					}
					offset += width;
				}
			}
			if (m_bOutputUVs) {
				if (m_bOutputNormals) {
					AddScissoredTrianglesToResults<VertexNormalAndUV, ScissorVertexNormalAndUVFunc>(scissoredTriangleCount.m_nTriangleCount, 0);
				}
				else {
					AddScissoredTrianglesToResults<VertexAndUV, ScissorVertexAndUVFunc>(scissoredTriangleCount.m_nTriangleCount, 0);
				}
			}
			else if (m_bOutputNormals) {
				AddScissoredTrianglesToResults<VertexAndNormal, ScissorVertexAndNormalFunc>(scissoredTriangleCount.m_nTriangleCount, 0);
			}
			else {
				AddScissoredTrianglesToResults<Vector3, ScissorVertexFunc>(scissoredTriangleCount.m_nTriangleCount, 0);
			}
		}
		private Vector3 CalculateNormal(float[,] heightMap, float invScaleX, float invScaleZ, int z, int x)
		{
			int x0 = Mathf.Max(0, x - 1);
			int x1 = Mathf.Min(heightMap.GetLength(1) - 1, x + 1);
			int z0 = Mathf.Max(0, z - 1);
			int z1 = Mathf.Min(heightMap.GetLength(0) - 1, z + 1);
			float dx = invScaleX*(heightMap[z, x1] - heightMap[z, x0])/(float)(x1 - x0);
			float dz = invScaleZ*(heightMap[z1, x] - heightMap[z0, x])/(float)(z1 - z0);
			return new Vector3(dx, 1.0f, dz).normalized;
		}
		internal uint IsInView(Vector3 center, Vector3 extents, out bool isPartial)
		{
			int numClipPlanes = m_clipPlanes.clipPlaneCount;
			uint flags = 0xf;
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
				uint clipFlags = m_clipFlags[i];
				if (maxDistance < 0 || clipDistance < minDistance) {
					return 0U;
				}
				if (minDistance < 0) {
					isPartial = true;
					float d = distance + yExtent;
					if (d < 0) {
						flags &= clipFlags;
						if (d + xExtent < 0) {
							flags &= (clipFlags >> 4);
						}
						if (d + zExtent < 0) {
							flags &= (clipFlags >> 8);
						}
					}
				}
				if (m_clipPlanes.twoSideClipping && clipDistance < maxDistance) {
					isPartial = true;
					float d = clipDistance - distance + yExtent;
					if (d < 0) {
						flags &= (clipFlags >> 16);
						if (d + xExtent < 0) {
							flags &= (clipFlags >> 20);
						}
						if (d + zExtent < 0) {
							flags &= (clipFlags >> 24);
						}
					}
				}
			}
			return flags;
		}
		private static uint UpdateClipFlags(uint flags, uint clipFlags, float distance, float xExtent, float yExtent, float zExtent)
		{
			float d1 = distance + yExtent;
			float d2 = distance - yExtent;
			if (d1 < 0) {
				flags &= clipFlags & 0xf;
				if (d1 + xExtent < 0) {
					flags &= (clipFlags >> 4);
				}
				if (d1 + zExtent < 0) {
					flags &= (clipFlags >> 8);
				}
			}
			else if (0 < d2) {
				uint f = clipFlags | 0xf;
				if (0 < d2 - xExtent) {
					f |= (clipFlags << 8);
				}
				if (0 < d2 - zExtent) {
					f |= (clipFlags << 4);
				}
				flags &= f;
			}
			else {
				flags &= 0xf;
			}
			return flags;
		}
		internal uint IsInView(Vector3 center, Vector3 extents)
		{
			int numClipPlanes = m_clipPlanes.clipPlaneCount;
			uint flags = 0xf00f; // clipFlags | (partialFlags << 12)
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
					flags = UpdateClipFlags(flags, m_clipFlags[i], distance, xExtent, yExtent, zExtent);
				}
				if (m_clipPlanes.twoSideClipping && clipDistance < maxDistance) {
					flags = UpdateClipFlags(flags, m_clipFlags[i] >> 16, clipDistance - distance, xExtent, yExtent, zExtent);
				}
			}
			return flags;
		}
		public void AddPatch(int posX, int posZ, int width, float minY, float maxY, bool isPartial)
		{
			if (isPartial && m_bScissor) {
				m_patchListToScissor.Add(new Patch(posX, posZ, width));
				return;
			}
			if (m_patchList.Count == 0) {
				m_minX = posX;
				m_results[0].minBounds.y = minY;
				m_minZ = posZ;
				m_maxX = posX + width;
				m_results[0].maxBounds.y = maxY;
				m_maxZ = posZ + width;
			}
			else {
				m_minX = Mathf.Min(m_minX, posX);
				m_minZ = Mathf.Min(m_minZ, posZ);
				m_maxX = Mathf.Max(m_maxX, posX + width);
				m_maxZ = Mathf.Max(m_maxZ, posZ + width);
				m_results[0].minBounds.y = Mathf.Min(m_results[0].minBounds.y, minY);
				m_results[0].maxBounds.y = Mathf.Max(m_results[0].maxBounds.y, maxY);
			}
			m_patchList.Add(new Patch(posX, posZ, width));
			m_indexCount += 6 * width * width;
		}
	}
}
