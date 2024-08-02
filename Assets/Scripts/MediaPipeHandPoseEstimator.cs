using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TextureProcUtils;
using Unity.Sentis;
using UnityEngine;
using OpenCVRange = OpenCVForUnity.CoreModule.Range;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;
using Size = OpenCVForUnity.CoreModule.Size;

/// <summary>
/// Referring to EnoxSoftware/OpenCVForUnity HandPoseEstimationMediaPipeExample:
/// https://github.com/EnoxSoftware/OpenCVForUnity/blob/master/Assets/OpenCVForUnity/Examples/MainModules/dnn/HandPoseEstimationMediaPipeExample
/// </summary>
public class MediaPipeHandPoseEstimator
{
    ModelAsset handpose_estimation_net;
    float conf_threshold;

    private Model model;
    private IWorker worker;

    Size input_size = new Size(224, 224);
    Mat tmpImage;
    Mat tmpRotatedImage;

    int PALM_LANDMARKS_INDEX_OF_PALM_BASE = 0;
    int PALM_LANDMARKS_INDEX_OF_MIDDLE_FINGER_BASE = 2;
    Point PALM_BOX_SHIFT_VECTOR = new Point(0, -0.4);
    double PALM_BOX_ENLARGE_FACTOR = 3.0;
    Point HAND_BOX_SHIFT_VECTOR = new Point(0, -0.1);
    double HAND_BOX_ENLARGE_FACTOR = 1.65;

    public MediaPipeHandPoseEstimator(ModelAsset modelAsset, float confThreshold=0.8f)
    {
        handpose_estimation_net = modelAsset;
        conf_threshold = confThreshold;
    }

    // Load model and create worker to run model inference
    public void Initialize()
    {
        model = ModelLoader.Load(handpose_estimation_net);
        worker = WorkerFactory.CreateWorker(BackendType.CPU, model);
    }

    // Performs pipeline for model inference and processing
    public async Task<Mat> StartAsync(Texture2D handTexture, Mat detectedPalms)
    {
        // Initialize variables for processing steps
        Mat rotated_palm_bbox;
        double angle;
        Mat rotation_matrix;
        var pad = Mathf.Abs(handTexture.width - handTexture.height) / 2;
        Mat pad_bias = new Mat(2, 1, CvType.CV_32FC1);
        pad_bias.put(0, 0, new float[] { -pad, -pad });

        // Pre-process texture for model
        Texture2D paddedHandTex = TexProcUtils.pad(handTexture);
        Mat handMat = _texture2DToMat(paddedHandTex);
        var processedTexture = Preprocess(handMat, detectedPalms, out rotated_palm_bbox, out angle, out rotation_matrix);
        var resizedTexture = TexProcUtils.resize(processedTexture, (int)input_size.width, (int)input_size.height);

        // Run model inference
        var outputBlob = await EstimateHandPose(resizedTexture);
        await Task.Delay(32);

        // Post-process model outputs
        Mat handPose = Postprocess(outputBlob, rotated_palm_bbox, angle, rotation_matrix, pad_bias);

        return handPose;
    }

