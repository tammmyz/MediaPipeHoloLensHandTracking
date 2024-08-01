using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using Unity.Sentis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OpenCVRange = OpenCVForUnity.CoreModule.Range;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;
using Size = OpenCVForUnity.CoreModule.Size;
using Unity.Mathematics;
using System.Linq;

public class MediaPipeHandPoseEstimator
{
    float conf_threshold;

    Size input_size = new Size(224, 224);

    ModelAsset handpose_estimation_net;

    private Model model;
    private IWorker worker;

    Mat tmpImage;
    Mat tmpRotatedImage;

    int PALM_LANDMARKS_INDEX_OF_PALM_BASE = 0;
    int PALM_LANDMARKS_INDEX_OF_MIDDLE_FINGER_BASE = 2;
    Point PALM_BOX_SHIFT_VECTOR = new Point(0, -0.4);
    double PALM_BOX_ENLARGE_FACTOR = 3;

    public MediaPipeHandPoseEstimator(ModelAsset modelAsset, float confThreshold = 0.8f)
    {
        handpose_estimation_net = modelAsset;
        conf_threshold = confThreshold;
    }

    public void Initialize()
    {
        // Load the model from the provided NNModel asset
        model = ModelLoader.Load(handpose_estimation_net);

        // Create a Barracuda worker to run the model on the GPU
        worker = WorkerFactory.CreateWorker(BackendType.CPU, model);
    }

