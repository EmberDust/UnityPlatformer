using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class Checkpoint : MonoBehaviour
{
    Light2D _activationLight;

    public bool IsActive 
    { 
        get => _isActive;
        set
        {
            if (_activationLight != null)
            {
                _activationLight.gameObject.SetActive(value);
            }

            _isActive = value;
        }
    }

    bool _isActive;

    void Start()
    {
        _activationLight = GetComponentInChildren<Light2D>();

        IsActive = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isActive && collision.gameObject == GameManager.Instance.PlayerObject)
        {
            GameManager.Instance.SetNewCheckpoint(this);
        }
    }
}
