namespace OpenCVForUnity.UnityUtils.Helper
{
    public interface IVideoSource2MatHelper : ISource2MatHelper
    {
        string requestedVideoFilePath
        {
            get;
            set;
        }

        bool loop
        {
            get;
            set;
        }

        float GetFPS();

        float GetFramePosRatio();

        void SetFramePosRatio(float ratio);

        int GetFrameIndex();

        void SetFrameIndex(int index);

        int GetFrameCount();
    }
}