using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPathTracker : MonoBehaviour
{
    [SerializeField] SavedObjectPath _playerPathFile;
    [SerializeField] int _framesBetweenSaves = 3;
    [SerializeField] bool _pauseTracker = false;
    [SerializeField] bool _overwriteCurrentPath = false;

    int _lastSaveFrame;
    int _currentFrame;

    void Start()
    {
        _currentFrame = 0;
        _lastSaveFrame = -_framesBetweenSaves;

        if (_overwriteCurrentPath)
        {
            _playerPathFile.playerPositions.Clear();
        }
    }

    void FixedUpdate()
    {
        if (_currentFrame - _lastSaveFrame > _framesBetweenSaves && !_pauseTracker)
        {
            _playerPathFile.playerPositions.Add(transform.position);
        }

        _currentFrame++;
    }
}
