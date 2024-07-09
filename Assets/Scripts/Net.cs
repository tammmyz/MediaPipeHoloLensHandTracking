using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Barracuda;
using UnityEngine;

namespace DnnModule
{
    public class Net : MonoBehaviour
    {
        [SerializeField]
        public NNModel modelAsset;
        private IWorker worker;
        public void Initialize(string filepath)
        {
            var model = ModelLoader.Load(modelAsset);
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
            Debug.Log("Model loaded");
        }

    }
} 