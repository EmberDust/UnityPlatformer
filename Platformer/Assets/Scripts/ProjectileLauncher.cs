using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    [Header("Projectile pool")]
    [SerializeField] Collider2D _projectilePrefab;
    [SerializeField] int _projectilePoolSize = 10;

    [Header("Launcher")]
    [SerializeField] Transform _shootFrom = null;
    [SerializeField] float _delayBetweenShots = 1f;

    [Header("Projectile")]
    [SerializeField] float _projectileVelocity = 0.2f;
    [SerializeField] float _projectileLifeTime = 3.0f;

    [Header("Homing Projectile")]
    [SerializeField] bool _isHoming = false;
    [SerializeField] float _rotationSpeed = 0.05f;

    [Header("Without player targeting")]
    [SerializeField] Vector2 _shootDirection = Vector2.up;
    [SerializeField] float _firstShotDelay = 0.0f;

    [Header("Player targeting")]
    [SerializeField] bool _targetPlayer = false;
    [SerializeField] float _maximumShootRange = 20f;
    [SerializeField] LayerMask _playerAndObstaclesLayers;

    Queue<Projectile> _projectilePool = new Queue<Projectile>();

    Vector2 _shootFromPosition;
    float _timeLastShot = 0.0f;
    bool _correctProjectileObject = true;

    void Start()
    {
        _shootFromPosition = _shootFrom != null ? _shootFrom.position : transform.position;
        // Sets up launchers to shoot exactly after _firstshotdelay
        _timeLastShot = Time.time - _delayBetweenShots + _firstShotDelay;

        _shootDirection.Normalize();

        // Fill the pool of projectiles to the base capacity
        for (int i = 0; i < _projectilePoolSize; i++)
        {
            CreateProjectileInPool();
        }
    }

    void FixedUpdate()
    {
        if (_timeLastShot + _delayBetweenShots < Time.time && _correctProjectileObject)
        {
            ShootProjectile();
        }
    }

    void CreateProjectileInPool()
    {
        var newProjectile = Instantiate(_projectilePrefab);

        GameObject newProjectileObject = newProjectile.gameObject;
        Projectile newProjectileScript;

        if (_isHoming)
        {
            newProjectileScript = newProjectileObject.AddComponent<HomingProjectile>();
        }
        else
        {
            newProjectileScript = newProjectileObject.AddComponent<Projectile>();
        }

        // Subscribe to projectile events
        newProjectileScript.projectileCollided += ReturnProjectileToPool;
        newProjectileScript.projectileExpired += ReturnProjectileToPool;

        // Avoid mess in the hierarchy
        newProjectileObject.transform.SetParent(transform);

        newProjectileObject.SetActive(false);

        _projectilePool.Enqueue(newProjectileScript);
    }

    void ShootProjectile()
    {
        if (_targetPlayer)
        {
            Vector2 vectorToPlayer = GameManager.Instance.PlayerObject.transform.position - transform.position;

            if (vectorToPlayer.magnitude < _maximumShootRange && !GameManager.Instance.PlayerScript.IsDisabled)
            {
                vectorToPlayer.Normalize();

                float distanceToPlayer = Vector2.Distance(GameManager.Instance.PlayerObject.transform.position, transform.position);

                // Check if player is in the line of sight
                RaycastHit2D raycastTowardsPlayer = Physics2D.Raycast(transform.position, vectorToPlayer, distanceToPlayer, _playerAndObstaclesLayers);

                if (raycastTowardsPlayer)
                {
                    if (raycastTowardsPlayer.collider.gameObject == GameManager.Instance.PlayerObject)
                    {
                        ShootProjectile(vectorToPlayer * _projectileVelocity);
                        _timeLastShot = Time.time;
                    }
                }
            }
        }
        else
        {
            ShootProjectile(_shootDirection * _projectileVelocity);
            _timeLastShot = Time.time;
        }
    }

    void ShootProjectile(Vector2 velocityVector)
    {
        if (_projectilePool.Count <= 0)
        {
            CreateProjectileInPool();
        }

        Projectile spawnedProjectileScript = _projectilePool.Dequeue();
        spawnedProjectileScript.gameObject.SetActive(true);

        spawnedProjectileScript.Launch(_shootFromPosition, velocityVector, _projectileLifeTime, _rotationSpeed);
    }

    void ReturnProjectileToPool(Projectile projectileScript)
    {
        StartCoroutine(ReturnToPoolWhenReady(projectileScript));
    }

    IEnumerator ReturnToPoolWhenReady(Projectile projectileScript)
    {
        yield return new WaitUntil(() => projectileScript.ReadyToBeReturned);

        projectileScript.gameObject.SetActive(false);
        _projectilePool.Enqueue(projectileScript);
    }
}
