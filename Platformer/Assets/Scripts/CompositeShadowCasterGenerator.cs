using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

[ExecuteInEditMode]
public class CompositeShadowCasterGenerator : MonoBehaviour
{
    [SerializeField] CompositeCollider2D _compositeCollider;

    [SerializeField] Vector2 _shadowOffset = Vector2.zero;

    [Space]
    [SerializeField] bool _getColliderShapes;
    [SerializeField] bool _visualizeColliderShapes;
    [SerializeField] bool _createShadowCasters;
    [SerializeField] bool _destroyChildShadowCasters;

    List<List<Vector2>> _colliderPaths;

    Converter<Vector2, Vector3> Vec2ToVec3 = vectorToConvert => { return new Vector3(vectorToConvert.x, vectorToConvert.y, 0.0f); };

    // Crutches to access shadowcaster fields
    static BindingFlags _flagsToAccessPrivate = BindingFlags.NonPublic | BindingFlags.Instance;
    static FieldInfo meshField = typeof(ShadowCaster2D).GetField("m_Mesh", _flagsToAccessPrivate);
    static FieldInfo shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", _flagsToAccessPrivate);
    static MethodInfo OnEnableMethod = typeof(ShadowCaster2D).GetMethod("OnEnable", _flagsToAccessPrivate);

    private void Start()
    {
        _getColliderShapes = false;
        _visualizeColliderShapes = false;
        _createShadowCasters = false;
        _destroyChildShadowCasters = false;
    }

    void Update()
    {
        if (_getColliderShapes)
        {
            SaveColliderShapes();
            _getColliderShapes = false;
        }

        if (_visualizeColliderShapes)
        {
            VisualizeColliderShapes();
            _visualizeColliderShapes = false;
        }

        if (_createShadowCasters)
        {
            CreateShadowCasters();
            _createShadowCasters = false;
        }

        if (_destroyChildShadowCasters)
        {
            DestroyChildShadowCasters();
            _destroyChildShadowCasters = false;
        }
    }

    void SaveColliderShapes()
    {
        _colliderPaths = new List<List<Vector2>>();

        for (int i = 0; i < _compositeCollider.pathCount; i++)
        {
            Vector2[] pathVerticesArray = new Vector2[_compositeCollider.GetPathPointCount(i)];
            _compositeCollider.GetPath(i, pathVerticesArray);

            List<Vector2> pathVerticesList = pathVerticesArray.ToList();

            if (_shadowOffset != Vector2.zero)
            {
                for (int j = 0; j < pathVerticesList.Count; j++)
                {
                    pathVerticesList[j] += _shadowOffset;
                }
            }

            _colliderPaths.Add(pathVerticesList);
        }

        Debug.Log("Composite collider vertices have been saved");
    }

    void VisualizeColliderShapes()
    {
        foreach (List<Vector2> path in _colliderPaths)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i], path[i + 1], Color.red, 5f);
            }

            Debug.DrawLine(path.Last(), path.First(), Color.red, 5f);
        }

        Debug.Log("Collider vertices have been visualized");
    }

    void CreateShadowCasters()
    {
        foreach (List<Vector2> colliderVertices in _colliderPaths)
        {
            GameObject shadowCasterObject = new GameObject("Shadow Caster Object");
            shadowCasterObject.transform.SetParent(gameObject.transform);

            ShadowCaster2D shadowCaster = shadowCasterObject.AddComponent<ShadowCaster2D>();

            Vector3[] shadowVertices = Array.ConvertAll<Vector2, Vector3>(colliderVertices.ToArray(), Vec2ToVec3);

            shapePathField.SetValue(shadowCaster, shadowVertices);
            // Set mesh to null, so shadowcaster regenerate it with provided shapePath
            meshField.SetValue(shadowCaster, null);
            // Mesh generation is handled in OnEnable
            OnEnableMethod.Invoke(shadowCaster, new object[0]);

            shadowCaster.selfShadows = true;
            shadowCaster.useRendererSilhouette = false;
        }

        Debug.Log("Shadow casters have been created");
    }

    void DestroyChildShadowCasters()
    {
        var shadowCasters = GetComponentsInChildren<ShadowCaster2D>();

        foreach (var child in shadowCasters)
        {
            DestroyImmediate(child.gameObject);
        }

        Debug.Log("Shadow casters has been deleted");
    }
}
