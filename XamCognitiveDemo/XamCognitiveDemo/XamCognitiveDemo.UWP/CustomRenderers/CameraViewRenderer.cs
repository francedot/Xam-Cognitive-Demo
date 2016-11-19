using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Xamarin.Forms.Platform.UWP;
using XamCognitiveDemo.Controls;
using XamCognitiveDemo.UWP.Controls;
using XamCognitiveDemo.UWP.CustomRenderers;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewRenderer))]
namespace XamCognitiveDemo.UWP.CustomRenderers
{
    public class CameraViewRenderer : ViewRenderer<CameraView, NativeCameraView>
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
        private CaptureElement _captureElement;
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _externalCamera;
        private bool _mirroringPreview;

        private Page _page;
        private AppBarButton _takePhotoButton;
        private Application _app;

        protected override async void OnElementChanged(ElementChangedEventArgs<CameraView> e)
        {
            base.OnElementChanged(e);
            if (e.OldElement != null || Element == null)
            {
                return;
            }

            var nativeCameraView = new NativeCameraView();
            await nativeCameraView.InitializeCameraAsync();

            SetNativeControl(nativeCameraView);
        }

        //protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.Page> e)
        //{
        //    base.OnElementChanged(e);

        //    if (e.OldElement != null || Element == null)
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        _app = Application.Current;
        //        _app.Suspending += OnAppSuspending;
        //        _app.Resuming += OnAppResuming;

        //        SetupUserInterface();
        //        SetupCamera();

        //        this.Children.Add(_page);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(@"      ERROR: ", ex.Message);
        //    }
        //}

        //protected override Size ArrangeOverride(Size finalSize)
        //{
        //    _page.Arrange(new Windows.Foundation.Rect(0, 0, finalSize.Width, finalSize.Height));
        //    return finalSize;
        //}

        private void SetupUserInterface()
        {
            _takePhotoButton = new AppBarButton
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Icon = new SymbolIcon(Symbol.Camera)
            };

            var commandBar = new CommandBar();
            commandBar.PrimaryCommands.Add(_takePhotoButton);

            _captureElement = new CaptureElement();
            _captureElement.Stretch = Stretch.UniformToFill;

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(_captureElement);

