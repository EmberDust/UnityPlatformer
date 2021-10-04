using System.Collections;
using UnityEngine;

public abstract class PowerUp : MonoBehaviour
{
    [SerializeField] protected float _respawnDelay = 5.0f;

    public bool WasConsumed 
    { 
        get
        {
            return _wasConsumed;
        }

        protected set 
        {
            _wasConsumed = value;

            transform.position = _startingPosition;

            _animator.SetBool(_hashWasConsumed, value);
        } 
    }

    protected bool _wasConsumed;

    protected Vector2 _startingPosition;

    protected Animator _animator;
    protected int _hashWasConsumed;

    protected Coroutine _respawnCoroutine;

    void Start()
    {
        _animator = GetComponent<Animator>();

        _hashWasConsumed = Animator.StringToHash("WasConsumed");

        _startingPosition = transform.position;

        StartCoroutine(Utils.DoAfterAFrame(SetupPowerUp));
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!WasConsumed && collision.gameObject == GameManager.Instance.PlayerObject)
        {
            GrantPowerUp();

            WasConsumed = true;

            _respawnCoroutine = StartCoroutine(RespawnAfterDelay());
        }
    }

    void OnDisable()
    {
        GameManager.Instance.PlayerScript.playerHasBeenEnabled -= RespawnPowerUp;
    }

    void SetupPowerUp()
    {
        GameManager.Instance.PlayerScript.playerHasBeenEnabled += RespawnPowerUp;
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(_respawnDelay);
        RespawnPowerUp();
    }

    void RespawnPowerUp()
    {
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
        }

        WasConsumed = false;
    }

    protected abstract void GrantPowerUp();
}
