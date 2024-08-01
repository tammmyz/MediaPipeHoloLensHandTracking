using HandTracking.Interfaces;
using TextureProcUtils;
using Unity.Sentis;
using UnityEngine;

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
        StartAsync(handTexture, debugRenderer1, debugRenderer2,debugRenderer3);
    }

    async void StartAsync(Texture2D handTexture, Renderer r1, Renderer r2, Renderer r3)
    {
        var detectedPalms = await palmDetector.StartAsync(handTexture);
        var visTex = TexProcUtils.resizePad(handTexture, 192);
        palmDetector.visualize(visTex, detectedPalms, 192, 192, r1);
        var handEstimate = await handPoseEstimator.StartAsyncDebug(handTexture, detectedPalms, r2, r3);
        Debug.Log($"handEstimate: {handEstimate.size()} {handEstimate.dump()}");
    }

    private void OnDestroy()
    {
        if (handTracker != null)
        {
            handTracker.Dispose();
        }
    }
}
