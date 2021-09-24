using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GlobalText : MonoBehaviour
{
    TextMeshProUGUI _textMesh;
    StringBuilder _textString = new StringBuilder();

    // Singleton
    static public GlobalText Instance { get; private set; }

    void Awake()
    {
        if (GlobalText.Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            Debug.Log("Global text singleton has been instantiated");
        }
    }

    void Start()
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
    }

    void LateUpdate()
    {
        _textMesh.SetText(_textString.ToString());
        _textString.Clear();
    }

    /// <summary>
    /// Use in regular update
    /// </summary>
    public void AppendText(string text)
    {
        _textString.AppendLine(text);
    }
}
