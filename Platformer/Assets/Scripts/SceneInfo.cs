using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneInfo : MonoBehaviour
{
    [SerializeField] string _sceneName;
    public Vector2 PlayerSpawnPosition { get; private set; }

    private void Awake()
    {
        Debug.Log($"Hello from awake of sceneinfo ID: {_sceneName}");
    }

    private void Start()
    {
        Debug.Log($"Hello from start of sceneinfo ID: {_sceneName}");

        PlayerSpawn playerSpawn = FindObjectOfType<PlayerSpawn>();

        if (playerSpawn != null) 
        {
            PlayerSpawnPosition = playerSpawn.transform.position;
        }
        else
        {
            Debug.LogWarning("PlayerSpawn script wasn't found");
            PlayerSpawnPosition = Vector2.zero;
        }
    }
}
