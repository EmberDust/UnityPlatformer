using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawn : Checkpoint
{
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        // Player spawn should be activated through the game manager
        // so unlike regular checkpoint it shouldn't be triggered by player
    }
}
