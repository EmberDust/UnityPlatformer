using UnityEngine;

public class ExitScript : MonoBehaviour
{
    [SerializeField] ParticleSystem _particlesOnDisappear = null;

    Animator _doorAnimator;

    bool _exitWasTriggered = false;

    void Start()
    {
        _doorAnimator = GetComponent<Animator>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject && !_exitWasTriggered)
        {
            _exitWasTriggered = true;
            _doorAnimator.SetTrigger("ExitWasTriggered");

            if (_particlesOnDisappear != null)
            {
                _particlesOnDisappear.Play();
            }

            GameManager.Instance.CompleteTheScene();
        }
    }
}
