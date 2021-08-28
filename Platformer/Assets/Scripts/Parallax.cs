using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] float parallax = 0.5f;
    [SerializeField] float verticalParallax = 0.1f;

    GameObject mainCamera;

    Vector2 cameraOrigin;
    Vector2 backgroundOrigin;

    void Start()
    {
        mainCamera = Camera.main.gameObject;
        cameraOrigin = mainCamera.transform.position;
        backgroundOrigin = transform.position;
    }

    void Update()
    {
        Vector2 cameraPosition = mainCamera.transform.position;

        Vector2 newSkyPosition = new Vector2 (backgroundOrigin.x + (cameraPosition.x - cameraOrigin.x) * parallax,
                                              backgroundOrigin.y + (cameraPosition.y - cameraOrigin.y) * verticalParallax);

        transform.position = newSkyPosition;
    }
}
