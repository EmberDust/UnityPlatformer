using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SpriteSorter : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        var children = GetComponentsInChildren<Transform>();

        Debug.Log($"Sorted sprites in {gameObject.name} based on Y coordinate, total children: " + children.Length);

        foreach (var child in children)
        {
            if (child.gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer sprite))
            {
                sprite.sortingOrder = (int)(-child.position.y * 10);
            }
        }
    }
}
