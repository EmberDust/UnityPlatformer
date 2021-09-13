using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityBooster : MonoBehaviour
{
    [SerializeField] Vector2 _boostAmount = new Vector2( 1.0f, 0.0f );
    [SerializeField] float _respawnDelay = 5.0f;

    public bool WasConsumed { get; private set; }

    GameObject _playerObject;
    PlayerMovement _playerScript;
    Animator _animator;

    Vector2 _startingPosition;
    int _hashWasConsumed;

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
            _playerScript.GiveVelocityBoost(_boostAmount);

            WasConsumed = true;
            _animator.SetBool(_hashWasConsumed, WasConsumed);

            StartCoroutine(RespawnAfterDelay());
        }
    }
    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(_respawnDelay);

        WasConsumed = false;
        _animator.SetBool(_hashWasConsumed, WasConsumed);

        transform.position = _startingPosition;
    }
}
