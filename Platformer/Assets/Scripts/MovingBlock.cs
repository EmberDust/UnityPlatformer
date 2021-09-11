using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class MovingBlock : MonoBehaviour
{
    [SerializeField] Transform _blockBody = null;
    [SerializeField] bool _connectEnds;
    [SerializeField] float _speed = 1f;

    LineRenderer _line = null;
    RoutePoint[] _routePoints;
    List<Vector3> _routePointsWorldPos;

    int _destinationPointIndex = 0;
    int _traverseDirection = 1;

    void Start()
    {
        // Make a route out of all children objects with RoutePoint component on them
        _routePointsWorldPos = new List<Vector3>();

        _routePoints = GetComponentsInChildren<RoutePoint>();
        foreach (RoutePoint point in _routePoints)
        {
            _routePointsWorldPos.Add(point.transform.position);
        }

        // If line renderer is present - set it up
        _line = GetComponentInChildren<LineRenderer>();
        DrawLineBetweenPoints();
    }

    void FixedUpdate()
    {
        MoveAlongTheRoute();
    }

    void DrawLineBetweenPoints()
    {
        if (_line != null)
        {
            _line.positionCount = _routePointsWorldPos.Count;
            for (int i = 0; i < _line.positionCount; i++)
            {
                _line.SetPosition(i, _routePoints[i].transform.localPosition);
            }

            if (_connectEnds)
            {
                _line.positionCount++;
                _line.SetPosition(_line.positionCount - 1, _line.GetPosition(0));
            }
        }
    }

    void MoveAlongTheRoute()
    {
        if (_blockBody != null)
        {
            if (Vector2.Distance(_routePointsWorldPos[_destinationPointIndex], _blockBody.position) < _speed / 2f)
            {
                // If we've reached the end of route
                if (_destinationPointIndex + _traverseDirection >= _routePointsWorldPos.Count || _destinationPointIndex + _traverseDirection < 0)
                {
                    // Loop
                    if (_connectEnds)
                    {
                        _traverseDirection = 1;
                        _destinationPointIndex = -1;
                    }
                    // Reverse
                    else
                    {
                        _traverseDirection *= -1;
                    }
                }

                _destinationPointIndex += _traverseDirection;
            }
            else
            {
                Vector3 movementDirection = (_routePointsWorldPos[_destinationPointIndex] - _blockBody.position).normalized;
                _blockBody.Translate(movementDirection * _speed, Space.World);
            }
        }
    }

}
