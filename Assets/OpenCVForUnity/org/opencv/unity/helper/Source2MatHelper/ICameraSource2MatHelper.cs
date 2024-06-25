using UnityEngine;

namespace OpenCVForUnity.UnityUtils.Helper
{
    public interface ICameraSource2MatHelper : ISource2MatHelper
    {
        string requestedDeviceName
        {
            get;
            set;
        }

        int requestedWidth
        {
            get;
            set;
        }

        int requestedHeight
        {
            get;
            set;
        }

        bool requestedIsFrontFacing
        {
            get;
            set;
        }

        float requestedFPS
        {
            get;
            set;
        }

        bool IsFrontFacing();

        float GetFPS();

        Matrix4x4 GetCameraToWorldMatrix();

        Matrix4x4 GetProjectionMatrix();
    }
}