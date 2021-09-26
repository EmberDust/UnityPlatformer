using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfterImage : MonoBehaviour
{
    public SpriteRenderer Sprite { get; set; }
    public Color StartingColor { get; set; }
    public float FadeOutRate { get; set; }
    public float FadeOutAmount { get; set; }
    public float CurrentFadeOut { get; set; }

    public event Action<AfterImage> afterImageVanished;

    void Awake()
    {
        Sprite = GetComponent<SpriteRenderer>();
    }

    public void StartFadeOut()
    {
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        while (CurrentFadeOut < 1.0f)
        {
            Sprite.color = StartingColor * (1.0f - CurrentFadeOut);
            CurrentFadeOut += FadeOutAmount;

            yield return new WaitForSeconds(FadeOutRate);
        }

        afterImageVanished?.Invoke(this);
    }
}
