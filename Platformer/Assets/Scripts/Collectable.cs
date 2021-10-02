using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    enum State { NotCollected, Collected }

    public Action hasBeenCollected;
    public Action hasBeenReturned;

    State CurrentState 
    { 
        get 
        { 
            return _currentState; 
        } 

        set 
        {
            if (_currentState != value)
            {
                if (value == State.Collected)
                {
                    _sprite.color = _startingColor * 0.5f;
                    GameManager.Instance.CurrentCollectablesScore++;
                    hasBeenCollected?.Invoke();
                }
                else
                {
                    _sprite.color = _startingColor;
                    GameManager.Instance.CurrentCollectablesScore--;
                    hasBeenReturned?.Invoke();
                }

                _currentState = value;
            }
        }
    }

    State _savedState   = State.NotCollected;
    State _currentState = State.NotCollected;

    SpriteRenderer _sprite;

    Color _startingColor;

    void Start()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _startingColor = _sprite.color;

        StartCoroutine(Utils.DoAfterAFrame(SetupCollectable));
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (CurrentState == State.NotCollected && collision.gameObject == GameManager.Instance.PlayerObject)
        {
            CurrentState = State.Collected;
        }
    }

    void SetupCollectable()
    {
        GameManager.Instance.checkPointReached += SaveCurrentState;
        GameManager.Instance.PlayerScript.playerHasBeenEnabled += ResetToSavedState;
    }

    void SaveCurrentState()
    {
        _savedState = CurrentState;
    }

    void ResetToSavedState()
    {
        CurrentState = _savedState;
    }
}
