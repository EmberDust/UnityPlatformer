using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [Header("Running Animation Speed")]
    [SerializeField] float _runningVelocity = 3f;
    [SerializeField] float _baseAnimationSpeed = 1.0f;

    [Space]
    [Header("Particle Systems")]
    [SerializeField] ParticleSystem _runningParticles   = null;
    [SerializeField] ParticleSystem _jumpingParticles   = null;
    [SerializeField] ParticleSystem _multijumpParticles = null;
    [SerializeField] ParticleSystem _walljumpParticles  = null;
    [SerializeField] ParticleSystem _wallHangParticles  = null;
    [SerializeField] ParticleSystem _deathParticles     = null;

    ParticleSystem _leftWalljumpParticles  = null;
    ParticleSystem _rightWalljumpParticles = null;

    Rigidbody2D _rb;
    PlayerMovement _player;

    SpriteRenderer _sprite;
    Animator _anim;

    // Hashed animator params
    int _hashHorizontalVelocity;
    int _hashIsFalling;
    int _hashIsJumping;
    int _hashIsDead;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponentInChildren<SpriteRenderer>();

        _anim = GetComponent<Animator>();

        _hashHorizontalVelocity = Animator.StringToHash("horizontalVelocity");
        _hashIsFalling = Animator.StringToHash("isFalling");
        _hashIsJumping = Animator.StringToHash("isJumping");
        _hashIsDead = Animator.StringToHash("isDead");

        CreateMirroredWalljumpParticles();

        _player = GetComponent<PlayerMovement>();

        _player.playerWalljumped += EmitWalljumpParticles;
        _player.playerGroundjumped += EmitJumpingParticles;
        _player.playerMultijumped += EmitMultijumpParticles;
        _player.playerDied += EmitDeathParticles;
    }

    void Update()
    {
        HandleRunningAnimationSpeed();
        PlayRunningParticles();
        PlayWallHangParticles();
        UpdateAnimator();
    }

    void CreateMirroredWalljumpParticles()
    {
        if (_walljumpParticles != null)
        {
            _leftWalljumpParticles = _walljumpParticles;

            if (_leftWalljumpParticles.velocityOverLifetime.x.mode == ParticleSystemCurveMode.TwoConstants)
            {
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

    void HandleRunningAnimationSpeed()
    {
        // Increases animation speed based on player velocity
        if (Mathf.Abs(_rb.velocity.x) > _runningVelocity)
        {
            _anim.speed = Mathf.Abs(_rb.velocity.x) / _runningVelocity * _baseAnimationSpeed;
        }
        else if (_anim.speed != _baseAnimationSpeed)
        {
            _anim.speed = _baseAnimationSpeed;
        }
    }

    void PlayRunningParticles()
    {
        if (_runningParticles != null)
        {
            if (Mathf.Abs(_rb.velocity.x) > 0.1f && _player.Grounded && !_player.IsDead)
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

    void PlayWallHangParticles()
    {
        if (_wallHangParticles != null)
        {
            if (_player.WallHanging && _player.Falling && !_player.IsDead)
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

    void EmitDeathParticles()
    {
        if (_deathParticles != null)
        {
            _deathParticles.Play();
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
        _anim.SetBool(_hashIsFalling, _player.Falling);
        _anim.SetBool(_hashIsJumping, _player.Jumping);
        _anim.SetBool(_hashIsDead, _player.IsDead);
    }
}
