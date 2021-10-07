using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSpaceText : MonoBehaviour
{
    [SerializeField] float _transparentAlpha = 0.2f;
    [SerializeField] float _alphaChangeDuration = 0.1f;

    RectTransform _canvasRect;
    CanvasGroup _canvasGroup;
    BoxCollider2D _fadeOutTrigger;

    float _baseAlpha;

    Coroutine _currentlyActiveCoroutine;

    void Start()
    {
        _canvasRect = GetComponent<RectTransform>();

        _canvasGroup = GetComponent<CanvasGroup>();
        _baseAlpha = _canvasGroup.alpha;

        _fadeOutTrigger = GetComponent<BoxCollider2D>();
        _fadeOutTrigger.size = _canvasRect.sizeDelta;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject)
        {
            _currentlyActiveCoroutine = StartCoroutine(ChangeAlphaOverTime(_canvasGroup.alpha, _transparentAlpha, _alphaChangeDuration));
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject)
        {
            _currentlyActiveCoroutine = StartCoroutine(ChangeAlphaOverTime(_canvasGroup.alpha, _baseAlpha, _alphaChangeDuration));
        }
    }

    IEnumerator ChangeAlphaOverTime(float startingAlpha, float finalAlpha, float duration)
    {
        if (_currentlyActiveCoroutine != null)
        {
            StopCoroutine(_currentlyActiveCoroutine);
        }

        float timeStep = 0.02f;
        float timeStepsCount = duration / timeStep;
        
        for (int i = 0; i < timeStepsCount; i++)
        {
            float changeProgress = i / timeStepsCount;
            _canvasGroup.alpha = Mathf.Lerp(startingAlpha, finalAlpha, changeProgress);
            yield return new WaitForSeconds(timeStep);
        }

        _canvasGroup.alpha = finalAlpha;
        _currentlyActiveCoroutine = null;
    }
}
