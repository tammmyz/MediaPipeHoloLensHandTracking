using OpenCVForUnity.CoreModule;
using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

public class HandPoseData : MonoBehaviour
{
    public List<float[]> outputs { get; }
    public TensorShape[] dimensions { get; }
    public int[] types { get; }

    public HandPoseData(TensorFloat out1, TensorFloat out2, TensorFloat out3, TensorFloat out4)
    {
        dimensions = new TensorShape[] { out1.shape, out2.shape, out3.shape, out4.shape };
        var output1 = out1.ToReadOnlyArray();
        var output2 = out2.ToReadOnlyArray();
        var output3 = out3.ToReadOnlyArray();
        var output4 = out4.ToReadOnlyArray();
        outputs = new List<float[]>
        {
            output1,
            output2,
            output3,
            output4
        };
    }
}
