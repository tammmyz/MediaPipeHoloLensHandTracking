
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
    }
}