    // Performs pipeline for model inference and processing, for debugging
    public async Task<Mat> StartAsyncDebug(Texture2D handTexture, Mat detectedPalms, Renderer r1, Renderer r2)
    {
        r1.sharedMaterial.mainTexture = handTexture;

        //Texture2D inputTexture = new Texture2D(handTexture.width, handTexture.height, TextureFormat.RGB24, false);
        //Graphics.CopyTexture(handTexture, inputTexture);

        // Initialize variables for processing steps
        Mat rotated_palm_bbox;
        double angle;
        Mat rotation_matrix;
        var pad = Mathf.Abs(handTexture.width - handTexture.height) / 2;
        Mat pad_bias = new Mat(2, 1, CvType.CV_32FC1);
        pad_bias.put(0, 0, new float[] { -pad, -pad });

        // Pre-process texture for model
        Texture2D paddedHandTex = TexProcUtils.pad(handTexture);
        Mat handMat = _texture2DToMat(paddedHandTex);
        //Debug.Log($"handMat: {handMat.size()}");
        var processedTexture = Preprocess(handMat, detectedPalms, out rotated_palm_bbox, out angle, out rotation_matrix);
        r2.sharedMaterial.mainTexture = processedTexture;
        //Debug.Log($"processedTexture (w,h): {processedTexture.width}, {processedTexture.height}");
        var resizedTexture = TexProcUtils.resize(processedTexture, (int)input_size.width, (int)input_size.height);

        //UnityEngine.Object.Destroy(processedTexture);
        //handMat.Dispose();
        //Debug.Log($"resizedTexture (w,h): {resizedTexture.width}, {resizedTexture.height}");

        // Run model inference
        var outputBlob = await EstimateHandPose(resizedTexture);
        await Task.Delay(32);
        Debug.Log($"outputBlob: {outputBlob[0].size()}");

        // Post-process model outputs
        Mat handPose = Postprocess(outputBlob, rotated_palm_bbox, angle, rotation_matrix, pad_bias);
        Debug.Log("finished post processing");

        return handPose;
    }

