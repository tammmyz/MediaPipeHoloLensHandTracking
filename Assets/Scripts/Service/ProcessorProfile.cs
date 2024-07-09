
using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.ServiceFramework.Interfaces;
using Unity.Barracuda;
using UnityEngine;

namespace Dnn
{
    [CreateAssetMenu(menuName = "ProcessorProfile", fileName = "ProcessorProfile", order = (int)CreateProfileMenuItemIndices.ServiceConfig)]
    public class ProcessorProfile : BaseServiceProfile<IServiceModule>
    { 
        [SerializeField]
        private NNModel model;
        public NNModel Model => model;

        [SerializeField]
        private float minimumProbability = 0.65f;
        public float MinimumProbability => minimumProbability;

        [SerializeField]
        private float overlapThreshold = 0.5f;
        public float OverlapThreshold => overlapThreshold;

        [SerializeField]
        private int channels = 3;

        public int Channels => channels;

    }
}
