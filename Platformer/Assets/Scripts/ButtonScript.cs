using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    CanvasGroup _parentCanvasGroup;
    TextMeshProUGUI _buttonText;

    float _fontSizeStarting;
    float _fontSizeOnHover;
    float _fontSizeOnClick;

    Coroutine _textResizeCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        _buttonText = GetComponentInChildren<TextMeshProUGUI>();
        _parentCanvasGroup = GetComponentInParent<CanvasGroup>();

        _fontSizeStarting = _buttonText.fontSize;
        _fontSizeOnHover = _fontSizeStarting * 1.15f;
        _fontSizeOnClick = _fontSizeStarting * 1.05f;
    }

    public void OnButtonClick()
    {
        StartFontResizeCoroutine(_buttonText.fontSize, _fontSizeOnClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartFontResizeCoroutine(_buttonText.fontSize, _fontSizeOnHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartFontResizeCoroutine(_buttonText.fontSize, _fontSizeStarting);
    }

    void StartFontResizeCoroutine(float startingSize, float finalSize)
    {
        if (_parentCanvasGroup.interactable)
        {
            if (_textResizeCoroutine != null)
            {
                StopCoroutine(_textResizeCoroutine);
            }

            _textResizeCoroutine = StartCoroutine(ResizeFont(startingSize, finalSize));
        }
    }

    IEnumerator ResizeFont(float startingSize, float finalSize)
    {
        int resizeTimeStepsCount = 5;

        for (int i = 0; i < resizeTimeStepsCount; i++)
        {
            _buttonText.fontSize = Mathf.Lerp(startingSize, finalSize, (float)i / resizeTimeStepsCount);
            yield return new WaitForSecondsRealtime(0.01f);
        }

        _buttonText.fontSize = finalSize;
    }
}
