using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Menu : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI menuText;

    [HideInInspector] public Coroutine activeTransition;
    [HideInInspector] public CanvasGroup canvasGroup;
    [HideInInspector] public bool active;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        active = false;
    }
}
