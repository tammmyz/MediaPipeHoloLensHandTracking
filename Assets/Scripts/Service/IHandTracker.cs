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
    }
}