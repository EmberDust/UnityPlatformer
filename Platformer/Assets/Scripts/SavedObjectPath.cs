using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPath", menuName = "ScriptableObjects/SavedPlayerPath")]
public class SavedObjectPath : ScriptableObject
{
    [SerializeField] public List<Vector3> playerPositions = new List<Vector3>();
}
