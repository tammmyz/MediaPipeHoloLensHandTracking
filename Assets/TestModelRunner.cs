using OpenCVForUnityExample.DnnModel;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Barracuda;
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
    NNModel modelOnnx;

    private int inputSize = 192;

    private MediaPipePalmDetector palmDetector;

    // Start is called before the first frame update
    void Start()
    {
        palmDetector = new MediaPipePalmDetector(modelOnnx);
        //var spriteTex = sprite2Texture(handImage);
        var newH = getNewHeight(handTexture.width, handTexture.height);
        var rHandTexture = resize(handTexture, inputSize, newH);
        debugRenderer1.material.mainTexture = rHandTexture;
        var procTex = palmDetector.preprocess(rHandTexture);
        debugRenderer2.material.mainTexture = procTex;
        var id = StartAsync(procTex);
        Debug.Log($"id: {id}");
    }

    async Task<int> StartAsync(Texture2D procTex)
    {
        var id = await palmDetector.DetectPalms(procTex);
        return id;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private int getNewHeight(int imgW, int imgH)
    {
        // Scale image to fit input size of the model
        int newHeight = inputSize * imgH / imgW;
        return newHeight;
    }

    private Texture2D resize(Texture2D texture2D, int targetX, int targetY)
    {
        RenderTexture rt = new RenderTexture(targetX, targetY, 24);
        RenderTexture.active = rt;
        Graphics.Blit(texture2D, rt);
        Texture2D result = new Texture2D(targetX, targetY);
        result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
        result.Apply();
        return result;
    }


}
