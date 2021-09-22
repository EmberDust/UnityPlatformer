using System.Collections;
using UnityEngine;

public abstract class PowerUp : MonoBehaviour
{
    [SerializeField] protected float _respawnDelay = 5.0f;

    public bool WasConsumed { get; protected set; }

    protected Animator _animator;

    protected Vector2 _startingPosition;
    protected int _hashWasConsumed;

    void Start()
    {
        _animator = GetComponent<Animator>();

        _hashWasConsumed = Animator.StringToHash("WasConsumed");

        _startingPosition = transform.position;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject)
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
