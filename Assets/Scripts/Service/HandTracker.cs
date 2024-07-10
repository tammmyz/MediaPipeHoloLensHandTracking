using HandTracking.Interfaces;
using Microsoft.MixedReality.Toolkit;
using OpenCVForUnityExample.DnnModel;
using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.ServiceFramework.Interfaces;
using RealityCollective.ServiceFramework.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Barracuda;
using UnityEngine;

namespace HandTracking
{
    [System.Runtime.InteropServices.Guid("21b8764c-f9e8-4ce2-8c86-d22330602ede")]
    public class HandTracker : BaseServiceWithConstructor, IHandTracker
    {
        private IWorker worker;

        private readonly HandTrackerProfile profile;
        private MediaPipePalmDetector palmDetector;
        public HandTracker(string name, uint priority, HandTrackerProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
            this.palmDetector = profile.PalmDetector;
        }

        public async Task<List<int>> DetectPalm(Texture2D texture)
        {
            Debug.Log("Palm detected - dummy");
            List<int> result = await GetDummyTask();
            return result;
        }

        static Task<List<int>> GetDummyTask()
        {
            List<int> dummyList = new List<int> { 1, 2, 3, 4, 5 };
            return Task.FromResult(dummyList);
        }

        // Below are the Unity events that are replicated by the Service Framework, simply delete any that are not required.
        #region MonoBehaviour callbacks
        /// <inheritdoc />
        public override void Initialize()
        {
            // Initialize is called when the Service Framework first instantiates the service.  ( during MonoBehaviour 'Awake')
            // This is called AFTER all services have been registered but before the 'Start' call.
        }

        /// <inheritdoc />
        public override void Start()
        {
            // Start is called when the Service Framework receives the "Start" call on loading of the Scene it is attached to.
		    // If "Do Not Destroy" is enabled on the Root Service Profile, this is received only once on startup, Else it will occur for each scene load with a Service Framework Instance.
        }

        /// <inheritdoc />
        public override void Reset()
        {
            // Whenever the Service Framework is forcibly "Reset" whilst running, each service will also receive the "Reset" call to request they reinitialize.
        }

        /// <inheritdoc />
        public override void Update()
        {
            // The Unity "Update" MonoBehaviour, this is called when the Service Manager Instance receives the Update Event.
        }

        /// <inheritdoc />
        public override void LateUpdate()
        {
            // The Unity "LateUpdate" MonoBehaviour, this is called when the Service Manager Instance receives the LateUpdate Event.
        }

        /// <inheritdoc />
        public override void FixedUpdate()
        {
            // The Unity "FixedUpdate" MonoBehaviour, this is called when the Service Manager Instance receives the FixedUpdate Event.
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            // The Unity "Destroy" MonoBehaviour, this is called when the Service Manager Instance receives the Destroy Event.
        }

        /// <inheritdoc />
        public override void OnApplicationFocus(bool isFocused)
        {
            // The Unity "OnApplicationFocus" MonoBehaviour, this is called when Unity generates the OnFocus event on App start or resume.
        }

        /// <inheritdoc />
        public override void OnApplicationPause(bool isPaused)
        {
            // The Unity "OnApplicationPause" MonoBehaviour, this is called when Unity generates the OnPause event on App pauses or is about to suspend.
        }        
        #endregion MonoBehaviour callbacks
    }
}
