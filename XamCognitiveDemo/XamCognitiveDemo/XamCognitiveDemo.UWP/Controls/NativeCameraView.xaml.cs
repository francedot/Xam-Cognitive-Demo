using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace XamCognitiveDemo.UWP.Controls
{
    public sealed partial class NativeCameraView : UserControl
    {

        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;

        // Rotation metadata to apply to preview stream (https://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx)
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1"); // (MF_MT_VIDEO_ROTATION)

        private StorageFolder _captureFolder = null;

        private readonly SystemMediaTransportControls _systemMediaControls = SystemMediaTransportControls.GetForCurrentView();

        private MediaCapture _mediaCapture;
        //private CaptureElement _captureElement;
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _externalCamera;
        private bool _mirroringPreview;

        private AppBarButton _takePhotoButton;
        private Application _app;
        private DeviceInformation _backCamera;
        private DeviceInformation _frontCamera;
        private bool _isBackCamera = false;

        public NativeCameraView()
        {
            this.InitializeComponent();
        }

        public async Task InitializeCameraAsync()
        {
            if (_mediaCapture == null)
            {
                var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                _frontCamera = devices.FirstOrDefault(f => f.Name.Contains("Front"));
                _backCamera = devices.FirstOrDefault(b => b.Name.Contains("Back"));
                //var cameraDevice = devices.FirstOrDefault(c => c.EnclosureLocation != null && c.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);
                //// Get any camera if there isn't one on the back panel
                //cameraDevice = cameraDevice ?? devices.FirstOrDefault();

                var cameraDevice = _isBackCamera ? _backCamera : _frontCamera;

                if (cameraDevice == null)
                {
                    Debug.WriteLine("No camera found");
                    return;
                }

                _mediaCapture = new MediaCapture();

                try
                {
                    await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = cameraDevice.Id,
                        AudioDeviceId = string.Empty,
                        StreamingCaptureMode = StreamingCaptureMode.Video,
                        PhotoCaptureSource = PhotoCaptureSource.Photo
                    });
                    _isInitialized = true;
                }

                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("Camera access denied");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception initializing MediaCapture - {0}: {1}", cameraDevice.Id, ex.ToString());
                }

                if (_isInitialized)
                {
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        _externalCamera = true;
                    }
                    else
                    {
                        // Camera is on device
                        _externalCamera = false;

                        // Mirror preview if camera is on front panel
                        _mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }
                    await StartPreviewAsync();
                }
            }
        }

        private async Task StartPreviewAsync()
        {
            // Prevent the device from sleeping while the preview is running
            _displayRequest.RequestActive();

            // Setup preview source in UI and mirror if required
            VideoCapture.Source = _mediaCapture;
            VideoCapture.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Start preview
            await _mediaCapture.StartPreviewAsync();
            _isPreviewing = true;

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private async Task SetPreviewRotationAsync()
        {
            // Only update the orientation if the camera is mounted on the device
            if (_externalCamera)
            {
                return;
            }

            // Derive the preview rotation
            int rotation = ConvertDisplayOrientationToDegrees(_displayOrientation);

            // Invert if mirroring
            if (_mirroringPreview)
            {
                rotation = (360 - rotation) % 360;
            }

            // Add rotation metadata to preview stream
            var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotation);
            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        private async Task StopPreviewAsync()
        {
            _isPreviewing = false;
            await _mediaCapture.StopPreviewAsync();

            // Use dispatcher because sometimes this method is called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // UI cleanup
                VideoCapture.Source = null;

                // Allow device screen to sleep now preview is stopped
                _displayRequest.RequestRelease();
            });
        }

        private async Task CleanupCameraAsync()
        {
            if (_isInitialized)
            {
                if (_isPreviewing)
                {
                    await StopPreviewAsync();
                }
                _isInitialized = false;
            }
            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        private static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        private async void OnSwitchCamera(object sender, TappedRoutedEventArgs e)
        {
            await CleanupCameraAsync();
            _isBackCamera = !_isBackCamera;
            await InitializeCameraAsync();
        }
    }
}
