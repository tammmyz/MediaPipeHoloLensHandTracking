
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity.ObjdetectModule
{

    // C++: class BarcodeDetector


    public class BarcodeDetector : GraphicalCodeDetector
    {

        protected override void Dispose(bool disposing)
        {

            try
            {
                if (disposing)
                {
                }
                if (IsEnabledDispose)
                {
                    if (nativeObj != IntPtr.Zero)
                        objdetect_BarcodeDetector_delete(nativeObj);
                    nativeObj = IntPtr.Zero;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }

        }

        protected internal BarcodeDetector(IntPtr addr) : base(addr) { }

        // internal usage only
        public static new BarcodeDetector __fromPtr__(IntPtr addr) { return new BarcodeDetector(addr); }

        //
        // C++:   cv::barcode::BarcodeDetector::BarcodeDetector()
        //

        /**
         @brief Initialize the BarcodeDetector.
         */
        public BarcodeDetector() :
            base(DisposableObject.ThrowIfNullIntPtr(objdetect_BarcodeDetector_BarcodeDetector_10()))
        {



        }


        //
        // C++:   cv::barcode::BarcodeDetector::BarcodeDetector(string prototxt_path, string model_path)
        //

        /**
         @brief Initialize the BarcodeDetector.
              *
              * Parameters allow to load _optional_ Super Resolution DNN model for better quality.
              * @param prototxt_path prototxt file path for the super resolution model
              * @param model_path model file path for the super resolution model
         */
        public BarcodeDetector(string prototxt_path, string model_path) :
            base(DisposableObject.ThrowIfNullIntPtr(objdetect_BarcodeDetector_BarcodeDetector_11(prototxt_path, model_path)))
        {



        }


        //
        // C++:  bool cv::barcode::BarcodeDetector::decodeWithType(Mat img, Mat points, vector_string& decoded_info, vector_string& decoded_type)
        //

        /**
         @brief Decodes barcode in image once it's found by the detect() method.
              *
              * @param img grayscale or color (BGR) image containing bar code.
              * @param points vector of rotated rectangle vertices found by detect() method (or some other algorithm).
              * For N detected barcodes, the dimensions of this array should be [N][4].
              * Order of four points in vector&lt;Point2f&gt; is bottomLeft, topLeft, topRight, bottomRight.
              * @param decoded_info UTF8-encoded output vector of string or empty vector of string if the codes cannot be decoded.
              * @param decoded_type vector strings, specifies the type of these barcodes
              * @return true if at least one valid barcode have been found
         */
        public bool decodeWithType(Mat img, Mat points, List<string> decoded_info, List<string> decoded_type)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            Mat decoded_type_mat = new Mat();
            bool retVal = objdetect_BarcodeDetector_decodeWithType_10(nativeObj, img.nativeObj, points.nativeObj, decoded_info_mat.nativeObj, decoded_type_mat.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            Converters.Mat_to_vector_string(decoded_type_mat, decoded_type);
            decoded_type_mat.release();
            return retVal;
        }


        //
        // C++:  bool cv::barcode::BarcodeDetector::detectAndDecodeWithType(Mat img, vector_string& decoded_info, vector_string& decoded_type, Mat& points = Mat())
        //

        /**
         @brief Both detects and decodes barcode
         
              * @param img grayscale or color (BGR) image containing barcode.
              * @param decoded_info UTF8-encoded output vector of string(s) or empty vector of string if the codes cannot be decoded.
              * @param decoded_type vector of strings, specifies the type of these barcodes
              * @param points optional output vector of vertices of the found  barcode rectangle. Will be empty if not found.
              * @return true if at least one valid barcode have been found
         */
        public bool detectAndDecodeWithType(Mat img, List<string> decoded_info, List<string> decoded_type, Mat points)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            if (points != null) points.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            Mat decoded_type_mat = new Mat();
            bool retVal = objdetect_BarcodeDetector_detectAndDecodeWithType_10(nativeObj, img.nativeObj, decoded_info_mat.nativeObj, decoded_type_mat.nativeObj, points.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            Converters.Mat_to_vector_string(decoded_type_mat, decoded_type);
            decoded_type_mat.release();
            return retVal;
        }

        /**
         @brief Both detects and decodes barcode
         
              * @param img grayscale or color (BGR) image containing barcode.
              * @param decoded_info UTF8-encoded output vector of string(s) or empty vector of string if the codes cannot be decoded.
              * @param decoded_type vector of strings, specifies the type of these barcodes
              * @param points optional output vector of vertices of the found  barcode rectangle. Will be empty if not found.
              * @return true if at least one valid barcode have been found
         */
        public bool detectAndDecodeWithType(Mat img, List<string> decoded_info, List<string> decoded_type)
        {
            ThrowIfDisposed();
            if (img != null) img.ThrowIfDisposed();
            Mat decoded_info_mat = new Mat();
            Mat decoded_type_mat = new Mat();
            bool retVal = objdetect_BarcodeDetector_detectAndDecodeWithType_11(nativeObj, img.nativeObj, decoded_info_mat.nativeObj, decoded_type_mat.nativeObj);
            Converters.Mat_to_vector_string(decoded_info_mat, decoded_info);
            decoded_info_mat.release();
            Converters.Mat_to_vector_string(decoded_type_mat, decoded_type);
            decoded_type_mat.release();
            return retVal;
        }


        //
        // C++:  double cv::barcode::BarcodeDetector::getDownsamplingThreshold()
        //

        /**
         @brief Get detector downsampling threshold.
              *
              * @return detector downsampling threshold
         */
        public double getDownsamplingThreshold()
        {
            ThrowIfDisposed();

            return objdetect_BarcodeDetector_getDownsamplingThreshold_10(nativeObj);


        }


        //
        // C++:  BarcodeDetector cv::barcode::BarcodeDetector::setDownsamplingThreshold(double thresh)
        //

        /**
         @brief Set detector downsampling threshold.
              *
              * By default, the detect method resizes the input image to this limit if the smallest image size is is greater than the threshold.
              * Increasing this value can improve detection accuracy and the number of results at the expense of performance.
              * Correlates with detector scales. Setting this to a large value will disable downsampling.
              * @param thresh downsampling limit to apply (default 512)
              * @see setDetectorScales
         */
        public BarcodeDetector setDownsamplingThreshold(double thresh)
        {
            ThrowIfDisposed();

            return new BarcodeDetector(DisposableObject.ThrowIfNullIntPtr(objdetect_BarcodeDetector_setDownsamplingThreshold_10(nativeObj, thresh)));


        }


        //
        // C++:  void cv::barcode::BarcodeDetector::getDetectorScales(vector_float& sizes)
        //

        /**
         @brief Returns detector box filter sizes.
              *
              * @param sizes output parameter for returning the sizes.
         */
        public void getDetectorScales(MatOfFloat sizes)
        {
            ThrowIfDisposed();
            if (sizes != null) sizes.ThrowIfDisposed();
            Mat sizes_mat = sizes;
            objdetect_BarcodeDetector_getDetectorScales_10(nativeObj, sizes_mat.nativeObj);


        }


        //
        // C++:  BarcodeDetector cv::barcode::BarcodeDetector::setDetectorScales(vector_float sizes)
        //

        /**
         @brief Set detector box filter sizes.
              *
              * Adjusts the value and the number of box filters used in the detect step.
              * The filter sizes directly correlate with the expected line widths for a barcode. Corresponds to expected barcode distance.
              * If the downsampling limit is increased, filter sizes need to be adjusted in an inversely proportional way.
              * @param sizes box filter sizes, relative to minimum dimension of the image (default [0.01, 0.03, 0.06, 0.08])
         */
        public BarcodeDetector setDetectorScales(MatOfFloat sizes)
        {
            ThrowIfDisposed();
            if (sizes != null) sizes.ThrowIfDisposed();
            Mat sizes_mat = sizes;
            return new BarcodeDetector(DisposableObject.ThrowIfNullIntPtr(objdetect_BarcodeDetector_setDetectorScales_10(nativeObj, sizes_mat.nativeObj)));


        }


        //
        // C++:  double cv::barcode::BarcodeDetector::getGradientThreshold()
        //

        /**
         @brief Get detector gradient magnitude threshold.
              *
              * @return detector gradient magnitude threshold.
         */
        public double getGradientThreshold()
        {
            ThrowIfDisposed();

            return objdetect_BarcodeDetector_getGradientThreshold_10(nativeObj);


        }


        //
        // C++:  BarcodeDetector cv::barcode::BarcodeDetector::setGradientThreshold(double thresh)
        //

        /**
         @brief Set detector gradient magnitude threshold.
              *
              * Sets the coherence threshold for detected bounding boxes.
              * Increasing this value will generate a closer fitted bounding box width and can reduce false-positives.
              * Values between 16 and 1024 generally work, while too high of a value will remove valid detections.
              * @param thresh gradient magnitude threshold (default 64).
         */
        public BarcodeDetector setGradientThreshold(double thresh)
        {
            ThrowIfDisposed();

            return new BarcodeDetector(DisposableObject.ThrowIfNullIntPtr(objdetect_BarcodeDetector_setGradientThreshold_10(nativeObj, thresh)));


        }


#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        const string LIBNAME = "__Internal";
#else
        const string LIBNAME = "opencvforunity";
#endif



        // C++:   cv::barcode::BarcodeDetector::BarcodeDetector()
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_BarcodeDetector_BarcodeDetector_10();

        // C++:   cv::barcode::BarcodeDetector::BarcodeDetector(string prototxt_path, string model_path)
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_BarcodeDetector_BarcodeDetector_11(string prototxt_path, string model_path);

        // C++:  bool cv::barcode::BarcodeDetector::decodeWithType(Mat img, Mat points, vector_string& decoded_info, vector_string& decoded_type)
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_BarcodeDetector_decodeWithType_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr points_nativeObj, IntPtr decoded_info_mat_nativeObj, IntPtr decoded_type_mat_nativeObj);

        // C++:  bool cv::barcode::BarcodeDetector::detectAndDecodeWithType(Mat img, vector_string& decoded_info, vector_string& decoded_type, Mat& points = Mat())
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_BarcodeDetector_detectAndDecodeWithType_10(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr decoded_info_mat_nativeObj, IntPtr decoded_type_mat_nativeObj, IntPtr points_nativeObj);
        [DllImport(LIBNAME)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool objdetect_BarcodeDetector_detectAndDecodeWithType_11(IntPtr nativeObj, IntPtr img_nativeObj, IntPtr decoded_info_mat_nativeObj, IntPtr decoded_type_mat_nativeObj);

        // C++:  double cv::barcode::BarcodeDetector::getDownsamplingThreshold()
        [DllImport(LIBNAME)]
        private static extern double objdetect_BarcodeDetector_getDownsamplingThreshold_10(IntPtr nativeObj);

        // C++:  BarcodeDetector cv::barcode::BarcodeDetector::setDownsamplingThreshold(double thresh)
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_BarcodeDetector_setDownsamplingThreshold_10(IntPtr nativeObj, double thresh);

        // C++:  void cv::barcode::BarcodeDetector::getDetectorScales(vector_float& sizes)
        [DllImport(LIBNAME)]
        private static extern void objdetect_BarcodeDetector_getDetectorScales_10(IntPtr nativeObj, IntPtr sizes_mat_nativeObj);

        // C++:  BarcodeDetector cv::barcode::BarcodeDetector::setDetectorScales(vector_float sizes)
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_BarcodeDetector_setDetectorScales_10(IntPtr nativeObj, IntPtr sizes_mat_nativeObj);

        // C++:  double cv::barcode::BarcodeDetector::getGradientThreshold()
        [DllImport(LIBNAME)]
        private static extern double objdetect_BarcodeDetector_getGradientThreshold_10(IntPtr nativeObj);

        // C++:  BarcodeDetector cv::barcode::BarcodeDetector::setGradientThreshold(double thresh)
        [DllImport(LIBNAME)]
        private static extern IntPtr objdetect_BarcodeDetector_setGradientThreshold_10(IntPtr nativeObj, double thresh);

        // native support for java finalize()
        [DllImport(LIBNAME)]
        private static extern void objdetect_BarcodeDetector_delete(IntPtr nativeObj);

    }
}
