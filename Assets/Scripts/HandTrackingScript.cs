using HandTracking.Interfaces;
using RealityCollective.ServiceFramework.Services;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

public class HandTrackingScript : MonoBehaviour
{
    private IHandTracker handtracker;

    [SerializeField]
    private Vector2Int requestedCameraSize = new(896, 504);

    [SerializeField]
    private int cameraFPS = 4;

    private WebCamTexture webCamTexture;

    void Start()
    {
        handtracker = ServiceManager.Instance.GetService<IHandTracker>();
        webCamTexture = new WebCamTexture(requestedCameraSize.x, requestedCameraSize.y, cameraFPS);
        webCamTexture.Play();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
