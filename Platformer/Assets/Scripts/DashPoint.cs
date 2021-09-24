using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashPoint : MonoBehaviour
{
    public static DashPoint ClosestInRange { get; private set; }

    static HashSet<DashPoint> _dashPointsInRange = new HashSet<DashPoint>();
    static bool _hasUpdatedClosestThisFrame = false;

    [SerializeField] LineRenderer _lineToPlayer;

    void Start()
    {
        _lineToPlayer.positionCount = 2;
        _lineToPlayer.SetPosition(0, transform.position);

        _lineToPlayer.enabled = false;
    }

    void FixedUpdate()
    {
        if (!_hasUpdatedClosestThisFrame)
        {
            FindClosestInRange();
            _hasUpdatedClosestThisFrame = true;
        }
    }

    void Update()
    {
        if (this == ClosestInRange)
        {
            if (!_lineToPlayer.enabled)
            {
                _lineToPlayer.enabled = true;
            }

            _lineToPlayer.SetPosition(1, GameManager.Instance.PlayerObject.transform.position);
        }
        else if (_lineToPlayer.enabled)
        {
            _lineToPlayer.enabled = false;
        }

        _hasUpdatedClosestThisFrame = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject)
        {
            _dashPointsInRange.Add(this);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject)
        {
            _dashPointsInRange.Remove(this);
        }
    }

    static void FindClosestInRange()
    {
        float distanceToClosest = float.MaxValue;
        DashPoint closestDashPoint = null;

        Vector2 playerPosition = GameManager.Instance.PlayerObject.transform.position;

        foreach (DashPoint currentPoint in _dashPointsInRange)
        {
            float distanceToCurrent = Vector2.Distance(currentPoint.transform.position, playerPosition);

            if (distanceToClosest > distanceToCurrent)
            {
                distanceToClosest = distanceToCurrent;
                closestDashPoint = currentPoint;
            }
        }

        ClosestInRange = closestDashPoint;
    }
}
