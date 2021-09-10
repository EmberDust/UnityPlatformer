using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class MovingBlock : MonoBehaviour
{
    [SerializeField] Transform _blockBody = null;
    [SerializeField] bool _connectEnds;
    [SerializeField] float _speed = 1f;

    List<Vector3> _routePoints;

    int _destinationPointIndex = 0;
    int _traverseDirection = 1;

    void Start()
    {
        // Make a route out of all objects with RoutePoint objects among children
        _routePoints = new List<Vector3>();

        RoutePoint[] route = GetComponentsInChildren<RoutePoint>();
        foreach (RoutePoint point in route)
        {
            _routePoints.Add(point.transform.position);
        }
    }

    void FixedUpdate()
    {
        MoveAlongTheRoute();
    }

    void MoveAlongTheRoute()
    {
        if (_blockBody != null)
        {
            if (Vector2.Distance(_routePoints[_destinationPointIndex], _blockBody.position) < _speed / 2f)
            {
                if (_destinationPointIndex + _traverseDirection >= _routePoints.Count || _destinationPointIndex + _traverseDirection < 0)
                {
                    if (_connectEnds)
                    {
                        _traverseDirection = 1;
                        _destinationPointIndex = -1;
                    }
                    else
                    {
                        _traverseDirection *= -1;
                    }
                }

                _destinationPointIndex += _traverseDirection;
            }
            else
            {
                Vector3 movementDirection = (_routePoints[_destinationPointIndex] - _blockBody.position).normalized;
                _blockBody.Translate(movementDirection * _speed, Space.World);
            }
        }
    }

}
