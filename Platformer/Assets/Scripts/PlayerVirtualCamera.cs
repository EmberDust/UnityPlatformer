using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerVirtualCamera : MonoBehaviour
{
    CinemachineVirtualCamera _virtualCamera;
    CinemachineVirtualCamera _passiveCamera;

    private void Awake()
    {
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();
        _passiveCamera = GetComponentInChildren<CinemachineVirtualCamera>();
    }

    void Start()
    {
        StartCoroutine(Utils.DoAfterAFrame(SetupCamera));
    }

    void FollowPlayer()
    {
        Debug.Log("Started following player transform");
        _virtualCamera.Follow = GameManager.Instance.PlayerObject.transform;
    }

    void StopFollowing()
    {
        Debug.Log("Stopped following");
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
