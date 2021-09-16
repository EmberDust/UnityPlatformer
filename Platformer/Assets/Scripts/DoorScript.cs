using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : MonoBehaviour
{
    [SerializeField] ParticleSystem _particlesOnDisappear = null;

    Animator _doorAnimator;

    bool _exitWasTriggered = false;

    private void Start()
    {
        _doorAnimator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject && !_exitWasTriggered)
        {
            GameManager.Instance.LoadNextScene();

            _exitWasTriggered = true;
            _doorAnimator.SetTrigger("ExitWasTriggered");

            if (_particlesOnDisappear != null)
            {
                _particlesOnDisappear.Play();
            }
        }
    }
}