            _page = new Page();
            _page.BottomAppBar = commandBar;
            _page.Content = stackPanel;
            _page.Unloaded += OnPageUnloaded;
        }

        private async void SetupCamera()
        {
            await SetupUIAsync();
            await InitializeCameraAsync();
        }

        #region Event Handlers

        private async void OnSystemMediaControlsPropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Only handle event if the page is being displayed
                if (args.Property == SystemMediaTransportControlsProperty.SoundLevel && _page.Frame.CurrentSourcePageType == typeof(MainPage))
                {
                    // Check if the app is being muted. If so, it's being minimized
                    // Otherwise if it is not initialized, it's being brought into focus
                    if (sender.SoundLevel == SoundLevel.Muted)
                    {
                        await CleanupCameraAsync();
                    }
                    else if (!_isInitialized)
                    {
                        await InitializeCameraAsync();
                    }
                }
            });
        }

        private void OnOrientationSensorOrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            // Only update orientatino if the device is not parallel to the ground
            if (args.Orientation != SimpleOrientation.Faceup && args.Orientation != SimpleOrientation.Facedown)
            {
                _deviceOrientation = args.Orientation;
            }
        }

        private async void OnDisplayInformationOrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private async void OnTakePhotoButtonClicked(object sender, RoutedEventArgs e)
        {
            await TakePhotoAsync();
        }

        private async void OnHardwareCameraButtonPressed(object sender, CameraEventArgs e)
        {
            await TakePhotoAsync();
        }

        #endregion

        #region Media Capture

        private async Task InitializeCameraAsync()
        {
            if (_mediaCapture == null)
            {
                var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                var cameraDevice = devices.FirstOrDefault(c => c.EnclosureLocation != null && c.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);
                // Get any camera if there isn't one on the back panel
                cameraDevice = cameraDevice ?? devices.FirstOrDefault();

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
            _captureElement.Source = _mediaCapture;
            _captureElement.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Start preview
            await _mediaCapture.StartPreviewAsync();
            _isPreviewing = true;

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private async Task StopPreviewAsync()
        {
            _isPreviewing = false;
            await _mediaCapture.StopPreviewAsync();

            // Use dispatcher because sometimes this method is called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // UI cleanup
                _captureElement.Source = null;

                // Allow device screen to sleep now preview is stopped
                _displayRequest.RequestRelease();
            });
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

        private async Task TakePhotoAsync()
        {
            var stream = new InMemoryRandomAccessStream();
            await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

            try
            {
                var file = await _captureFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);
                var orientation = ConvertOrientationToPhotoOrientation(GetCameraOrientation());
                await ReencodeAndSavePhotoAsync(stream, file, orientation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when taking photo: " + ex.ToString());
            }
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

        #endregion

        #region Helpers

        private async Task SetupUIAsync()
        {
            // Lock page to landscape to prevent the capture element from rotating
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            // Hide status bar
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync();
            }

            _displayOrientation = _displayInformation.CurrentOrientation;
            if (_orientationSensor != null)
            {
                _deviceOrientation = _orientationSensor.GetCurrentOrientation();
            }

            RegisterEventHandlers();

            var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            // Fallback to local app storage if no pictures library
            _captureFolder = picturesLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;
        }

        private async Task CleanupUIAsync()
        {
            UnregisterEventHandlers();

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ShowAsync();
            }

            // Revert orientation preferences
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
        }

        private void RegisterEventHandlers()
        {
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                HardwareButtons.CameraPressed += OnHardwareCameraButtonPressed;
            }

            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged += OnOrientationSensorOrientationChanged;
            }

            _displayInformation.OrientationChanged += OnDisplayInformationOrientationChanged;
            _systemMediaControls.PropertyChanged += OnSystemMediaControlsPropertyChanged;
            _takePhotoButton.Click += OnTakePhotoButtonClicked;
        }

        private void UnregisterEventHandlers()
        {
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                HardwareButtons.CameraPressed -= OnHardwareCameraButtonPressed;
            }

            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged -= OnOrientationSensorOrientationChanged;
            }

            _displayInformation.OrientationChanged -= OnDisplayInformationOrientationChanged;
            _systemMediaControls.PropertyChanged -= OnSystemMediaControlsPropertyChanged;
            _takePhotoButton.Click -= OnTakePhotoButtonClicked;
        }

        private static async Task ReencodeAndSavePhotoAsync(IRandomAccessStream stream, StorageFile file, PhotoOrientation orientation)
        {
            using (var inputStream = stream)
            {
                var decoder = await BitmapDecoder.CreateAsync(inputStream);

                using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);
                    var properties = new BitmapPropertySet
                    {
                        {
                            "System.Photo.Orientation", new BitmapTypedValue(orientation, Windows.Foundation.PropertyType.UInt16)
                        }
                    };

                    await encoder.BitmapProperties.SetPropertiesAsync(properties);
                    await encoder.FlushAsync();
                }
            }
        }

        #endregion

        #region Rotation

        private SimpleOrientation GetCameraOrientation()
        {
            if (_externalCamera)
            {
                // Cameras that aren't attached to the device do not rotate along with it
                return SimpleOrientation.NotRotated;
            }

            var result = _deviceOrientation;

            // On portrait-first devices, the camera sensor is mounted at a 90 degree offset to the native orientation
            if (_displayInformation.NativeOrientation == DisplayOrientations.Portrait)
            {
                switch (result)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        result = SimpleOrientation.NotRotated;
                        break;
                    case SimpleOrientation.Rotated180DegreesCounterclockwise:
                        result = SimpleOrientation.Rotated90DegreesCounterclockwise;
                        break;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        result = SimpleOrientation.Rotated180DegreesCounterclockwise;
                        break;
                    case SimpleOrientation.NotRotated:
                        result = SimpleOrientation.Rotated270DegreesCounterclockwise;
                        break;
                }
            }

            // If the preview is mirrored for a front-facing camera, invert the rotation
            if (_mirroringPreview)
            {
                // Rotating 0 and 180 ddegrees is the same clockwise and anti-clockwise
                switch (result)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        return SimpleOrientation.Rotated270DegreesCounterclockwise;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        return SimpleOrientation.Rotated90DegreesCounterclockwise;
                }
            }

            return result;
        }

        private static int ConvertDeviceOrientationToDegrees(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return 90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return 180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return 270;
                case SimpleOrientation.NotRotated:
                default:
                    return 0;
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

        private static PhotoOrientation ConvertOrientationToPhotoOrientation(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return PhotoOrientation.Rotate90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return PhotoOrientation.Rotate180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return PhotoOrientation.Rotate270;
                case SimpleOrientation.NotRotated:
                default:
                    return PhotoOrientation.Normal;
            }
        }

        #endregion

        #region Lifecycle

        private async void OnAppSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await CleanupCameraAsync();
            await CleanupUIAsync();
            deferral.Complete();
        }

        private async void OnAppResuming(object sender, object o)
        {
            await SetupUIAsync();
            await InitializeCameraAsync();
        }

        private async void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            await CleanupCameraAsync();
            await CleanupUIAsync();
        }

        #endregion
    }
}
