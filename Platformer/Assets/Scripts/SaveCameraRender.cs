using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveCameraRender : MonoBehaviour
{
    Camera renderCamera;

    void Start()
    {
        renderCamera = GetComponent<Camera>();
    }

    void Update()
    {
        if (Input.GetButtonDown("Screenshot"))
        {
            ScreenshotToFile();
        }
    }

    void ScreenshotToFile()
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderCamera.targetTexture;

        renderCamera.Render();

        Texture2D image = new Texture2D(renderCamera.targetTexture.width, renderCamera.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, renderCamera.targetTexture.width, renderCamera.targetTexture.height), 0, 0);
        image.Apply();

        RenderTexture.active = currentRT;

        var bytes = image.EncodeToPNG();
        Destroy(image);

        File.WriteAllBytes(Application.dataPath + "/background.png", bytes);

        Debug.Log("Screen saved");
    }
}
