using OpenCVForUnity.ImgprocModule;

namespace OpenCVForUnity.UnityUtils.Helper
{
    public class Source2MatHelperUtils
    {
        static public int Channels(Source2MatHelperColorFormat type)
        {
            switch (type)
            {
                case Source2MatHelperColorFormat.GRAY:
                    return 1;
                case Source2MatHelperColorFormat.RGB:
                case Source2MatHelperColorFormat.BGR:
                    return 3;
                case Source2MatHelperColorFormat.RGBA:
                case Source2MatHelperColorFormat.BGRA:
                    return 4;
                default:
                    return 4;
            }
        }

        static public int ColorConversionCodes(Source2MatHelperColorFormat srcType, Source2MatHelperColorFormat dstType)
        {
            if (srcType == Source2MatHelperColorFormat.GRAY)
            {
                if (dstType == Source2MatHelperColorFormat.RGB) return Imgproc.COLOR_GRAY2RGB;
                else if (dstType == Source2MatHelperColorFormat.BGR) return Imgproc.COLOR_GRAY2BGR;
                else if (dstType == Source2MatHelperColorFormat.RGBA) return Imgproc.COLOR_GRAY2RGBA;
                else if (dstType == Source2MatHelperColorFormat.BGRA) return Imgproc.COLOR_GRAY2BGRA;
            }
            else if (srcType == Source2MatHelperColorFormat.RGB)
            {
                if (dstType == Source2MatHelperColorFormat.GRAY) return Imgproc.COLOR_RGB2GRAY;
                else if (dstType == Source2MatHelperColorFormat.BGR) return Imgproc.COLOR_RGB2BGR;
                else if (dstType == Source2MatHelperColorFormat.RGBA) return Imgproc.COLOR_RGB2RGBA;
                else if (dstType == Source2MatHelperColorFormat.BGRA) return Imgproc.COLOR_RGB2BGRA;
            }
            else if (srcType == Source2MatHelperColorFormat.BGR)
            {
                if (dstType == Source2MatHelperColorFormat.GRAY) return Imgproc.COLOR_BGR2GRAY;
                else if (dstType == Source2MatHelperColorFormat.RGB) return Imgproc.COLOR_BGR2RGB;
                else if (dstType == Source2MatHelperColorFormat.RGBA) return Imgproc.COLOR_BGR2RGBA;
                else if (dstType == Source2MatHelperColorFormat.BGRA) return Imgproc.COLOR_BGR2BGRA;
            }
            else if (srcType == Source2MatHelperColorFormat.RGBA)
            {
                if (dstType == Source2MatHelperColorFormat.GRAY) return Imgproc.COLOR_RGBA2GRAY;
                else if (dstType == Source2MatHelperColorFormat.RGB) return Imgproc.COLOR_RGBA2RGB;
                else if (dstType == Source2MatHelperColorFormat.BGR) return Imgproc.COLOR_RGBA2BGR;
                else if (dstType == Source2MatHelperColorFormat.BGRA) return Imgproc.COLOR_RGBA2BGRA;
            }
            else if (srcType == Source2MatHelperColorFormat.BGRA)
            {
                if (dstType == Source2MatHelperColorFormat.GRAY) return Imgproc.COLOR_BGRA2GRAY;
                else if (dstType == Source2MatHelperColorFormat.RGB) return Imgproc.COLOR_BGRA2RGB;
                else if (dstType == Source2MatHelperColorFormat.BGR) return Imgproc.COLOR_BGRA2BGR;
                else if (dstType == Source2MatHelperColorFormat.RGBA) return Imgproc.COLOR_BGRA2RGBA;
            }

            return -1;
        }
    }
}
