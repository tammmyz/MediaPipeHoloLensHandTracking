using HandTracking.Interfaces;
using OpenCVForUnity.CoreModule;
using RealityCollective.ServiceFramework.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace HandTracking
{
    [System.Runtime.InteropServices.Guid("21b8764c-f9e8-4ce2-8c86-d22330602ede")]
    public class HandTracker : BaseServiceWithConstructor, IHandTracker
    {
        private readonly HandTrackerProfile profile;
        private MediaPipePalmDetector palmDetector;
        private MediaPipeHandPoseEstimator handPoseEstimator;

        public Vector3[] thumbPos;
        public Vector3[] indexPos;
        public Vector3[] middlePos;
        public Vector3[] ringPos;
        public Vector3[] pinkyPos;
        public Vector3 wristPos;

        private static int WORLD_LANDMARK_INDEX = 67;
        private static int NUM_JOINTS = 3;
        private int WRIST_POS_INDEX = WORLD_LANDMARK_INDEX + 0;
        private int THUMB_BASE_POS_INDEX = WORLD_LANDMARK_INDEX + 1 * 3;
        private int INDEX_BASE_POS_INDEX = WORLD_LANDMARK_INDEX + 5 * 3;
        private int MIDDLE_BASE_POS_INDEX = WORLD_LANDMARK_INDEX + 9 * 3;
        private int RING_BASE_POS_INDEX = WORLD_LANDMARK_INDEX + 13 * 3;
        private int PINKY_BASE_POS_INDEX = WORLD_LANDMARK_INDEX + 17 * 3;

        private string indent = "  ";

        public HandTracker(string name, uint priority, HandTrackerProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
            palmDetector = new MediaPipePalmDetector(profile.PalmDetectorAsset);
            handPoseEstimator = new MediaPipeHandPoseEstimator(profile.HandPoseEstimatorAsset);
            thumbPos = new Vector3[NUM_JOINTS];
            indexPos = new Vector3[NUM_JOINTS];
            middlePos = new Vector3[NUM_JOINTS];
            ringPos = new Vector3[NUM_JOINTS];
            pinkyPos = new Vector3[NUM_JOINTS];
            wristPos = new Vector3();
        }

        public override void Initialize()
        {
            palmDetector.Initialize();
            handPoseEstimator.Initialize();
        }

        public async Task<Mat> DetectPalms(Texture2D texture)
        {
            var palms = await palmDetector.StartAsync(texture);
            await Task.Delay(32);
            //Debug.Log($"palms: {palms.dump()}");
            return palms;
        }

        public async Task<Mat> EstimateHandPose(Texture2D handTexture, Mat detectedPalms)
        {
            var handPose = await handPoseEstimator.StartAsync(handTexture, detectedPalms);
            await Task.Delay(32);
            Debug.Log($"hand pose: {handPose.size()} {handPose.dump()}");
            storeHandPose(handPose);
            return handPose;
        }

        public async Task<Mat> EstimateHandPose(Texture2D handTexture, Mat detectedPalms, Renderer d1, Renderer d2)
        {
            var handPose = await handPoseEstimator.StartAsyncDebug(handTexture, detectedPalms, d1, d2);
            await Task.Delay(32);
            Debug.Log($"hand pose: {handPose.size()} {handPose.rows()}  {handPose.dump()}");
            if (handPose.rows() > 0)
            {
                storeHandPose(handPose);
            }
            return handPose;
        }

        public string attributeToJSON(string key, string value, string lastLineEnd = ",\n")
        {
            return $"{lastLineEnd}{indent}\"{key}\": \"{value}\"";
        }

        // Method to format the pose estimation data as a JSON
        // @Returns String formatted with joint and inference data
        public string jointToJSON(int i, float inferenceTime = -9999, string lastLineEnd = ",\n")
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string export = $"{lastLineEnd}{indent}\"{i}\": {{\n";
            export += indent + formatVector3List("thumb", thumbPos, indent + indent);
            export += indent + formatVector3List("index", indexPos, indent + indent);
            export += indent + formatVector3List("middle", middlePos, indent + indent);
            export += indent + formatVector3List("ring", ringPos, indent + indent);
            export += indent + formatVector3List("pinky", pinkyPos, indent + indent);
            export += $"{indent}{indent}\"wrist\": [{wristPos.x}, {wristPos.y}, {wristPos.z}],\n";
            if (inferenceTime >= 0)
            {
                export += $"{indent}{indent}\"inference_time\": {inferenceTime},\n";
            }
            export += $"{indent}{indent}\"timestamp\": \"{timestamp}\"\n";
            export += $"{indent}}}";
            return export;
        }

        private void storeHandPose(Mat handPose)
        {
            //Debug.Log($"storing hand pose, {handPose.get(1, 0)[0]}");
            wristPos = mat2Vector3(handPose, WRIST_POS_INDEX);
            thumbPos = mat2Vector3Array(handPose, THUMB_BASE_POS_INDEX, NUM_JOINTS);
            indexPos = mat2Vector3Array(handPose, INDEX_BASE_POS_INDEX, NUM_JOINTS);
            middlePos = mat2Vector3Array(handPose, MIDDLE_BASE_POS_INDEX, NUM_JOINTS);
            ringPos = mat2Vector3Array(handPose, RING_BASE_POS_INDEX, NUM_JOINTS);
            pinkyPos = mat2Vector3Array(handPose, PINKY_BASE_POS_INDEX, NUM_JOINTS);
        }

        private Vector3 mat2Vector3(Mat mat, int startIndex)
        {
            Vector3 vector = new Vector3(
                (float)mat.get(startIndex, 0)[0],
                (float)mat.get(startIndex + 1, 0)[0],
                (float)mat.get(startIndex + 2, 0)[0]
            );
            return vector;
        }

        private Vector3[] mat2Vector3Array(Mat mat, int startIndex, int N)
        {
            Vector3[] vec3array = new Vector3[N];
            for (int i = 0; i < N; i++) 
            {
                vec3array[i] = mat2Vector3(mat, startIndex + i * N);
            }
            return vec3array;
        }


        // Helper method for toTxt, formats a Vector3 array as JSON-formatted text
        // @param label: key value for vector, assuming "key": [vector] in JSON format
        // @param joints: vector array to be reformatted
        // @param indent: value used as indentation
        // @Returns String representing formatted array
        private string formatVector3List(string label, Vector3[] joints, string indent = "")
        {
            string export = $"{indent}\"{label}\": [\n";
            Vector3 joint;
            for (int i = 0; i < joints.Length - 1; i++)
            {
                joint = joints[i];
                export += $"{indent}{indent}[{joint.x}, {joint.y}, {joint.z}],\n";
            }
            joint = joints[joints.Length - 1];
            export += $"{indent}{indent}[{joint.x}, {joint.y}, {joint.z}]\n";
            export += $"{indent}],\n";
            return export;
        }

        public override void Destroy()
        {
            palmDetector.Destroy();
            handPoseEstimator.Destroy();
        }
    }
}
