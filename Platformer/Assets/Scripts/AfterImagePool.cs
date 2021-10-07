using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AfterImagePool : MonoBehaviour
{
    [SerializeField] AfterImage _afterImagePrefab;
    [SerializeField] int _poolSize = 10;

    Queue<AfterImage> _afterImagePool = new Queue<AfterImage>();

    void Start()
    {
        GameManager.Instance.sceneLoaded += FillThePool;
    }

    public AfterImage SpawnAfterImage(Vector2 position, Quaternion rotation)
    {
        if (_afterImagePool.Count <= 0)
        {
            CreateAfterImageInPool();
        }

        AfterImage spawnedAfterImage = _afterImagePool.Dequeue();
        spawnedAfterImage.gameObject.SetActive(true);

        spawnedAfterImage.transform.position = position;
        spawnedAfterImage.transform.rotation = rotation;

        return spawnedAfterImage;
    }

    void FillThePool()
    {
        _afterImagePool.Clear();

        for (int i = 0; i < _poolSize; i++)
        {
            CreateAfterImageInPool();
        }
    }

    void CreateAfterImageInPool()
    {
        AfterImage newAfterImage = Instantiate<AfterImage>(_afterImagePrefab);

        newAfterImage.afterImageVanished += ReturnAfterImageToPool;

        newAfterImage.gameObject.SetActive(false);

        _afterImagePool.Enqueue(newAfterImage);
    }


    void ReturnAfterImageToPool(AfterImage afterImage)
    {
        afterImage.gameObject.SetActive(false);
        _afterImagePool.Enqueue(afterImage);
    }
}