    public Texture2D preprocess(Mat image, Mat palm, out Mat rotated_palm_bbox, out double angle, out Mat rotation_matrix)
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
            //Debug.Log("initialize tmpImage");
            tmpImage = new Mat(tmpImageSize, tmpImageSize, image.type(), Scalar.all(0));
            tmpRotatedImage = tmpImage.clone();
            //Debug.Log($"tmpRotatedImage sum: {Core.sumElems(tmpRotatedImage)}");
        }

        Mat _tmpImage_roi = new Mat(tmpImage, new OpenCVRect(0, 0, image.width(), image.height()));
        //Mat _tmpImage_roi = new Mat(image.height(), image.width(), CvType.CV_8UC3);
        image.copyTo(_tmpImage_roi);

        // Rotate input to have vertically oriented hand image
        // compute rotation
        Mat palm_bbox = palm.colRange(new OpenCVRange(0, 4)).reshape(1, 2);
        Mat palm_landmarks = palm.colRange(new OpenCVRange(4, 18)).reshape(1, 7);

        Scalar scale = new Scalar(image.width(), image.height());
        Mat adjusted_bbox = new Mat(palm_bbox.size(), palm_bbox.type());
        Core.multiply(palm_bbox, scale, adjusted_bbox);
        Mat adjusted_landmarks = new Mat(palm_landmarks.size(), palm_landmarks.type());
        Core.multiply(palm_landmarks, scale, adjusted_landmarks);
        Debug.Log($"adjusted_bbox:\t{adjusted_bbox.dump()}");
        Debug.Log($"adjusted_landmarks:\t{adjusted_landmarks.dump()}");

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
        Debug.Log($"center_palm_bbox (x,y): {center_palm_bbox.x},{center_palm_bbox.y}");

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

        Debug.Log($"rotated_palm_landmarks: {rotated_palm_landmarks.dump()}");

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
        //Debug.Log($"center_rotated_plam_bbox (x,y): {center_rotated_plam_bbox.x}, {center_rotated_plam_bbox.y}");

        // enlarge bounding box
        center_rotated_plam_bbox = new Point((_rotated_palm_bbox_tl.x + _rotated_palm_bbox_br.x) / 2, (_rotated_palm_bbox_tl.y + _rotated_palm_bbox_br.y) / 2);
        wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
        Point new_half_size2 = new Point(wh_rotated_palm_bbox.x * PALM_BOX_ENLARGE_FACTOR / 2.0, wh_rotated_palm_bbox.y * PALM_BOX_ENLARGE_FACTOR / 2.0);
        OpenCVRect _rotated_palm_bbox_rect = new OpenCVRect((int)(center_rotated_plam_bbox.x - new_half_size2.x), (int)(center_rotated_plam_bbox.y - new_half_size2.y)
            , (int)(new_half_size2.x * 2), (int)(new_half_size2.y * 2));
        _rotated_palm_bbox_tl = _rotated_palm_bbox_rect.tl();
        _rotated_palm_bbox_br = _rotated_palm_bbox_rect.br();
        rotated_palm_bbox.put(0, 0, new double[] { _rotated_palm_bbox_tl.x, _rotated_palm_bbox_tl.y, _rotated_palm_bbox_br.x, _rotated_palm_bbox_br.y });
        Debug.Log($"center_rotated_plam_bbox (x,y): {center_rotated_plam_bbox.x}, {center_rotated_plam_bbox.y}");
        Debug.Log($"_rotated_palm_bbox_rect (x,y,w,h): {_rotated_palm_bbox_rect.x}, {_rotated_palm_bbox_rect.y}, {_rotated_palm_bbox_rect.width}, {_rotated_palm_bbox_rect.height}");

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
        Debug.Log($"warp_roi_rect (x,y,w,h): {warp_roi_rect.x}, {warp_roi_rect.y}, {warp_roi_rect.width}, {warp_roi_rect.height}");
        Mat _tmpRotatedImage_warp_roi = new Mat(tmpRotatedImage, warp_roi_rect);
        //Debug.Log($"_tmpImage_warp_roi: {_tmpImage_warp_roi.size()}\n{Core.sumElems(_tmpImage_warp_roi)}");
        Point warp_roi_center_palm_bbox = center_palm_bbox - warp_roi_rect.tl();
        Debug.Log($"warp_roi_center_palm_bbox (x,y): {warp_roi_center_palm_bbox.x}, {warp_roi_center_palm_bbox.y}");

        Mat warp_roi_rotation_matrix = Imgproc.getRotationMatrix2D(warp_roi_center_palm_bbox, angle, 1.0);
        //Debug.Log($"warp_roi_rotation_matrix: {warp_roi_rotation_matrix.dump()}");
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
        //Debug.Log($"crop_rect (x,y,w,h): {crop_rect.x},{crop_rect.y},{crop_rect.width},{crop_rect.height},");
        //Debug.Log($"crop2_rect (x,y,w,h): {crop2_rect.x},{crop2_rect.y},{crop2_rect.width},{crop2_rect.height},");
        //Debug.Log($"_tmpRotatedImage_warp_roi size:  {_tmpRotatedImage_warp_roi.size()}");
        //Debug.Log($"_tmpRotatedImage_warp_roi: {_tmpRotatedImage_warp_roi.row(0).get(0,0)[0]}, {_tmpRotatedImage_warp_roi.row(0).get(0, 1)[0]}, " +
        //    $"{_tmpRotatedImage_warp_roi.row(0).get(0, 2)[0]}, {_tmpRotatedImage_warp_roi.row(0).get(0, 3)[0]}, {_tmpRotatedImage_warp_roi.row(0).get(0, 4)[0]}" +
        //    $"\nsum: {Core.sumElems(_tmpRotatedImage_warp_roi)}");

        //////////////////////////////////////////////////////////////////////////////////////////////////
        //Texture2D texture = new Texture2D(_tmpImage_crop_roi.cols(), _tmpImage_crop_roi.rows(), TextureFormat.RGB24, false);
        //Utils.fastMatToTexture2D(_tmpImage_crop_roi, texture);
        //////////////////////////////////////////////////////////////////////////////////////////////////

        Texture2D texture = new Texture2D(_tmpRotatedImage_warp_roi.cols(), _tmpRotatedImage_warp_roi.rows(), TextureFormat.RGB24, false);
        Utils.fastMatToTexture2D(_tmpRotatedImage_warp_roi, texture);
        return texture;
    }


    public Texture2D preprocess1(Texture2D tex)
    {
        // 2) adjust palm landmarks relative to new image - DONE!
        // 3) rotate the image so that the palm is vertical (use the landmarks)
        // 4) adjust bounding box to encompass whole hand (enlargen within bounds of padded image)
        // 5) crop image around padded palm
        // 6) normalize image for input
        // Pad image to make square
        int maxSize = Mathf.Max(tex.width, tex.height);
        int offsetX = (maxSize - tex.width) / 2;
        int offsetY = (maxSize - tex.height) / 2;
        Debug.Log("Max: " + maxSize.ToString()
            + "\tOffsetX: " + offsetX.ToString()
            + "\tOffsety: " + offsetY.ToString());

        Texture2D paddedTex = new Texture2D(maxSize, maxSize, TextureFormat.RGB24, false);
        Color[] texPixels = tex.GetPixels();
        paddedTex.SetPixels(offsetX, offsetY, tex.width, tex.height, texPixels);
        paddedTex.Apply();

        RenderTexture.active = null;

        return paddedTex;
    }

    public async Task<Mat> EstimateHandPose(Texture2D texture)
    {
        TensorFloat inputTensor = TextureConverter.ToTensor(texture);
        inputTensor.MakeReadable();
        var shape = inputTensor.shape;
        Debug.Log($"input tensor shape: [{string.Join(',', shape)}], {inputTensor}");
        await Task.Delay(32);

        // Log the contents of the tensor for debugging
        var tensorData = inputTensor.ToReadOnlyArray();
        Debug.Log($"Input tensor data: [{string.Join("\n", tensorData.Take(50))}]..."); // Log first 10 values

        var outputData = await ForwardAsync(worker, inputTensor);
        //var outputData = ForwardSync(worker, inputTensor);
        inputTensor.Dispose();
        await Task.Delay(32);

        List<Mat> outputBlob = HandData2MatList(outputData);
        Debug.Log($"mat:\n{outputBlob}");
        //Mat processedMat = postprocess(outputBlob);
        //Debug.Log($"mat post processing: {processedMat.size()}\n{processedMat.dump()}");
        //Texture2D debug3 = new Texture2D(texture.width, texture.height, texture.format, false);
        //debug3.SetPixels(texture.GetPixels());
        //debug3.Apply();
        //visualize(debug3, processedMat, 192, 192);
        //debugRenderer.material.mainTexture = debug3;
        //return mat;
        return outputBlob[0];
    }

    public List<Mat> HandData2MatList(HandPoseData handPoseData)
    {
        var lm = new List<Mat>();
        for (int i = 0; i < handPoseData.outputs.Count; i++)
        {
            Mat mat = new Mat(
                handPoseData.dimensions[i][0],
                handPoseData.dimensions[i][1],
                handPoseData.types[i]
            );
            mat.put(0, 0, handPoseData.outputs[i]);
            lm.Add(mat);
        }
        return lm;
    }

    // Nicked from https://github.com/Unity-Technologies/barracuda-release/issues/236#issue-1049168663
    public async Task<HandPoseData> ForwardAsync(IWorker modelWorker, TensorFloat inputs)
    {
        Debug.Log("starting forward async");
        var executor = modelWorker.StartManualSchedule(inputs);
        var it = 0;
        //Debug.Log("iteration 0");
        bool hasMoreWork;
        do
        {
            hasMoreWork = executor.MoveNext();
            if (++it % 20 == 0)
            {
                modelWorker.FlushSchedule();
                await Task.Delay(32);
            }
            //Debug.Log($"iteration {it}]\thasMoreWork: {hasMoreWork}");
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
}
