using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.VideoioModule;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace OpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// VideoCapture 2 mat helper.
    /// v 1.0.0
    /// 
    /// By setting outputColorFormat to BGR, processing that does not include extra color conversion is performed.
    /// </summary>
    public class VideoCapture2MatHelper : MonoBehaviour, IVideoSource2MatHelper
    {
        /// <summary>
        /// Set the video file path, relative to the starting point of the "StreamingAssets" folder, or absolute path.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("requestedVideoFilePath"), TooltipAttribute("Set the video file path, relative to the starting point of the \"StreamingAssets\" folder, or absolute path.")]
        protected string _requestedVideoFilePath = string.Empty;

        public virtual string requestedVideoFilePath
        {
            get { return _requestedVideoFilePath; }
            set
            {
                if (_requestedVideoFilePath != value)
                {
                    _requestedVideoFilePath = value;
                    if (hasInitDone)
                        Initialize(IsPlaying());
                }
            }
        }

        /// <summary>
        /// Set the apiPreference. VideoCapture API backends identifier. (Advanced Option)
        /// See ReadMe.pdf for setup instructions for using CAP_FFMPEG on Windows platforms.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("apiPreference"), TooltipAttribute("Set the apiPreference. VideoCapture API backends identifier. (Advanced Option)\n" +
            "0 = CAP_ANY (Auto detect)[default]\n" +
            "1400 = CAP_MSMF (Microsoft Media Foundation (via videoInput))\n" +
            "1900 = CAP_FFMPEG (Open and record video file or stream using the FFMPEG library)\n" +
            "2200 = CAP_OPENCV_MJPEG (Built-in OpenCV MotionJPEG codec)")]
        protected int _apiPreference = Videoio.CAP_ANY;

        public virtual int apiPreference
        {
            get { return _apiPreference; }
            set
            {
                if (_apiPreference != value)
                {
                    _apiPreference = value;
                    if (hasInitDone)
                        Initialize(IsPlaying());
                }
            }
        }

        /// <summary>
        /// Select the output color format.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("outputColorFormat"), TooltipAttribute("Select the output color format.")]
        protected Source2MatHelperColorFormat _outputColorFormat = Source2MatHelperColorFormat.BGR;

        public virtual Source2MatHelperColorFormat outputColorFormat
        {
            get { return _outputColorFormat; }
            set
            {
                if (_outputColorFormat != value)
                {
                    _outputColorFormat = value;
                    if (hasInitDone)
                        Initialize(IsPlaying());
                }
            }
        }

        /// <summary>
        /// The number of frames before the initialization process times out.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("timeoutFrameCount"), TooltipAttribute("The number of frames before the initialization process times out.")]
        protected int _timeoutFrameCount = 1500;

        public virtual int timeoutFrameCount
        {
            get { return _timeoutFrameCount; }
            set { _timeoutFrameCount = (int)Mathf.Clamp(value, 0f, float.MaxValue); }
        }

        /// <summary>
        /// Indicate whether to play this video in a loop.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("loop"), TooltipAttribute("Indicate whether to play this video in a loop.")]
        protected bool _loop = true;
        public virtual bool loop
        {
            get { lock (sync) return _loop; }
            set { lock (sync) _loop = value; }
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is initialized.
        /// </summary>
        [SerializeField, FormerlySerializedAs("onInitialized"), TooltipAttribute("UnityEvent that is triggered when this instance is initialized.")]
        protected UnityEvent _onInitialized;
        public UnityEvent onInitialized
        {
            get => _onInitialized;
            set => _onInitialized = value;
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is disposed.
        /// </summary>
        [SerializeField, FormerlySerializedAs("onDisposed"), TooltipAttribute("UnityEvent that is triggered when this instance is disposed.")]
        protected UnityEvent _onDisposed;
        public UnityEvent onDisposed
        {
            get => _onDisposed;
            set => _onDisposed = value;
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is error Occurred.
        /// </summary>
        [SerializeField, FormerlySerializedAs("onErrorOccurred"), TooltipAttribute("UnityEvent that is triggered when this instance is error Occurred.")]
        protected Source2MatHelperErrorUnityEvent _onErrorOccurred;
        public Source2MatHelperErrorUnityEvent onErrorOccurred
        {
            get => _onErrorOccurred;
            set => _onErrorOccurred = value;
        }


        protected System.Object videoCaptureLockObject = new System.Object();
        protected VideoCapture _videoCapture;
        protected VideoCapture videoCapture
        {
            get { lock (videoCaptureLockObject) return _videoCapture; }
            set { lock (videoCaptureLockObject) _videoCapture = value; }
        }

        protected System.Object sync = new System.Object();
        protected bool _isReading = false;
        protected virtual bool isReading
        {
            get { lock (sync) return _isReading; }
            set { lock (sync) _isReading = value; }
        }

        protected bool _isPlaying = false;
        protected virtual bool isPlaying
        {
            get { lock (sync) return _isPlaying; }
            set { lock (sync) _isPlaying = value; }
        }

        protected bool _didUpdateThisFrame = false;
        protected virtual bool didUpdateThisFrame
        {
            get { lock (sync) return _didUpdateThisFrame; }
            set { lock (sync) _didUpdateThisFrame = value; }
        }

        protected bool _didUpdateImageBufferInCurrentFrame = false;
        protected virtual bool didUpdateImageBufferInCurrentFrame
        {
            get { lock (sync) return _didUpdateImageBufferInCurrentFrame; }
            set { lock (sync) _didUpdateImageBufferInCurrentFrame = value; }
        }

        /// <summary>
        /// The frame mat.
        /// </summary>
        protected Mat frameMat;

        /// <summary>
        /// The base mat.
        /// </summary>
        protected Mat baseMat;

        /// <summary>
        /// The image buffer mat.
        /// </summary>
        protected System.Object imageBufferMatLockObject = new System.Object();
        protected Mat _imageBufferMat;
        protected Mat imageBufferMat
        {
            get { lock (imageBufferMatLockObject) return _imageBufferMat; }
            set { lock (imageBufferMatLockObject) _imageBufferMat = value; }
        }

        /// <summary>
        /// The base color format.
        /// </summary>
        protected Source2MatHelperColorFormat baseColorFormat = Source2MatHelperColorFormat.BGR;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        protected bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        protected bool hasInitDone = false;

        /// <summary>
        /// The initialization coroutine.
        /// </summary>
        protected IEnumerator initCoroutine;

        /// <summary>
        /// The get file path coroutine.
        /// </summary>
        protected IEnumerator getFilePathCoroutine;

#if UNITY_WEBGL
        /// <summary>
        /// The wait frame time coroutine.
        /// </summary>
        protected IEnumerator waitFrameTimeCoroutine;
#endif

        protected bool _isThreadRunning = false;
        protected virtual bool isThreadRunning
        {
            get { lock (sync) return _isThreadRunning; }
            set { lock (sync) _isThreadRunning = value; }
        }

        protected bool _shouldStopThread = false;
        protected virtual bool shouldStopThread
        {
            get { lock (sync) return _shouldStopThread; }
            set { lock (sync) _shouldStopThread = value; }
        }

        /// <summary>
        /// If set to true play after completion of initialization.
        /// </summary>
        protected bool autoPlayAfterInitialize;

        protected virtual void OnValidate()
        {
            _timeoutFrameCount = (int)Mathf.Clamp(_timeoutFrameCount, 0f, float.MaxValue);
        }

        protected virtual void LateUpdate()
        {
            if (!hasInitDone)
                return;

            if (didUpdateThisFrame && !didUpdateImageBufferInCurrentFrame)
            {

#if UNITY_WEBGL
                ReadFrame();

#elif WINDOWS_UWP || (!UNITY_WSA_10_0 && (NET_4_6 || NET_STANDARD_2_0))
                System.Threading.Tasks.Task.Run(() => ReadFrame());
#else
                System.Threading.ThreadPool.QueueUserWorkItem(_ => ReadFrame());
#endif

                didUpdateThisFrame = false;
            }

            didUpdateImageBufferInCurrentFrame = false;
        }

        protected virtual void ReadFrame()
        {
            isReading = true;

            if (isPlaying)
            {
                lock (videoCaptureLockObject)
                {
                    if (_videoCapture != null && !_videoCapture.IsDisposed)
                    {
                        if (_videoCapture.get(Videoio.CAP_PROP_POS_FRAMES) >= _videoCapture.get(Videoio.CAP_PROP_FRAME_COUNT))
                        {
                            if (loop)
                            {
                                _videoCapture.set(Videoio.CAP_PROP_POS_FRAMES, 0);
                            }
                            else
                            {
                                isPlaying = false;
                            }
                        }

                        if (isPlaying)
                        {
                            lock (imageBufferMatLockObject)
                            {
                                if (_imageBufferMat != null && !_imageBufferMat.IsDisposed)
                                {
                                    _videoCapture.read(_imageBufferMat);
                                }
                            }
                        }
                    }
                }
            }

            isReading = false;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        protected virtual void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <param name="autoPlay">If set to <c>true</c> play after completion of initialization.</param>
        public virtual void Initialize(bool autoPlay = true)
        {
            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            autoPlayAfterInitialize = autoPlay;
            if (_onInitialized == null)
                _onInitialized = new UnityEvent();
            if (_onDisposed == null)
                _onDisposed = new UnityEvent();
            if (_onErrorOccurred == null)
                _onErrorOccurred = new Source2MatHelperErrorUnityEvent();

            initCoroutine = _Initialize();
            StartCoroutine(initCoroutine);
        }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <param name="requestedVideoFilePath">Requested video file path.</param>
        /// <param name="autoPlay">If set to <c>true</c> play after completion of initialization.</param>
        public virtual void Initialize(string requestedVideoFilePath, bool autoPlay = true)
        {
            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            _requestedVideoFilePath = requestedVideoFilePath;
            autoPlayAfterInitialize = autoPlay;
            if (_onInitialized == null)
                _onInitialized = new UnityEvent();
            if (_onDisposed == null)
                _onDisposed = new UnityEvent();
            if (_onErrorOccurred == null)
                _onErrorOccurred = new Source2MatHelperErrorUnityEvent();

            initCoroutine = _Initialize();
            StartCoroutine(initCoroutine);
        }

        /// <summary>
        /// Initialize this instance by coroutine.
        /// </summary>
        protected virtual IEnumerator _Initialize()
        {
            if (hasInitDone)
            {
                ReleaseResources();

                if (_onDisposed != null)
                    _onDisposed.Invoke();
            }

            isInitWaiting = true;


            bool hasFilePathCoroutineCompleted = false;
            string fullPath = string.Empty;

            Uri uri;
            if (Uri.TryCreate(requestedVideoFilePath, UriKind.Absolute, out uri))
            {
                hasFilePathCoroutineCompleted = true;
                fullPath = uri.OriginalString;
            }
            else
            {
                getFilePathCoroutine = Utils.getFilePathAsync(requestedVideoFilePath, (result) =>
            {
                hasFilePathCoroutineCompleted = true;
                fullPath = result;
            });
                StartCoroutine(getFilePathCoroutine);
            }

            int initFrameCount = 0;
            bool isTimeout = false;

            while (true)
            {
                if (initFrameCount > timeoutFrameCount)
                {
                    isTimeout = true;
                    break;
                }
                else if (hasFilePathCoroutineCompleted)
                {
                    if (string.IsNullOrEmpty(fullPath))
                    {
                        videoCapture = null;
                        isInitWaiting = false;
                        initCoroutine = null;
                        getFilePathCoroutine = null;

                        if (_onErrorOccurred != null)
                            _onErrorOccurred.Invoke(Source2MatHelperErrorCode.VIDEO_FILE_NOT_EXIST, requestedVideoFilePath);

                        yield break;
                    }

                    videoCapture = new VideoCapture();
                    videoCapture.open(fullPath, apiPreference);

                    if (!videoCapture.isOpened())
                    {
                        videoCapture.Dispose();
                        videoCapture = null;
                        isInitWaiting = false;
                        initCoroutine = null;
                        getFilePathCoroutine = null;

                        if (_onErrorOccurred != null)
                            _onErrorOccurred.Invoke(Source2MatHelperErrorCode.VIDEO_FILE_CANT_OPEN, fullPath);

                        yield break;
                    }

                    imageBufferMat = new Mat();
                    videoCapture.read(imageBufferMat);
                    videoCapture.set(Videoio.CAP_PROP_POS_FRAMES, 0);
                    videoCapture.grab();

                    baseMat = new Mat(imageBufferMat.rows(), imageBufferMat.cols(), imageBufferMat.type(), new Scalar(0, 0, 0, 255));
                    imageBufferMat.copyTo(baseMat);

                    if (baseColorFormat == outputColorFormat)
                    {
                        frameMat = baseMat;
                    }
                    else
                    {
                        frameMat = new Mat(baseMat.rows(), baseMat.cols(), CvType.CV_8UC(Source2MatHelperUtils.Channels(outputColorFormat)), new Scalar(0, 0, 0, 255));
                    }

                    Debug.Log("VideoCaptrue2MatHelper:: " + " filePath:" + requestedVideoFilePath + " width:" + frameMat.width() + " height:" + frameMat.height() + " fps:" + videoCapture.get(Videoio.CAP_PROP_FPS));

                    isInitWaiting = false;
                    hasInitDone = true;
                    initCoroutine = null;
                    getFilePathCoroutine = null;

                    isPlaying = autoPlayAfterInitialize;

                    if (_onInitialized != null)
                        _onInitialized.Invoke();

                    StartWaitFrameTimeThread();

                    break;
                }
                else
                {
                    initFrameCount++;
                    yield return null;
                }
            }

            if (isTimeout)
            {
                videoCapture = null;
                isInitWaiting = false;
                initCoroutine = null;
                getFilePathCoroutine = null;

                if (_onErrorOccurred != null)
                    _onErrorOccurred.Invoke(Source2MatHelperErrorCode.TIMEOUT, string.Empty);
            }
        }

        protected virtual void StartWaitFrameTimeThread()
        {
            if (isThreadRunning)
                return;

            //Debug.Log("VideoCaptrue2MatHelper:: " + "Thread Start");

            isThreadRunning = true;
            shouldStopThread = false;

#if UNITY_WEBGL
            waitFrameTimeCoroutine = WaitFrameTimeThreadWorker();
            StartCoroutine(waitFrameTimeCoroutine);

#elif WINDOWS_UWP || (!UNITY_WSA_10_0 && (NET_4_6 || NET_STANDARD_2_0))
            System.Threading.Tasks.Task.Run(() => WaitFrameTimeThreadWorker());
#else
            System.Threading.ThreadPool.QueueUserWorkItem(_ => WaitFrameTimeThreadWorker());
#endif

        }

        protected virtual void StopWaitFrameTimeThread()
        {
            if (!isThreadRunning)
                return;

#if UNITY_WEBGL
            if (waitFrameTimeCoroutine != null)
            {
                StopCoroutine(waitFrameTimeCoroutine);
                ((IDisposable)waitFrameTimeCoroutine).Dispose();
                waitFrameTimeCoroutine = null;
            }
            isThreadRunning = false;
#else
            shouldStopThread = true;
#endif

            while (isThreadRunning)
            {
                //Wait threading stop
            }
            //Debug.Log("VideoCaptrue2MatHelper:: " + "Thread Stop");
        }

#if UNITY_WEBGL
        protected virtual IEnumerator WaitFrameTimeThreadWorker()
        {
            double videoFPS = (videoCapture.get(Videoio.CAP_PROP_FPS) <= 0) ? 10.0 : videoCapture.get(Videoio.CAP_PROP_FPS);
            float frameTime_sec = (float)(1000.0 / videoFPS / 1000.0);
            WaitForSeconds wait = new WaitForSeconds(frameTime_sec);

            while (true)
            {
                if (isPlaying)
                {
                    while (isReading || didUpdateThisFrame)
                    {
                        yield return null;
                    }

                    didUpdateThisFrame = true;
                    didUpdateImageBufferInCurrentFrame = true;

                    yield return wait;
                }
                else
                {
                    yield return null;
                }
            }
        }
#else
        protected virtual void WaitFrameTimeThreadWorker()
        {
            double videoFPS = (videoCapture.get(Videoio.CAP_PROP_FPS) <= 0) ? 10.0 : videoCapture.get(Videoio.CAP_PROP_FPS);
            int frameTime_msec = (int)(1000.0 / videoFPS);

            while (!shouldStopThread)
            {
                if (isPlaying)
                {
                    while (isReading || didUpdateThisFrame) { }

                    didUpdateThisFrame = true;
                    didUpdateImageBufferInCurrentFrame = true;

                    System.Threading.Thread.Sleep(frameTime_msec);
                }
            }

            isThreadRunning = false;
        }
#endif

        /// <summary>
        /// Indicate whether this instance has been initialized.
        /// </summary>
        /// <returns><c>true</c>, if this instance has been initialized, <c>false</c> otherwise.</returns>
        public virtual bool IsInitialized()
        {
            return hasInitDone;
        }

        /// <summary>
        /// Start the video.
        /// </summary>
        public virtual void Play()
        {
            if (hasInitDone)
            {
                lock (videoCaptureLockObject)
                {
                    if (_videoCapture.get(Videoio.CAP_PROP_POS_FRAMES) >= _videoCapture.get(Videoio.CAP_PROP_FRAME_COUNT))
                    {
                        _videoCapture.set(Videoio.CAP_PROP_POS_FRAMES, 0);
                        _videoCapture.read(imageBufferMat);
                    }
                }

                isPlaying = true;
            }
        }

        /// <summary>
        /// Pause the video.
        /// </summary>
        public virtual void Pause()
        {
            if (hasInitDone)
                isPlaying = false;
        }

        /// <summary>
        /// Stop the video.
        /// </summary>
        public virtual void Stop()
        {
            if (hasInitDone)
                isPlaying = false;
        }

        /// <summary>
        /// Indicate whether the video is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the video is playing, <c>false</c> otherwise.</returns>
        public virtual bool IsPlaying()
        {
            return hasInitDone ? isPlaying : false;
        }

        /// <summary>
        /// Indicate whether the video is paused.
        /// </summary>
        /// <returns><c>true</c>, if the video is paused, <c>false</c> otherwise.</returns>
        public virtual bool IsPaused()
        {
            return hasInitDone ? isPlaying : false;
        }

        /// <summary>
        /// Return the active video device name.
        /// </summary>
        /// <returns>The active video device name.</returns>
        public virtual string GetDeviceName()
        {
            return "OpenCV_VideoCapture";
        }

        /// <summary>
        /// Return the video width.
        /// </summary>
        /// <returns>The video width.</returns>
        public virtual int GetWidth()
        {
            if (!hasInitDone)
                return -1;
            return frameMat.width();
        }

        /// <summary>
        /// Return the video height.
        /// </summary>
        /// <returns>The video height.</returns>
        public virtual int GetHeight()
        {
            if (!hasInitDone)
                return -1;
            return frameMat.height();
        }

        /// <summary>
        /// Return the video framerate.
        /// </summary>
        /// <returns>The video framerate.</returns>
        public virtual float GetFPS()
        {
            return hasInitDone ? (float)videoCapture.get(Videoio.CAP_PROP_FPS) : -1f;
        }

        /// <summary>
        /// Return the relative position of the video file: 0=start of the film, 1=end of the film.
        /// </summary>
        /// <returns>The relative position of the video file: 0=start of the film, 1=end of the film.</returns>
        public virtual float GetFramePosRatio()
        {
            if (hasInitDone)
            {
                return (float)videoCapture.get(Videoio.CAP_PROP_POS_AVI_RATIO);
            }
            else
            {
                return 0f;
            }
        }

        /// <summary>
        /// Set the relative position of the video file: 0=start of the film, 1=end of the film.
        /// </summary>
        /// <param name="ratio">The relative position of the video file: 0=start of the film, 1=end of the film.</param>
        public virtual void SetFramePosRatio(float ratio)
        {
            if (hasInitDone)
            {
                lock (videoCaptureLockObject)
                {
                    ratio = Mathf.Clamp01(ratio);

                    bool supported = _videoCapture.set(Videoio.CAP_PROP_POS_AVI_RATIO, ratio);

                    if (!supported)
                    {
                        _videoCapture.set(Videoio.CAP_PROP_POS_FRAMES, (int)(ratio * _videoCapture.get(Videoio.CAP_PROP_FRAME_COUNT)));
                    }

                    if (_videoCapture.get(Videoio.CAP_PROP_POS_FRAMES) >= _videoCapture.get(Videoio.CAP_PROP_FRAME_COUNT))
                    {
                        if (loop)
                        {
                            _videoCapture.set(Videoio.CAP_PROP_POS_FRAMES, 0);
                        }
                        else
                        {
                            _videoCapture.set(Videoio.CAP_PROP_POS_FRAMES, (int)_videoCapture.get(Videoio.CAP_PROP_FRAME_COUNT) - 1);
                            isPlaying = false;
                        }
                    }

                    _videoCapture.read(imageBufferMat);
                }
            }
        }

        /// <summary>
        /// Return the video frame index.
        /// </summary>
        /// <returns>The video frame index.</returns>
        public virtual int GetFrameIndex()
        {
            if (hasInitDone)
            {
                return (int)videoCapture.get(Videoio.CAP_PROP_POS_FRAMES);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Set the video frame index.
        /// </summary>
        /// <param name="index">The video frame index.</param>
        public virtual void SetFrameIndex(int index)
        {
            if (hasInitDone)
            {
                lock (videoCaptureLockObject)
                {
                    SetFramePosRatio((float)(index / _videoCapture.get(Videoio.CAP_PROP_FRAME_COUNT)));
                }
            }
        }

        /// <summary>
        /// Return the number of frames in the current video content.
        /// </summary>
        /// <returns>The number of frames in the current video content.</returns>
        public virtual int GetFrameCount()
        {
            if (hasInitDone)
            {
                return (int)videoCapture.get(Videoio.CAP_PROP_FRAME_COUNT);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Return the video base color format.
        /// </summary>
        /// <returns>The video base color format.</returns>
        public virtual Source2MatHelperColorFormat GetBaseColorFormat()
        {
            return baseColorFormat;
        }

        /// <summary>
        /// Return the VideoCapture instance.
        /// </summary>
        /// <returns>The VideoCapture instance.</returns>
        public virtual VideoCapture GetVideoCapture()
        {
            return hasInitDone ? videoCapture : null;
        }

        /// <summary>
        /// Indicate whether the video buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the video buffer has been updated <c>false</c> otherwise.</returns>
        public virtual bool DidUpdateThisFrame()
        {
            if (!hasInitDone)
                return false;

            return didUpdateThisFrame;
        }

        /// <summary>
        /// Get the mat of the current frame.
        /// The Mat object's type is 'CV_8UC4' or 'CV_8UC3' or 'CV_8UC1' (ColorFormat is determined by the outputColorFormat setting).
        /// </summary>
        /// <returns>The mat of the current frame.</returns>
        public virtual Mat GetMat()
        {
            if (!hasInitDone)
            {
                return frameMat;
            }

            didUpdateImageBufferInCurrentFrame = false;

            if (baseColorFormat == outputColorFormat)
            {
                imageBufferMat.copyTo(frameMat);
            }
            else
            {
                Imgproc.cvtColor(imageBufferMat, frameMat, Source2MatHelperUtils.ColorConversionCodes(baseColorFormat, outputColorFormat));
            }

            return frameMat;
        }

        /// <summary>
        /// Cancel Init Coroutine.
        /// </summary>
        protected virtual void CancelInitCoroutine()
        {
            if (getFilePathCoroutine != null)
            {
                StopCoroutine(getFilePathCoroutine);
                ((IDisposable)getFilePathCoroutine).Dispose();
                getFilePathCoroutine = null;
            }

            if (initCoroutine != null)
            {
                StopCoroutine(initCoroutine);
                ((IDisposable)initCoroutine).Dispose();
                initCoroutine = null;
            }
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        protected virtual void ReleaseResources()
        {
            isInitWaiting = false;
            hasInitDone = false;

            isReading = false;
            isPlaying = false;
            didUpdateThisFrame = false;
            didUpdateImageBufferInCurrentFrame = false;

            if (videoCapture != null)
            {
                videoCapture.Dispose();
                videoCapture = null;
            }
            if (frameMat != null)
            {
                frameMat.Dispose();
                frameMat = null;
            }
            if (baseMat != null)
            {
                baseMat.Dispose();
                baseMat = null;
            }

            if (imageBufferMat != null)
            {
                imageBufferMat.Dispose();
                imageBufferMat = null;
            }

            StopWaitFrameTimeThread();
        }

        /// <summary>
        /// Releases all resource used by the <see cref="VideoCaptrueToMatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="VideoCaptrueToMatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="VideoCaptrueToMatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="VideoCaptrueToMatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="VideoCaptrueToMatHelper"/> was occupying.</remarks>
        public virtual void Dispose()
        {
            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }
            else if (hasInitDone)
            {
                ReleaseResources();

                if (_onDisposed != null)
                    _onDisposed.Invoke();
            }
        }
    }
}