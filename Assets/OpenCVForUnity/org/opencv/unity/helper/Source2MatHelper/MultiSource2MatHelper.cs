using OpenCVForUnity.CoreModule;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace OpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// MultiSource 2 mat helper.
    /// v 1.0.0
    /// </summary>
    public class MultiSource2MatHelper : MonoBehaviour, ISource2MatHelper
    {
        public enum MultiSource2MatHelperClassName : int
        {
            WebCamTexture2MatHelper = 0,
            //VideoCaptureCameraInput2MatHelper,
            VideoCapture2MatHelper,
            UnityVideoPlayer2MatHelper,
            Image2MatHelper,
            CustomSource2MatHelper,
        }

        public enum MultiSource2MatHelperClassCategory : int
        {
            Camera = 0,
            Video,
            Image,
            Custom,
        }

        protected ISource2MatHelper _source2MatHelper;
        public ISource2MatHelper source2MatHelper
        {
            get => _source2MatHelper;
        }

        /// <summary>
        /// Select the source to mat helper class name. If CustomSource2MatHelper is selected, only the UnityEvent inspector is overridden for the custom helper class component.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("requestedSource2MatHelperClassName"), TooltipAttribute("Select the source to mat helper class name. If CustomSource2MatHelper is selected, only the UnityEvent inspector is overridden for the custom helper class component.")]
        protected MultiSource2MatHelperClassName _requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatHelper;
        public MultiSource2MatHelperClassName requestedSource2MatHelperClassName
        {
            get => _requestedSource2MatHelperClassName;
            set
            {
                if (_requestedSource2MatHelperClassName != value)
                {
                    _requestedSource2MatHelperClassName = value;

                    _currentSource2MatHelperClassCategory = ClassNameToClassCategory(_requestedSource2MatHelperClassName);

                    bool autoPlay = true;
                    if (_source2MatHelper != null)
                    {
                        autoPlay = _source2MatHelper.IsPlaying();
                        _source2MatHelper.Dispose();
                        Destroy(_source2MatHelper as Component);
                        _source2MatHelper = null;
                    }

                    Initialize(autoPlay);
                }
            }
        }

        protected MultiSource2MatHelperClassName _currentSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatHelper;

        [SerializeField]
        [HideInInspector]
        protected MultiSource2MatHelperClassCategory _currentSource2MatHelperClassCategory = MultiSource2MatHelperClassCategory.Camera;

        /// <summary>
        /// Set the name of the camera device to use. (or device index number)
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Camera, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("requestedDeviceName"), TooltipAttribute("Set the name of the device to use. (or device index number)")]
        protected string _requestedDeviceName = null;
        string requestedDeviceName
        {
            get => _requestedDeviceName;
            set
            {
                if (_requestedDeviceName != value)
                {
                    _requestedDeviceName = value;
                    if (_source2MatHelper != null && _source2MatHelper is ICameraSource2MatHelper helper)
                        helper.requestedDeviceName = value;
                }
            }
        }

        /// <summary>
        /// Set the width of camera.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Camera, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("requestedWidth"), TooltipAttribute("Set the width of camera.")]
        protected int _requestedWidth = 640;

        public virtual int requestedWidth
        {
            get => _requestedWidth;
            set
            {
                int _value = (int)Mathf.Clamp(value, 0f, float.MaxValue);
                if (_requestedWidth != _value)
                {
                    _requestedWidth = _value;
                    if (_source2MatHelper != null && _source2MatHelper is ICameraSource2MatHelper helper)
                        helper.requestedWidth = _value;
                }
            }
        }

        /// <summary>
        /// Set the height of camera.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Camera, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("requestedHeight"), TooltipAttribute("Set the height of camera.")]
        protected int _requestedHeight = 480;

        public virtual int requestedHeight
        {
            get => _requestedHeight;
            set
            {
                int _value = (int)Mathf.Clamp(value, 0f, float.MaxValue);
                if (_requestedHeight != _value)
                {
                    _requestedHeight = _value;
                    if (_source2MatHelper != null && _source2MatHelper is ICameraSource2MatHelper helper)
                        helper.requestedHeight = _value;
                }
            }
        }

        /// <summary>
        /// Set whether to use the front facing camera.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Camera, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("requestedIsFrontFacing"), TooltipAttribute("Set whether to use the front facing camera.")]
        protected bool _requestedIsFrontFacing = false;

        public virtual bool requestedIsFrontFacing
        {
            get => _requestedIsFrontFacing;
            set
            {
                if (_requestedIsFrontFacing != value)
                {
                    _requestedIsFrontFacing = value;
                    if (_source2MatHelper != null && _source2MatHelper is ICameraSource2MatHelper helper)
                        helper.requestedIsFrontFacing = value;
                }
            }
        }

        /// <summary>
        /// Set the frame rate of camera.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Camera, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("requestedFPS"), TooltipAttribute("Set the frame rate of camera.")]
        protected float _requestedFPS = 30f;

        public virtual float requestedFPS
        {
            get => _requestedFPS;
            set
            {
                float _value = Mathf.Clamp(value, -1f, float.MaxValue);
                if (_requestedFPS != _value)
                {
                    _requestedFPS = _value;
                    if (_source2MatHelper != null && _source2MatHelper is ICameraSource2MatHelper helper)
                        helper.requestedFPS = _value;
                }
            }
        }




        /// <summary>
        /// Set the video file path, relative to the starting point of the "StreamingAssets" folder, or absolute path.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Video, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("requestedVideoFilePath"), TooltipAttribute("Set the video file path, relative to the starting point of the \"StreamingAssets\" folder, or absolute path.")]
        protected string _requestedVideoFilePath = string.Empty;

        public virtual string requestedVideoFilePath
        {
            get => _requestedVideoFilePath;
            set
            {
                if (_requestedVideoFilePath != value)
                {
                    _requestedVideoFilePath = value;
                    if (_source2MatHelper != null && _source2MatHelper is IVideoSource2MatHelper helper)
                        helper.requestedVideoFilePath = value;
                }
            }
        }

        /// <summary>
        /// Indicate whether to play this video in a loop.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Video, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("loop"), TooltipAttribute("Indicate whether to play this video in a loop.")]
        protected bool _loop = true;
        public virtual bool loop
        {
            get => _loop;
            set
            {
                if (_loop != value)
                {
                    _loop = value;
                    if (_source2MatHelper != null && _source2MatHelper is IVideoSource2MatHelper helper)
                        helper.loop = value;
                }
            }
        }



        /// <summary>
        /// Set the image file path, relative to the starting point of the "StreamingAssets" folder, or absolute path.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Image, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("requestedImageFilePath"), TooltipAttribute("Set the image file path, relative to the starting point of the \"StreamingAssets\" folder, or absolute path.")]
        protected string _requestedImageFilePath = string.Empty;

        public virtual string requestedImageFilePath
        {
            get => _requestedImageFilePath;
            set
            {
                if (_requestedImageFilePath != value)
                {
                    _requestedImageFilePath = value;
                    if (_source2MatHelper != null && _source2MatHelper is IImageSource2MatHelper helper)
                        helper.requestedImageFilePath = value;
                }
            }
        }

        /// <summary>
        /// Indicate whether to play this image in a repeat.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Image, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("repeat"), TooltipAttribute("Indicate whether to play this image in a repeat.")]
        protected bool _repeat = true;
        public virtual bool repeat
        {
            get => _repeat;
            set
            {
                if (_repeat != value)
                {
                    _repeat = value;
                    if (_source2MatHelper != null && _source2MatHelper is IImageSource2MatHelper helper)
                        helper.repeat = value;
                }
            }
        }



        /// <summary>
        /// Set the custom class <ISource2MatHelper> component.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityConditionalDisableInInspector("_currentSource2MatHelperClassCategory", (int)MultiSource2MatHelperClassCategory.Custom, conditionalInvisible: true, runtimeDisable: true)]
