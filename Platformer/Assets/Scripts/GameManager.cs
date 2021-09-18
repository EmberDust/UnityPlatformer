using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] float _delayBeforeFadeOut = 0.4f;
    [SerializeField] float _delayAfterFadeOutStarted  = 0.4f;

    [SerializeField] bool _loopLevels = false;
    [SerializeField] List<string> _scenesLoadOrder;

    [SerializeField] float _fallHeight = -10f;

    // Singleton
    public static GameManager Instance { get; private set; }

    public Vector2 CheckpointPosition { get; set; }
    public Vector2 ExitPosition       { get; set; }

    // Cached player info for other scripts to use
    public PlayerMovement PlayerScript { get; private set; }
    public GameObject     PlayerObject { get; private set; }

    public Action sceneLoaded;
    public Action sceneEnded;

    string _currentSceneName;
    int    _currentSceneIndex = 0;

    Animator _transitionAnimator;

    AsyncOperation _loadSceneOperator;
    bool _fadeOutFinished = true;

    void Awake()
    {
        if (GameManager.Instance != null)
        {
            Destroy(gameObject);
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
        _transitionAnimator = GetComponentInChildren<Animator>();

        PlayerScript = FindObjectOfType<PlayerMovement>();
        PlayerObject = PlayerScript.gameObject;

        sceneLoaded += OnSceneLoad;
        sceneEnded += OnSceneEnd;

        StartCoroutine(Utils.DoAfterAFrame(sceneLoaded.Invoke));
    }

    void Update()
    {
        if (PlayerObject.transform.position.y < _fallHeight)
        {
            PlayerScript.Kill();
        }
    }

    public void LoadNextScene()
    {
        int nextSceneIndex = _currentSceneIndex + 1;

        if (nextSceneIndex < _scenesLoadOrder.Count)
        {
            StartCoroutine(LoadScene(_scenesLoadOrder[nextSceneIndex]));
        }
        else if (_loopLevels)
        {
            StartCoroutine(LoadScene(_scenesLoadOrder[0]));
        }
        else 
        {
            Debug.Log("No more scenes in load order");
        }
    }

    IEnumerator LoadScene(string sceneName)
    {
        sceneEnded?.Invoke();

        while (!_fadeOutFinished)
        {
            yield return null;
        }

        _loadSceneOperator = SceneManager.LoadSceneAsync(sceneName);

        while (!_loadSceneOperator.isDone)
        {
            yield return null;
        }

        sceneLoaded?.Invoke();
    }

    void OnSceneLoad()
    {
        _currentSceneName = SceneManager.GetActiveScene().name;
        _currentSceneIndex = _scenesLoadOrder.IndexOf(_currentSceneName);

        Debug.Log($"Scene name: {_currentSceneName} Index: {_currentSceneIndex}. Loaded.");

        PlayerSpawn playerSpawn = FindObjectOfType<PlayerSpawn>();

        if (playerSpawn != null)
        {
            CheckpointPosition = playerSpawn.transform.position;
        }
        else
        {
            Debug.LogWarning("Player Spawn wasn't found");
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

        _transitionAnimator.SetTrigger("FadeInTriggered");

        StartCoroutine(PlayerScript.RespawnPlayerAtCheckpoint(0.2f));
    }

    void OnSceneEnd()
    {
        PlayerScript.DisablePlayer();

        _fadeOutFinished = false;
        StartCoroutine(FadeOutToNextScene());
    }

    IEnumerator FadeOutToNextScene()
    {
        yield return new WaitForSeconds(_delayBeforeFadeOut);

        _transitionAnimator.SetTrigger("FadeOutTriggered");

        yield return new WaitForSeconds(_delayAfterFadeOutStarted);

        _fadeOutFinished = true;
    }
}
