using HandTracking.Interfaces;
using RealityCollective.ServiceFramework.Services;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

public class HandTrackingScript : MonoBehaviour
{
    private IHandTracker handtracker;

    [SerializeField]
    private Vector2Int requestedCameraSize = new(896, 504);

    [SerializeField]
    private int cameraFPS = 4;

    [SerializeField]
    private Vector2Int handImageSize = new(192,108);

    private int inputSize = 192;
    private Vector2Int actualCameraSize;

    private WebCamTexture webCamTexture;

    [SerializeField]
    private Renderer debugRenderer;

    [SerializeField]
    private Renderer textureDebugger;

    void Start()
    {
        handtracker = ServiceManager.Instance.GetService<IHandTracker>();
        webCamTexture = new WebCamTexture(requestedCameraSize.x, requestedCameraSize.y, cameraFPS);
        webCamTexture.Play();
        StartTrackingHandAsync();
    }

    private async Task StartTrackingHandAsync()
    {
        await Task.Delay(1000);
        Debug.Log("Start StartTrackingHandAsync");

        actualCameraSize = new Vector2Int(webCamTexture.width, webCamTexture.height);
        handImageSize = getHandImageSize(webCamTexture.width, webCamTexture.height);
        var renderTexture = new RenderTexture(handImageSize.x, handImageSize.y, 24);
        if (debugRenderer != null && debugRenderer.gameObject.activeInHierarchy)
        {
            debugRenderer.material.mainTexture = renderTexture;
        }
        while (true)
        {
            Debug.Log("doing while loop");
            Graphics.Blit(webCamTexture, renderTexture);
            Debug.Log("graphics blit");
            await Task.Delay(32);

            var texture = ToTexture2D(renderTexture);
            await Task.Delay(32);

            Debug.Log("to texture 2d");
            textureDebugger.material.mainTexture = preprocess(texture);
            Debug.Log("detected palm");

            Destroy(texture);
        }
    }

    private Vector2Int getHandImageSize(int actualCameraWidth, int actualCameraHeight)
    {
        // Scale image to fit input size of the model
        int newHeight = inputSize * actualCameraHeight / actualCameraWidth;
        handImageSize = new(inputSize, newHeight);
        return handImageSize;
    }

    private Texture2D ToTexture2D(RenderTexture rTex)
    {
        // Convert RenderTexture type to Texture2D
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        var oldRt = RenderTexture.active;
        RenderTexture.active = rTex;

        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();

        RenderTexture.active = oldRt;
        return tex;
    }

    private Texture2D preprocess(Texture2D tex)
    {
        // Pad image to make square
        int maxSize = Mathf.Max(tex.width, tex.height);
        int offsetX = (maxSize - tex.width) / 2;
        int offsetY = (maxSize - tex.height) / 2;
        Debug.Log("Max: " + maxSize.ToString()
            + "\tOffsetX: " + offsetX.ToString()
            + "\tOffsety: " + offsetY.ToString());

        Texture2D paddedTex = new Texture2D(maxSize, maxSize, TextureFormat.ARGB32, false);
        Color[] texPixels = tex.GetPixels();
        paddedTex.SetPixels(offsetX, offsetY, tex.width, tex.height, texPixels);
        paddedTex.Apply();

        RenderTexture.active = null;

        return paddedTex;
    }
}