#endif
        [SerializeField, FormerlySerializedAs("customClassComponent"), TooltipAttribute("Set the custom class <ISource2MatHelper> component.")]
        protected Component _customClassComponent = null;
        Component customClassComponent
        {
            get => _customClassComponent;
            set => _customClassComponent = value;
        }


        [Space(10)]


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
                    if (_source2MatHelper != null)
                        _source2MatHelper.outputColorFormat = _outputColorFormat;
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
            set
            {
                _timeoutFrameCount = (int)Mathf.Clamp(value, 0f, float.MaxValue);
                if (_source2MatHelper != null)
                    _source2MatHelper.timeoutFrameCount = _timeoutFrameCount;
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
            set
            {
                _onInitialized = value;
                if (_source2MatHelper != null)
                    _source2MatHelper.onInitialized = _onInitialized;
            }
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is disposed.
        /// </summary>
        [SerializeField, FormerlySerializedAs("onDisposed"), TooltipAttribute("UnityEvent that is triggered when this instance is disposed.")]
        protected UnityEvent _onDisposed;
        public UnityEvent onDisposed
        {
            get => _onDisposed;
            set
            {
                _onDisposed = value;
                if (_source2MatHelper != null)
                    _source2MatHelper.onDisposed = _onDisposed;
            }
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is error Occurred.
        /// </summary>
        [SerializeField, FormerlySerializedAs("onErrorOccurred"), TooltipAttribute("UnityEvent that is triggered when this instance is error Occurred.")]
        protected Source2MatHelperErrorUnityEvent _onErrorOccurred;
        public Source2MatHelperErrorUnityEvent onErrorOccurred
        {
            get => _onErrorOccurred;
            set
            {
                _onErrorOccurred = value;
                if (_source2MatHelper != null)
                    _source2MatHelper.onErrorOccurred = _onErrorOccurred;
            }
        }

        protected virtual void OnValidate()
        {
            _currentSource2MatHelperClassCategory = ClassNameToClassCategory(_requestedSource2MatHelperClassName);

            _requestedWidth = (int)Mathf.Clamp(_requestedWidth, 0f, float.MaxValue);
            _requestedHeight = (int)Mathf.Clamp(_requestedHeight, 0f, float.MaxValue);
            _requestedFPS = Mathf.Clamp(_requestedFPS, -1f, float.MaxValue);
            _timeoutFrameCount = (int)Mathf.Clamp(_timeoutFrameCount, 0f, float.MaxValue);
        }

        protected virtual MultiSource2MatHelperClassCategory ClassNameToClassCategory(MultiSource2MatHelperClassName className)
        {
            switch (_requestedSource2MatHelperClassName)
            {
                case MultiSource2MatHelperClassName.WebCamTexture2MatHelper:
                    //case MultiSource2MatHelperClassName.VideoCaptureCameraInput2MatHelper:
                    return MultiSource2MatHelperClassCategory.Camera;
                case MultiSource2MatHelperClassName.VideoCapture2MatHelper:
                case MultiSource2MatHelperClassName.UnityVideoPlayer2MatHelper:
                    return MultiSource2MatHelperClassCategory.Video;
                case MultiSource2MatHelperClassName.Image2MatHelper:
                    return MultiSource2MatHelperClassCategory.Image;
                case MultiSource2MatHelperClassName.CustomSource2MatHelper:
                default:
                    return MultiSource2MatHelperClassCategory.Custom;
            }
        }

        public virtual MultiSource2MatHelperClassName GetCurrentSource2MatHelperClassName()
        {
            return _currentSource2MatHelperClassName;
        }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <param name="autoPlay">If set to <c>true</c> play after completion of initialization.</param>
        public virtual void Initialize(bool autoPlay = true)
        {
            if (_source2MatHelper == null)
            {
                switch (_requestedSource2MatHelperClassName)
                {
                    case MultiSource2MatHelperClassName.WebCamTexture2MatHelper:
                        _source2MatHelper = gameObject.AddComponent(typeof(WebCamTexture2MatHelper)) as ISource2MatHelper;
                        break;
                    //case MultiSource2MatHelperClassName.VideoCaptureCameraInput2MatHelper:
                    //    _source2MatHelper = gameObject.AddComponent(typeof(VideoCaptureCameraInput2MatHelper)) as ISource2MatHelper;
                    //    break;
                    case MultiSource2MatHelperClassName.VideoCapture2MatHelper:
                        _source2MatHelper = gameObject.AddComponent(typeof(VideoCapture2MatHelper)) as ISource2MatHelper;
                        break;
                    case MultiSource2MatHelperClassName.UnityVideoPlayer2MatHelper:
                        _source2MatHelper = gameObject.AddComponent(typeof(UnityVideoPlayer2MatHelper)) as ISource2MatHelper;
                        break;
                    case MultiSource2MatHelperClassName.Image2MatHelper:
                        _source2MatHelper = gameObject.AddComponent(typeof(Image2MatHelper)) as ISource2MatHelper;
                        break;
                    case MultiSource2MatHelperClassName.CustomSource2MatHelper:
                        _source2MatHelper = _customClassComponent as ISource2MatHelper;
                        break;
                }

                _currentSource2MatHelperClassName = _requestedSource2MatHelperClassName;
            }

            if (_source2MatHelper == null)
            {
                Debug.LogError("MultiSource2MatHelper:: " + "requestedCustomClass == null, <ISource2MatHelper> component must be set.");
                return;
            }
            else
            {
                _source2MatHelper.outputColorFormat = _outputColorFormat;
                _source2MatHelper.timeoutFrameCount = _timeoutFrameCount;
                _source2MatHelper.onInitialized = _onInitialized;
                _source2MatHelper.onDisposed = _onDisposed;
                _source2MatHelper.onErrorOccurred = _onErrorOccurred;

                if (_requestedSource2MatHelperClassName != MultiSource2MatHelperClassName.CustomSource2MatHelper)
                {
                    switch (_source2MatHelper)
                    {
                        case ICameraSource2MatHelper helper:
                            helper.requestedDeviceName = _requestedDeviceName;
                            helper.requestedWidth = _requestedWidth;
                            helper.requestedHeight = _requestedHeight;
                            helper.requestedIsFrontFacing = _requestedIsFrontFacing;
                            helper.requestedFPS = _requestedFPS;
                            break;
                        case IVideoSource2MatHelper helper:
                            helper.requestedVideoFilePath = _requestedVideoFilePath;
                            helper.loop = _loop;
                            break;
                        case IImageSource2MatHelper helper:
                            helper.requestedImageFilePath = _requestedImageFilePath;
                            helper.repeat = _repeat;
                            break;
                    }
                }

                _source2MatHelper.Initialize(autoPlay);
            }
        }

        /// <summary>
        /// Indicate whether this instance has been initialized.
        /// </summary>
        /// <returns><c>true</c>, if this instance has been initialized, <c>false</c> otherwise.</returns>
        public virtual bool IsInitialized()
        {
            return _source2MatHelper != null ? _source2MatHelper.IsInitialized() : false;
        }

        /// <summary>
        /// Start the source device.
        /// </summary>
        public virtual void Play()
        {
            if (IsInitialized())
                _source2MatHelper.Play();
        }

        /// <summary>
        /// Pause the source device.
        /// </summary>
        public virtual void Pause()
        {
            if (IsInitialized())
                _source2MatHelper.Pause();
        }

        /// <summary>
        /// Stop the source device.
        /// </summary>
        public virtual void Stop()
        {
            if (IsInitialized())
                _source2MatHelper.Stop();
        }

        /// <summary>
        /// Indicate whether the source device is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the source device is playing, <c>false</c> otherwise.</returns>
        public virtual bool IsPlaying()
        {
            return IsInitialized() ? _source2MatHelper.IsPlaying() : false;
        }

        /// <summary>
        /// Indicate whether the device is paused.
        /// </summary>
        /// <returns><c>true</c>, if the device is paused, <c>false</c> otherwise.</returns>
        public virtual bool IsPaused()
        {
            return IsInitialized() ? _source2MatHelper.IsPaused() : false;
        }

        /// <summary>
        /// Return the source device name.
        /// </summary>
        /// <returns>The source device name.</returns>
        public virtual string GetDeviceName()
        {
            return IsInitialized() ? _source2MatHelper.GetDeviceName() : "";
        }

        /// <summary>
        /// Return the source width.
        /// </summary>
        /// <returns>The source width.</returns>
        public virtual int GetWidth()
        {
            return IsInitialized() ? _source2MatHelper.GetWidth() : -1;
        }

        /// <summary>
        /// Return the source height.
        /// </summary>
        /// <returns>The source height.</returns>
        public virtual int GetHeight()
        {
            return IsInitialized() ? _source2MatHelper.GetHeight() : -1;
        }

        /// <summary>
        /// Return the source base color format.
        /// </summary>
        /// <returns>The source base color format.</returns>
        public virtual Source2MatHelperColorFormat GetBaseColorFormat()
        {
            return _source2MatHelper != null ? _source2MatHelper.GetBaseColorFormat() : Source2MatHelperColorFormat.RGBA;
        }

        /// <summary>
        /// Indicate whether the source buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the source buffer has been updated <c>false</c> otherwise.</returns>
        public virtual bool DidUpdateThisFrame()
        {
            return IsInitialized() ? _source2MatHelper.DidUpdateThisFrame() : false;
        }

        /// <summary>
        /// Get the mat of the current frame.
        /// The Mat object's type is 'CV_8UC4' or 'CV_8UC3' or 'CV_8UC1' (ColorFormat is determined by the outputColorFormat setting).
        /// Please do not dispose of the returned mat as it will be reused.
        /// </summary>
        /// <returns>The mat of the current frame.</returns>
        public virtual Mat GetMat()
        {
            return IsInitialized() ? _source2MatHelper.GetMat() : null;
        }

        public virtual void Dispose()
        {
            if (IsInitialized())
                _source2MatHelper.Dispose();
        }
    }
}