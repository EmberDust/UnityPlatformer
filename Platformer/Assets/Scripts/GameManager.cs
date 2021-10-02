using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Player Rules")]
    [SerializeField] float _playerSpawnDelay = 0.25f;
    [SerializeField] float _playerRespawnDelay = 1.0f;
    [SerializeField] float _fallHeight = -10f;

    [Header("Level Transition")]
    [SerializeField] float _delayBeforeFadeOut = 0.4f;
    [SerializeField] float _delayAfterFadeOutTriggered  = 0.4f;

    [Header("Levels")]
    [SerializeField] bool _loopLevels = false;
    [SerializeField] List<string> _scenesLoadOrder;

    // Singleton
    public static GameManager Instance { get; private set; }

    public int CurrentCollectablesScore { get; set; }
    public Vector2 CheckpointPosition  { get; set; }
    public Vector2 ExitPosition        { get; set; }
    public Checkpoint ActiveCheckpoint { get; private set; }

    public Player     PlayerScript { get; private set; }
    public GameObject PlayerObject { get; private set; }

    public Action sceneLoaded;
    public Action sceneEnded;
    public Action checkPointReached;

    string _currentSceneName;
    int    _currentSceneIndex = 0;

    float _currentSceneTimer = 0.0f;
    bool _pauseSceneTimer = false;

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

        FindPlayer();

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

    void Update()
    {
        if (!_pauseSceneTimer)
        {
            _currentSceneTimer += Time.deltaTime;
        }

        TimeSpan formattedTime = TimeSpan.FromMilliseconds(_currentSceneTimer * 1000);

        GlobalText.Instance.AppendText(formattedTime.ToString("mm':'ss'.'ff"));
        GlobalText.Instance.AppendText("Current Score: " + CurrentCollectablesScore.ToString());
    }

    public void AddCollectablePoint()
    {

    }

    public void RemoveCollectablePoint()
    {

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

        checkPointReached?.Invoke();
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

        FindPlayerSpawn();
        FindLevelExit();

        if (_transitionAnimator != null)
        {
            _transitionAnimator.SetTrigger("FadeInTriggered");
        }

        _currentSceneTimer = 0.0f;
        _pauseSceneTimer = false;

        StartCoroutine(SpawnPlayerAfterDelay(_playerSpawnDelay));
    }

    void FindPlayer()
    {
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
    }

    void FindPlayerSpawn()
    {
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
    }

    void FindLevelExit()
    {
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
    }

    void OnSceneEnd()
    {
        PlayerScript.DisablePlayer();

        _pauseSceneTimer = true;

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
