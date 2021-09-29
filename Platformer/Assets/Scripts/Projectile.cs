using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] protected float _lifeTime = 3.0f;
    [SerializeField] protected float _velocity = 1.0f;
    [SerializeField] protected float _delayAfterCollision = 0.5f;

    public Action<Projectile> projectileCollided;
    public Action<Projectile> projectileExpired;

    public float TimeSpawned { get; private set; }
    public bool ReadyToBeReturned { get; private set; }

    protected Animator _animator;
    protected Rigidbody2D _rb;

    protected bool _hasCollided = false;

    int _hashHasCollided;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();

        _hashHasCollided = Animator.StringToHash("hasCollided");
    }

    void OnEnable()
    {
        _hasCollided = false;
        ReadyToBeReturned = false;
        TimeSpawned = Time.time;
    }

    void FixedUpdate()
    {
        if (TimeSpawned + _lifeTime < Time.time && !_hasCollided)
        {
            TriggerProjectileExpiration();
        }

        ProjectileLogicUpdate();
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (!_hasCollided)
        {
            if (collider.gameObject == GameManager.Instance.PlayerObject)
            {
                GameManager.Instance.PlayerScript.Kill();
            }

            TriggerProjectileExpiration();
        }
    }

    public virtual void Shoot(Vector2 from, Vector2 direction)
    {
        transform.position = from;
        _rb.velocity = direction * _velocity;
    }

    protected void TriggerProjectileExpiration()
    {
        _hasCollided = true;
        _animator.SetTrigger(_hashHasCollided);
        _rb.velocity *= 0.15f;

        // Give time to play an animation, before disabling the projectile
        StartCoroutine(Utils.DoAfterDelay(() => { ReadyToBeReturned = true; }, _delayAfterCollision));

        projectileCollided?.Invoke(this);
    }

    protected virtual void ProjectileLogicUpdate()
    {
    }
}
