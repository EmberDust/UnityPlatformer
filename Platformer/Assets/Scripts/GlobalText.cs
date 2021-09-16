using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GlobalText : MonoBehaviour
{
    TextMeshProUGUI _textObject;

    // Singleton
    static public GlobalText Instance { get; private set; }

    void Awake()
    {
        if (GlobalText.Instance != null)
        {
            Destroy(this.gameObject);
        }

        Instance = this;
        Debug.Log("Global text singleton has been instantiated");
    }

    void Start()
    {
        _textObject = GetComponent<TextMeshProUGUI>();
    }

    public void Show(string text)
    {
        _textObject.SetText(text);
    }
}
