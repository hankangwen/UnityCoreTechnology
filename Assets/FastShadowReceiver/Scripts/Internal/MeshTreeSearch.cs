//
// MeshTreeSearch.cs
//
// Fast Shadow Receiver
//
// Copyright 2014 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FastShadowReceiver {
	public class MeshTreeSearch
	{
		public ClipPlanes m_clipPlanes;
		public float m_scissorMargin = 0.0f; // applicable for BinaryMeshTreeSearch
		public bool m_bScissor;
		public bool m_bOutputNormals = false;
		public bool m_bOutputUVs = false;
		public bool m_bBackfaceCulling = false;

		protected class SearchResult
		{
			public List<Vector3> vertices;
			public List<Vector3> normals;
			public List<Vector2> uvs;
			public List<int> indices;
			public Vector3 minBounds;
			public Vector3 maxBounds;
		}
		Dictionary<int, List<SearchResult>> m_resultBuffer;
		protected Vector3[] m_clipMetric;
		protected SearchResult[] m_results;

		protected struct VertexAndNormal
		{
			public VertexAndNormal(Vector3 v0, Vector3 n0)
			{
				v = v0; n = n0;
			}
			public Vector3 v;
			public Vector3 n;
		}
		protected struct VertexAndNormalArray
		{
			public Vector3[] v;
			public Vector3[] n;
		}
		protected struct VertexAndUV
		{
			public VertexAndUV(Vector3 v0, Vector2 uv0)
			{
				v = v0; uv = uv0;
			}
			public Vector3 v;
			public Vector2 uv;
		}
		protected struct VertexAndUVArray
		{
			public Vector3[] v;
			public Vector2[] uv;
		}
		protected struct VertexNormalAndUV
		{
			public VertexNormalAndUV(Vector3 v0, Vector3 n0, Vector2 uv0)
			{
				v = v0; n = n0; uv = uv0;
			}
			public Vector3 v;
			public Vector3 n;
			public Vector2 uv;
		}
		protected struct VertexNormalAndUVArray
		{
			public Vector3[] v;
			public Vector3[] n;
			public Vector2[] uv;
		}
		[StructLayout(LayoutKind.Explicit)]
		protected struct ScissoredTriangles
		{
			[FieldOffset(0)] public Vector3[] scissoredVertices;
			[FieldOffset(0)] public VertexAndNormal[] scissoredVerticesAndNormals;
			[FieldOffset(0)] public VertexAndUV[] scissoredVerticesAndUVs;
			[FieldOffset(0)] public VertexNormalAndUV[] scissoredVerticesNormalsAndUVs;
		}
		protected interface IVertexArrayFunction<T, TArray>
		{
			T GetVertexFromArray(ref TArray array, int index);
			void AddVertex(SearchResult result, ref TArray array, int srcIndex);
			Vector3 GetVertexPosition(ref TArray array, int index);
		}
		protected interface IScissoredVertexFunctions<T>
		{
			void Initialize(ref ScissoredTriangles vertices, int size);
			T Lerp(ref T a, ref T b, float t);
			T[] GetVertexArray(ref ScissoredTriangles vertices);
			void SetVertexArray(ref ScissoredTriangles vertice, T[] vertexArray);
			int GetSize(ref ScissoredTriangles vertices);
			Vector3 GetVertexPosition(ref T vertex);
			void AddVertex(SearchResult result, T vertex);
		}
		protected struct ScissorVertexFunc : IScissoredVertexFunctions<Vector3>
		{
			public void Initialize(ref ScissoredTriangles vertices, int size)
			{
				vertices.scissoredVertices = new Vector3[size];
			}
			public Vector3 Lerp(ref Vector3 a, ref Vector3 b, float t)
			{
				return Vector3.Lerp(a, b, t);
			}
			public Vector3[] GetVertexArray(ref ScissoredTriangles vertices)
			{
				return vertices.scissoredVertices;
			}
			public void SetVertexArray(ref ScissoredTriangles vertices, Vector3[] vertexArray)
			{
				vertices.scissoredVertices = vertexArray;
			}
			public int GetSize(ref ScissoredTriangles vertices)
			{
				return vertices.scissoredVertices != null ? vertices.scissoredVertices.Length : 0;
			}
			public Vector3 GetVertexPosition(ref Vector3 vertex)
			{
				return vertex;
			}
			public void AddVertex(SearchResult result, Vector3 vertex)
			{
				result.vertices.Add(vertex);
			}
		}
		protected struct VertexArrayFunc : IVertexArrayFunction<Vector3, Vector3[]>
		{
			public Vector3 GetVertexFromArray(ref Vector3[] array, int index)
			{
				return array[index];
			}
			public void AddVertex(SearchResult result, ref Vector3[] array, int srcIndex)
			{
				result.vertices.Add(array[srcIndex]);
			}
			public Vector3 GetVertexPosition(ref Vector3[] array, int index)
			{
				return array[index];
			}
		}
		protected struct ScissorVertexAndNormalFunc : IScissoredVertexFunctions<VertexAndNormal>
		{
			public void Initialize(ref ScissoredTriangles vertices, int size)
			{
				vertices.scissoredVerticesAndNormals = new VertexAndNormal[size];
			}
			public VertexAndNormal Lerp(ref VertexAndNormal a, ref VertexAndNormal b, float t)
			{
				return new VertexAndNormal(Vector3.Lerp(a.v, b.v, t), Vector3.Lerp(a.n, b.n, t));
			}
			public VertexAndNormal[] GetVertexArray(ref ScissoredTriangles vertices)
			{
				return vertices.scissoredVerticesAndNormals;
			}
			public void SetVertexArray(ref ScissoredTriangles vertices, VertexAndNormal[] vertexArray)
			{
				vertices.scissoredVerticesAndNormals = vertexArray;
			}
			public int GetSize(ref ScissoredTriangles vertices)
			{
				return vertices.scissoredVerticesAndNormals != null ? vertices.scissoredVerticesAndNormals.Length : 0;
			}
			public Vector3 GetVertexPosition(ref VertexAndNormal vertex)
			{
				return vertex.v;
			}
			public void AddVertex(SearchResult result, VertexAndNormal vertex)
			{
				result.vertices.Add(vertex.v);
				result.normals.Add(vertex.n);
			}
		}
		protected struct VertexAndNormalArrayFunc : IVertexArrayFunction<VertexAndNormal, VertexAndNormalArray>
		{
			public VertexAndNormal GetVertexFromArray(ref VertexAndNormalArray array, int index)
			{
				return new VertexAndNormal(array.v[index], array.n[index]);
			}
			public void AddVertex(SearchResult result, ref VertexAndNormalArray array, int srcIndex)
			{
				result.vertices.Add(array.v[srcIndex]);
				result.normals.Add(array.n[srcIndex]);
			}
			public Vector3 GetVertexPosition(ref VertexAndNormalArray array, int index)
			{
				return array.v[index];
			}
		}
		protected struct ScissorVertexAndUVFunc : IScissoredVertexFunctions<VertexAndUV>
		{
			public void Initialize(ref ScissoredTriangles vertices, int size)
			{
				vertices.scissoredVerticesAndUVs = new VertexAndUV[size];
			}
			public VertexAndUV Lerp(ref VertexAndUV a, ref VertexAndUV b, float t)
			{
				return new VertexAndUV(Vector3.Lerp(a.v, b.v, t), Vector2.Lerp(a.uv, b.uv, t));
			}
			public VertexAndUV[] GetVertexArray(ref ScissoredTriangles vertices)
			{
				return vertices.scissoredVerticesAndUVs;
			}
			public void SetVertexArray(ref ScissoredTriangles vertices, VertexAndUV[] vertexArray)
			{
				vertices.scissoredVerticesAndUVs = vertexArray;
			}
			public int GetSize(ref ScissoredTriangles vertices)
			{
				return vertices.scissoredVerticesAndUVs != null ? vertices.scissoredVerticesAndUVs.Length : 0;
			}
			public Vector3 GetVertexPosition(ref VertexAndUV vertex)
			{
				return vertex.v;
			}
			public void AddVertex(SearchResult result, VertexAndUV vertex)
			{
				result.vertices.Add(vertex.v);
				result.uvs.Add(vertex.uv);
			}
		}
		protected struct VertexAndUVArrayFunc : IVertexArrayFunction<VertexAndUV, VertexAndUVArray>
		{
			public VertexAndUV GetVertexFromArray(ref VertexAndUVArray array, int index)
			{
				return new VertexAndUV(array.v[index], array.uv[index]);
			}
			public void AddVertex(SearchResult result, ref VertexAndUVArray array, int srcIndex)
			{
				Debug.Assert(srcIndex < array.uv.Length);
				result.vertices.Add(array.v[srcIndex]);
				result.uvs.Add(array.uv[srcIndex]);
			}
			public Vector3 GetVertexPosition(ref VertexAndUVArray array, int index)
			{
				return array.v[index];
			}
		}
		protected struct ScissorVertexNormalAndUVFunc : IScissoredVertexFunctions<VertexNormalAndUV>
		{
			public void Initialize(ref ScissoredTriangles vertices, int size)
			{
				vertices.scissoredVerticesNormalsAndUVs = new VertexNormalAndUV[size];
			}
			public VertexNormalAndUV Lerp(ref VertexNormalAndUV a, ref VertexNormalAndUV b, float t)
			{
				return new VertexNormalAndUV(Vector3.Lerp(a.v, b.v, t), Vector3.Lerp(a.n, b.n, t), Vector2.Lerp(a.uv, b.uv, t));
			}
			public VertexNormalAndUV[] GetVertexArray(ref ScissoredTriangles vertices)
			{
				return vertices.scissoredVerticesNormalsAndUVs;
			}
			public void SetVertexArray(ref ScissoredTriangles vertices, VertexNormalAndUV[] vertexArray)
			{
				vertices.scissoredVerticesNormalsAndUVs = vertexArray;
			}
			public int GetSize(ref ScissoredTriangles vertices)
			{
				return vertices.scissoredVerticesNormalsAndUVs != null ? vertices.scissoredVerticesNormalsAndUVs.Length : 0;
			}
			public Vector3 GetVertexPosition(ref VertexNormalAndUV vertex)
			{
				return vertex.v;
			}
			public void AddVertex(SearchResult result, VertexNormalAndUV vertex)
			{
				result.vertices.Add(vertex.v);
				result.normals.Add(vertex.n);
				result.uvs.Add(vertex.uv);
			}
		}
		protected struct VertexNormalAndUVArrayFunc : IVertexArrayFunction<VertexNormalAndUV, VertexNormalAndUVArray>
		{
			public VertexNormalAndUV GetVertexFromArray(ref VertexNormalAndUVArray array, int index)
			{
				return new VertexNormalAndUV(array.v[index], array.n[index], array.uv[index]);
			}
			public void AddVertex(SearchResult result, ref VertexNormalAndUVArray array, int srcIndex)
			{
				result.vertices.Add(array.v[srcIndex]);
				result.normals.Add(array.n[srcIndex]);
				result.uvs.Add(array.uv[srcIndex]);
			}
			public Vector3 GetVertexPosition(ref VertexNormalAndUVArray array, int index)
			{
				return array.v[index];
			}
		}

		private ManualResetEvent m_event;
		private MeshTreeBase m_tree;

		protected System.Type m_scissoredVertexType;
		protected ScissoredTriangles[] m_scissoredTriangles;
		protected int[] m_scissoredTriangleVertexCount;
		protected ScissoredTriangles m_tempScissorBuffer;

		private Vector3 m_projectionDir;
		private Vector3 m_projectionPos;
		private bool m_isOrthographic;
		public void SetProjectionDir(bool isOrtho, Vector3 direction, Vector3 position)
		{
			m_projectionDir = direction;
			m_projectionPos = position;
			m_isOrthographic = isOrtho;
		}
		public bool isFrontFaceTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
		{
			Vector3 triNormal = Vector3.Cross(v1 - v0, v2 - v0);
			if (Vector3.Dot(triNormal, m_projectionDir) < 0)
			{
				return true;
			}
			return m_isOrthographic && Vector3.Dot(triNormal, v0 - m_projectionPos) < 0;
		}
		public List<Vector3> vertices
		{
			get { return m_results[0].vertices; }
		}
		public List<Vector3> normals
		{
			get { return m_results[0].normals; }
		}
		public List<int> triangles
		{
			get { return m_results[0].indices; }
		}

		public Bounds bounds
		{
			get
			{
				Bounds b = new Bounds();
				b.SetMinMax(m_results[0].minBounds, m_results[0].maxBounds);
				return b;
			}
		}

		public int lightmapCount
		{
			get { return m_results.Length - 1; }
		}
		public bool HasResult(int lightmapIndex)
		{
			++lightmapIndex;
			return m_results[lightmapIndex] != null && m_results[lightmapIndex].vertices != null && m_results[lightmapIndex].vertices.Count != 0;
		}
		public List<Vector3> GetVertices(int lightmapIndex)
		{
			return m_results[lightmapIndex+1].vertices;
		}
		public List<Vector3> GetNormals(int lightmapIndex)
		{
			return m_results[lightmapIndex+1].normals;
		}
		public List<Vector2> GetLightmapUVs(int lightmapIndex)
		{
			return m_results[lightmapIndex+1].uvs;
		}
		public List<int> GetTriangles(int lightmapIndex)
		{
			return m_results[lightmapIndex+1].indices;
		}
		public Bounds GetBounds(int lightmapIndex)
		{
			++lightmapIndex;
			Bounds b = new Bounds();
			b.SetMinMax(m_results[lightmapIndex].minBounds, m_results[lightmapIndex].maxBounds);
			return b;
		}
		public void FillMesh(Mesh mesh, int lightmapIndex)
		{
			SearchResult result = m_results[lightmapIndex+1];
			mesh.SetVertices(result.vertices);
			if (m_bOutputNormals)
			{
				mesh.SetNormals(result.normals);
			}
			if (m_bOutputUVs)
			{
				mesh.SetUVs(1, result.uvs);
			}
			Bounds b = new Bounds();
			b.SetMinMax(result.minBounds, result.maxBounds);
			mesh.bounds = b;
			mesh.SetTriangles(result.indices, 0);
		}
#if NETFX_CORE && !UNITY_2019_1_OR_NEWER
		private Windows.System.Threading.WorkItemHandler m_workItemHandler;
		public MeshTreeSearch()
		{
			m_workItemHandler = (source) => this.Search();
		}
		public void AsyncStart(MeshTreeBase tree)
		{
			if (m_event == null) {
				m_event = new ManualResetEvent(false);
			}
			m_event.Reset();
			m_tree = tree;
			// we don't need to wait the following async function, because we are using m_event to wait for completion.
			// of course, we can use the return value of the following function call to wait, but we don't want to change code for Windows Store App.
			var suppressWarning = Windows.System.Threading.ThreadPool.RunAsync(m_workItemHandler);
		}
#else
		private static WaitCallback s_searchCallback = (arg => ((MeshTreeSearch)arg).Search());
		public void AsyncStart(MeshTreeBase tree)
		{
			if (m_event == null)
			{
				m_event = new ManualResetEvent(false);
			}
			m_event.Reset();
			m_tree = tree;
			Nyahoon.ThreadPool.QueueUserWorkItem(s_searchCallback, this);
		}
#endif

		private void Search()
		{
			try
			{
				m_tree.Search(this);
				m_tree = null;
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
				m_event.Set();
				throw;
			}
			finally
			{
				m_event.Set();
			}
		}

		public void Wait()
		{
			if (m_event != null)
			{
				m_event.WaitOne();
			}
		}

		public bool IsDone()
		{
			if (m_event != null)
			{
				return m_event.WaitOne(0);
			}
			return false;
		}

		void PushResultBuffer(SearchResult result)
		{
			int n = 0;
			if (result.vertices != null)
			{
				n = GetResultBufferIndex(result.vertices.Capacity);
				result.vertices.Clear();
				if (result.normals != null)
				{
					result.normals.Clear();
				}
				if (result.uvs != null)
				{
					result.uvs.Clear();
				}
				if (result.indices != null)
				{
					result.indices.Clear();
				}
			}
			List<SearchResult> buffer;
			if (!m_resultBuffer.TryGetValue(n, out buffer))
			{
				buffer = new List<SearchResult>();
				m_resultBuffer.Add(n, buffer);
			}
			buffer.Add(result);
		}
		SearchResult PopResultBuffer(int vertexCount)
		{
			int n = GetResultBufferIndex(vertexCount);
			List<SearchResult> buffer;
			if (!m_resultBuffer.TryGetValue(n, out buffer))
			{
				return null;
			}
			if (buffer.Count == 0)
			{
				return null;
			}
			SearchResult result = buffer[buffer.Count - 1];
			buffer.RemoveAt(buffer.Count - 1);
			return result;
		}
		protected void Initialize(int lightmapCountPlusOne)
		{
			InitClipMetrics();
			if (m_results == null || m_results.Length < lightmapCountPlusOne)
			{
				if (m_results != null && m_resultBuffer != null)
				{
					for (int i = 0; i < m_results.Length; ++i)
					{
						PushResultBuffer(m_results[i]);
					}
				}
				m_results = new SearchResult[lightmapCountPlusOne];
				for (int i = 0; i < lightmapCountPlusOne; ++i)
				{
					m_results[i] = new SearchResult();
				}
			}
		}

		protected void InitClipMetrics()
		{
			int numClipPlanes = m_clipPlanes.clipPlaneCount;
			if (m_clipMetric == null || m_clipMetric.Length < numClipPlanes)
			{
				m_clipMetric = new Vector3[numClipPlanes];
			}
			for (int i = 0; i < numClipPlanes; ++i)
			{
				m_clipMetric[i].x = Mathf.Abs(m_clipPlanes.clipPlanes[i].normal.x);
				m_clipMetric[i].y = Mathf.Abs(m_clipPlanes.clipPlanes[i].normal.y);
				m_clipMetric[i].z = Mathf.Abs(m_clipPlanes.clipPlanes[i].normal.z);
			}
		}

		protected void InitScissorBuffer<Vertex, VertexFunc>(int maxTriangleNum) where VertexFunc : IScissoredVertexFunctions<Vertex>
		{
			if (m_bScissor && 0 < maxTriangleNum)
			{
				int scissorePlaneNum = m_clipPlanes.twoSideClipping ? 2 * m_clipPlanes.scissorPlaneCount : m_clipPlanes.scissorPlaneCount;
				int maxVertexNum = scissorePlaneNum + 3;
				bool needInitialize = m_scissoredVertexType != typeof(Vertex) || default(VertexFunc).GetSize(ref m_tempScissorBuffer) < maxVertexNum;
				m_scissoredVertexType = typeof(Vertex);
				if (m_scissoredTriangles == null || m_scissoredTriangles.Length < maxTriangleNum)
				{
					ScissoredTriangles[] scissoredTriangles = new ScissoredTriangles[maxTriangleNum];
					if (!needInitialize)
					{
						int i = 0;
						maxVertexNum = default(VertexFunc).GetSize(ref m_tempScissorBuffer);
						if (m_scissoredTriangles != null)
						{
							for (; i < m_scissoredTriangles.Length; ++i)
							{
								scissoredTriangles[i] = m_scissoredTriangles[i];
							}
						}
						for (; i < scissoredTriangles.Length; ++i)
						{
							default(VertexFunc).Initialize(ref scissoredTriangles[i], maxVertexNum);
						}
					}
					m_scissoredTriangles = scissoredTriangles;
					m_scissoredTriangleVertexCount = new int[maxTriangleNum];
				}
				if (needInitialize)
				{
					default(VertexFunc).Initialize(ref m_tempScissorBuffer, maxVertexNum);
					for (int i = 0; i < m_scissoredTriangles.Length; ++i)
					{
						default(VertexFunc).Initialize(ref m_scissoredTriangles[i], maxVertexNum);
					}
				}
			}
		}

		private int GetResultBufferIndex(int count)
		{
			if (count == 0) return 0;
			int n = (count - 1) >> 2;
			int i = 1;
			while (0 < n && i < 8)
			{
				++i;
				n >>= 1;
			}
			return i + n;
		}
		private int GetResultBufferCapacity(int count)
		{
			if (count == 0) return 0;
			int n = (count - 1) >> 2;
			int i = 1;
			while (0 < n && i < 8)
			{
				++i;
				n >>= 1;
			}
			int capacity = (1 << (i + 1)) * (n + 1);
			Debug.Assert(count <= capacity);
			Debug.Assert(GetResultBufferIndex(capacity) == GetResultBufferIndex(count));
			return capacity;
		}
		protected void InitResultBuffer(int vertexCount, int indexCount, bool bNormal, bool bUV, int lightmapIndexPlusOne)
		{
			if (m_resultBuffer == null)
			{
				m_resultBuffer = new Dictionary<int, List<SearchResult>>();
			}
			SearchResult result = m_results[lightmapIndexPlusOne];
			if (result != null || (result.vertices == null && 0 < vertexCount))
			{
				Vector3 minBounds = result.minBounds;
				Vector3 maxBounds = result.maxBounds;
				PushResultBuffer(result);
				result = PopResultBuffer(vertexCount);
				if (result == null)
				{
					result = new SearchResult();
				}
				result.minBounds = minBounds;
				result.maxBounds = maxBounds;
				m_results[lightmapIndexPlusOne] = result;
			}
			if (vertexCount == 0)
			{
				return;
			}
			if (result.vertices == null)
			{
				int capacity = GetResultBufferCapacity(vertexCount);
				result.vertices = new List<Vector3>(capacity);
				if (bNormal)
				{
					result.normals = new List<Vector3>(capacity);
				}
				if (bUV)
				{
					result.uvs = new List<Vector2>(capacity);
				}
			}
			else
			{
				int capacity = GetResultBufferCapacity(vertexCount);
				if (bNormal && result.normals == null)
				{
					result.normals = new List<Vector3>(capacity);
				}
				if (bUV && result.uvs == null)
				{
					result.uvs = new List<Vector2>(capacity);
				}
			}
			if (result.indices == null)
			{
				int capacity = GetResultBufferCapacity(indexCount);
				result.indices = new List<int>(capacity);
			}
		}

		protected struct ScissoredTriangleCount
		{
			public int m_nTriangleCount;
			public int m_nVertexCount;
			public int m_nIndexCount;
		}
		protected int ScissorTriangle<Vertex, VertexFunc>(ref Vertex v0, ref Vertex v1, ref Vertex v2, ref ScissoredTriangleCount triCount) where VertexFunc : IScissoredVertexFunctions<Vertex>
		{
			int numVertices = 3;
			Vertex[] vertices = default(VertexFunc).GetVertexArray(ref m_scissoredTriangles[triCount.m_nTriangleCount]);
			vertices[0] = v0;
			vertices[1] = v1;
			vertices[2] = v2;
			Vertex[] tempScissorBuffer = default(VertexFunc).GetVertexArray(ref m_tempScissorBuffer);
			if (m_clipPlanes.twoSideClipping)
			{
				for (int j = 0; j < m_clipPlanes.scissorPlaneCount; ++j)
				{
					Plane clipPlane = m_clipPlanes.clipPlanes[j];
					float maxDistance = m_clipPlanes.maxDistance[j];
					float firstDistance = clipPlane.GetDistanceToPoint(default(VertexFunc).GetVertexPosition(ref vertices[0]));
					float firstDistance2 = maxDistance - firstDistance;
					float lastDistance = firstDistance;
					float lastDistance2 = firstDistance2;
					int newVertexCount = 0;
					for (int k = 1; k < numVertices; ++k)
					{
						float distance = clipPlane.GetDistanceToPoint(default(VertexFunc).GetVertexPosition(ref vertices[k]));
						float distance2 = maxDistance - distance;
						if (distance <= 0)
						{
							if (lastDistance2 <= 0)
							{
								tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[k], ref vertices[k - 1], distance2 / (distance2 - lastDistance2));
							}
							if (0 < lastDistance)
							{
								tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[k - 1], ref vertices[k], lastDistance / (lastDistance - distance));
							}
						}
						else
						{
							if (lastDistance <= 0)
							{
								tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[k], ref vertices[k - 1], distance / (distance - lastDistance));
								if (distance2 <= 0)
								{
									tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[k - 1], ref vertices[k], lastDistance2 / (lastDistance2 - distance2));
								}
							}
							else if (distance2 <= 0)
							{
								if (0 < lastDistance2)
								{
									tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[k - 1], ref vertices[k], lastDistance2 / (lastDistance2 - distance2));
								}
							}
							else
							{
								if (lastDistance2 <= 0)
								{
									tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[k], ref vertices[k - 1], distance2 / (distance2 - lastDistance2));
								}
							}
						}
						if (0 < distance && 0 < distance2)
						{
							tempScissorBuffer[newVertexCount++] = vertices[k];
						}
						lastDistance = distance;
						lastDistance2 = distance2;
					}
					if (firstDistance <= 0)
					{
						if (lastDistance2 <= 0)
						{
							tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[0], ref vertices[numVertices - 1], firstDistance2 / (firstDistance2 - lastDistance2));
						}
						if (0 < lastDistance)
						{
							tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[numVertices - 1], ref vertices[0], lastDistance / (lastDistance - firstDistance));
						}
					}
					else
					{
						if (lastDistance <= 0)
						{
							tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[0], ref vertices[numVertices - 1], firstDistance / (firstDistance - lastDistance));
							if (firstDistance2 <= 0)
							{
								tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[numVertices - 1], ref vertices[0], lastDistance2 / (lastDistance2 - firstDistance2));
							}
						}
						else if (firstDistance2 <= 0)
						{
							if (0 < lastDistance2)
							{
								tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[numVertices - 1], ref vertices[0], lastDistance2 / (lastDistance2 - firstDistance2));
							}
						}
						else
						{
							if (lastDistance2 <= 0)
							{
								tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[0], ref vertices[numVertices - 1], firstDistance2 / (firstDistance2 - lastDistance2));
							}
						}
					}
					if (0 < firstDistance && 0 < firstDistance2)
					{
						tempScissorBuffer[newVertexCount++] = vertices[0];
					}
					numVertices = newVertexCount;
					default(VertexFunc).SetVertexArray(ref m_tempScissorBuffer, vertices);
					default(VertexFunc).SetVertexArray(ref m_scissoredTriangles[triCount.m_nTriangleCount], tempScissorBuffer);
					vertices = tempScissorBuffer;
					tempScissorBuffer = default(VertexFunc).GetVertexArray(ref m_tempScissorBuffer);
					if (numVertices < 3)
					{
						return 0;
					}
				}
			}
			else
			{
				for (int j = 0; j < m_clipPlanes.scissorPlaneCount; ++j)
				{
					Plane clipPlane = m_clipPlanes.clipPlanes[j];
					float firstDistance = clipPlane.GetDistanceToPoint(default(VertexFunc).GetVertexPosition(ref vertices[0]));
					float lastDistance = firstDistance;
					int newVertexCount = 0;
					for (int k = 1; k < numVertices; ++k)
					{
						float distance = clipPlane.GetDistanceToPoint(default(VertexFunc).GetVertexPosition(ref vertices[k]));
						if (distance <= 0)
						{
							if (0 < lastDistance)
							{
								tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[k - 1], ref vertices[k], lastDistance / (lastDistance - distance));
							}
						}
						else
						{
							if (lastDistance <= 0)
							{
								tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[k], ref vertices[k - 1], distance / (distance - lastDistance));
							}
							tempScissorBuffer[newVertexCount++] = vertices[k];
						}
						lastDistance = distance;
					}
					if (firstDistance <= 0)
					{
						if (0 < lastDistance)
						{
							tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[numVertices - 1], ref vertices[0], lastDistance / (lastDistance - firstDistance));
						}
					}
					else
					{
						if (lastDistance <= 0)
						{
							tempScissorBuffer[newVertexCount++] = default(VertexFunc).Lerp(ref vertices[0], ref vertices[numVertices - 1], firstDistance / (firstDistance - lastDistance));
						}
						tempScissorBuffer[newVertexCount++] = vertices[0];
					}
					numVertices = newVertexCount;
					default(VertexFunc).SetVertexArray(ref m_tempScissorBuffer, vertices);
					default(VertexFunc).SetVertexArray(ref m_scissoredTriangles[triCount.m_nTriangleCount], tempScissorBuffer);
					vertices = tempScissorBuffer;
					tempScissorBuffer = default(VertexFunc).GetVertexArray(ref m_tempScissorBuffer);
					if (numVertices < 3)
					{
						return 0;
					}
				}
			}
			m_scissoredTriangleVertexCount[triCount.m_nTriangleCount++] = numVertices;
			triCount.m_nVertexCount += numVertices;
			triCount.m_nIndexCount += (numVertices - 2) * 3;
			return numVertices;
		}
		protected void AddScissoredTrianglesToResults<Vertex, VertexFunc>(int scissoredTriangleCount, int lightmapIndexPlusOne) where VertexFunc : IScissoredVertexFunctions<Vertex>
		{
			SearchResult result = m_results[lightmapIndexPlusOne];
			int nVertex = result.vertices.Count;
			for (int i = 0; i < scissoredTriangleCount; ++i)
			{
				Vertex[] vertexArray = default(VertexFunc).GetVertexArray(ref m_scissoredTriangles[i]);
				int firstIndex = nVertex;
				result.indices.Add(nVertex++);
				default(VertexFunc).AddVertex(result, vertexArray[0]);
				result.indices.Add(nVertex++);
				default(VertexFunc).AddVertex(result, vertexArray[1]);
				result.indices.Add(nVertex++);
				default(VertexFunc).AddVertex(result, vertexArray[2]);
				for (int j = 3; j < m_scissoredTriangleVertexCount[i]; ++j)
				{
					result.indices.Add(firstIndex);
					result.indices.Add(nVertex - 1);
					result.indices.Add(nVertex++);
					default(VertexFunc).AddVertex(result, vertexArray[j]);
				}
			}
		}
		protected void CalculateScissoredVertexBounds<Vertex, VertexFunc>(int scissoredTriangleCount, int lightmapIndexPlusOne, bool resetBounds) where VertexFunc : IScissoredVertexFunctions<Vertex>
		{
			SearchResult result = m_results[lightmapIndexPlusOne];
			int i = 0;
			if (resetBounds)
			{
				result.minBounds = result.maxBounds = Vector3.zero;
				for (; i < scissoredTriangleCount; ++i)
				{
					if (0 < m_scissoredTriangleVertexCount[i])
					{
						Vertex[] vertexArray = default(VertexFunc).GetVertexArray(ref m_scissoredTriangles[i]);
						result.minBounds = result.maxBounds = default(VertexFunc).GetVertexPosition(ref vertexArray[0]);
						break;
					}
				}
			}
			for (; i < scissoredTriangleCount; ++i)
			{
				Vertex[] vertexArray = default(VertexFunc).GetVertexArray(ref m_scissoredTriangles[i]);
				for (int j = 0, end = m_scissoredTriangleVertexCount[i]; j < end; ++j)
				{
					Vector3 v = default(VertexFunc).GetVertexPosition(ref vertexArray[j]);
					result.minBounds.x = Mathf.Min(result.minBounds.x, v.x);
					result.minBounds.y = Mathf.Min(result.minBounds.y, v.y);
					result.minBounds.z = Mathf.Min(result.minBounds.z, v.z);
					result.maxBounds.x = Mathf.Max(result.maxBounds.x, v.x);
					result.maxBounds.y = Mathf.Max(result.maxBounds.y, v.y);
					result.maxBounds.z = Mathf.Max(result.maxBounds.z, v.z);
				}
			}
		}
	}
}
