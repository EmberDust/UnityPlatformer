using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : MonoBehaviour
{
    bool _exitWasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.PlayerObject && !_exitWasTriggered)
        {
            GameManager.Instance.LoadNextScene();

            _exitWasTriggered = true;
        }
    }
}
