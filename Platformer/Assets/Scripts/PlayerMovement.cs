using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
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
    public event Action playerDied;

    public bool Grounded      { get; private set; }
    public bool Jumping       { get; private set; }
    public bool Falling       { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsDead        { get; private set; }

    // Player state
    Vector3 _spawnPoint;

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

    // Cashed components
    PlayerInputs _inputs;
    Rigidbody2D _rb;

    // DEBUG VALUES
    System.Text.StringBuilder _debugString = new System.Text.StringBuilder();

    void Start()
    {
        _rb     = GetComponent<Rigidbody2D>();
        _inputs = GetComponent<PlayerInputs>();

        _spawnPoint = transform.position;
    }

    void Update()
    {
        if (!IsDead)
        {
            FaceMovementDirection();
        }
    }

    void FixedUpdate()
    {
        Time.timeScale = _timeScale;

        _debugString.Clear();

        if (!IsDead)
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
        _debugString.AppendLine($"Total velocity: {_rb.velocity.x}");
        GlobalText.Instance.Show(_debugString.ToString());
    }

    public void Kill()
    {
        if (!IsDead)
        {
            IsDead = true;

            _rb.velocity = Vector2.zero;
            _rb.isKinematic = true;

            playerDied?.Invoke();

            StartCoroutine(RespawnPlayerAfterDelay(1f));
        }
    }

    IEnumerator RespawnPlayerAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        transform.position = _spawnPoint;
        IsDead = false;
        _rb.isKinematic = false;
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

