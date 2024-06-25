using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Video;

namespace OpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// UnityVideoPlayer 2 mat helper.
    /// v 1.0.0
    /// 
    /// By setting outputColorFormat to RGBA processing that does not include extra color conversion is performed.
    /// </summary>
    public class UnityVideoPlayer2MatHelper : MonoBehaviour, IVideoSource2MatHelper
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
        /// Select the output color format.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("outputColorFormat"), TooltipAttribute("Select the output color format.")]
        protected Source2MatHelperColorFormat _outputColorFormat = Source2MatHelperColorFormat.RGBA;

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
            get { return _loop; }
            set
            {
                if (videoPlayer != null) videoPlayer.isLooping = value;
                _loop = value;
            }
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

        /// <summary>
        /// The videoPlayer
        /// </summary>
        protected VideoPlayer videoPlayer;

        /// <summary>
        /// 
        /// </summary>
        protected bool didUpdateThisFrame = false;

        /// <summary>
        /// The texture.
        /// </summary>
        protected Texture2D texture;

        /// <summary>
        /// The frame mat.
        /// </summary>
        protected Mat frameMat;

        /// <summary>
        /// The base mat.
        /// </summary>
        protected Mat baseMat;

        /// <summary>
        /// The frame index
        /// </summary>
        protected long frameIndex;

        /// <summary>
        /// The useAsyncGPUReadback
        /// </summary>
        protected bool useAsyncGPUReadback;

        /// <summary>
        /// The base color format.
        /// </summary>
        protected Source2MatHelperColorFormat baseColorFormat = Source2MatHelperColorFormat.RGBA;

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

            if (didUpdateThisFrame)
            {
                didUpdateThisFrame = false;
            }

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

#if !OPENCV_DONT_USE_UNSAFE_CODE
            useAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback;
#else
            useAsyncGPUReadback = false;
#endif

            Debug.Log("UnityVideoPlayer2MatHelper:: " + "useAsyncGPUReadback: " + useAsyncGPUReadback);

            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            autoPlayAfterInitialize = autoPlay;
            if (onInitialized == null)
                onInitialized = new UnityEvent();
            if (onDisposed == null)
                onDisposed = new UnityEvent();
            if (onErrorOccurred == null)
                onErrorOccurred = new Source2MatHelperErrorUnityEvent();

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

#if !OPENCV_DONT_USE_UNSAFE_CODE
            useAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback;
#else
            useAsyncGPUReadback = false;
