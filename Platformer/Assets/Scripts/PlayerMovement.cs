using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("JumpValues")]
    [SerializeField] int   _additionalJumps = 1;
    [SerializeField] float _jumpSpeed       = 5f;

    [Space]
    [SerializeField] float _baseGravityScale      = 2f;
    [SerializeField] float _ascendingGravityScale = 1f;
    [SerializeField] float _fallingGravityScale   = 5f;

    [Space]
    [Header("Horizontal Movement Values")]
    [SerializeField] float _runningSpeed = 5f;
    [SerializeField] float _deceleration = 5f;

    [Space]
    [Header("Ground Check")]
    [SerializeField] Transform _groundCheckPoint;
    [SerializeField] float _coyoteTime       = 0.1f;
    [SerializeField] LayerMask _groundMask;

    [Space]
    [Header("Wall Hanging")]
    [SerializeField] Transform _wallCheckPoint;
    [SerializeField] float _walljumpWindow = 0.2f;
    [SerializeField] float _wallHangingGravityScale = 0.2f;

    [Space]
    [Header("Particle Systems")]
    [SerializeField] ParticleSystem _runningParticles   = null;
    [SerializeField] ParticleSystem _jumpingParticles   = null;
    [SerializeField] ParticleSystem _multijumpParticles = null;
    [SerializeField] ParticleSystem _walljumpParticles  = null;
    [SerializeField] ParticleSystem _wallHangParticles  = null;

    ParticleSystem _leftWalljumpParticles = null;
    ParticleSystem _rightWalljumpParticles = null;

    // Player state
    int _facingDirection = 1;

    bool _grounded       = false;
    bool _jumping        = false;
    bool _falling        = false;
    int   _jumpsLeft = 0;
    float _timeLastGrounded = -1.0f;

    // Wallhanging values
    Vector2 _touchedWallPosition = new Vector2(0f, 0f);
    bool _touchingWall   = false;
    bool _wallHanging    = false;
    bool _wallHangStoppedFall = false;
    int _wallDirection = 0; 
    float _lastWalljumpX = 0.0f; 
    float _timeLastWallHanging = -1.0f;

    System.Text.StringBuilder _debugString = new System.Text.StringBuilder();

    // Cashed components
    PlayerInputs _inputs;

    Rigidbody2D      _rb;

    SpriteRenderer _sprite;
    Animator       _anim;

    // Hashed animator params
    int _hashHorizontalVelocity;
    int _hashIsFalling;
    int _hashIsJumping;

    void Start()
    {
        _rb     = GetComponent<Rigidbody2D>();
        _sprite = GetComponentInChildren<SpriteRenderer>();
        _inputs = GetComponent<PlayerInputs>();

        _anim = GetComponent<Animator>();

        _hashHorizontalVelocity = Animator.StringToHash("horizontalVelocity");
        _hashIsFalling          = Animator.StringToHash("isFalling");
        _hashIsJumping          = Animator.StringToHash("isJumping");

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
        ProcessHorizontalInput();
        ProcessJumpInput();

        DisplayDebugInfo();
    }

    void ProcessHorizontalInput()
    {
        Vector2 oldVelocity = _rb.velocity;

        if (Mathf.Abs(_inputs.HorizontalInput) > 0.0)
        {
            _rb.velocity = new Vector2(_inputs.HorizontalInput * _runningSpeed, oldVelocity.y);
        }
        else
        {
            _rb.velocity = new Vector2(0.0f, oldVelocity.y);
        }
    }

    void ProcessJumpInput()
    {
        Vector2 oldVelocity = _rb.velocity;

        // Brake the fall speed on wallhang
        if (_wallHanging && _falling && !_wallHangStoppedFall)
        {
            _rb.velocity = new Vector2(oldVelocity.x, 0.0f);
            _wallHangStoppedFall = true;
        }

        // Wall jumps are possible only from "new" walls
        bool isNewWalljumpX = Mathf.Abs(_lastWalljumpX - _touchedWallPosition.x) > 0.05f;
        bool inWalljumpWindow = _timeLastWallHanging + _walljumpWindow > Time.time;
        bool walljumpPossible = inWalljumpWindow && isNewWalljumpX;

        bool groundjumpPossible = _timeLastGrounded + _coyoteTime > Time.time;

        bool multijumpPossible = _jumpsLeft > 0;

        bool jumpPossible = groundjumpPossible || walljumpPossible || multijumpPossible;

        bool jumpInput = _inputs.ConsumeJumpInput();

        if (jumpInput && jumpPossible)
        {
            // Regular from ground jump here
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
                    EmitWalljumpParticles(_wallDirection, _touchedWallPosition);
                }
                // Multi jump here
                else
                {
                    _jumpsLeft--;
                    EmitMultijumpParticles();
                }
            }

            _rb.velocity = new Vector2(oldVelocity.x, _jumpSpeed);

            _timeLastGrounded = -1.0f;
        }

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
            _lastWalljumpX = 0;

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

