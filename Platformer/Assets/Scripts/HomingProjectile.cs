using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingProjectile : Projectile
{
    float _rotationSpeed = 2.0f;

    Vector2 _currentMovementDirection;

    public override void Launch(Vector2 from, Vector2 direction, float lifeTime, float rotationSpeed = 0.0f)
    {
        base.Launch(from, direction, lifeTime);

        _currentMovementDirection = direction;
        _rotationSpeed = rotationSpeed;
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

                _rb.velocity = _absoluteVelocity * _currentMovementDirection;
            }
        }
    }
}
