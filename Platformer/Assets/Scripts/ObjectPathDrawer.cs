using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ObjectPathDrawer : MonoBehaviour
{
    [SerializeField] SavedObjectPath _objectPath;
    [SerializeField] LineRenderer _lineToDrawPath = null;
    [Header("Pretend it is buttons:")]
    [SerializeField] bool _drawPath = false;
    [SerializeField] bool _discardLine = false;

    void Start()
    {
        _drawPath = false;
        _discardLine = true;
    }

    void Update()
    {
        if (_drawPath)
        {
            Debug.Log("Path drawed");
            DrawPath();
            _drawPath = false;
        }

        if (_discardLine)
        {
            DiscardLine();
            _discardLine = false;
        }
    }

    void DrawPath()
    {
        if (_lineToDrawPath != null)
        {
            _lineToDrawPath.positionCount = _objectPath.playerPositions.Count;
            _lineToDrawPath.SetPositions(_objectPath.playerPositions.ToArray());
        }
        else
        {
            Debug.LogWarning("Need a LineRenderer to draw an object path");
        }
    }

    void DiscardLine()
    {
        _lineToDrawPath.positionCount = 0;
    }
}
