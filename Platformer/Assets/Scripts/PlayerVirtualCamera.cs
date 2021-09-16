using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerVirtualCamera : MonoBehaviour
{
    CinemachineVirtualCamera _virtualCamera;

    private void Awake()
    {
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    void Start()
    {
        StartCoroutine(Utils.DoAfterAFrame(SetupCamera));
    }

    void FollowPlayer()
    {
        _virtualCamera.Follow = GameManager.Instance.PlayerObject.transform;
    }

    void StopFollowing()
    {
        _virtualCamera.Follow = null;
    }

    void SetupCamera()
    {
        if (GameManager.Instance.PlayerScript != null)
        {
            GameManager.Instance.PlayerScript.playerHasBeenDisabled += StopFollowing;
            GameManager.Instance.PlayerScript.playerHasBeenEnabled += FollowPlayer;
        }
        else
        {
            Debug.LogWarning("PlayerScript wasn't found!");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance.PlayerScript)
        {
            GameManager.Instance.PlayerScript.playerHasBeenDisabled -= StopFollowing;
            GameManager.Instance.PlayerScript.playerHasBeenEnabled -= FollowPlayer;
        }
        else
        {
            Debug.LogWarning("PlayerScript wasn't found!");
        }
    }
}
