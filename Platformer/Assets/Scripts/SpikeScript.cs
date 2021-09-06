using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeScript : MonoBehaviour
{
    GameObject _playerObject;
    PlayerMovement _playerScript;

    private void Start()
    {
        _playerScript = FindObjectOfType<PlayerMovement>();
        _playerObject = _playerScript.gameObject;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == _playerObject)
        {
            _playerScript.Kill();
        }
    }
}
