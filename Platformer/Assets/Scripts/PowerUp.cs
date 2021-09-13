using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PowerUp : MonoBehaviour
{
    [SerializeField] protected float _respawnDelay = 5.0f;

    public bool WasConsumed { get; protected set; }

    protected GameObject _playerObject;
    protected PlayerMovement _playerScript;
    protected Animator _animator;

    protected Vector2 _startingPosition;
    protected int _hashWasConsumed;

    private void Start()
    {
        _playerScript = FindObjectOfType<PlayerMovement>();
        _playerObject = _playerScript.gameObject;
        _animator = GetComponent<Animator>();

        _hashWasConsumed = Animator.StringToHash("WasConsumed");

        _startingPosition = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == _playerObject)
        {
            if (!WasConsumed)
            {
                GrantPowerUp();

                WasConsumed = true;
                _animator.SetBool(_hashWasConsumed, WasConsumed);

                StartCoroutine(RespawnAfterDelay());
            }
        }
    }
    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(_respawnDelay);

        WasConsumed = false;
        _animator.SetBool(_hashWasConsumed, WasConsumed);

        transform.position = _startingPosition;
    }

    protected abstract void GrantPowerUp();
}
