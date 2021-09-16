using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityBooster : PowerUp
{
    [SerializeField] Vector2 _velocityBoost = new Vector2(2.0f, 0.0f);
    [SerializeField] bool _resetGravity = false;

    protected override void GrantPowerUp()
    {
        GameManager.Instance.PlayerScript.GiveVelocityBoost(_velocityBoost, _resetGravity);
    }
}
