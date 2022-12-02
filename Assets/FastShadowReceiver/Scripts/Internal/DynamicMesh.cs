//
// DynamicMesh.cs
//
// Fast Shadow Receiver
//
// Copyright 2019 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FastShadowReceiver {
	[RequireComponent(typeof(MeshRenderer))]
	public class DynamicMesh : MonoBehaviour {
		const int BUFFER_COUNT = 2;
		private int m_nCurrentBuffer;
		private Mesh[] m_meshes;
		private MeshFilter m_meshFilter;

		public Mesh[] meshes
		{
			get { return m_meshes; }
		}

		public Mesh currentMesh
		{
			get { return m_meshes[m_nCurrentBuffer]; }
		}

		public void SwapMesh()
		{
			m_nCurrentBuffer = (m_nCurrentBuffer + 1) % BUFFER_COUNT;
			m_meshFilter.mesh = currentMesh;
		}

		public void Awake()
		{
			if (m_meshes == null)
			{
				m_meshes = new Mesh[BUFFER_COUNT];
				for (int i = 0; i < BUFFER_COUNT; ++i)
				{
					m_meshes[i] = new Mesh();
					m_meshes[i].hideFlags = HideFlags.HideAndDontSave;
				}
			}
			m_nCurrentBuffer = 0;
			m_meshFilter = GetComponent<MeshFilter>();
			if (m_meshFilter == null)
			{
				m_meshFilter = gameObject.AddComponent<MeshFilter>();
			}
			m_meshFilter.mesh = m_meshes[0];
		}

		void OnDestroy()
		{
			if (m_meshFilter != null)
			{
				m_meshFilter.mesh = null;
			}
			if (m_meshes != null)
			{
				for (int i = 0; i < m_meshes.Length; ++i)
				{
#if UNITY_EDITOR
					DestroyImmediate(m_meshes[i]);
#else
					Destroy(m_meshes[i]);
#endif
					m_meshes[i] = null;
				}
				m_meshes = null;
			}
		}
	}
}
