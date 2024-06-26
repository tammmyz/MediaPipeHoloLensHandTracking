//using OpenCVForUnity.DnnModule;
//using OpenCVForUnity.UnityUtils;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandTracking : MonoBehaviour
{
    private string PALM_DETECTION_MODEL_FILENAME = "OpenCVForUnity/dnn/palm_detection_mediapipe_2023feb.onnx";
    private string HANDPOSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnity/dnn/handpose_estimation_mediapipe_2023feb.onnx";

    //Net palmDetector;
    //Net handPoseEstimator;

    [SerializeField]
    private Vector2Int requestedCameraSize = new(896, 504);

    [SerializeField]
    private Renderer debugRenderer;

    private Vector2Int actualCameraSize;

    [SerializeField]
    private int cameraFPS = 4;

    [SerializeField]
    private Vector2Int handImageSize = new(320, 256);

    private WebCamTexture webCamTexture;

    void Start()
    {
        //string palmDetectorFilepath = Utils.getFilePath(PALM_DETECTION_MODEL_FILENAME);
        //palmDetector = Dnn.readNet(palmDetectorFilepath);
        //palmDetector.setPreferableBackend(Dnn.DNN_BACKEND_OPENCV);
        //palmDetector.setPreferableTarget(Dnn.DNN_TARGET_CPU);

        //string handPoseEstimatorFilepath = Utils.getFilePath(HANDPOSE_ESTIMATION_MODEL_FILENAME);
        //handPoseEstimator = Dnn.readNet(handPoseEstimatorFilepath);
        //handPoseEstimator.setPreferableBackend(Dnn.DNN_BACKEND_OPENCV);
        //handPoseEstimator.setPreferableTarget(Dnn.DNN_TARGET_CPU);

        webCamTexture = new WebCamTexture(requestedCameraSize.x, requestedCameraSize.y, cameraFPS);
        webCamTexture.Play();
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = webCamTexture;
        StartTrackingHand();
    }

    private async Task StartTrackingHand()
    {
        await Task.Delay(1000);

        //actualCameraSize = new Vector2Int(webCamTexture.width, webCamTexture.height);
        //var renderTexture = new RenderTexture(handImageSize.x, handImageSize.y, 24);
        //if (debugRenderer != null && debugRenderer.gameObject.activeInHierarchy)
        //{
        //    debugRenderer.material.mainTexture = renderTexture;
        //}
        //while (true)
        //{
        //    var cameraTransform = CopyCameraTransForm(Camera.main);
        //    Graphics.Blit(webCamTexture, renderTexture);
        //    await Task.Delay(32);

        //    var texture = ToTexture2D(renderTexture);
        //    await Task.Delay(32);
            
        //    // add task here for object recognition

        //    Destroy(texture);
        //    Destroy(cameraTransform.gameObject);
        //}
    }

    private Transform CopyCameraTransForm(Camera camera)
    {
        var g = new GameObject
        {
            transform =
                {
                    position = camera.transform.position,
                    rotation = camera.transform.rotation,
                    localScale = camera.transform.localScale
                }
        };
        return g.transform;
    }

    private Texture2D ToTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        var oldRt = RenderTexture.active;
        RenderTexture.active = rTex;

        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();

        RenderTexture.active = oldRt;
        return tex;
    }


}
