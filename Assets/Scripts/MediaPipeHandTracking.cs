using OpenCVForUnityExample.DnnModel;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OpenCVForUnity.CoreModule;



[RequireComponent(typeof(WebCamTextureToMatHelper))]
public class HandTracking : MonoBehaviour
{
    public bool showSkeleton;

    public MediaPipeHandPoseSkeletonVisualizer skeletonVisualizer;

    private string PALM_DETECTION_MODEL_FILENAME = "OpenCVForUnity/dnn/palm_detection_mediapipe_2023feb.onnx";
    private string HANDPOSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnity/dnn/handpose_estimation_mediapipe_2023feb.onnx";

    string palmDetectorFilepath;
    string handPoseEstimatorFilepath;

    Texture2D texture;
    WebCamTextureToMatHelper webCamTextureToMatHelper;
    Mat bgrMat;

    MediaPipePalmDetector palmDetector;
    MediaPipeHandPoseEstimator handPoseEstimator;

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
        palmDetectorFilepath = Utils.getFilePath(PALM_DETECTION_MODEL_FILENAME);
        palmDetector = new MediaPipePalmDetector(palmDetectorFilepath, 0.3f, 0.6f);

        handPoseEstimatorFilepath = Utils.getFilePath(HANDPOSE_ESTIMATION_MODEL_FILENAME);
        handPoseEstimator = new MediaPipeHandPoseEstimator(handPoseEstimatorFilepath, 0.9f);

        //webCamTexture = new WebCamTexture(requestedCameraSize.x, requestedCameraSize.y, cameraFPS);
        //webCamTexture.Play();
        webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
        webCamTextureToMatHelper.Initialize();

        StartTrackingHand();
    }


    /// <summary>
    /// Raises the webcam texture to mat helper initialized event.
    /// </summary>
    public void OnWebCamTextureToMatHelperInitialized()
    {
        Debug.Log("OnWebCamTextureToMatHelperInitialized");

        Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

        texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
        Utils.matToTexture2D(webCamTextureMat, texture);

        gameObject.GetComponent<Renderer>().material.mainTexture = texture;

        gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
        Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

        float width = webCamTextureMat.width();
        float height = webCamTextureMat.height();

        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }

        bgrMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
    }

    /// <summary>
    /// Raises the webcam texture to mat helper disposed event.
    /// </summary>
    public void OnWebCamTextureToMatHelperDisposed()
    {
        Debug.Log("OnWebCamTextureToMatHelperDisposed");

        if (bgrMat != null)
            bgrMat.Dispose();

        if (texture != null)
        {
            Texture2D.Destroy(texture);
            texture = null;
        }
    }

    /// <summary>
    /// Raises the webcam texture to mat helper error occurred event.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
    }

    // Update is called once per frame
    void Update()
    {

        if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
        {

            Mat rgbaMat = webCamTextureToMatHelper.GetMat();

            if (palmDetector == null || handPoseEstimator == null)
            {
                Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {
                Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                //TickMeter tm = new TickMeter();
                //tm.start();

                Mat palms = palmDetector.infer(bgrMat);

                //tm.stop();
                //Debug.Log("MediaPipePalmDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                List<Mat> hands = new List<Mat>();

                // Estimate the pose of each hand
                for (int i = 0; i < palms.rows(); ++i)
                {
                    //tm.reset();
                    //tm.start();

                    // Handpose estimator inference
                    Mat handpose = handPoseEstimator.infer(bgrMat, palms.row(i));

                    //tm.stop();
                    //Debug.Log("MediaPipeHandPoseEstimator Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                    if (!handpose.empty())
                        hands.Add(handpose);
                }

                Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                //palmDetector.visualize(rgbaMat, palms, false, true);

                foreach (var hand in hands)
                    handPoseEstimator.visualize(rgbaMat, hand, false, true);


                if (skeletonVisualizer != null && skeletonVisualizer.showSkeleton)
                {
                    if (hands.Count > 0 && !hands[0].empty())
                        skeletonVisualizer.UpdatePose(hands[0]);
                }
            }

            Utils.matToTexture2D(rgbaMat, texture);
        }

    }


    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        webCamTextureToMatHelper.Dispose();

        if (palmDetector != null)
            palmDetector.dispose();

        if (handPoseEstimator != null)
            handPoseEstimator.dispose();

        Utils.setDebugMode(false);
    }

    /// <summary>
    /// Raises the show skeleton toggle value changed event.
    /// </summary>
    //public void OnShowSkeletonToggleValueChanged()
    //{
    //    if (showSkeletonToggle.isOn != showSkeleton)
    //    {
    //        showSkeleton = showSkeletonToggle.isOn;
    //        if (skeletonVisualizer != null) skeletonVisualizer.showSkeleton = showSkeleton;
    //    }
    //}


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

    //private Texture2D ToTexture2D(RenderTexture rTex)
    //{
    //    Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
    //    var oldRt = RenderTexture.active;
    //    RenderTexture.active = rTex;

    //    tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
    //    tex.Apply();

    //    RenderTexture.active = oldRt;
    //    return tex;
    //}


}
