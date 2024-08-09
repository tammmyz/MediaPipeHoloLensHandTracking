using HandTracking.Interfaces;
using RealityCollective.ServiceFramework.Services;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class TestPalmExporter : MonoBehaviour
{
    private IHandTracker handtracker;

    [SerializeField]
    private Vector2Int requestedCameraSize = new(896, 504);

    [SerializeField]
    private int cameraFPS = 4;

    private Vector2Int actualCameraSize;

    private WebCamTexture webCamTexture;

    [SerializeField]
    private Renderer debugRenderer;

    [SerializeField]
    private Renderer debugRenderer1;

    [SerializeField]
    private Renderer debugRenderer2;

    JointExporter jointExporter;

    void Start()
    {
        handtracker = ServiceManager.Instance.GetService<IHandTracker>();
        webCamTexture = new WebCamTexture(requestedCameraSize.x, requestedCameraSize.y, cameraFPS);
        webCamTexture.Play();
        jointExporter = new JointExporter("palm");
        var time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var startTime = handtracker.attributeToJSON("startTime", time, "\n");
        jointExporter.appendToFile(startTime);
        StartTrackingHandAsync();
    }

    private async Task StartTrackingHandAsync()
    {
        int count = 0;
        await Task.Delay(1000);
        //Debug.Log("Start StartTrackingHandAsync");
        actualCameraSize = new Vector2Int(webCamTexture.width, webCamTexture.height);
        var renderTexture = new RenderTexture(actualCameraSize.x, actualCameraSize.y, 24);

        if (debugRenderer != null && debugRenderer.gameObject.activeInHierarchy)
        {
            debugRenderer.material.mainTexture = renderTexture;
        }
        while (true)
        {
            Graphics.Blit(webCamTexture, renderTexture);
            await Task.Delay(32);

            var texture = ToTexture2D(renderTexture);

            await Task.Delay(32);

            var palms = await handtracker.DetectPalms(texture);
            
            for (int i = 0; i < palms.rows(); i++)
            {
                var palm = handtracker.palmToJSON(count, palms.row(i));
                jointExporter.appendToFile(palm);
                Debug.Log($"{palm}");
            }

            count++;
        Destroy(texture);
        }
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

    public void OnDestroy()
    {
        jointExporter.Dispose();
    }
}
