using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // Singleton
    static Player Instance { get; set; }

    #region Serialized Fields
    [Space]
    [Header("Horizontal Movement Values")]
    [SerializeField] float _baseDeceleration      = 1.0f;
    [SerializeField] float _baseAcceleration      = 1.5f;
    [SerializeField] float _thresholdVelocity     = 3.25f;
    [SerializeField] float _thresholdDeceleration = 0.505f;

    [Header("Ground Slide")]
    [SerializeField] float _slideDeceleration = 0.1f;
    [SerializeField] float _slideAcceleration = 0.0f;
    [SerializeField] float _slideThresholdDeceleration = 0.1f;

    [Header("Jump")]
    [SerializeField] int   _additionalJumps =  1;
    [SerializeField] float _jumpSpeed = 4f;

    [Header("Dash")]
    [SerializeField] Vector2 _dashVelocityMultipliers = new Vector2(0.25f, 0.90f);
    [SerializeField] AnimationCurve _dashVelocityCurve;

    [Header("Walljump")]
    [SerializeField] Vector2 _walljumpVelocityMultipliers = new Vector2(1.0f, 1.0f);
    [SerializeField] AnimationCurve _walljumpVelocityCurve;

    [Header("Gravity Scales")]
    [SerializeField] float _baseGravityScale      = 2.75f;
    [SerializeField] float _ascendingGravityScale = 0.66f;
    [SerializeField] float _fallingGravityScale   = 2.75f;
    [SerializeField] float _wallSlideGravityScale = 0.15f;

    [Header("Clamp Velocity")]
    [SerializeField] float _maximumHorizontalVelocity = 10.0f;
    [SerializeField] float _maximumVerticalVelocity   =  6.0f;
    [SerializeField] float _minimumVerticalVelocity   = -7.0f;

    [Header("Ground Check")]
    [SerializeField] Transform _groundCheckPoint;
    [SerializeField] float _coyoteTime = 0.1f;
    [SerializeField] LayerMask _groundMask;

    [Header("Wall Check")]
    [SerializeField] Transform _wallCheckPoint;
    [SerializeField] LayerMask _wallMask;
    #endregion

    #region Public Properties\Events
    public event Action<int, Vector2> playerWalljumped;
    public event Action<DashPoint> playerDashed;
    public event Action playerGroundjumped;
    public event Action playerMultijumped;
    public event Action playerHasBeenDisabled;
    public event Action playerHasBeenEnabled;
    public event Action playerDied;

    public bool IsGrounded    { get; private set; }
    public bool IsJumping     { get; private set; }
    public bool IsFalling     { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsDisabled    { get; private set; }
    public bool IsGroudSliding { get { return _inputs.SlidePressed && IsGrounded; } }

    ///<summary>Is character movement is controled by AnimationCurve</summary>
    public bool IsMovementOverridenByCurve { get => _currentCurve != null && _timeOnCurve < _currentCurve.keys.Last().time; }
    #endregion

    #region Local Variables
    // Player State
    Vector2 _velocityChange;

    int _facingDirection = 1;
    int _jumpsLeft = 0;

    float _timeLastGrounded = -10.0f;

    // Wall Slide Values
    Vector2 _touchedWallPosition = new Vector2(0f, 0f);
    bool _isTouchingWall = false;
    int _wallDirection = 0; 

    bool _wallSlideStoppedFall = false;

    // Values Used If Movement Controlled By Curve
    AnimationCurve _currentCurve;
    Vector2 _curveVelocityVector;
    float _timeOnCurve;

    // Cached Components
    PlayerInputs _inputs;
    Rigidbody2D _rb;
    List<Collider2D> _colliders;
    #endregion

    #region Unity Callbacks
    void Awake()
    {
        if (Player.Instance != null)
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
        GlobalText.Instance.AppendText($"Player velocity: {_rb.velocity}");
    }

    void FixedUpdate()
    {
        if (!IsDisabled)
        {
            _rb.gravityScale = _baseGravityScale;
            _velocityChange = new Vector2(0.0f, 0.0f);

            // State changes, without directly affecting physics or logic
            FaceMovementDirection();
            GroundCheck();
            UpdateFallingState();
            WallCheck();
            WallSlideCheck();

            // Movement, depending on state and inputs
            if (IsMovementOverridenByCurve)
            {
                ChangeVelocityByCurve();
            }
            else
            {
                // Regular movement
                HandleHorizontalAcceleration();
                HandleVerticalMovement();
                HandleDashing();
                UpdateGravityScales();
            }

            _rb.velocity += _velocityChange;

            ClampVelocity();
        }
    }
    #endregion

// Public Regions
    #region Player Activation
    public void DisablePlayer()
    {
        if (!IsDisabled)
        {
            IsDisabled = true;

            _rb.velocity = Vector2.zero;
            _rb.isKinematic = true;

            _currentCurve = null;

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
            DisablePlayer();

            playerDied?.Invoke();
        }
    }
#endregion

    #region Give Boosts
    public void GiveVelocityBoost(Vector2 boostAmount, bool overrideVertical = false)
    {
        // Horizontal boost applied in the direction of current horizontal velocity
        float newHorizontalVelocity = _rb.velocity.x + Mathf.Sign(_rb.velocity.x) * Mathf.Abs(boostAmount.x);
        float newVerticalVelocity = overrideVertical ? boostAmount.y : _rb.velocity.y + boostAmount.y;

        _rb.velocity = new Vector2(newHorizontalVelocity, newVerticalVelocity);
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
    #endregion

//Private Regions
    #region Player Movement
    void HandleHorizontalAcceleration()
    {
        float deceleration;
        float acceleration;
        float thresholdDeceleration;

        if (IsGroudSliding)
        {
            deceleration = _slideDeceleration;
            acceleration = _slideAcceleration;
            thresholdDeceleration = _slideThresholdDeceleration;
        }
        else
        {
            deceleration = _baseDeceleration;
            acceleration = _baseAcceleration;
            thresholdDeceleration = _thresholdDeceleration;
        }

        float horizontalVelocity = _rb.velocity.x;
        float velocityDirection = Mathf.Sign(horizontalVelocity);

        // Deceleration, can't exceed current velocity to prevent character from becoming a pendulum
        _velocityChange.x += Mathf.Min(deceleration, Mathf.Abs(horizontalVelocity)) * -velocityDirection;

        // Horizontal acceleration
        float accelerationDirection = 0.0f;

        // Regular acceleration, if player is not wall sliding
        if (!IsWallSliding)
        {
            accelerationDirection = _inputs.HorizontalInput;
        }

        _velocityChange.x += acceleration * accelerationDirection;

        // Soft limit player horizontal velocity
        float velocityExcess = Mathf.Abs(horizontalVelocity + _velocityChange.x) - _thresholdVelocity;
        if (velocityExcess > 0.0f)
        {
            float velocityExcessDirection = Mathf.Sign(horizontalVelocity + _velocityChange.x);
            // Don't drop velocity below threshold value
            _velocityChange.x += Mathf.Min(thresholdDeceleration, velocityExcess) * -velocityExcessDirection;
        }
    }

    void HandleVerticalMovement()
    {
        float newVerticalVelocity = _rb.velocity.y;

        // Brake the fall speed on wall slide
        if (IsWallSliding && IsFalling && !_wallSlideStoppedFall)
        {
            newVerticalVelocity = 0.0f;
            _wallSlideStoppedFall = true;
        }

        // Rules for different kinds of jump
        bool groundjumpPossible = _timeLastGrounded + _coyoteTime > Time.time;
        bool walljumpPossible = IsWallSliding;
        bool multijumpPossible = _jumpsLeft > 0;

        bool jumpPossible = groundjumpPossible || walljumpPossible || multijumpPossible;

        if (_inputs.JumpInput && jumpPossible)
        {
            _inputs.ConsumeJumpInput();

            if (groundjumpPossible)
            {
                playerGroundjumped?.Invoke();
            }
            else
            {
                if (walljumpPossible && _inputs.WallGrabPressed)
                {
                    SetCurrentVelocityCurve(_walljumpVelocityCurve, _walljumpVelocityMultipliers, new Vector2(-_wallDirection, 1.0f));

                    playerWalljumped?.Invoke(_wallDirection, _touchedWallPosition);
                }
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

    void SetCurrentVelocityCurve(AnimationCurve velocityCurve, Vector2 velocityMultipliers, Vector2 velocityDirection)
    {
        _currentCurve = velocityCurve;
        _curveVelocityVector = new Vector2(velocityMultipliers.x * velocityDirection.x,
                                           velocityMultipliers.y * velocityDirection.y);
        _timeOnCurve = 0.0f;

        _rb.velocity = new Vector2(_rb.velocity.x, 0.0f);
    }

    void ChangeVelocityByCurve()
    {
        _velocityChange += _currentCurve.Evaluate(_timeOnCurve) * _curveVelocityVector;
        _timeOnCurve += 1.0f;
    }

    void HandleDashing()
    {
        if (_inputs.DashInput)
        {
            if (DashPointsManager.Instance.ClosestInRange != null)
            {
                _inputs.ConsumeDashInput();

                DashPoint dashPoint = DashPointsManager.Instance.ClosestInRange;

                // To make dash more consistent - limit number of possible dash directions
                Vector2 dashDirection = new Vector2(Mathf.Sign(dashPoint.transform.position.x - transform.position.x),
                                                    Mathf.Sign(dashPoint.transform.position.y - transform.position.y));
                dashDirection.Normalize();

                SetCurrentVelocityCurve(_dashVelocityCurve, _dashVelocityMultipliers, dashDirection);

                playerDashed?.Invoke(dashPoint);
            }
        }
    }

    void ClampVelocity()
    {
        float clampedHorizontalVelocity = Mathf.Clamp(_rb.velocity.x, -_maximumHorizontalVelocity, _maximumHorizontalVelocity);
        float clampedVerticalVelocity = Mathf.Clamp(_rb.velocity.y, _minimumVerticalVelocity, _maximumVerticalVelocity);
        _rb.velocity = new Vector2(clampedHorizontalVelocity, clampedVerticalVelocity);
    }
    #endregion

    #region Player State
    void UpdateGravityScales()
    {
        // -Can we have a state machine? -We have a state machine at home
        // State machine at home:

        // Changing gravity scale for better game feel
        if (IsJumping)
        {
            // Increase jump height by reducing the gravity
            if (_inputs.JumpPressed || _inputs.DashPressed)
            {
                _rb.gravityScale = _ascendingGravityScale;
            }
        }
        else if (IsWallSliding)
        {
            // Reduce fall acceleration during wall slide
            _rb.gravityScale = _wallSlideGravityScale;
        }
        else if (IsFalling)
        {
            // Increase fall acceleration during free fall to remove floatiness
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

        IsGrounded = currentlyGrounded;
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
        bool satisfyWallSlideConditions = _isTouchingWall && !IsGrounded;

        if (satisfyWallSlideConditions && _inputs.WallGrabPressed)
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
        IsJumping = false;
        IsFalling = false;

        if (_rb.velocity.y > 0.005f)
        {
            IsJumping = true;
        }
        else if (_rb.velocity.y < -0.005f)
        {
            IsFalling = true;
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
    #endregion
}
