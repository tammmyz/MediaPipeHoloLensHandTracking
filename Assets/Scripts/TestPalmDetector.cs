using HandTracking.Interfaces;
using RealityCollective.ServiceFramework.Services;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;

public class TestPalmDetector : MonoBehaviour
{
    [SerializeField]
    public Texture2D handTexture;

    [SerializeField]
    public Renderer debugRenderer1;

    [SerializeField]
    public Renderer debugRenderer2;

    [SerializeField]
    public Renderer debugRenderer3;

    [SerializeField]
    ModelAsset modelOnnx;

    private int inputSize = 192;

    private MediaPipePalmDetector palmDetector;

    private IHandTracker handTracker;

    // Start is called before the first frame update
    void Start()
    {
        palmDetector = new MediaPipePalmDetector(modelOnnx);
        palmDetector.Initialize();
        var newPalmH = getNewHeight(handTexture.width, handTexture.height, 192);
        var rHandTexture = resize(handTexture, inputSize, newPalmH);
        debugRenderer1.material.mainTexture = rHandTexture;
        var procTex = palmDetector.preprocess(rHandTexture);
        debugRenderer2.material.mainTexture = procTex;
        //Debug.Log($"procTex dimensions: {procTex.Size()}");
        //var id = palmDetector.DetectPalms(procTex);
        StartAsync(procTex, debugRenderer3);
    }

    async void StartAsync(Texture2D procTex, Renderer renderer)
    {
        var mat = await palmDetector.DetectPalms(procTex, renderer);
        await Task.Delay(32);
        Debug.Log($"id: {mat.dump()}");
    }

    private int getNewHeight(int imgW, int imgH, int targetMax)
    {
        // Scale image to fit input size of the model
        int newHeight = targetMax * imgH / imgW;
        return newHeight;
    }

    private Texture2D resize(Texture2D texture2D, int targetX, int targetY)
    {
        RenderTexture rt = new RenderTexture(targetX, targetY, 24, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;
        Graphics.Blit(texture2D, rt);
        Texture2D result = new Texture2D(targetX, targetY, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
        result.Apply();
        return result;
    }

    private void OnDestroy()
    {
        if (handTracker != null)
        {
            handTracker.Dispose();
        }
    }
}
