using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [Header("Running Animation Speed")]
    [SerializeField] float _runningVelocity = 3f;
    [SerializeField] float _baseAnimationSpeed = 1.0f;
    [SerializeField] float _baseEmissionMultiplier = 1.0f;

    [Space]
    [Header("Particle Systems")]
    [SerializeField] ParticleSystem _runningParticles   = null;
    [SerializeField] ParticleSystem _jumpingParticles   = null;
    [SerializeField] ParticleSystem _multijumpParticles = null;
    [SerializeField] ParticleSystem _walljumpParticles  = null;
    [SerializeField] ParticleSystem _wallSlideParticles = null;
    [SerializeField] ParticleSystem _deathParticles     = null;

    [Space]
    [Header("AfterImage Effect")]
    [SerializeField] AfterImagePool _afterImagePool;
    [SerializeField] float _afterImageVelocity = 4.0f;
    [SerializeField] float _secondsBetweenImages = 0.1f;
    [SerializeField] float _startingFadeOut = 0.5f;
    [SerializeField] float _fadeOutRate = 0.1f;
    [SerializeField] float _fadeOutAmount = 0.1f;

    ParticleSystem _leftWalljumpParticles  = null;
    ParticleSystem _rightWalljumpParticles = null;

    Rigidbody2D _rb;
    Player _player;

    SpriteRenderer _sprite;
    Animator _anim;

    ParticleSystem.EmissionModule _runningParticlesEmission;

    float _timeLastImageCreated;

    // Hashed animator params
    int _hashHorizontalVelocity;
    int _hashIsFalling;
    int _hashIsJumping;
    int _hashIsDead;
    int _hashIsSliding;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponentInChildren<SpriteRenderer>();

        _anim = GetComponent<Animator>();

        _hashHorizontalVelocity = Animator.StringToHash("horizontalVelocity");
        
        _hashIsFalling = Animator.StringToHash("isFalling");
        _hashIsJumping = Animator.StringToHash("isJumping");
        _hashIsDead    = Animator.StringToHash("isDead");
        _hashIsSliding = Animator.StringToHash("isSliding");

        CreateMirroredWalljumpParticles();

        if (_runningParticles != null)
        {
            _runningParticlesEmission = _runningParticles.emission;
        }

        _player = GetComponent<Player>();

        _player.playerWalljumped   += EmitWalljumpParticles;
        _player.playerGroundjumped += EmitJumpingParticles;
        _player.playerMultijumped  += EmitMultijumpParticles;
        _player.playerDied         += EmitDeathParticles;
    }

    void Update()
    {
        AdjustRunningAnimationSpeed();
        AdjustRunningEmissionRate();
        SpawnAfterImageEffect();
        PlayRunningParticles();
        PlayWallSlideParticles();
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

    void SpawnAfterImageEffect()
    {
        if (_afterImagePool != null)
        {
            bool IsSpawningAfterImages = _player.IsMovementOverridenByCurve || Mathf.Abs(_rb.velocity.x) > _afterImageVelocity;

            if (IsSpawningAfterImages)
            {
                if (_timeLastImageCreated + _secondsBetweenImages < Time.time)
                {
                    AfterImage afterImage = _afterImagePool.GetAfterImageFromPool(transform.position, transform.rotation);
                    afterImage.transform.localScale = transform.localScale;

                    afterImage.Sprite.sprite = _sprite.sprite;
                    afterImage.StartingColor = _sprite.color;

                    afterImage.CurrentFadeOut = _startingFadeOut;
                    afterImage.FadeOutAmount = _fadeOutAmount;
                    afterImage.FadeOutRate = _fadeOutRate;

                    afterImage.StartFadeOut();

                    _timeLastImageCreated = Time.time;
                }
            }
            else
            {
                _timeLastImageCreated = -_secondsBetweenImages;
            }
        }
    }

    void AdjustRunningAnimationSpeed()
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

    void AdjustRunningEmissionRate()
    {
        if (_runningParticles != null)
        {
            if (Mathf.Abs(_rb.velocity.x) > _runningVelocity)
            {
                _runningParticlesEmission.rateOverTimeMultiplier = Mathf.Abs(_rb.velocity.x) / _runningVelocity * _baseEmissionMultiplier;
            }
            else if (_runningParticlesEmission.rateOverTimeMultiplier != _baseEmissionMultiplier)
            {
                _runningParticlesEmission.rateOverTimeMultiplier = _baseEmissionMultiplier;
            }
        }
    }

    void PlayRunningParticles()
    {
        if (_runningParticles != null)
        {
            if (Mathf.Abs(_rb.velocity.x) > 0.1f && _player.IsGrounded && !_player.IsDisabled)
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

    void PlayWallSlideParticles()
    {
        if (_wallSlideParticles != null)
        {
            if (_player.IsWallSliding && _player.IsFalling && !_player.IsDisabled)
            {
                if (!_wallSlideParticles.isEmitting)
                {
                    _wallSlideParticles.Play();
                }
            }
            else
            {
                _wallSlideParticles.Stop();
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
            if (direction == 1)
            {
                Utils.EmitParticleBurstAtPosition(_rightWalljumpParticles, contactPosition);
            }

            if (direction == -1)
            {
                Utils.EmitParticleBurstAtPosition(_leftWalljumpParticles, contactPosition);
            }
        }
    }

    void UpdateAnimator()
    {
        _anim.SetFloat(_hashHorizontalVelocity, Mathf.Abs(_rb.velocity.x));
        _anim.SetBool(_hashIsFalling, _player.IsFalling);
        _anim.SetBool(_hashIsJumping, _player.IsJumping);
        _anim.SetBool(_hashIsDead, _player.IsDisabled);
        _anim.SetBool(_hashIsSliding, _player.IsGroudSliding);
    }
}
