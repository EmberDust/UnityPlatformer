using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] List<string> _scenesLoadOrder;
    [SerializeField] string _loadscreenScene;

    // Singleton
    public static GameManager Instance { get; private set; }

    int _currentSceneIndex = 0;

    SceneInfo _currentSceneInfo;

    public Vector2 CheckpointPosition { get; set; }
    public Vector2 ExitPosition { get; set; }

    // Cashed player info for other scripts to use
    public PlayerMovement PlayerScript { get; private set; }
    public GameObject PlayerObject { get; private set; }

    public Action sceneLoaded;

    AsyncOperation loadSceneOperation;

    void Awake()
    {
        if (GameManager.Instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            Debug.Log("GameManager singleton has been instantiated");

            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        PlayerScript = FindObjectOfType<PlayerMovement>();
        PlayerObject = PlayerScript.gameObject;

        sceneLoaded += OnSceneLoad;

        StartCoroutine(Utils.DoAfterAFrame(sceneLoaded.Invoke));
    }

    public void LoadNextScene()
    {
        int nextSceneIndex = _currentSceneIndex + 1;

        if (nextSceneIndex < _scenesLoadOrder.Count)
        {
            Debug.Log($"Started loading scene {_scenesLoadOrder[nextSceneIndex]}");

            PlayerScript.DisablePlayer();

            SceneManager.LoadScene(_loadscreenScene, LoadSceneMode.Single);
            StartCoroutine(AsyncLoadScene(_scenesLoadOrder[nextSceneIndex]));

            _currentSceneIndex++;
        }
        else
        {
            Debug.Log("No more scenes in load order");
        }

    }

    IEnumerator AsyncLoadScene(string SceneName)
    {
        loadSceneOperation = SceneManager.LoadSceneAsync(SceneName);

        while (!loadSceneOperation.isDone)
        {
            yield return null;
        }

        sceneLoaded?.Invoke();
    }

    void OnSceneLoad()
    {
        Debug.Log("New scene loaded");

        _currentSceneInfo = FindObjectOfType<SceneInfo>();

        if (_currentSceneInfo != null)
        {
            CheckpointPosition = _currentSceneInfo.PlayerSpawnPosition;
        }
        else
        {
            Debug.LogWarning("Scene info wasn't found");
            CheckpointPosition = Vector2.zero;
        }

        DoorScript doorScript = FindObjectOfType<DoorScript>();

        if (doorScript != null)
        {
            ExitPosition = doorScript.transform.position;
        }
        else
        {
            Debug.LogWarning("DoorScript wasn't found");
            ExitPosition = Vector2.zero;
        }

        StartCoroutine(PlayerScript.RespawnPlayerAtCheckpoint(1.0f));
    }
}
