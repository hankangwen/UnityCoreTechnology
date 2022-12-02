using System.Collections.Generic;
using UnityEngine;

public class GhostShadow : MonoBehaviour
{
    public float interval = 0.1f;
    public float lifeCycle = 1.0f;

    private float _lastCombinedTime = 0.0f;
    private MeshFilter[] _meshFilters = null;
    private MeshRenderer[] _meshRenderers = null;
    private SkinnedMeshRenderer[] _skinnedMeshRenderers = null;
    private List<GameObject> _objects = new List<GameObject>();

    void Start()
    {
        _meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
        _skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    void Update()
    {
        if (Time.time - _lastCombinedTime > interval)
        {
            _lastCombinedTime = Time.time;

            SkinnedMeshRenderer tempSkin;
            Transform tempTran;
            for (int i = 0; _skinnedMeshRenderers != null && i < _skinnedMeshRenderers.Length; i++)
            {
                tempSkin = _skinnedMeshRenderers[i];
                tempTran = tempSkin.transform;
                
                Mesh mesh = new Mesh();
                tempSkin.BakeMesh(mesh);
                
                GameObject go = new GameObject();
                go.hideFlags = HideFlags.HideAndDontSave;
                MeshFilter meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.mesh = mesh;
                MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
                meshRenderer.material = tempSkin.material;
                InitFadeInObj(go, tempTran.position, tempTran.rotation, lifeCycle);
            }

            MeshFilter tempMeshFilter;
            for (int i = 0; _meshFilters != null && i < _meshFilters.Length; i++)
            {
                tempMeshFilter = _meshFilters[i];
                tempTran = tempMeshFilter.transform;
                GameObject go = Instantiate(tempMeshFilter.gameObject);
                InitFadeInObj(go, tempTran.position, tempTran.rotation, lifeCycle);
            }
        }
    }

    void OnDisable()
    {
        foreach (var go in _objects)
        {
            DestroyImmediate(go);
        }
        _objects.Clear();
        _objects = null;
    }

    private void InitFadeInObj(GameObject go, Vector3 position, Quaternion rotation, float duration)
    {
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.position = position;
        go.transform.rotation = rotation;

        FadInOut fi = go.AddComponent<FadInOut>();
        fi.lifeCycle = duration;
        _objects.Add(go);
    }
}
