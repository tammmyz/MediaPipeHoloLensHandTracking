using Dnn.Interfaces;

using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.ServiceFramework.Interfaces;
using RealityCollective.ServiceFramework.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Barracuda;
using UnityEngine;

namespace Dnn
{
    [System.Runtime.InteropServices.Guid("73f0c0b8-0d21-4501-b494-d2f07fadf667")]
    public class Processor : BaseServiceWithConstructor, IProcessor
    {
        private IWorker worker;

        private readonly ProcessorProfile profile;
        public Processor(string name, uint priority, ProcessorProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
            // The constructor should be used to gather required properties from the profile (or cache the profile) and to ready any components needed.
            // Note, during this call, not all services will be registered with the Service Framework, so this should only be used to ready this individual service.
        }
        public override void Initialize()
        {
            var model = ModelLoader.Load(profile.Model);
            //Debug.Log("Model Inputs:");
            //foreach (var input in model.inputs)
            //{
            //    Debug.Log($"Name: {input.name}, Shape: {string.Join(", ", input.shape)}");
            //}

            //// Inspect model outputs
            //Debug.Log("Model Outputs:");
            //foreach (var output in model.outputs)
            //{
            //    Debug.Log($"Name: {output}");
            //}
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        }

        public async Task<List<int>> DetectPalm(Texture2D texture)
        {
            Debug.Log("starting detect palm");
            Tensor inputTensor = null;
            try
            {
                //inputTensor = new Tensor(texture, channels: 3);
                var inputShape = new TensorShape(1, 192, 192, 3);
                var colourPixels = texture.GetPixels32();
                inputTensor = new Tensor(inputShape.ToArray(), ConvertToFloatArray(colourPixels));
                Debug.Log("Input tensor:\t" + inputTensor.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating input tensor: {e}");
                throw;
            }
            //var inputTensor = new Tensor(texture, channels: profile.Channels);
            //Debug.Log(inputTensor.ToString());
            await Task.Delay(32);
            Debug.Log("create input tensor");
            Tensor outputTensor = null;
            try
            {
                outputTensor = await ForwardAsync(worker, inputTensor);
                Debug.Log("got output tensor");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during ForwardAsync: {e}");
                throw;
            }
            finally
            {
                inputTensor.Dispose();
                Debug.Log("disposed input tensor");
            }
            //var outputTensor = await ForwardAsync(worker, inputTensor);
            Debug.Log("got output tensor");
            inputTensor.Dispose();
            Debug.Log("disposed input tensor");

            var result = GetData(outputTensor);
            Debug.Log("got output tensor data");
            outputTensor.Dispose();
            return result;
        }

        private float[] ConvertToFloatArray(Color32[] pixels)
        {
            // Create a float array with the size 4 times the length of the Color array (RGBA -> 4 values per pixel)
            float[] floatArray = new float[pixels.Length * 4];

            for (int i = 0; i < pixels.Length; i++)
            {
                // Directly assign the float values from Color (which are already in the range 0.0-1.0)
                floatArray[i * 4 + 0] = pixels[i].r; // Red channel
                floatArray[i * 4 + 1] = pixels[i].g; // Green channel
                floatArray[i * 4 + 2] = pixels[i].b; // Blue channel
                floatArray[i * 4 + 3] = pixels[i].a; // Alpha channel
            }

            return floatArray;
        }

        public List<int> GetData(Tensor tensor)
        {
            Debug.Log("getting data");
            var boxesMeetingConfidenceLevel = new List<int>();
            for (var i = 0; i < tensor.channels; i++)
            {
                if (tensor[0, 0, 4, i] > 0.8)
                {
                    boxesMeetingConfidenceLevel.Add(i);
                }
            }
            return boxesMeetingConfidenceLevel;
        }

        public async Task<Tensor> ForwardAsync(IWorker modelWorker, Tensor inputs)
        {
            var executor = worker.StartManualSchedule(inputs);
            var it = 0;
            bool hasMoreWork;
            do
            {
                hasMoreWork = executor.MoveNext();
                if (++it % 20 == 0)
                {
                    worker.FlushSchedule();
                    await Task.Delay(32);
                }
            } while (hasMoreWork);

            return modelWorker.PeekOutput();
        }

        public override void Destroy()
        {
            // Dispose of the Barracuda worker when it is no longer needed
            worker?.Dispose();
        }
    }
}
