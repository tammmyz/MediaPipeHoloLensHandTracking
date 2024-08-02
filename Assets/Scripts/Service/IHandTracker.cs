using OpenCVForUnity.CoreModule;
using RealityCollective.ServiceFramework.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace HandTracking.Interfaces
{
    public interface IHandTracker : IService
    {
        Task<Mat> DetectPalms(Texture2D texture);
        Task<Mat> EstimateHandPose(Texture2D handTexture, Mat detectedPalms);
        Task<Mat> EstimateHandPose(Texture2D handTexture, Mat detectedPalms, Renderer d1, Renderer d2);
        string attributeToJSON(string key, string value, string lastLineEnd=",\n");
        string jointToJSON(int i, float inferenceTime=-9999, string lastLineEnd=",\n");
    }
}