using UnityEngine;

[ExecuteInEditMode]
public class SpriteSorter : MonoBehaviour
{
    [SerializeField] bool _sortSprites = false;

    void Update()
    {
        if (_sortSprites)
        {
            var children = GetComponentsInChildren<Transform>();

            foreach (var child in children)
            {
                if (child.gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer sprite))
                {
                    sprite.sortingOrder = (int)(-child.position.y * 10);
                }
            }

            Debug.Log($"Sorted sprites in {gameObject.name} based on Y coordinate, total children count: " + children.Length);
            _sortSprites = false;
        }
    }
}
