using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class DashPoint : MonoBehaviour
{
    [SerializeField] float _activationCooldown = 2.0f;
    [SerializeField] LineRenderer _lineToPlayer;
    [SerializeField] Light2D _activationLight;

    public bool IsClosest { get { return this == DashPointsManager.Instance.ClosestInRange; } }
    public bool IsOnCooldown { get { return _timeLastActivated + _activationCooldown > Time.time; } }

    float _timeLastActivated = 0.0f;

    Animator _animator;

    int hashInFocus;
    int hashWasActivated;

    void Start()
    {
        _animator = GetComponent<Animator>();

        hashInFocus = Animator.StringToHash("InFocus");
        hashWasActivated = Animator.StringToHash("WasActivated");

        _lineToPlayer.positionCount = 2;
        _lineToPlayer.SetPosition(0, transform.position);
    }

    void Update()
    {
        if (IsClosest && !IsOnCooldown)
        {
            _lineToPlayer.SetPosition(1, GameManager.Instance.PlayerObject.transform.position - new Vector3(0.0f, 0.1f, 0.0f));
        }

        UpdateAnimator();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject)
        {
            DashPointsManager.Instance.AddDashPoint(this);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject)
        {
            DashPointsManager.Instance.RemoveDashPoint(this);
        }
    }

    public void Activate()
    {
        _animator.SetTrigger(hashWasActivated);
        _timeLastActivated = Time.time;
    }

    void UpdateAnimator()
    {
        _animator.SetBool(hashInFocus, IsClosest && !IsOnCooldown);
    }
}
