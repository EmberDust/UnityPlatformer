using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashPointsManager : MonoBehaviour
{
    public static DashPointsManager Instance { get; private set; }

    public DashPoint ClosestInRange { get; private set; }
    HashSet<DashPoint> _dashPointsInRange = new HashSet<DashPoint>();

    private void Awake()
    {
        if (DashPointsManager.Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        StartCoroutine(Utils.DoAfterAFrame(SetupManager));
    }

    private void FixedUpdate()
    {
        UpdateClosestInRange();
    }

    public void AddDashPoint(DashPoint dashPoint)
    {
        _dashPointsInRange.Add(dashPoint);
    }

    public void RemoveDashPoint(DashPoint dashPoint)
    {
        _dashPointsInRange.Remove(dashPoint);
    }

    void ActivateDashPoint(DashPoint dashPoint)
    {
        dashPoint.Activate();
    }

    void UpdateClosestInRange()
    {
        float distanceToClosest = float.MaxValue;
        DashPoint closestDashPoint = null;

        Vector2 playerPosition = GameManager.Instance.PlayerObject.transform.position;

        foreach (DashPoint currentPoint in _dashPointsInRange)
        {
            if (!currentPoint.IsOnCooldown)
            {
                float distanceToCurrent = Vector2.Distance(currentPoint.transform.position, playerPosition);

                if (distanceToClosest > distanceToCurrent)
                {
                    distanceToClosest = distanceToCurrent;
                    closestDashPoint = currentPoint;
                }
            }
        }

        ClosestInRange = closestDashPoint;
    }

    void SetupManager()
    {
        GameManager.Instance.PlayerScript.playerDashed += ActivateDashPoint;
    }
}
