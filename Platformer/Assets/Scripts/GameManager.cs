using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }

    [Header("Player Rules")]
    [SerializeField] float _playerSpawnDelay = 0.25f;
    [SerializeField] float _playerRespawnDelay = 1.0f;
    [SerializeField] float _fallHeight = -10f;

    [Header("Level Transition")]
    [SerializeField] float _delayBeforeFadeOut = 0.4f;
    [SerializeField] float _delayAfterFadeOutTriggered  = 0.4f;

    [Header("Pause Screen")]
    [SerializeField] float _transitionToPauseDuration = 0.2f;
    [SerializeField] float _transitionToPauseTimeStep = 0.02f;

    [Header("Levels")]
    [SerializeField] bool _loopLevels = false;
    [SerializeField] List<string> _scenesLoadOrder;

    public bool IsGamePaused { get; private set; }
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

    bool _currentlyInPauseTransition;
    ColorAdjustments _pauseColorAdjustment;

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
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        _transitionAnimator = GetComponentInChildren<Animator>();

        Volume pauseVolume = GetComponentInChildren<Volume>();
        pauseVolume.profile.TryGet(out _pauseColorAdjustment);

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

        if (!_pauseSceneTimer)
        {
            _currentSceneTimer += Time.deltaTime;
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Pause"))
        {
            TogglePause();
        }

        TimeSpan formattedTime = TimeSpan.FromMilliseconds(_currentSceneTimer * 1000);

        GlobalText.Instance.AppendText(formattedTime.ToString("mm':'ss'.'ff"));
        GlobalText.Instance.AppendText("Current Score: " + CurrentCollectablesScore.ToString());
    }

    #region Scene Management
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
    #endregion

    #region Checkpoints\Respawn
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
    #endregion

    #region Pause Screen
    void TogglePause()
    {
        if (!_currentlyInPauseTransition)
        {
            if (IsGamePaused)
            {
                StartCoroutine(PauseTransition(1.0f, 5.0f, false, false));
            }
            else
            {
                StartCoroutine(PauseTransition(0.0f, -75.0f, true, true));
            }
        }
    }

    IEnumerator PauseTransition(float finalTimeScale, float finalSaturation, bool gamePausedAfter, bool volumeEnabledAfter)
    {
        _currentlyInPauseTransition = true;

        _pauseColorAdjustment.active = true;
        float startingSaturation = _pauseColorAdjustment.saturation.value;

        float startingTimeScale = Time.timeScale;

        float totalTimeSteps = _transitionToPauseDuration / _transitionToPauseTimeStep;

        for (int currentTimeStep = 1; currentTimeStep < totalTimeSteps; currentTimeStep++)
        {
            _pauseColorAdjustment.saturation.value = Mathf.Lerp(startingSaturation, finalSaturation, currentTimeStep / totalTimeSteps);
            Time.timeScale = Mathf.Lerp(startingTimeScale, finalTimeScale, currentTimeStep / totalTimeSteps);

            yield return new WaitForSeconds(_transitionToPauseTimeStep);
        }

        Time.timeScale = finalTimeScale;
        _pauseColorAdjustment.saturation.value = finalSaturation;

        IsGamePaused = gamePausedAfter;
        _pauseColorAdjustment.active = volumeEnabledAfter;

        _currentlyInPauseTransition = false;
    }
    #endregion
}
