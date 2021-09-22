using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] float _parallax = 0.5f;
    [SerializeField] float _verticalParallax = 0.1f;

    GameObject _mainCamera;

    Vector2 _cameraOrigin;
    Vector2 _backgroundOrigin;

    void Start()
    {
        _mainCamera = Camera.main.gameObject;
        _cameraOrigin = _mainCamera.transform.position;
        _backgroundOrigin = transform.position;
    }

    void Update()
    {
        Vector2 cameraPosition = _mainCamera.transform.position;

        Vector2 newBackgroundPosition = new Vector2 (_backgroundOrigin.x + (cameraPosition.x - _cameraOrigin.x) * _parallax,
                                                     _backgroundOrigin.y + (cameraPosition.y - _cameraOrigin.y) * _verticalParallax);

        transform.position = newBackgroundPosition;
    }
}
