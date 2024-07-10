
using OpenCVForUnityExample.DnnModel;
using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.ServiceFramework.Interfaces;
using Unity.Barracuda;
using UnityEngine;

namespace HandTracking
{
    [CreateAssetMenu(menuName = "HandTrackerProfile", fileName = "HandTrackerProfile", order = (int)CreateProfileMenuItemIndices.ServiceConfig)]
    public class HandTrackerProfile : BaseServiceProfile<IServiceModule>
    {
        [SerializeField]
        private MediaPipePalmDetector palmDetector;
        public MediaPipePalmDetector PalmDetector => palmDetector;
    }
}
