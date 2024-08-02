using OpenCVForUnity.CoreModule;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

public class PalmData : MonoBehaviour
{
    public List<float[]> outputs { get; }
    public TensorShape[] dimensions { get; }
    public int[] types { get; }

    public PalmData(TensorFloat outputTensor1, TensorFloat outputTensor2)
    {
        dimensions = new TensorShape[] { outputTensor1.shape, outputTensor2.shape };
        var output1 = outputTensor1.ToReadOnlyArray();
        var output2 = outputTensor2.ToReadOnlyArray();
        outputs = new List<float[]>
        {
            output1,
            output2
        };
        types = new int[] 
        {
            CvType.CV_32FC(dimensions[0][2]),
            CvType.CV_32FC(dimensions[1][2])
        };
    }

    public PalmData() { }


}
