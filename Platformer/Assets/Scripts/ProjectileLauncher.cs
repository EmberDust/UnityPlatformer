using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    [Header("Projectile pool")]
    [SerializeField] GameObject _projectilePrefab;
    [SerializeField] int _projectilePoolSize = 10;

    [Header("Projectile launcher")]
    [SerializeField] float _delayBetweenShots = 1f;
    [SerializeField] float _projectileVelocity = 0.2f;
    [Space]
    [SerializeField] bool _targetPlayer = false;
    [SerializeField] LayerMask _playerAndObstaclesLayers;
    [SerializeField] Vector2 _shootDirection = Vector2.up;

    Queue<SpikeProjectile> _projectilePool = new Queue<SpikeProjectile>();

    float _timeLastShot = 0.0f;
    bool _correctProjectileObject = true;

    void Start()
    {
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
            _timeLastShot = Time.time;
        }
    }

    void CreateProjectileInPool()
    {
        GameObject newProjectile = Instantiate(_projectilePrefab);
        SpikeProjectile newProjectileScript = newProjectile.GetComponent<SpikeProjectile>();

        if (newProjectileScript != null && _correctProjectileObject)
        {
            // Subscribe to projectile events
            newProjectileScript.projectileCollided += ReturnProjectileToPool;
            newProjectileScript.projectileExpired += ReturnProjectileToPool;

            // Put them under the current object to avoid mess in the hierarchy
            newProjectile.transform.SetParent(transform);

            newProjectile.SetActive(false);

            _projectilePool.Enqueue(newProjectileScript);
        }
        else
        {
            Debug.LogError("Projectile Game Object doesn't have required component");
            _correctProjectileObject = false;
        }
    }

    void ShootProjectile()
    {
        if (_targetPlayer)
        {
            Vector2 playerDirection = GameManager.Instance.PlayerObject.transform.position - transform.position;
            playerDirection.Normalize();

            float distanceToPlayer = Vector2.Distance(GameManager.Instance.PlayerObject.transform.position, transform.position);

            // Check if player is in the line of sight
            RaycastHit2D raycastTowardsPlayer = Physics2D.Raycast(transform.position, playerDirection, distanceToPlayer, _playerAndObstaclesLayers);

            if (raycastTowardsPlayer)
            {
                if (raycastTowardsPlayer.collider.gameObject == GameManager.Instance.PlayerObject)
                {
                    ShootProjectile(transform.position, playerDirection * _projectileVelocity);
                }
            }
        }
        else
        {
            ShootProjectile(transform.position, _shootDirection * _projectileVelocity);
        }
    }

    void ShootProjectile(Vector2 spawnPosition, Vector2 velocity)
    {
        if (_projectilePool.Count <= 0)
        {
            CreateProjectileInPool();
        }

        SpikeProjectile spawnedProjectileScript = _projectilePool.Dequeue();
        spawnedProjectileScript.gameObject.SetActive(true);

        spawnedProjectileScript.Shoot(spawnPosition, velocity);
    }

    void ReturnProjectileToPool(SpikeProjectile projectileScript)
    {
        StartCoroutine(ReturnToPoolWhenReady(projectileScript));
    }

    IEnumerator ReturnToPoolWhenReady(SpikeProjectile projectileScript)
    {
        while (!projectileScript.ReadyToBeReturned)
        {
            yield return null;
        }

        projectileScript.gameObject.SetActive(false);
        _projectilePool.Enqueue(projectileScript);
    }
}
