using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float _lifeTime = 3.0f;
    [SerializeField] float _velocity = 1.0f;
    [SerializeField] float _delayAfterCollision = 0.5f;

    public Action<Projectile> projectileCollided;
    public Action<Projectile> projectileExpired;

    public float TimeSpawned { get; private set; }
    public bool ReadyToBeReturned { get; private set; }

    Animator animator;
    Rigidbody2D rb;

    bool hasCollided = false;

    int hashHasCollided;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        hashHasCollided = Animator.StringToHash("hasCollided");
    }

    void OnEnable()
    {
        hasCollided = false;
        ReadyToBeReturned = false;
        TimeSpawned = Time.time;
    }

    void FixedUpdate()
    {
        if (TimeSpawned + _lifeTime < Time.time && !hasCollided)
        {
            ReadyToBeReturned = true;
            projectileExpired?.Invoke(this);
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (!hasCollided)
        {
            if (collider.gameObject == GameManager.Instance.PlayerObject)
            {
                GameManager.Instance.PlayerScript.Kill();
            }

            hasCollided = true;
            animator.SetTrigger(hashHasCollided);

            rb.velocity = rb.velocity * 0.15f;
            // Give time to play an animation, before disabling the projectile
            StartCoroutine(Utils.DoAfterDelay(() => { ReadyToBeReturned = true; }, _delayAfterCollision));

            projectileCollided?.Invoke(this);
        }
    }

    public void Shoot(Vector2 from, Vector2 direction)
    {
        transform.position = from;
        rb.velocity = direction * _velocity;
    }
}
