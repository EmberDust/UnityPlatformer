using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [SerializeField] Menu _pauseMenu;
    [SerializeField] Menu _sceneCompletionMenu;
    [Header("Transition")]
    [SerializeField] float _transitionDuration = 0.1f;
    [SerializeField] float _transitionTimeStep = 0.01f;
    [SerializeField] float _saturationInPause = -50f;

    public bool InPauseMenu { get => _pauseMenu.active; }
    public bool InCompletionMenu { get => _sceneCompletionMenu.active; }
    public bool InMenuTransition { get => _activeTransition != null; }
    public TextMeshProUGUI CompletionText { get => _completionText; }
    public Menu ActiveMenu { get; private set; }

    ColorAdjustments _colorOverride;

    Coroutine _activeTransition;
    TextMeshProUGUI _completionText;

    void Start()
    {
        Volume colorOverrideVolume = GetComponentInChildren<Volume>();
        colorOverrideVolume.profile.TryGet(out _colorOverride);

        _completionText = _sceneCompletionMenu.menuText;

        ActiveMenu = null;
    }

    public void ToggleCompletionMenu()
    {
        ToggleMenu(_sceneCompletionMenu);
    }

    public void TogglePauseMenu()
    {
        ToggleMenu(_pauseMenu);
    }

    public void ToggleMenu(Menu menu)
    {
        if (menu.active)
        {
            DisableMenu(menu);
            _activeTransition = StartCoroutine(TransitionToPause(1.0f, 0.0f));
        }
        else
        {
            if (ActiveMenu != null)
            {
                DisableMenu(ActiveMenu);
            }

            EnableMenu(menu);
            _activeTransition = StartCoroutine(TransitionToPause(0.0f, _saturationInPause));
        }
    }

    public void TransitionOutOfActiveMenu()
    {
        if (ActiveMenu != null && ActiveMenu.active)
        {
            DisableMenu(ActiveMenu);
            _activeTransition = StartCoroutine(TransitionToPause(1.0f, 0.0f));
        }
    }

    void EnableMenu(Menu menu)
    {
        menu.active = true;
        menu.canvasGroup.interactable = true;
        menu.canvasGroup.blocksRaycasts = true;
        ActiveMenu = menu;

        menu.activeTransition = StartCoroutine(AnimateMenuAlpha(menu, 1.0f));
    }

    void DisableMenu(Menu menu)
    {
        menu.active = false;
        menu.canvasGroup.interactable = false;
        menu.canvasGroup.blocksRaycasts = false;
        ActiveMenu = null;

        menu.activeTransition = StartCoroutine(AnimateMenuAlpha(menu, 0.0f));
    }

    IEnumerator AnimateMenuAlpha(Menu targetMenu, float finalAlpha)
    {
        if (targetMenu.activeTransition != null)
        {
            StopCoroutine(targetMenu.activeTransition);
        }

        float startingAlpha = targetMenu.canvasGroup.alpha;

        for (float transitionTime = 0; transitionTime < _transitionDuration; transitionTime += _transitionTimeStep)
        {
            float transitionProgress = transitionTime / _transitionDuration;

            targetMenu.canvasGroup.alpha = Mathf.Lerp(startingAlpha, finalAlpha, transitionProgress);

            yield return new WaitForSecondsRealtime(_transitionTimeStep);
        }

        targetMenu.canvasGroup.alpha = finalAlpha;
        targetMenu.activeTransition = null;
    }

    IEnumerator TransitionToPause(float finalTimeScale, float finalSaturation)
    {
        if (_activeTransition != null)
        {
            StopCoroutine(_activeTransition);
        }

        float startingTimeScale = Time.timeScale;

        _colorOverride.active = true;
        float startingSaturation = _colorOverride.saturation.value;

        for (float transitionTime = 0; transitionTime < _transitionDuration; transitionTime += _transitionTimeStep)
        {
            float transitionProgress = transitionTime / _transitionDuration;

            Time.timeScale = Mathf.Lerp(startingTimeScale, finalTimeScale, transitionProgress);
            _colorOverride.saturation.value = Mathf.Lerp(startingSaturation, finalSaturation, transitionProgress);

            yield return new WaitForSecondsRealtime(_transitionTimeStep);
        }

        Time.timeScale = finalTimeScale;
        _colorOverride.saturation.value = finalSaturation;

        _colorOverride.active = finalSaturation == _saturationInPause;

        _activeTransition = null;
    }
}
