using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingProjectile : Projectile
{
    [Header("Homing")]
    [SerializeField] float _rotationSpeed = 0.5f;

    Vector2 _currentMovementDirection;

    public override void Shoot(Vector2 from, Vector2 direction)
    {
        base.Shoot(from, direction);

        _currentMovementDirection = direction;
    }

    protected override void ProjectileLogicUpdate()
    {
        base.ProjectileLogicUpdate();

        if (!_hasCollided)
        {
            if (GameManager.Instance.PlayerScript.IsDisabled)
            {
                TriggerProjectileExpiration();
            }
            else
            {
                Vector2 targetDirection = GameManager.Instance.PlayerObject.transform.position - transform.position;
                float angleToTarget = Vector2.SignedAngle(_currentMovementDirection, targetDirection);

                if (Mathf.Abs(angleToTarget) > _rotationSpeed)
                {
                    float rotationAmount = Mathf.Min(Mathf.Abs(angleToTarget), _rotationSpeed);
                    rotationAmount = Mathf.Sign(angleToTarget) * rotationAmount;

                    _currentMovementDirection = Quaternion.Euler(0, 0, rotationAmount) * _currentMovementDirection;
                }

                _rb.velocity = _velocity * _currentMovementDirection;
            }
        }
    }
}
