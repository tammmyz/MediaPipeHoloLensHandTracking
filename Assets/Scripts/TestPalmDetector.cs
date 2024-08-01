using Unity.Sentis;
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

    private MediaPipePalmDetector palmDetector;

    // Start is called before the first frame update
    void Start()
    {
        palmDetector = new MediaPipePalmDetector(modelOnnx);
        palmDetector.Initialize();
        StartAsync();
    }

    public async void StartAsync()
    {
        var detectedPalms = await palmDetector.StartAsyncDebug(handTexture, debugRenderer1, debugRenderer2, debugRenderer3);
        Debug.Log($"detectedPalms: {detectedPalms.dump()}");
    }
    private void OnDestroy()
    {
        palmDetector.Destroy();
    }
}