    // Adapted from EnoxSoftware's OpenCVForUnity HandPoseEstimationMediaPipeExample
    // Reference: https://github.com/EnoxSoftware/OpenCVForUnity/blob/master/Assets/OpenCVForUnity/Examples
    public Texture2D Preprocess(Mat image, Mat palm, out Mat rotated_palm_bbox, out double angle, out Mat rotation_matrix)
    {
        // '''
        // Rotate input for inference.
        // Parameters:
        //  image - input image of BGR channel order
        //  palm_bbox - palm bounding box found in image of format[[x1, y1], [x2, y2]] (top - left and bottom - right points)
        //            palm_landmarks - 7 landmarks(5 finger base points, 2 palm base points) of shape[7, 2]
        // Returns:
        //        rotated_hand - rotated hand image for inference
        //        rotate_palm_bbox - palm box of interest range
        //        angle - rotate angle for hand
        //        rotation_matrix - matrix for rotation and de - rotation
        //        pad_bias - pad pixels of interest range
        // '''

        // Generate an image with padding added after the squarify process.
        int maxSize = Math.Max(image.width(), image.height());
        int tmpImageSize = (int)(maxSize * 1.5);
        if (tmpImage != null && (tmpImage.width() != tmpImageSize || tmpImage.height() != tmpImageSize))
        {
            tmpImage.Dispose();
            tmpImage = null;
            tmpRotatedImage.Dispose();
            tmpRotatedImage = null;
        }
        if (tmpImage == null)
        {
            tmpImage = new Mat(tmpImageSize, tmpImageSize, image.type(), Scalar.all(0));
            tmpRotatedImage = tmpImage.clone();
        }

        Mat _tmpImage_roi = new Mat(tmpImage, new OpenCVRect(0, 0, image.width(), image.height()));
        image.copyTo(_tmpImage_roi);

        // Adjust normalized bbox and landmarks to actual image size
        Mat palm_bbox = palm.colRange(new OpenCVRange(0, 4)).reshape(1, 2);
        Mat palm_landmarks = palm.colRange(new OpenCVRange(4, 18)).reshape(1, 7);
        Scalar scale = new Scalar(image.width(), image.height());
        Mat adjusted_bbox = new Mat(palm_bbox.size(), palm_bbox.type());
        Core.multiply(palm_bbox, scale, adjusted_bbox);
        Mat adjusted_landmarks = new Mat(palm_landmarks.size(), palm_landmarks.type());
        Core.multiply(palm_landmarks, scale, adjusted_landmarks);

        // Rotate input to have vertically oriented hand image compute rotation
        Mat p1 = palm_landmarks.row(PALM_LANDMARKS_INDEX_OF_PALM_BASE);
        Mat p2 = palm_landmarks.row(PALM_LANDMARKS_INDEX_OF_MIDDLE_FINGER_BASE);
        float[] p1_arr = new float[2];
        p1.get(0, 0, p1_arr);
        float[] p2_arr = new float[2];
        p2.get(0, 0, p2_arr);
        double radians = Math.PI / 2 - Math.Atan2(-(p2_arr[1] - p1_arr[1]), p2_arr[0] - p1_arr[0]);
        radians = radians - 2 * Math.PI * Math.Floor((radians + Math.PI) / (2 * Math.PI));
        angle = Mathf.Rad2Deg * radians;

        // get bbox center
        float[] palm_bbox_arr = new float[4];
        adjusted_bbox.get(0, 0, palm_bbox_arr);
        Point center_palm_bbox = new Point((palm_bbox_arr[0] + palm_bbox_arr[2]) / 2, (palm_bbox_arr[1] + palm_bbox_arr[3]) / 2);

        // get rotation matrix
        rotation_matrix = Imgproc.getRotationMatrix2D(center_palm_bbox, angle, 1.0);

        // get bounding boxes from rotated palm landmarks
        Mat rotated_palm_landmarks = new Mat(2, 7, CvType.CV_32FC1);
        Mat _a = new Mat(1, 3, CvType.CV_64FC1);
        Mat _b = new Mat(1, 3, CvType.CV_64FC1);
        float[] _a_arr = new float[2];
        double[] _b_arr = new double[3];

        Point[] rotated_palm_landmarks_points = new Point[7];

        for (int i = 0; i < 7; ++i)
        {
            adjusted_landmarks.get(i, 0, _a_arr);
            _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1], 1f });
            rotation_matrix.get(0, 0, _b_arr);
            _b.put(0, 0, new double[] { _b_arr[0], _b_arr[1], _b_arr[2] });
            double x = _a.dot(_b);
            rotated_palm_landmarks.put(0, i, new float[] { (float)x });

            rotation_matrix.get(1, 0, _b_arr);
            _b.put(0, 0, new double[] { _b_arr[0], _b_arr[1], _b_arr[2] });
            double y = _a.dot(_b);
            rotated_palm_landmarks.put(1, i, new float[] { (float)y });

            rotated_palm_landmarks_points[i] = new Point(x, y);
        }

        // get landmark bounding box
        MatOfPoint points = new MatOfPoint(rotated_palm_landmarks_points);
        OpenCVRect _rotated_palm_bbox = Imgproc.boundingRect(points);
        rotated_palm_bbox = new Mat(2, 2, CvType.CV_64FC1);

        // shift bounding box
        Point _rotated_palm_bbox_tl = _rotated_palm_bbox.tl();
        Point _rotated_palm_bbox_br = _rotated_palm_bbox.br();
        Point wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
        Point shift_vector = new Point(PALM_BOX_SHIFT_VECTOR.x * wh_rotated_palm_bbox.x, PALM_BOX_SHIFT_VECTOR.y * wh_rotated_palm_bbox.y);
        _rotated_palm_bbox_tl = _rotated_palm_bbox_tl + shift_vector;
        _rotated_palm_bbox_br = _rotated_palm_bbox_br + shift_vector;

        // squarify bounding boxx
        Point center_rotated_plam_bbox = new Point((_rotated_palm_bbox_tl.x + _rotated_palm_bbox_br.x) / 2, (_rotated_palm_bbox_tl.y + _rotated_palm_bbox_br.y) / 2);
        wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
        double new_half_size = Math.Max(wh_rotated_palm_bbox.x, wh_rotated_palm_bbox.y) / 2.0;
        _rotated_palm_bbox_tl = new Point(center_rotated_plam_bbox.x - new_half_size, center_rotated_plam_bbox.y - new_half_size);
        _rotated_palm_bbox_br = new Point(center_rotated_plam_bbox.x + new_half_size, center_rotated_plam_bbox.y + new_half_size);

        // enlarge bounding box
        center_rotated_plam_bbox = new Point((_rotated_palm_bbox_tl.x + _rotated_palm_bbox_br.x) / 2, (_rotated_palm_bbox_tl.y + _rotated_palm_bbox_br.y) / 2);
        wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
        Point new_half_size2 = new Point(wh_rotated_palm_bbox.x * PALM_BOX_ENLARGE_FACTOR / 2.0, wh_rotated_palm_bbox.y * PALM_BOX_ENLARGE_FACTOR / 2.0);
        OpenCVRect _rotated_palm_bbox_rect = new OpenCVRect((int)(center_rotated_plam_bbox.x - new_half_size2.x), (int)(center_rotated_plam_bbox.y - new_half_size2.y)
            , (int)(new_half_size2.x * 2), (int)(new_half_size2.y * 2));
        _rotated_palm_bbox_tl = _rotated_palm_bbox_rect.tl();
        _rotated_palm_bbox_br = _rotated_palm_bbox_rect.br();
        rotated_palm_bbox.put(0, 0, new double[] { _rotated_palm_bbox_tl.x, _rotated_palm_bbox_tl.y, _rotated_palm_bbox_br.x, _rotated_palm_bbox_br.y });

        // crop bounding box
        int[] diff = new int[] {
                    Math.Max((int)-_rotated_palm_bbox_tl.x, 0),
                    Math.Max((int)-_rotated_palm_bbox_tl.y, 0),
                    Math.Max((int)_rotated_palm_bbox_br.x - tmpRotatedImage.width(), 0),
                    Math.Max((int)_rotated_palm_bbox_br.y - tmpRotatedImage.height(), 0)
                };
        Point tl = new Point(_rotated_palm_bbox_tl.x + diff[0], _rotated_palm_bbox_tl.y + diff[1]);
        Point br = new Point(_rotated_palm_bbox_br.x + diff[2], _rotated_palm_bbox_br.y + diff[3]);
        OpenCVRect rotated_palm_bbox_rect = new OpenCVRect(tl, br);
        OpenCVRect rotated_image_rect = new OpenCVRect(0, 0, tmpRotatedImage.width(), tmpRotatedImage.height());

        // get rotated image
        OpenCVRect warp_roi_rect = rotated_image_rect.intersect(rotated_palm_bbox_rect);
        Mat _tmpImage_warp_roi = new Mat(tmpImage, warp_roi_rect);
        Mat _tmpRotatedImage_warp_roi = new Mat(tmpRotatedImage, warp_roi_rect);
        Point warp_roi_center_palm_bbox = center_palm_bbox - warp_roi_rect.tl();
        Mat warp_roi_rotation_matrix = Imgproc.getRotationMatrix2D(warp_roi_center_palm_bbox, angle, 1.0);
        Imgproc.warpAffine(_tmpImage_warp_roi, _tmpRotatedImage_warp_roi, warp_roi_rotation_matrix, _tmpImage_warp_roi.size());

        // get rotated_palm_bbox-size rotated image
        OpenCVRect crop_rect = rotated_image_rect.intersect(
            new OpenCVRect(0, 0, (int)_rotated_palm_bbox_br.x - (int)_rotated_palm_bbox_tl.x, (int)_rotated_palm_bbox_br.y - (int)_rotated_palm_bbox_tl.y));

        Mat _tmpImage_crop_roi = new Mat(tmpImage, crop_rect);

        Imgproc.rectangle(_tmpImage_crop_roi, new OpenCVRect(0, 0, _tmpImage_crop_roi.width(), _tmpImage_crop_roi.height()), Scalar.all(0), -1);
        OpenCVRect crop2_rect = rotated_image_rect.intersect(new OpenCVRect(diff[0], diff[1], _tmpRotatedImage_warp_roi.width(), _tmpRotatedImage_warp_roi.height()));
        Mat _tmpImage_crop2_roi = new Mat(tmpImage, crop2_rect);
        if (_tmpRotatedImage_warp_roi.size() == _tmpImage_crop2_roi.size())
            _tmpRotatedImage_warp_roi.copyTo(_tmpImage_crop2_roi);
        Texture2D texture = new Texture2D(_tmpImage_crop_roi.cols(), _tmpImage_crop_roi.rows(), TextureFormat.RGB24, false);
        Utils.fastMatToTexture2D(_tmpImage_crop_roi, texture);
        return texture;
    }

    // Function to run model inference to estimate hand pose 
    public async Task<List<Mat>> EstimateHandPose(Texture2D texture)
    {
        // Create input tensor from Texture2D image
        TensorFloat inputTensor = TextureConverter.ToTensor(texture);
        inputTensor.MakeReadable();
        //var shape = inputTensor.shape;
        //Debug.Log($"input tensor shape: [{string.Join(',', shape)}]");
        await Task.Delay(32);

        // Run model inference
        var outputData = await ForwardAsync(worker, inputTensor);
        //Debug.Log($"outputData: [{string.Join(',', outputData.outputs[0])}]");
        inputTensor.Dispose();
        await Task.Delay(32);

        // Process output data
        List<Mat> outputBlob = HandPoseData2MatList(outputData);

        return outputBlob;
    }

    // Performs forward pass on hand pose estimation model 
    // Adapted from https://github.com/Unity-Technologies/barracuda-release/issues/236#issue-1049168663
    public async Task<HandPoseData> ForwardAsync(IWorker modelWorker, TensorFloat inputs)
    {
        var executor = modelWorker.StartManualSchedule(inputs);
        var it = 0;
        bool hasMoreWork;
        do
        {
            hasMoreWork = executor.MoveNext();
            if (++it % 20 == 0)
            {
                modelWorker.FlushSchedule();
                await Task.Delay(32);
            }
        } while (hasMoreWork);
        var out_1 = modelWorker.PeekOutput("Identity") as TensorFloat;
        var out_2 = modelWorker.PeekOutput("Identity_1") as TensorFloat;
        var out_3 = modelWorker.PeekOutput("Identity_2") as TensorFloat;
        var out_4 = modelWorker.PeekOutput("Identity_3") as TensorFloat;
        HandPoseData handPoseData = new HandPoseData(out_1, out_2, out_3, out_4);
        out_1.Dispose();
        out_2.Dispose();
        out_3.Dispose();
        out_4.Dispose();
        return handPoseData;
    }

    // Extract output tensor values as HandPoseData
    public List<Mat> HandPoseData2MatList(HandPoseData handPoseData)
    {
        var lm = new List<Mat>();
        for (int i = 0; i < handPoseData.outputs.Count; i++)
        {
            Mat mat = new Mat(
                handPoseData.dimensions[i][0],
                handPoseData.dimensions[i][1],
                CvType.CV_32F
            );
            mat.put(0, 0, handPoseData.outputs[i]);
            lm.Add(mat);
        }
        return lm;
    }

    // From EnoxSoftware's OpenCVForUnity HandPoseEstimationMediaPipeExample
    // Reference: https://github.com/EnoxSoftware/OpenCVForUnity/blob/master/Assets/OpenCVForUnity/Examples
    public virtual Mat Postprocess(List<Mat> output_blob, Mat rotated_palm_bbox, double angle, Mat rotation_matrix, Mat pad_bias)
    {
        Mat landmarks = output_blob[0];
        float conf = (float)output_blob[1].get(0, 0)[0];
        float handedness = (float)output_blob[2].get(0, 0)[0];
        Mat landmarks_world = output_blob[3];

        if (conf < conf_threshold)
            return new Mat();

        landmarks = landmarks.reshape(1, 21); // shape: (1, 63) -> (21, 3)
        landmarks_world = landmarks_world.reshape(1, 21); // shape: (1, 63) -> (21, 3)

        // transform coords back to the input coords
        double[] rotated_palm_bbox_arr = new double[4];
        rotated_palm_bbox.get(0, 0, rotated_palm_bbox_arr);
        Point _rotated_palm_bbox_tl = new Point(rotated_palm_bbox_arr[0], rotated_palm_bbox_arr[1]);
        Point _rotated_palm_bbox_br = new Point(rotated_palm_bbox_arr[2], rotated_palm_bbox_arr[3]);
        Point wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
        Point scale_factor = new Point(wh_rotated_palm_bbox.x / input_size.width, wh_rotated_palm_bbox.y / input_size.height);

        Mat _landmarks_21x1_c3 = landmarks.reshape(3, 21);
        Core.subtract(_landmarks_21x1_c3, new Scalar(input_size.width / 2.0, input_size.height / 2.0, 0.0), _landmarks_21x1_c3);
        double max_scale_factor = Math.Max(scale_factor.x, scale_factor.y);
        Core.multiply(_landmarks_21x1_c3, new Scalar(scale_factor.x, scale_factor.y, max_scale_factor), _landmarks_21x1_c3); //  # depth scaling

        Mat coords_rotation_matrix = Imgproc.getRotationMatrix2D(new Point(0, 0), angle, 1.0);

        Mat rotated_landmarks = landmarks.clone();
        Mat _a = new Mat(1, 2, CvType.CV_64FC1);
        Mat _b = new Mat(1, 2, CvType.CV_64FC1);
        float[] _a_arr = new float[2];
        double[] _b_arr = new double[6];
        coords_rotation_matrix.get(0, 0, _b_arr);

        for (int i = 0; i < 21; ++i)
        {
            landmarks.get(i, 0, _a_arr);
            _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1] });

            _b.put(0, 0, new double[] { _b_arr[0], _b_arr[3] });
            rotated_landmarks.put(i, 0, new float[] { (float)_a.dot(_b) });
            _b.put(0, 0, new double[] { _b_arr[1], _b_arr[4] });
            rotated_landmarks.put(i, 1, new float[] { (float)_a.dot(_b) });
        }

        Mat rotated_landmarks_world = landmarks_world.clone();
        for (int i = 0; i < 21; ++i)
        {
            landmarks_world.get(i, 0, _a_arr);
            _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1] });

            _b.put(0, 0, new double[] { _b_arr[0], _b_arr[3] });
            rotated_landmarks_world.put(i, 0, new float[] { (float)_a.dot(_b) });
            _b.put(0, 0, new double[] { _b_arr[1], _b_arr[4] });
            rotated_landmarks_world.put(i, 1, new float[] { (float)_a.dot(_b) });
        }

        // invert rotation
        double[] rotation_matrix_arr = new double[6];
        rotation_matrix.get(0, 0, rotation_matrix_arr);
        Mat rotation_component = new Mat(2, 2, CvType.CV_64FC1);
        rotation_component.put(0, 0, new double[] { rotation_matrix_arr[0], rotation_matrix_arr[3], rotation_matrix_arr[1], rotation_matrix_arr[4] });
        Mat translation_component = new Mat(2, 1, CvType.CV_64FC1);
        translation_component.put(0, 0, new double[] { rotation_matrix_arr[2], rotation_matrix_arr[5] });
        Mat inverted_translation = new Mat(2, 1, CvType.CV_64FC1);
        inverted_translation.put(0, 0, new double[] { -rotation_component.row(0).dot(translation_component.reshape(1, 1)), -rotation_component.row(1).dot(translation_component.reshape(1, 1)) });

        Mat inverse_rotation_matrix = new Mat(2, 3, CvType.CV_64FC1);
        rotation_component.copyTo(inverse_rotation_matrix.colRange(new OpenCVRange(0, 2)));
        inverted_translation.copyTo(inverse_rotation_matrix.colRange(new OpenCVRange(2, 3)));

        // get box center
        Mat center = new Mat(3, 1, CvType.CV_64FC1);
        center.put(0, 0, new double[] { (rotated_palm_bbox_arr[0] + rotated_palm_bbox_arr[2]) / 2.0, (rotated_palm_bbox_arr[1] + rotated_palm_bbox_arr[3]) / 2.0, 1.0 });
        Mat original_center = new Mat(2, 1, CvType.CV_64FC1);
        original_center.put(0, 0, new double[] { inverse_rotation_matrix.row(0).dot(center.reshape(1, 1)), inverse_rotation_matrix.row(1).dot(center.reshape(1, 1)) });

        Core.add(rotated_landmarks.reshape(3, 21)
            , new Scalar(original_center.get(0, 0)[0] + pad_bias.get(0, 0)[0], original_center.get(1, 0)[0] + pad_bias.get(1, 0)[0], 0.0)
            , landmarks.reshape(3, 21));

        // get bounding box from rotated_landmarks
        Point[] landmarks_points = new Point[21];
        for (int i = 0; i < 21; ++i)
        {
            landmarks.get(i, 0, _a_arr);
            landmarks_points[i] = new Point(_a_arr[0], _a_arr[1]);
        }
        MatOfPoint points = new MatOfPoint(landmarks_points);
        OpenCVRect bbox = Imgproc.boundingRect(points);

        // shift bounding box
        Point wh_bbox = bbox.br() - bbox.tl();
        Point shift_vector = new Point(HAND_BOX_SHIFT_VECTOR.x * wh_bbox.x, HAND_BOX_SHIFT_VECTOR.y * wh_bbox.y);
        bbox = bbox + shift_vector;

        // enlarge bounding box
        Point center_bbox = new Point((bbox.tl().x + bbox.br().x) / 2, (bbox.tl().y + bbox.br().y) / 2);
        wh_bbox = bbox.br() - bbox.tl();
        Point new_half_size = new Point(wh_bbox.x * HAND_BOX_ENLARGE_FACTOR / 2.0, wh_bbox.y * HAND_BOX_ENLARGE_FACTOR / 2.0);
        bbox = new OpenCVRect(new Point(center_bbox.x - new_half_size.x, center_bbox.y - new_half_size.y), new Point(center_bbox.x + new_half_size.x, center_bbox.y + new_half_size.y));


        Mat results = new Mat(132, 1, CvType.CV_32FC1);
        results.put(0, 0, new float[] { (float)bbox.tl().x, (float)bbox.tl().y, (float)bbox.br().x, (float)bbox.br().y });
        Mat results_col4_67_21x3 = results.rowRange(new OpenCVRange(4, 67)).reshape(1, 21);
        landmarks.colRange(new OpenCVRange(0, 3)).copyTo(results_col4_67_21x3);
        Mat results_col67_130_21x3 = results.rowRange(new OpenCVRange(67, 130)).reshape(1, 21);
        rotated_landmarks_world.colRange(new OpenCVRange(0, 3)).copyTo(results_col67_130_21x3);
        results.put(130, 0, new float[] { handedness });
        results.put(131, 0, new float[] { conf });

        // # [0: 4]: hand bounding box found in image of format [x1, y1, x2, y2] (top-left and bottom-right points)
        // # [4: 67]: screen landmarks with format [x1, y1, z1, x2, y2 ... x21, y21, z21], z value is relative to WRIST
        // # [67: 130]: world landmarks with format [x1, y1, z1, x2, y2 ... x21, y21, z21], 3D metric x, y, z coordinate
        // # [130]: handedness, (left)[0, 1](right) hand
        // # [131]: confidence
        return results;//np.r_[bbox.reshape(-1), landmarks.reshape(-1), rotated_landmarks_world.reshape(-1), handedness[0][0], conf]
    }

    // Convert Unity Texture2D to OpenCV Mat
    private Mat _texture2DToMat(Texture2D texture)
    {
        // Get pixel data from Texture2D
        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;

        // Create a Mat with the same size and type as the texture
        Mat mat = new Mat(height, width, CvType.CV_8UC3);

        // Convert pixel data to Mat
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 color = pixels[(height - y - 1) * width + x];
                mat.put(y, x, new byte[] { color.r, color.g, color.b });
            }
        }
        return mat;
    }

    public void Destroy()
    {
        if (worker != null)
        {
            worker.Dispose();
        }
    }
}
