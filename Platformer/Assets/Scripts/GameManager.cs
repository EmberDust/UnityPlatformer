using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Player rules")]
    [SerializeField] float _playerSpawnDelay = 0.25f;
    [SerializeField] float _playerRespawnDelay = 1.0f;
    [SerializeField] float _fallHeight = -10f;

    [Header("Level transitions")]
    [SerializeField] float _delayBeforeFadeOut = 0.4f;
    [SerializeField] float _delayAfterFadeOutTriggered  = 0.4f;

    [Header("Levels")]
    [SerializeField] bool _loopLevels = false;
    [SerializeField] List<string> _scenesLoadOrder;

    // Singleton
    public static GameManager Instance { get; private set; }

    public Vector2 CheckpointPosition  { get; set; }
    public Vector2 ExitPosition        { get; set; }
    public Checkpoint ActiveCheckpoint { get; private set; }

    public Player     PlayerScript { get; private set; }
    public GameObject PlayerObject { get; private set; }

    public Action sceneLoaded;
    public Action sceneEnded;

    string _currentSceneName;
    int    _currentSceneIndex = 0;

    Animator _transitionAnimator;

    AsyncOperation _loadSceneOperation;
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

        PlayerScript = FindObjectOfType<Player>();
        if (PlayerScript != null)
        {
            PlayerObject = PlayerScript.gameObject;

            PlayerScript.playerDied += OnPlayerDeath;
        }
        else
        {
            Debug.LogWarning("Object with Player Script wasn't found");
        }

        sceneLoaded += OnSceneLoad;
        sceneEnded += OnSceneEnd;

        StartCoroutine(Utils.DoAfterAFrame(sceneLoaded.Invoke));
    }

    void FixedUpdate()
    {
        if (PlayerObject.transform.position.y < _fallHeight)
        {
            PlayerScript.Kill();
        }
    }

    public void SetNewCheckpoint(Checkpoint newCheckpoint)
    {
        if (ActiveCheckpoint != null)
        {
            ActiveCheckpoint.IsActive = false;
        }

        newCheckpoint.IsActive = true;
        ActiveCheckpoint = newCheckpoint;

        CheckpointPosition = newCheckpoint.transform.position;
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

    public IEnumerator LoadScene(string sceneName)
    {
        sceneEnded?.Invoke();

        while (!_fadeOutFinished)
        {
            yield return null;
        }

        _loadSceneOperation = SceneManager.LoadSceneAsync(sceneName);

        while (!_loadSceneOperation.isDone)
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

        ExitScript exitScript = FindObjectOfType<ExitScript>();

        if (exitScript != null)
        {
            ExitPosition = exitScript.transform.position;
        }
        else
        {
            Debug.LogWarning("Exit Script wasn't found");
            ExitPosition = Vector2.zero;
        }

        if (_transitionAnimator != null)
        {
            _transitionAnimator.SetTrigger("FadeInTriggered");
        }

        StartCoroutine(SpawnPlayerAfterDelay(_playerSpawnDelay));
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

        if (_transitionAnimator != null)
        {
            _transitionAnimator.SetTrigger("FadeOutTriggered");
        }

        yield return new WaitForSeconds(_delayAfterFadeOutTriggered);

        _fadeOutFinished = true;
    }

    void OnPlayerDeath()
    {
        StartCoroutine(SpawnPlayerAfterDelay(_playerRespawnDelay));
    }

    IEnumerator SpawnPlayerAfterDelay(float secondsDelay)
    {
        yield return new WaitForSeconds(secondsDelay);

        PlayerObject.transform.position = CheckpointPosition;
        PlayerScript.EnablePlayer();
    }
}
