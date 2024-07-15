using HandTracking.Interfaces;
using RealityCollective.ServiceFramework.Services;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;

public class TestModelRunner : MonoBehaviour
{
    [SerializeField]
    public Texture2D handTexture;

    [SerializeField]
    public Renderer debugRenderer1;

    [SerializeField]
    public Renderer debugRenderer2;

    [SerializeField]
    ModelAsset modelOnnx;

    private int inputSize = 192;

    private MediaPipePalmDetector palmDetector;

    private IHandTracker handTracker;

    // Start is called before the first frame update
    void Start()
    {
        //handTracker = ServiceManager.Instance.GetService<IHandTracker>();
        //var newH = getNewHeight(handTexture.width, handTexture.height);
        //var rHandTexture = resize(handTexture, inputSize, newH);
        //debugRenderer1.material.mainTexture = rHandTexture;
        //var procTex = handTracker.preprocess(rHandTexture);
        //debugRenderer2.material.mainTexture = procTex;
        //Debug.Log($"procTex dimensions: {procTex.Size()}");
        //var id = handTracker.DetectPalms(procTex);

        palmDetector = new MediaPipePalmDetector(modelOnnx);
        palmDetector.Initialize();
        var newH = getNewHeight(handTexture.width, handTexture.height);
        var rHandTexture = resize(handTexture, inputSize, newH);
        debugRenderer1.material.mainTexture = rHandTexture;
        var procTex = palmDetector.preprocess(rHandTexture);
        debugRenderer2.material.mainTexture = procTex;
        Debug.Log($"procTex dimensions: {procTex.Size()}");
        //var id = palmDetector.DetectPalms(procTex);
        StartAsync(procTex);

        //StartAsync(procTex);
        //var rHandMat = palmDetector.toMat(procTex);
        //Debug.Log($"Mat size: {rHandMat.size()}");
        //Debug.Log($"Mat rows: {rHandMat}");
    }

    async void StartAsync(Texture2D procTex)
    {
        var id = await palmDetector.DetectPalms(procTex);
        await Task.Delay(32);
        Debug.Log($"id: {id}");
    }

    private int getNewHeight(int imgW, int imgH)
    {
        // Scale image to fit input size of the model
        int newHeight = inputSize * imgH / imgW;
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
