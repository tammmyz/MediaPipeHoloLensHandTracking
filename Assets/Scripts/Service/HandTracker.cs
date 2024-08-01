using HandTracking.Interfaces;
using OpenCVForUnity.CoreModule;
using RealityCollective.ServiceFramework.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;

namespace HandTracking
{
    [System.Runtime.InteropServices.Guid("21b8764c-f9e8-4ce2-8c86-d22330602ede")]
    public class HandTracker : BaseServiceWithConstructor, IHandTracker
    {
        private IWorker worker;

        private readonly HandTrackerProfile profile;
        private MediaPipePalmDetector palmDetector;
        private MediaPipeHandPoseEstimator handPoseEstimator;
        public HandTracker(string name, uint priority, HandTrackerProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
            palmDetector = new MediaPipePalmDetector(profile.PalmDetectorAsset);
            handPoseEstimator = new MediaPipeHandPoseEstimator(profile.HandPoseEstimatorAsset);
        }

        public override void Initialize()
        {
            palmDetector.Initialize();
        }

        public async Task<Mat> DetectPalms(Texture2D texture)
        {
            //Debug.Log("Palm detected - dummy");
            //List<int> result = await GetDummyTask();

            var result = await palmDetector.StartAsync(texture);
            await Task.Delay(32);
            Debug.Log($"id: {result}");
            return result;
        }

        //private static Task<List<int>> GetDummyTask()
        //{
        //    List<int> dummyList = new List<int> { 1, 2, 3, 4, 5 };
        //    return Task.FromResult(dummyList);
        //}

        public override void Destroy()
        {
            
        }
    }
}
