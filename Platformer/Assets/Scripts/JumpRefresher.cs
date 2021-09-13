using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpRefresher : PowerUp
{
    [SerializeField] int _numberOfMultijumps = 1;
    [SerializeField] bool _ignoreLimit = false;

    protected override void GrantPowerUp()
    {
        _playerScript.GiveMultijumpCharges(_numberOfMultijumps, _ignoreLimit);
    }
}
