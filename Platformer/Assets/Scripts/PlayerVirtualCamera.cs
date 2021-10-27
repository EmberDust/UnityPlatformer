using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerVirtualCamera : MonoBehaviour
{
    CinemachineVirtualCamera _virtualCamera;

    float _originalOrthSize;

    float _zoomedInOrthSize = 3.95f;
    float _zoomInDuration = 0.05f;
    float _zoomOutDuration = 0.4f;

    Coroutine _zoomCoroutine;
    bool _zoomInProgress;

    private void Awake()
    {
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();

        _originalOrthSize = _virtualCamera.m_Lens.OrthographicSize;
    }

    void Start()
    {
        StartCoroutine(Utils.DoAfterAFrame(SetupCamera));
    }

    void OnPlayerEnable()
    {
        _virtualCamera.Follow = GameManager.Instance.PlayerObject.transform;
    }

    void OnPlayerDisable()
    {
        _virtualCamera.Follow = null;
    }

    void OnPlayerDeath()
    {
        StartCoroutine(ZoomIn());
    }

    void SetupCamera()
    {
        if (GameManager.Instance.PlayerScript != null)
        {
            GameManager.Instance.PlayerScript.playerDied += OnPlayerDeath;
            GameManager.Instance.PlayerScript.playerHasBeenDisabled += OnPlayerDisable;
            GameManager.Instance.PlayerScript.playerHasBeenEnabled += OnPlayerEnable;
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
            GameManager.Instance.PlayerScript.playerHasBeenDisabled -= OnPlayerDisable;
            GameManager.Instance.PlayerScript.playerHasBeenEnabled -= OnPlayerEnable;
        }
    }
    
    IEnumerator ZoomIn()
    {
        _zoomCoroutine = StartCoroutine(ChangeOrthSize(_virtualCamera.m_Lens.OrthographicSize, _zoomedInOrthSize, _zoomInDuration));
        yield return new WaitWhile(() => _zoomInProgress);
        _zoomCoroutine = StartCoroutine(ChangeOrthSize(_virtualCamera.m_Lens.OrthographicSize, _originalOrthSize, _zoomOutDuration));
    }

    IEnumerator ChangeOrthSize(float startingOrthSize, float finalOrthSize, float totalDuration)
    {
        if (_zoomCoroutine != null)
            StopCoroutine(_zoomCoroutine);

        _zoomInProgress = true;

        float timeStep = 0.01f;

        for (float currentTime = 0.0f; currentTime < totalDuration; currentTime += timeStep)
        {
            _virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(startingOrthSize, finalOrthSize, currentTime / totalDuration);
            yield return new WaitForSecondsRealtime(timeStep);
        }

        _virtualCamera.m_Lens.OrthographicSize = finalOrthSize;

        _zoomInProgress = false;
    }
}
