using HandTracking.Interfaces;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using RealityCollective.ServiceFramework.Services;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;
using Rect = UnityEngine.Rect;

public class TestPoseEstimator : MonoBehaviour
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
    ModelAsset modelOnnxPalm;

    [SerializeField]
    ModelAsset modelOnnxHand;

    private MediaPipePalmDetector palmDetector;
    private MediaPipeHandPoseEstimator handPoseEstimator;

    private IHandTracker handTracker;

    // Start is called before the first frame update
    void Start()
    {
        palmDetector = new MediaPipePalmDetector(modelOnnxPalm);
        palmDetector.Initialize();
        handPoseEstimator = new MediaPipeHandPoseEstimator(modelOnnxHand);
        handPoseEstimator.Initialize();
        StartAsync(handTexture, debugRenderer3);
    }

    async void StartAsync(Texture2D handTexture, Renderer renderer)
    {
        Texture2D rHandTexPalm;
        if (handTexture.width > handTexture.height)
        {
            var newPalmH = getNewHeight(handTexture.width, handTexture.height, 192);
            rHandTexPalm = resize(handTexture, 192, newPalmH);
        }
        else
        {
            var newPalmW = getNewHeight(handTexture.height, handTexture.width, 192);
            rHandTexPalm = resize(handTexture, newPalmW, 192);
        }
        var procTexPalm = palmDetector.preprocess(rHandTexPalm);
        Destroy(rHandTexPalm);
        var mat = await palmDetector.DetectPalms(procTexPalm, renderer);
        await Task.Delay(32);
        Debug.Log($"palm: {mat.dump()}");
        //Debug.Log($"palm shape: {mat.size()}");

        var newHandH = getNewHeight(handTexture.width, handTexture.height, handTexture.width);
        var rHandTexHand = resize(handTexture, handTexture.width, newHandH);
        var procHandTexHand = palmDetector.preprocess(rHandTexHand);
        //Destroy(rHandTexHand);
        Debug.Log($"procHandTexHand (w, h): {procHandTexHand.width}, {procHandTexHand.height}");
        debugRenderer1.material.mainTexture = procHandTexHand;
        Mat rotated_palm_bbox;
        double angle;
        Mat rotation_matrix;

        Mat rHandMatHand = texture2DToMat(procHandTexHand);
        //Destroy(procHandTexHand);
        var procTexHand = handPoseEstimator.preprocess(rHandMatHand, mat, out rotated_palm_bbox, out angle, out rotation_matrix); // Mat
        debugRenderer2.material.mainTexture = procTexHand;
        Debug.Log($"procTexHand (w, h): {procTexHand.width}, {procTexHand.height}");
        var procTexHand2 = resize(procTexHand, 224, 224);
        //var matHand = await handPoseEstimator.EstimateHandPose(procTexHand2);
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

    private Mat texture2DToMat(Texture2D texture)
    {
        // Get pixel data from Texture2D
        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;

        // Create a Mat with the same size and type as the texture
        Mat mat = new Mat(height, width, CvType.CV_8UC3);

        // Convert pixel data to Mat
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 color = pixels[(height - y - 1) * width + x];
                mat.put(y, x, new byte[] { color.r, color.g, color.b });
            }
        }

        return mat;
    }

    private void OnDestroy()
    {
        if (handTracker != null)
        {
            handTracker.Dispose();
        }
    }
}
