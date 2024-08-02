
using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.ServiceFramework.Interfaces;
using Unity.Sentis;
using UnityEngine;

namespace HandTracking
{
    [CreateAssetMenu(menuName = "HandTrackerProfile", fileName = "HandTrackerProfile", order = (int)CreateProfileMenuItemIndices.ServiceConfig)]
    public class HandTrackerProfile : BaseServiceProfile<IServiceModule>
    {
        [SerializeField]
        private ModelAsset palmDetectorAsset;
        public ModelAsset PalmDetectorAsset => palmDetectorAsset;

        [SerializeField]
        private ModelAsset handPoseEstimatorAsset;
        public ModelAsset HandPoseEstimatorAsset => handPoseEstimatorAsset;

        [SerializeField]
        private Renderer debugger1;
        public Renderer Debugger1 => debugger1;

        [SerializeField]
        private Renderer debugger2;
        public Renderer Debugger2 => debugger2;
    }
}
