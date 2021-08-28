using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GlobalText : MonoBehaviour
{
    TextMeshProUGUI _textObject;

    static public GlobalText Instance { get; private set; }

    void Start()
    {
        _textObject = GetComponent<TextMeshProUGUI>();

        Instance = this;
    }

    public void Show(string text)
    {

        _textObject.SetText(text);
    }
}