#endif

            Debug.Log("UnityVideoPlayer2MatHelper:: " + "useAsyncGPUReadback: " + useAsyncGPUReadback);

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

                if (onDisposed != null)
                    onDisposed.Invoke();
            }

            isInitWaiting = true;

            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent(typeof(VideoPlayer)) as VideoPlayer;
            }

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = _requestedVideoFilePath;
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = _loop;
            videoPlayer.renderMode = VideoRenderMode.APIOnly;
            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.frameReady += FrameReady;
            videoPlayer.prepareCompleted += PrepareCompleted;
            videoPlayer.errorReceived += ErrorReceived;

            videoPlayer.Prepare();


            int initFrameCount = 0;
            bool isTimeout = false;

            while (true)
            {
                if (initFrameCount > timeoutFrameCount)
                {
                    isTimeout = true;
                    break;
                }
                else if (hasInitDone)
                {
                    if (autoPlayAfterInitialize)
                    {
                        videoPlayer.Play();
                    }
                    else
                    {
                        videoPlayer.Stop();
                    }

                    if (onInitialized != null)
                        onInitialized.Invoke();

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
                videoPlayer.sendFrameReadyEvents = false;
                videoPlayer.frameReady -= FrameReady;
                videoPlayer.prepareCompleted -= PrepareCompleted;
                videoPlayer.errorReceived -= ErrorReceived;
                videoPlayer.Stop();
                videoPlayer = null;
                isInitWaiting = false;
                initCoroutine = null;

                if (_onErrorOccurred != null)
                    _onErrorOccurred.Invoke(Source2MatHelperErrorCode.TIMEOUT, string.Empty);
            }

        }

        void PrepareCompleted(VideoPlayer vp)
        {
            //Debug.Log("UnityVideoPlayer2MatHelper:: " + "Video Url: " + vp.url);
            //Debug.Log("UnityVideoPlayer2MatHelper:: " + "width: " + vp.width + " height: " + vp.height);
            //Debug.Log("UnityVideoPlayer2MatHelper:: " + "canSetSkipOnDrop: " + vp.canSetSkipOnDrop);

            int frameWidth = (int)vp.width;
            int frameHeight = (int)vp.height;

            if (!useAsyncGPUReadback)
            {
                texture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
            }
            baseMat = new Mat(frameHeight, frameWidth, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));

            if (baseColorFormat == outputColorFormat)
            {
                frameMat = baseMat.clone();
            }
            else
            {
                frameMat = new Mat(baseMat.rows(), baseMat.cols(), CvType.CV_8UC(Source2MatHelperUtils.Channels(outputColorFormat)), new Scalar(0, 0, 0, 255));
            }

            Debug.Log("UnityVideoPlayer2MatHelper:: " + " fileUrl:" + videoPlayer.url + " width:" + frameMat.width() + " height:" + frameMat.height() + " fps:" + videoPlayer.frameRate);

            isInitWaiting = false;
            hasInitDone = true;
            initCoroutine = null;
        }

        void ErrorReceived(VideoPlayer source, string message)
        {
            //Debug.Log("UnityVideoPlayer2MatHelper:: " + "ErrorReceived: " + message);

            if (onErrorOccurred != null)
                onErrorOccurred.Invoke(Source2MatHelperErrorCode.UNKNOWN, "ErrorReceived: " + message);
        }

        void FrameReady(VideoPlayer vp, long frameIndex)
        {
            //Debug.Log("UnityVideoPlayer2MatHelper:: " + "FrameReady " + frameIndex);

            if (videoPlayer != null && (videoPlayer.isPaused || !videoPlayer.isPlaying))
            {
                this.frameIndex = frameIndex;
                return;
            }

            if (!useAsyncGPUReadback)
            {
                Utils.textureToTexture2D(vp.texture, texture);

                Utils.fastTexture2DToMat(texture, baseMat);

                didUpdateThisFrame = true;
                this.frameIndex = frameIndex;
            }
            else
            {
                AsyncGPUReadback.Request(vp.texture, 0, TextureFormat.RGBA32, (request) => { OnCompleteReadback(request, frameIndex); });
            }
        }

        void OnCompleteReadback(AsyncGPUReadbackRequest request, long frameIndex)
        {
            //Debug.Log("UnityVideoPlayer2MatHelper:: " + "OnCompleteReadback");

            if (videoPlayer != null && (videoPlayer.isPaused || !videoPlayer.isPlaying))
            {
                this.frameIndex = frameIndex;
                return;
            }

            if (request.hasError)
            {
                Debug.Log("UnityVideoPlayer2MatHelper:: " + "GPU readback error detected. " + frameIndex);

            }
            else if (request.done)
            {
                //Debug.Log("UnityVideoPlayer2MatHelper:: " + "Start GPU readback done. "+frameIndex);

                //Debug.Log("UnityVideoPlayer2MatHelper:: " + "Thread.CurrentThread.ManagedThreadId " + Thread.CurrentThread.ManagedThreadId);

#if !OPENCV_DONT_USE_UNSAFE_CODE
                MatUtils.copyToMat(request.GetData<byte>(), baseMat);
#endif

                Core.flip(baseMat, baseMat, 0);

                didUpdateThisFrame = true;
                this.frameIndex = frameIndex;

                //Debug.Log("UnityVideoPlayer2MatHelper:: " + "End GPU readback done. " + frameIndex);
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
        /// Start the video.
        /// </summary>
        public virtual void Play()
        {
            if (hasInitDone)
                videoPlayer.Play();
        }

        /// <summary>
        /// Pause the video.
        /// </summary>
        public virtual void Pause()
        {
            if (hasInitDone)
                videoPlayer.Pause();
        }

        /// <summary>
        /// Stop the video.
        /// </summary>
        public virtual void Stop()
        {
            if (hasInitDone)
                videoPlayer.Stop();
        }

        /// <summary>
        /// Indicate whether the video is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the video is playing, <c>false</c> otherwise.</returns>
        public virtual bool IsPlaying()
        {
            return hasInitDone ? videoPlayer.isPlaying : false;
        }

        /// <summary>
        /// Indicate whether the video is paused.
        /// </summary>
        /// <returns><c>true</c>, if the video is paused, <c>false</c> otherwise.</returns>
        public virtual bool IsPaused()
        {
            return hasInitDone ? videoPlayer.isPaused : false;
        }

        /// <summary>
        /// Return the active video device name.
        /// </summary>
        /// <returns>The active video device name.</returns>
        public virtual string GetDeviceName()
        {
            return "Unity_VideoPlayer";
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
            return hasInitDone ? videoPlayer.frameRate : -1f;
        }

        /// <summary>
        /// Return the relative position of the video file: 0=start of the film, 1=end of the film.
        /// </summary>
        /// <returns>The relative position of the video file: 0=start of the film, 1=end of the film.</returns>
        public virtual float GetFramePosRatio()
        {
            if (hasInitDone)
            {
                return (float)videoPlayer.frame / videoPlayer.frameCount;
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
                videoPlayer.frame = (long)(videoPlayer.frameCount * ratio);
            }
        }

        /// <summary>
        /// Return the video frame index.
        /// </summary>
        /// <returns>The video frame index.</returns>
        public virtual int GetFrameIndex()
        {
            return hasInitDone ? (int)frameIndex : 0;
        }

        /// <summary>
        /// Set the video frame index.
        /// </summary>
        /// <param name="index">The video frame index.</param>
        public virtual void SetFrameIndex(int index)
        {
            if (hasInitDone)
            {
                videoPlayer.frame = index;
            }
        }

        /// <summary>
        /// Return the number of frames in the current video content.
        /// </summary>
        /// <returns>The number of frames in the current video content.</returns>
        public virtual int GetFrameCount()
        {
            return hasInitDone ? (int)videoPlayer.frameCount : 0;
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
        public virtual VideoPlayer GetVideoPlayer()
        {
            return hasInitDone ? videoPlayer : null;
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

            if (baseColorFormat == outputColorFormat)
            {
                baseMat.copyTo(frameMat);
            }
            else
            {
                Imgproc.cvtColor(baseMat, frameMat, Source2MatHelperUtils.ColorConversionCodes(baseColorFormat, outputColorFormat));
            }

            return frameMat;
        }

        /// <summary>
        /// Cancel Init Coroutine.
        /// </summary>
        protected virtual void CancelInitCoroutine()
        {
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

            didUpdateThisFrame = false;

            frameIndex = 0;

            if (videoPlayer != null)
            {
                videoPlayer.sendFrameReadyEvents = false;
                videoPlayer.frameReady -= FrameReady;
                videoPlayer.prepareCompleted -= PrepareCompleted;
                videoPlayer.errorReceived -= ErrorReceived;

                videoPlayer.Stop();
                videoPlayer = null;
            }
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
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
            if (useAsyncGPUReadback)
            {
                AsyncGPUReadback.WaitAllRequests();
            }

            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }
            else if (hasInitDone)
            {
                ReleaseResources();

                if (onDisposed != null)
                    onDisposed.Invoke();
            }
        }
    }
}