using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using XamCognitiveDemo.Events;

namespace XamCognitiveDemo.UWP.Controls
{
    public sealed partial class NativeCameraView : UserControl
    {
        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");
        private MediaCapture _mediaCapture;
        private DeviceInformation _frontCamera;
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _externalCamera;
        private bool _mirroringPreview;
        private DispatcherTimer _timer;

        public event EventHandler<NewFrameEventArgs> NewFrameCaptured;

        public NativeCameraView()
        {
            this.InitializeComponent();
        }
        public int CameraResolutionWidth { get; set; }

        public int CameraResolutionHeight { get; set; }

        public double CameraAspectRatio { get; set; }

        public VideoEncodingProperties VideoProperties { get; set; }

        public async Task InitializeCameraAsync()
        {
            if (_mediaCapture == null)
            {
                var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                var audioDevice = (await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture)).FirstOrDefault();
                _frontCamera = devices.FirstOrDefault(f => f.Name.Contains("Front"));

                _mediaCapture = new MediaCapture();

                try
                {
                    await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = _frontCamera.Id,
                        AudioDeviceId = audioDevice.Id,
                        StreamingCaptureMode = StreamingCaptureMode.Video,
                        PhotoCaptureSource = PhotoCaptureSource.Photo,
                    });

                    await SetVideoEncodingToHighestResolution(true);

                    _isInitialized = true;
                }

                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("Camera access denied");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception initializing MediaCapture - {0}: {1}", _frontCamera.Id, ex.ToString());
                }

                if (_isInitialized)
                {
                    if (_frontCamera.EnclosureLocation == null || _frontCamera.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        _externalCamera = true;
                    }
                    else
                    {
                        // Camera is on device
                        _externalCamera = false;

                        // Mirror preview if camera is on front panel
                        _mirroringPreview = (_frontCamera.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
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

            this.VideoProperties = this._mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

            // Start preview
            await _mediaCapture.StartPreviewAsync();
            _isPreviewing = true;

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            _timer.Tick += async (sender, o) =>
            {
                if (_mediaCapture == null)
                {
                    return;
                }

                const BitmapPixelFormat inputPixelFormat = BitmapPixelFormat.Bgra8;
                var previewFrame = new Windows.Media.VideoFrame(inputPixelFormat, CameraResolutionWidth, CameraResolutionHeight);
                await _mediaCapture.GetPreviewFrameAsync(previewFrame);
                var imageBytes = await GetPixelBytesFromSoftwareBitmapAsync(previewFrame.SoftwareBitmap);
                var image = await AsBitmapImageAsync(imageBytes);

                NewFrameCaptured?.Invoke(this, new NewFrameEventArgs(new Models.VideoFrame()
                {
                    ImageBytes = imageBytes,
                    Timestamp = DateTime.Now,
                    PixelDimension = new Tuple<int, int>(image.PixelWidth, image.PixelHeight)
                }));

                previewFrame.Dispose();
            };
            _timer.Start();
        }

        public static async Task<BitmapImage> AsBitmapImageAsync(byte[] byteArray)
        {
            if (byteArray != null)
            {
                using (var stream = new InMemoryRandomAccessStream())
                {
                    await stream.WriteAsync(byteArray.AsBuffer());
                    var image = new BitmapImage();
                    stream.Seek(0);
                    image.SetSource(stream);
                    return image;
                }
            }
            return null;
        }

        private async Task StopPreviewAsync()
        {
            _isPreviewing = false;
            await _mediaCapture.StopPreviewAsync();

            _timer.Stop();
            _timer = default(DispatcherTimer);

            // Use dispatcher because sometimes this method is called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // UI cleanup
                VideoCapture.Source = null;

                // Allow device screen to sleep now preview is stopped
                _displayRequest.RequestRelease();
            });
        }

        #region Helpers

        private async Task SetVideoEncodingToHighestResolution(bool isForRealTimeProcessing = false)
        {
            VideoEncodingProperties highestVideoEncodingSetting;
            if (isForRealTimeProcessing)
            {
                uint maxHeightForRealTime = 720;
                highestVideoEncodingSetting = this._mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Cast<VideoEncodingProperties>().Where(v => v.Height <= maxHeightForRealTime).OrderByDescending(v => v.Width * v.Height * (v.FrameRate.Numerator / v.FrameRate.Denominator)).First();
            }
            else
            {
                highestVideoEncodingSetting = this._mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Cast<VideoEncodingProperties>().OrderByDescending(v => v.Width * v.Height * (v.FrameRate.Numerator / v.FrameRate.Denominator)).First();
            }

            if (highestVideoEncodingSetting != null)
            {
                CameraAspectRatio = (double)highestVideoEncodingSetting.Width / (double)highestVideoEncodingSetting.Height;
                CameraResolutionHeight = (int)highestVideoEncodingSetting.Height;
                CameraResolutionWidth = (int)highestVideoEncodingSetting.Width;

                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, highestVideoEncodingSetting);
            }
        }

        private static async Task<byte[]> GetPixelBytesFromSoftwareBitmapAsync(SoftwareBitmap softwareBitmap)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise270Degrees;
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();

                // Read the pixel bytes from the memory stream
                using (var reader = new DataReader(stream.GetInputStreamAt(0)))
                {
                    var bytes = new byte[stream.Size];
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(bytes);
                    return bytes;
                }

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

        #endregion
    }
}
