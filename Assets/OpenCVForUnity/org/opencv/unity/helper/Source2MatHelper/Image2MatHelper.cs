using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace OpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// Image 2 mat helper.
    /// v 1.0.0
    /// 
    /// By setting outputColorFormat to GRAY or BGR, processing that does not include extra color conversion is performed.
    /// </summary>
    public class Image2MatHelper : MonoBehaviour, IImageSource2MatHelper
    {
        /// <summary>
        /// Set the image file path, relative to the starting point of the "StreamingAssets" folder, or absolute path.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("requestedImageFilePath"), TooltipAttribute("Set the image file path, relative to the starting point of the \"StreamingAssets\" folder, or absolute path.")]
        protected string _requestedImageFilePath = string.Empty;

        public virtual string requestedImageFilePath
        {
            get { return _requestedImageFilePath; }
            set
            {
                if (_requestedImageFilePath != value)
                {
                    _requestedImageFilePath = value;
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
        /// Indicate whether to play this image in a repeat.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("repeat"), TooltipAttribute("Indicate whether to play this image in a repeat.")]
        protected bool _repeat = true;
        public virtual bool repeat
        {
            get { return _repeat; }
            set { _repeat = value; }
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

        protected bool isPlaying = false;

        protected bool didUpdateThisFrame = false;

        protected bool didUpdateImageBufferInCurrentFrame = false;

        /// <summary>
        /// The frame mat.
        /// </summary>
        protected Mat frameMat;

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

        protected string imageFileFullPath;

        /// <summary>
        /// The wait frame time coroutine.
        /// </summary>
        protected IEnumerator waitFrameTimeCoroutine;

        protected bool isThreadRunning = false;

        protected bool shouldStopThread = false;

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
                didUpdateThisFrame = false;
            }

            didUpdateImageBufferInCurrentFrame = false;

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
        /// <param name="requestedImageFilePath">Requested image file path.</param>
        /// <param name="autoPlay">If set to <c>true</c> play after completion of initialization.</param>
        public virtual void Initialize(string requestedImageFilePath, bool autoPlay = true)
        {
            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            _requestedImageFilePath = requestedImageFilePath;
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
            imageFileFullPath = string.Empty;

            Uri uri;
            if (Uri.TryCreate(requestedImageFilePath, UriKind.Absolute, out uri))
            {
                hasFilePathCoroutineCompleted = true;
                imageFileFullPath = uri.OriginalString;
            }
            else
            {
                getFilePathCoroutine = Utils.getFilePathAsync(requestedImageFilePath, (result) =>
                {
                    hasFilePathCoroutineCompleted = true;
                    imageFileFullPath = result;
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
                    if (string.IsNullOrEmpty(imageFileFullPath))
                    {
                        isInitWaiting = false;
                        initCoroutine = null;
                        getFilePathCoroutine = null;

                        if (_onErrorOccurred != null)
                            _onErrorOccurred.Invoke(Source2MatHelperErrorCode.IMAGE_FILE_NOT_EXIST, requestedImageFilePath);

                        yield break;
                    }

                    if (outputColorFormat == Source2MatHelperColorFormat.GRAY)
                        baseColorFormat = Source2MatHelperColorFormat.GRAY;

                    Mat img = Imgcodecs.imread(imageFileFullPath, baseColorFormat == Source2MatHelperColorFormat.GRAY ? Imgcodecs.IMREAD_GRAYSCALE : Imgcodecs.IMREAD_COLOR);

                    if (img.empty())
                    {
                        isInitWaiting = false;
                        initCoroutine = null;
                        getFilePathCoroutine = null;

                        if (_onErrorOccurred != null)
                            _onErrorOccurred.Invoke(Source2MatHelperErrorCode.IMAGE_FILE_CANT_OPEN, imageFileFullPath);

                        yield break;
                    }

                    frameMat = new Mat(img.rows(), img.cols(), CvType.CV_8UC(Source2MatHelperUtils.Channels(outputColorFormat)), new Scalar(0, 0, 0, 255));

                    Debug.Log("Image2MatHelper:: " + " filePath:" + requestedImageFilePath + " width:" + frameMat.width() + " height:" + frameMat.height());

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

            //Debug.Log("Image2MatHelper:: " + "Thread Start");

            isThreadRunning = true;
            shouldStopThread = false;

            waitFrameTimeCoroutine = WaitFrameTimeThreadWorker();
            StartCoroutine(waitFrameTimeCoroutine);

        }

        protected virtual void StopWaitFrameTimeThread()
        {
            if (!isThreadRunning)
                return;

            if (waitFrameTimeCoroutine != null)
            {
                StopCoroutine(waitFrameTimeCoroutine);
                ((IDisposable)waitFrameTimeCoroutine).Dispose();
                waitFrameTimeCoroutine = null;
            }
            isThreadRunning = false;

            //Debug.Log("Image2MatHelper:: " + "Thread Stop");
        }

        protected virtual IEnumerator WaitFrameTimeThreadWorker()
        {
            WaitForSeconds wait = new WaitForSeconds(1f);

            while (true)
            {
                if (isPlaying)
                {
                    while (didUpdateThisFrame)
                    {
                        yield return null;
                    }

                    didUpdateThisFrame = true;
                    didUpdateImageBufferInCurrentFrame = true;

                    if (_repeat)
                    {
                        yield return wait;
                    }
                    else
                    {
                        yield return null;
                        isPlaying = false;
                    }
                }
                else
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Indicate whether this instance has been initialized.
        /// </summary>
        /// <returns><c>true</c>, if this instance has been initialized, <c>false</c> otherwise.</returns>
        public virtual bool IsInitialized()
        {
            return hasInitDone;
        }

        /// <summary>
        /// Start the image.
        /// </summary>
        public virtual void Play()
        {
            if (hasInitDone)
                isPlaying = true;
        }

        /// <summary>
        /// Pause the image.
        /// </summary>
        public virtual void Pause()
        {
            if (hasInitDone)
                isPlaying = false;
        }

        /// <summary>
        /// Stop the image.
        /// </summary>
        public virtual void Stop()
        {
            if (hasInitDone)
                isPlaying = false;
        }

        /// <summary>
        /// Indicate whether the image is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the image is playing, <c>false</c> otherwise.</returns>
        public virtual bool IsPlaying()
        {
            return hasInitDone ? isPlaying : false;
        }

        /// <summary>
        /// Indicate whether the image is paused.
        /// </summary>
        /// <returns><c>true</c>, if the image is paused, <c>false</c> otherwise.</returns>
        public virtual bool IsPaused()
        {
            return hasInitDone ? isPlaying : false;
        }

        /// <summary>
        /// Return the active image device name.
        /// </summary>
        /// <returns>The active image device name.</returns>
        public virtual string GetDeviceName()
        {
            return "OpenCV_Imgcodecs.imread";
        }

        /// <summary>
        /// Return the image width.
        /// </summary>
        /// <returns>The image width.</returns>
        public virtual int GetWidth()
        {
            if (!hasInitDone)
                return -1;
            return frameMat.width();
        }

        /// <summary>
        /// Return the image height.
        /// </summary>
        /// <returns>The image height.</returns>
        public virtual int GetHeight()
        {
            if (!hasInitDone)
                return -1;
            return frameMat.height();
        }

        /// <summary>
        /// Return the image base color format.
        /// </summary>
        /// <returns>The image base color format.</returns>
        public virtual Source2MatHelperColorFormat GetBaseColorFormat()
        {
            return baseColorFormat;
        }

        /// <summary>
        /// Indicate whether the image buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the image buffer has been updated <c>false</c> otherwise.</returns>
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
                using (Mat img = Imgcodecs.imread(imageFileFullPath, baseColorFormat == Source2MatHelperColorFormat.GRAY ? Imgcodecs.IMREAD_GRAYSCALE : Imgcodecs.IMREAD_COLOR))
                    img.copyTo(frameMat);
            }
            else
            {
                using (Mat img = Imgcodecs.imread(imageFileFullPath, baseColorFormat == Source2MatHelperColorFormat.GRAY ? Imgcodecs.IMREAD_GRAYSCALE : Imgcodecs.IMREAD_COLOR))
                    Imgproc.cvtColor(img, frameMat, Source2MatHelperUtils.ColorConversionCodes(baseColorFormat, outputColorFormat));
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

            isPlaying = false;
            didUpdateThisFrame = false;
            didUpdateImageBufferInCurrentFrame = false;

            if (frameMat != null)
            {
                frameMat.Dispose();
                frameMat = null;
            }

            StopWaitFrameTimeThread();
        }

        /// <summary>
        /// Releases all resource used by the <see cref="Image2MatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Image2MatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="Image2MatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="Image2MatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="Image2MatHelper"/> was occupying.</remarks>
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