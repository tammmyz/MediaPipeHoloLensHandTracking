//using Dnn;
//using Dnn.Interfaces;
//using OpenCVForUnity.ImgprocModule;
//using OpenCVForUnity.UnityUtils;
//using OpenCVForUnity.UnityUtils.Helper;
//using System.Collections;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using UnityEngine;
//using OpenCVForUnity.CoreModule;
//using Unity.Barracuda;
//using Unity.VisualScripting;
//using RealityCollective.ServiceFramework.Services;
//using RealityCollective.ServiceFramework;
//using UnityEditor.MPE;

//[RequireComponent(typeof(WebCamTextureToMatHelper))]
//public class HandTracking1 : MonoBehaviour
//{
//    public bool showSkeleton;

//    Texture2D texture;
//    WebCamTextureToMatHelper webCamTextureToMatHelper;
//    Mat bgrMat;

//    [SerializeField]
//    NNModel palmDetector;

//    [SerializeField]
//    private Vector2Int requestedCameraSize = new(896, 504);

//    [SerializeField]
//    private Renderer debugRenderer;

//    private Vector2Int actualCameraSize;

//    [SerializeField]
//    private int cameraFPS = 4;

//    [SerializeField]
//    private Vector2Int handImageSize = new(320, 256);

//    private WebCamTexture webCamTexture;

//    [SerializeField]
//    ProcessorProfile profile;

//    [SerializeField]
//    Renderer textureDebugger;

//    private IProcessor dnnProcessor;


//    void Start()
//    {
//        dnnProcessor = ServiceManager.Instance.GetService<IProcessor>();
//        webCamTexture = new WebCamTexture(requestedCameraSize.x, requestedCameraSize.y, cameraFPS);
//        webCamTexture.Play();
//        //webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
//        //webCamTextureToMatHelper.Initialize();
//        StartTrackingHandAsync();
//    }


//    private async Task StartTrackingHandAsync()
//    {
//        await Task.Delay(1000);
//        Debug.Log("Start StartTrackingHandAsync");

//        actualCameraSize = new Vector2Int(webCamTexture.width, webCamTexture.height);
//        var renderTexture = new RenderTexture(handImageSize.x, handImageSize.y, 24);
//        if (debugRenderer != null && debugRenderer.gameObject.activeInHierarchy)
//        {
//            debugRenderer.material.mainTexture = renderTexture;
//        }
//        while (true)
//        {
//            Debug.Log("doing while loop");
//            var cameraTransform = CopyCameraTransForm(Camera.main);
//            Debug.Log("copied camera transform");
//            Debug.Log(cameraTransform.position);
//            Debug.Log(cameraTransform.rotation);
//            Debug.Log(cameraTransform.localScale);
//            Graphics.Blit(webCamTexture, renderTexture);
//            Debug.Log("graphics blit");
//            await Task.Delay(32);

//            var texture = ToTexture2D(renderTexture);
//            textureDebugger.material.mainTexture = texture;
//            await Task.Delay(32);

//            Debug.Log("to texture 2d");
//            var foundPalm = await dnnProcessor.DetectPalm(texture);
//            Debug.Log("detected palm");

//            Destroy(texture);
//            Destroy(cameraTransform.gameObject);
//        }
//    }

//    private Transform CopyCameraTransForm(Camera camera)
//    {
//        var g = new GameObject
//        {
//            transform =
//                {
//                    position = camera.transform.position,
//                    rotation = camera.transform.rotation,
//                    localScale = camera.transform.localScale
//                }
//        };
//        return g.transform;
//    }

//    private Texture2D ToTexture2D(RenderTexture rTex)
//    {
//        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
//        var oldRt = RenderTexture.active;
//        RenderTexture.active = rTex;

//        tex.ReadPixels(new UnityEngine.Rect(0, 0, rTex.width, rTex.height), 0, 0);
//        tex.Apply();

//        RenderTexture.active = oldRt;
//        return tex;
//    }


//}
