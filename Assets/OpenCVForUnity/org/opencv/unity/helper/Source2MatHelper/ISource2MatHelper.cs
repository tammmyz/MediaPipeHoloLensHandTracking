using OpenCVForUnity.CoreModule;
using UnityEngine.Events;

namespace OpenCVForUnity.UnityUtils.Helper
{
    public interface ISource2MatHelper
    {
        Source2MatHelperColorFormat outputColorFormat
        {
            get;
            set;
        }

        int timeoutFrameCount
        {
            get;
            set;
        }

        UnityEvent onInitialized
        {
            get;
            set;
        }

        UnityEvent onDisposed
        {
            get;
            set;
        }

        Source2MatHelperErrorUnityEvent onErrorOccurred
        {
            get;
            set;
        }

        void Initialize(bool autoPlay);

        bool IsInitialized();

        void Play();

        void Pause();

        void Stop();

        bool IsPlaying();

        bool IsPaused();

        string GetDeviceName();

        int GetWidth();

        int GetHeight();

        Source2MatHelperColorFormat GetBaseColorFormat();

        bool DidUpdateThisFrame();

        Mat GetMat();

        void Dispose();
    }
}