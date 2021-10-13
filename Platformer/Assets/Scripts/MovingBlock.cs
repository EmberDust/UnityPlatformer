using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class MovingBlock : MonoBehaviour
{
    [SerializeField] Rigidbody2D _blockBody = null;
    [SerializeField] bool _connectEnds;
    [SerializeField] float _speed = 1f;
    [SerializeField] float _pauseOnRoutePoints = 0f;
    [SerializeField] float _pauseOnRouteEnd = 0f;
    [SerializeField] bool _resetOnPlayerRespawn = false;

    LineRenderer _line = null;
    RoutePoint[] _routePoints;
    List<Vector3> _routePointsPositions;

    int _destinationPointIndex = 0;
    int _traverseDirection = 1;

    bool _stoppedOnRouteEnd = false;
    bool _stoppedOnRoutePoint = false;

    Vector2 _startingPosition;

    void Start()
    {
        _startingPosition = _blockBody.transform.position;

        // Make a route out of all children objects with RoutePoint component on them
        _routePointsPositions = new List<Vector3>();

        _routePoints = GetComponentsInChildren<RoutePoint>();
        foreach (RoutePoint point in _routePoints)
        {
            _routePointsPositions.Add(point.transform.position);
        }

        // If line renderer is present - set it up
        _line = GetComponentInChildren<LineRenderer>();
        if (_line != null)
        {
            DrawLineBetweenPoints();
        }

        StartCoroutine(Utils.DoAfterAFrame(SetupMovingBlock));
    }

    void FixedUpdate()
    {
        if (_blockBody != null)
        {
            MoveAlongTheRoute();
        }
    }

    void DrawLineBetweenPoints()
    {
        _line.positionCount = _routePointsPositions.Count;
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

    void MoveAlongTheRoute()
    {
        bool reachedRoutePoint = Vector2.Distance(_routePointsPositions[_destinationPointIndex], _blockBody.position) < _speed / 2f;

        if (reachedRoutePoint)
        {
            bool reachedEndOfRoute = _destinationPointIndex + _traverseDirection >= _routePointsPositions.Count || _destinationPointIndex + _traverseDirection < 0;

            if (reachedEndOfRoute)
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

                if (_pauseOnRouteEnd > 0f)
                {
                    _stoppedOnRouteEnd = true;
                    StartCoroutine(Utils.DoAfterDelay(() => { _stoppedOnRouteEnd = false; }, _pauseOnRouteEnd));
                }
            }

            _destinationPointIndex += _traverseDirection;

            if (_pauseOnRoutePoints > 0f)
            {
                _stoppedOnRoutePoint = true;
                StartCoroutine(Utils.DoAfterDelay(() => { _stoppedOnRoutePoint = false; }, _pauseOnRoutePoints));
            }
        }
        else if (!_stoppedOnRoutePoint && !_stoppedOnRouteEnd)
        {
            Vector3 movementDirection = (_routePointsPositions[_destinationPointIndex] - _blockBody.transform.position).normalized;
            _blockBody.MovePosition(_blockBody.transform.position + movementDirection * _speed);
        }
    }

    void SetupMovingBlock()
    {
        if (_resetOnPlayerRespawn)
        {
            GameManager.Instance.PlayerScript.playerHasBeenEnabled += ResetBlock;
        }

        GameManager.Instance.sceneEnded += OnSceneEnd;
    }
    
    void OnSceneEnd()
    {
        if (_resetOnPlayerRespawn)
        {
            GameManager.Instance.PlayerScript.playerHasBeenEnabled -= ResetBlock;
        }

        GameManager.Instance.sceneEnded -= OnSceneEnd;
    }

    void ResetBlock()
    {
        _blockBody.transform.position = _startingPosition;
        _destinationPointIndex = 0;
        _traverseDirection = 1;

        _stoppedOnRouteEnd = false;
        _stoppedOnRoutePoint = false;

        StopAllCoroutines();
    }
}
