using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Singleton
    static PlayerMovement Instance { get; set; }

    [SerializeField]float _timeScale = 1.0f;

    [Space]
    [Header("Horizontal Movement Values")]
    [SerializeField] float _baseDeceleration = 0.75f;
    [SerializeField] float _baseAcceleration = 1.5f;
    [SerializeField] float _thresholdVelocity = 4.0f;
    [SerializeField] float _thresholdDeceleration = 0.75f;

    [Header("Jump")]
    [SerializeField] int   _additionalJumps =  1;
    [SerializeField] float _jumpSpeed = 5f;
    [SerializeField] float _fallingVelocityLimit = -7f;

    [Header("Walljump")]
    [SerializeField] int _walljumpAccelerationFrames = 5;
    [SerializeField] float _walljumpAcceleration = 2.0f;

    [Header("Gravity Scales")]
    [SerializeField] float _baseGravityScale        = 2f;
    [SerializeField] float _ascendingGravityScale   = 1f;
    [SerializeField] float _fallingGravityScale     = 3f;
    [SerializeField] float _wallSlideGravityScale = 0.2f;

    [Space]
    [Header("Ground Check")]
    [SerializeField] Transform _groundCheckPoint;
    [SerializeField] float _coyoteTime = 0.1f;
    [SerializeField] LayerMask _groundMask;

    [Space]
    [Header("Wall Check")]
    [SerializeField] Transform _wallCheckPoint;
    [SerializeField] LayerMask _wallMask;

    public event Action<int, Vector2> playerWalljumped;
    public event Action playerGroundjumped;
    public event Action playerMultijumped;
    public event Action playerHasBeenDisabled;
    public event Action playerHasBeenEnabled;
    public event Action playerDied;

    public bool Grounded      { get; private set; }
    public bool Jumping       { get; private set; }
    public bool Falling       { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsDisabled    { get; private set; }

    // Player state
    Vector2 _velocityChange;

    int _facingDirection = 1;
    int _jumpsLeft = 0;

    float _timeLastGrounded = -10.0f;
    int _walljumpFrame = 0;

    // Wall slide values
    Vector2 _touchedWallPosition = new Vector2(0f, 0f);
    bool _isTouchingWall = false;
    int _wallDirection = 0; 

    bool _wallSlideStoppedFall = false;

    // Cached components
    PlayerInputs _inputs;
    Rigidbody2D _rb;
    List<Collider2D> _colliders;

    // DEBUG VALUES
    System.Text.StringBuilder _debugString = new System.Text.StringBuilder();

    void Awake()
    {
        if (PlayerMovement.Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        _rb     = GetComponent<Rigidbody2D>();
        _inputs = GetComponent<PlayerInputs>();

        _colliders = GetComponents<Collider2D>().ToList();

        DontDestroyOnLoad(gameObject);

        DisablePlayer();
    }

    void Update()
    {
        if (!IsDisabled)
        {
            FaceMovementDirection();
        }
    }

    void FixedUpdate()
    {
        Time.timeScale = _timeScale;

        _debugString.Clear();

        if (!IsDisabled)
        {
            _rb.gravityScale = _baseGravityScale;
            _velocityChange = new Vector2(0.0f, 0.0f);

            // State changes, without directly affecting physics or logic
            GroundCheck();
            UpdateFallingState();
            WallCheck();
            WallSlideCheck();

            // Physics, depending on state and inputs
            HandleHorizontalAcceleration();
            HandleVerticalMovement();
            UpdateGravityScales();

            // Apply velocity changes
            _rb.velocity += _velocityChange;

            // Clamp velocity values (for now only falling velocity)
            float clampedVerticalVelocity = Mathf.Max(_rb.velocity.y, _fallingVelocityLimit);
            _rb.velocity = new Vector2(_rb.velocity.x, clampedVerticalVelocity);
        }

        // DEBUG
        _debugString.AppendLine($"horizontal velocity: {_rb.velocity.x}");
        _debugString.AppendLine($"vertical velocity  : {_rb.velocity.y}");
        _debugString.AppendLine($"gravity scale      : {_rb.gravityScale}");
        GlobalText.Instance.Show(_debugString.ToString());
    }

    public void DisablePlayer()
    {
        if (!IsDisabled)
        {
            IsDisabled = true;

            _rb.velocity = Vector2.zero;
            _rb.isKinematic = true;

            foreach(var collider in _colliders)
            {
                collider.enabled = false;
            }

            playerHasBeenDisabled?.Invoke();
        }
    }

    public void EnablePlayer()
    {
        if (IsDisabled)
        {
            IsDisabled = false;
            _rb.isKinematic = false;

            foreach (var collider in _colliders)
            {
                collider.enabled = true;
            }

            playerHasBeenEnabled?.Invoke();
        }
    }

    public void Kill()
    {
        if (!IsDisabled)
        {
            playerDied?.Invoke();

            DisablePlayer();

            StartCoroutine(RespawnPlayerAtCheckpoint(1f));
        }
    }

    public IEnumerator RespawnPlayerAtCheckpoint(float secondsDelay = 0f)
    {
        yield return new WaitForSeconds(secondsDelay);

        _rb.velocity = Vector2.zero;
        transform.position = GameManager.Instance.CheckpointPosition;

        EnablePlayer();
    }

    public void GiveVelocityBoost(Vector2 boostAmount, bool resetGravity = false)
    {
        // Horizontal boost applied in the direction of current horizontal velocity
        Vector2 newVelocity = new Vector2(_rb.velocity.x + Mathf.Sign(_rb.velocity.x) * boostAmount.x,
                                          _rb.velocity.y + boostAmount.y);

        if (resetGravity)
        {
            newVelocity.y = Mathf.Max(newVelocity.y, boostAmount.y);
        }

        _rb.velocity = newVelocity;
    }

    public void GiveMultijumpCharges(int multijumpCharges, bool ignoreLimit = false)
    {
        if (ignoreLimit)
        {
            _jumpsLeft += multijumpCharges;
        }
        else
        {
            _jumpsLeft = Mathf.Min(_additionalJumps, _jumpsLeft + multijumpCharges);
        }
    }

    void HandleHorizontalAcceleration()
    {
        float horizontalVelocity = _rb.velocity.x;
        float velocityDirection = Mathf.Sign(horizontalVelocity);

        // Deceleration, can't exceed current velocity to prevent character from becoming a pendulum
        _velocityChange.x += Mathf.Min(_baseDeceleration, Mathf.Abs(horizontalVelocity)) * -velocityDirection;

        // Horizontal acceleration
        float acceleration = _baseAcceleration;
        float accelerationDirection = 0.0f;

        bool isDuringWallJump = _walljumpFrame < _walljumpAccelerationFrames;

        if (!isDuringWallJump)
        {
            // Regular acceleration, if player is not wall sliding
            if (!IsWallSliding)
            {
                accelerationDirection = _inputs.HorizontalInput;
            }
        }
        else
        {
            // Walljump overrides input acceleration for duration
            _walljumpFrame++;
            acceleration = _walljumpAcceleration;
            accelerationDirection = -_wallDirection;
        }

        _velocityChange.x += acceleration * accelerationDirection;

        // Soft limit player horizontal velocity
        float velocityExcess = Mathf.Abs(horizontalVelocity + _velocityChange.x) - _thresholdVelocity;
        if (velocityExcess > 0.0f)
        {
            float velocityExcessDirection = Mathf.Sign(horizontalVelocity + _velocityChange.x);
            // Don't drop velocity below threshold value
            _velocityChange.x += Mathf.Min(_thresholdDeceleration, velocityExcess) * -velocityExcessDirection;
        }
    }

    void HandleVerticalMovement()
    {
        float newVerticalVelocity = _rb.velocity.y;

        // Brake the fall speed on wall slide
        if (IsWallSliding && Falling && !_wallSlideStoppedFall)
        {
            newVerticalVelocity = 0.0f;
            _wallSlideStoppedFall = true;
        }

        // Rules for different kinds of jump
        bool groundjumpPossible = _timeLastGrounded + _coyoteTime > Time.time;
        bool walljumpPossible = IsWallSliding;
        bool multijumpPossible = _jumpsLeft > 0;

        bool jumpPossible = groundjumpPossible || walljumpPossible || multijumpPossible;

        bool jumpInput = _inputs.ConsumeJumpInput();

        if (jumpInput && jumpPossible)
        {
            // Regular jump from ground here
            if (groundjumpPossible)
            {
                playerGroundjumped?.Invoke();
            }
            else
            {
                // Wall jump here
                if (walljumpPossible && _inputs.WallGrabPressed)
                {
                    _walljumpFrame = 0;

                    playerWalljumped?.Invoke(_wallDirection, _touchedWallPosition);
                }
                // Multi jump here
                else
                {
                    _jumpsLeft--;

                    playerMultijumped?.Invoke();
                }
            }

            newVerticalVelocity = _jumpSpeed;

            _timeLastGrounded = -1.0f;
        }

        _rb.velocity = new Vector2(_rb.velocity.x, newVerticalVelocity);
    }

    void UpdateGravityScales()
    {
        // -Can we have a state machine? -We have a state machine at home
        // State machine at home:

        // Changing gravity scale for better game feel
        if (Jumping)
        {
            // If player holds jump button - increase jump height
            if (_inputs.JumpPressed)
                _rb.gravityScale = _ascendingGravityScale;
        }
        else if (IsWallSliding)
        {
            _rb.gravityScale = _wallSlideGravityScale;
        }
        else if (Falling)
        {
            _rb.gravityScale = _fallingGravityScale;
        }
    }

    void GroundCheck()
    {
        var currentlyGrounded = Physics2D.Raycast(transform.position,
                _groundCheckPoint.position - transform.position,
                Vector2.Distance(transform.position, _groundCheckPoint.position),
                _groundMask);

        if (currentlyGrounded)
        {
            // Refresh jump values
            _jumpsLeft = _additionalJumps;

            _timeLastGrounded = Time.time;
        }

        Grounded = currentlyGrounded;
    }

    void WallCheck()
    {
        var touchedWall = Physics2D.Raycast(transform.position,
                _wallCheckPoint.position - transform.position,
                Vector2.Distance(transform.position, _wallCheckPoint.position),
                _wallMask);

        _isTouchingWall = touchedWall;

        if (touchedWall)
        {
            _wallDirection = _facingDirection;
            _touchedWallPosition = touchedWall.point;
        }
    }

    void WallSlideCheck()
    {
        bool suitableForWallSlide = _isTouchingWall && !Grounded;

        if (suitableForWallSlide && _inputs.WallGrabPressed)
        {
            IsWallSliding = true;
        }
        else
        {
            IsWallSliding = false;
            _wallSlideStoppedFall = false;
        }
    }

    void UpdateFallingState()
    {
        Jumping = false;
        Falling = false;

        if (_rb.velocity.y > 0.005f)
        {
            Jumping = true;
        }

        if (_rb.velocity.y < -0.005f)
        {
            Falling = true;
        }
    }

    void FaceMovementDirection()
    {
        if (!IsWallSliding)
        {
            if (_inputs.HorizontalInput < 0.0f && _facingDirection == 1)
            {
                _facingDirection = -1;
                transform.eulerAngles = new Vector3(0f, 180f, 0f);
            }

            if (_inputs.HorizontalInput > 0.0f && _facingDirection == -1)
            {
                _facingDirection = 1;
                transform.eulerAngles = new Vector3(0f, 0f, 0f);
            }
        }
    }
}


