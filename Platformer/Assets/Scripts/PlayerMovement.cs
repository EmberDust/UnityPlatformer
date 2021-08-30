using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]float timeScale = 1.0f;

    [Header("Jump")]
    [SerializeField] int   _additionalJumps = 1;
    [SerializeField] float _jumpSpeed       = 5f;

    [Header("Walljump")]
    [SerializeField] int _walljumpAccelerationFrames = 5;
    [SerializeField] float _walljumpAcceleration = 2.0f;
    [SerializeField] float _walljumpWindow = 0.2f;

    [Space]
    [SerializeField] float _baseGravityScale      = 2f;
    [SerializeField] float _ascendingGravityScale = 1f;
    [SerializeField] float _fallingGravityScale   = 3f;

    [Space]
    [Header("Horizontal Movement Values")]
    [SerializeField] float _baseDeceleration = 0.75f;
    [SerializeField] float _baseAcceleration = 1.5f;
    [SerializeField] float _thresholdVelocity = 4.0f;
    [SerializeField] float _thresholdDeceleration = 0.75f;

    [Space]
    [Header("Ground Check")]
    [SerializeField] Transform _groundCheckPoint;
    [SerializeField] float _coyoteTime = 0.1f;
    [SerializeField] LayerMask _groundMask;

    [Space]
    [Header("Wall Hanging")]
    [SerializeField] Transform _wallCheckPoint;
    [SerializeField] float _wallHangingGravityScale = 0.2f;

    [Space]
    [Header("Particle Systems")]
    [SerializeField] ParticleSystem _runningParticles   = null;
    [SerializeField] ParticleSystem _jumpingParticles   = null;
    [SerializeField] ParticleSystem _multijumpParticles = null;
    [SerializeField] ParticleSystem _walljumpParticles  = null;
    [SerializeField] ParticleSystem _wallHangParticles  = null;

    ParticleSystem _leftWalljumpParticles  = null;
    ParticleSystem _rightWalljumpParticles = null;

    Vector2 _velocityChange;

    // Player state
    int _facingDirection = 1;

    bool _grounded = false;
    bool _jumping  = false;
    bool _falling  = false;
    int _jumpsLeft = 0;
    float _timeLastGrounded = -10.0f;

    // Wallhanging values
    Vector2 _touchedWallPosition = new Vector2(0f, 0f);
    bool _touchingWall = false;
    bool _wallHanging = false;
    bool _wallHangStoppedFall = false;
    int _wallDirection = 0; 
    float _timeLastWallHanging = -10.0f;

    float _lastWalljumpX = float.PositiveInfinity;
    int _walljumpFrame = 0;

    // Cashed components
    PlayerInputs _inputs;

    Rigidbody2D _rb;

    Animator _anim;

    // Hashed animator params
    int _hashHorizontalVelocity;
    int _hashIsFalling;
    int _hashIsJumping;

    System.Text.StringBuilder _debugString = new System.Text.StringBuilder();

    void Start()
    {
        _rb     = GetComponent<Rigidbody2D>();
        _inputs = GetComponent<PlayerInputs>();

        _anim = GetComponent<Animator>();

        _hashHorizontalVelocity = Animator.StringToHash("horizontalVelocity");
        _hashIsFalling          = Animator.StringToHash("isFalling");
        _hashIsJumping          = Animator.StringToHash("isJumping");

        _walljumpFrame = _walljumpAccelerationFrames;

        if (_walljumpParticles != null)
        {
            _leftWalljumpParticles = _walljumpParticles;

            if (_leftWalljumpParticles.velocityOverLifetime.x.mode == ParticleSystemCurveMode.TwoConstants) {
                // Create a mirrored walljump particle system if it uses expected mode
                _rightWalljumpParticles = Instantiate<ParticleSystem>(_walljumpParticles, _walljumpParticles.transform.parent);
                _rightWalljumpParticles.name = "Right " + _walljumpParticles.name;

                var newVelocityCurve = _rightWalljumpParticles.velocityOverLifetime;
                newVelocityCurve.x = new ParticleSystem.MinMaxCurve(-newVelocityCurve.x.constantMin, -newVelocityCurve.x.constantMax);
            }
            else
            {
                // If curve mode is of unexpected type - don't touch anything to avoid errors
                _rightWalljumpParticles = _walljumpParticles;
            }
        }
    }

    void Update()
    {
        Time.timeScale = timeScale;
        // Update player visual state
        FaceMovementDirection();
        PlayRunningParticles();
        PlayWallHangParticles();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        _debugString.Clear();

        _rb.gravityScale = _baseGravityScale;

        // State changes, without directly affecting physics or logic
        GroundCheck();
        UpdateFallingState();
        WallCheck();
        WallHangingCheck();

        // Physics, depending on state and inputs
        HandleHorizontalAcceleration();
        HandleJumpInput();

        // Apply velocity changes
        _rb.velocity += _velocityChange;

        _debugString.AppendLine($"Velocity change: {_velocityChange.x}");
        _debugString.AppendLine($"Total velocity: {_rb.velocity.x}");

        _velocityChange = new Vector2(0.0f, 0.0f);

        DisplayDebugInfo();
    }

    void HandleHorizontalAcceleration()
    {
        float horizontalVelocity = _rb.velocity.x;
        float velocityDirection = Mathf.Sign(horizontalVelocity);

        // Deceleration, can't exceed current velocity to prevent character from becoming a pendulum
        _velocityChange.x += Mathf.Clamp(_baseDeceleration * -velocityDirection, -Mathf.Abs(horizontalVelocity), Mathf.Abs(horizontalVelocity));

        // Horizontal input acceleration
        float acceleration = _baseAcceleration;
        float accelerationDirection = 0.0f;

        // Walljump overrides acceleration for duration
        bool isDuringWallJump = _walljumpFrame < _walljumpAccelerationFrames;

        if (!isDuringWallJump)
        {
            if (!_wallHanging)
            {
                _walljumpFrame = _walljumpAccelerationFrames;
                accelerationDirection = _inputs.HorizontalInput;
            }
        }
        else
        {
            _walljumpFrame++;
            acceleration = _walljumpAcceleration;
            accelerationDirection = -_wallDirection;
        }

        _velocityChange.x += acceleration * accelerationDirection;

        // Soft limit player horizontal velocity
        if (Mathf.Abs(horizontalVelocity + _velocityChange.x) > _thresholdVelocity)
        {
            float excessiveVelocity = Mathf.Abs(horizontalVelocity + _velocityChange.x) - _thresholdVelocity;
            float excessiveVelocityDirection = Mathf.Sign(horizontalVelocity + _velocityChange.x);
            // Don't change speed below threshold for more consistent movement
            _velocityChange.x += Mathf.Clamp(_thresholdDeceleration * -excessiveVelocityDirection, -excessiveVelocity, excessiveVelocity);
        }
    }

    void HandleJumpInput()
    {
        float newVerticalVelocity = _rb.velocity.y;

        // Brake the fall speed on wallhang
        if (_wallHanging && _falling && !_wallHangStoppedFall)
        {
            newVerticalVelocity = 0.0f;
            _wallHangStoppedFall = true;
        }

        // Wall jumps are possible only from "new" walls
        bool isDifferentWall = Mathf.Abs(_lastWalljumpX - _touchedWallPosition.x) > 0.05f;
        bool inWalljumpWindow = _timeLastWallHanging + _walljumpWindow > Time.time;
        bool walljumpPossible = inWalljumpWindow && isDifferentWall;

        bool groundjumpPossible = _timeLastGrounded + _coyoteTime > Time.time;

        bool multijumpPossible = _jumpsLeft > 0;

        bool jumpPossible = groundjumpPossible || walljumpPossible || multijumpPossible;

        bool jumpInput = _inputs.ConsumeJumpInput();

        if (jumpInput && jumpPossible)
        {
            // Regular jump from ground here
            if (groundjumpPossible)
            {
                EmitJumpingParticles();
            }
            else
            {
                // Wall jump here
                if (walljumpPossible && _inputs.WallGrabPressed)
                {
                    _lastWalljumpX = _touchedWallPosition.x;
                    _walljumpFrame = 0;

                    EmitWalljumpParticles(_wallDirection, _touchedWallPosition);
                }
                // Multi jump here
                else
                {
                    _jumpsLeft--;
                    EmitMultijumpParticles();
                }
            }

            newVerticalVelocity = _jumpSpeed;

            _timeLastGrounded = -1.0f;
        }

        _rb.velocity = new Vector2(_rb.velocity.x, newVerticalVelocity);

        // -Can we have a state machine? -We have a state machine at home
        // State machine at home:

        // Update gravity scale to allow player to control his jump height
        if (_jumping)
        {
            if (_inputs.JumpPressed)
                _rb.gravityScale = _ascendingGravityScale;
        }
        else if (_wallHanging)
        {
            _rb.gravityScale = _wallHangingGravityScale;
        }
        else if (_falling)
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
            _lastWalljumpX = float.PositiveInfinity;

            _timeLastGrounded = Time.time;
        }

        _grounded = currentlyGrounded;
    }

    void WallCheck()
    {
        var wallTouched = Physics2D.Raycast(transform.position,
                _wallCheckPoint.position - transform.position,
                Vector2.Distance(transform.position, _wallCheckPoint.position),
                _groundMask);

        _touchingWall = wallTouched;

        if (wallTouched)
        {
            _wallDirection = _facingDirection;
            _touchedWallPosition = wallTouched.point;
        }
    }

    void WallHangingCheck()
    {
        bool suitableForWallHanging = _touchingWall && !_grounded;

        if (suitableForWallHanging && _inputs.WallGrabPressed)
        {
            _wallHanging = true;
            _timeLastWallHanging = Time.time;
        }
        else
        {
            _wallHanging = false;
            _wallHangStoppedFall = false;
        }
    }

    void UpdateFallingState()
    {
        _jumping = false;
        _falling = false;

        if (_rb.velocity.y > 0.005f)
        {
            _jumping = true;
        }

        if (_rb.velocity.y < -0.005f)
        {
            _falling = true;
        }
    }

    void FaceMovementDirection()
    {
        if (!_wallHanging)
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

    // FIX, remove the logic
    void PlayRunningParticles()
    {
        if (_runningParticles != null)
        {
            if (Mathf.Abs(_rb.velocity.x) > 0.1f && _grounded)
            {
                if (!_runningParticles.isEmitting)
                {
                    _runningParticles.Play();
                }
            }
            else
            {
                _runningParticles.Stop();
            }
        }
    }

    // FIX, remove the logic
    void PlayWallHangParticles()
    {
        if (_wallHangParticles != null)
        {
            if (_wallHanging && _falling)
            {
                if (!_wallHangParticles.isEmitting)
                {
                    _wallHangParticles.Play();
                }
            }
            else
            {
                _wallHangParticles.Stop();
            }
        }
    }

    void EmitJumpingParticles()
    {
        if (_jumpingParticles != null)
        {
            _jumpingParticles.Play();
        }
    }

    void EmitMultijumpParticles()
    {
        if (_multijumpParticles != null)
        {
            _multijumpParticles.Play();
        }
    }

    /// <summary>
    /// Emit wall jump particles at the contact position
    /// </summary>
    /// <param name="direction">1 - wall to the right, -1 - to the left</param>
    /// <param name="contactPosition"></param>
    void EmitWalljumpParticles(int direction, Vector2 contactPosition)
    {
        if (_walljumpParticles != null)
        {
            int particlesCount = 50;

            // If particle system has bursts - take the first burst values
            // Yes, this is a crutch
            if (_walljumpParticles.emission.burstCount > 0)
            {
                particlesCount = (int)_walljumpParticles.emission.GetBurst(0).count.constantMax;
            }

            var emitParams = new ParticleSystem.EmitParams();
            emitParams.position = contactPosition;

            if (direction == 1)
            {
                _rightWalljumpParticles.Emit(emitParams, particlesCount);
            }

            if (direction == -1)
            {
                _walljumpParticles.Emit(emitParams, particlesCount);
            }
        }
    }

    void UpdateAnimator()
    {
        _anim.SetFloat(_hashHorizontalVelocity, Mathf.Abs(_rb.velocity.x));
        _anim.SetBool(_hashIsFalling, _falling);
        _anim.SetBool(_hashIsJumping, _jumping);
    }

    void DisplayDebugInfo()
    {

        GlobalText.Instance.Show(_debugString.ToString());
    }
}

