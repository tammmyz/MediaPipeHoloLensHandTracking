namespace OpenCVForUnity.UnityUtils.Helper
{
    public interface IImageSource2MatHelper : ISource2MatHelper
    {
        string requestedImageFilePath
        {
            get;
            set;
        }

        bool repeat
        {
            get;
            set;
        }
    }
}