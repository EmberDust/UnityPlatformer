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
                    GameManager.Instance.CurrentCollectablesScore++;
                    hasBeenCollected?.Invoke();
                }
                else
                {
                    transform.position = _startingPosition;
                    GameManager.Instance.CurrentCollectablesScore--;
                    hasBeenReturned?.Invoke();
                }

                _currentState = value;
                _animator.SetBool(_hashWasCollected, value == State.Collected);
            }
        }
    }

    State _savedState   = State.NotCollected;
    State _currentState = State.NotCollected;

    Vector2 _startingPosition;

    Animator _animator;
    int _hashWasCollected;

    void Start()
    {
        _animator = GetComponent<Animator>();

        _hashWasCollected = Animator.StringToHash("WasCollected");

        _startingPosition = transform.position;

        StartCoroutine(Utils.DoAfterAFrame(SetupCollectable));
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (CurrentState == State.NotCollected && collision.gameObject == GameManager.Instance.PlayerObject)
        {
            CurrentState = State.Collected;
        }
    }

    void OnDisable()
    {
        GameManager.Instance.checkPointReached -= SaveCurrentState;
        GameManager.Instance.PlayerScript.playerHasBeenEnabled -= ResetToSavedState;
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
