using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Xamarin.Forms;
using XamCognitiveDemo.Events;
using XamCognitiveDemo.Models;

namespace XamCognitiveDemo.UWP.Controls
{
    public sealed partial class NativeCameraView : UserControl
    {

        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;

        // Rotation metadata to apply to preview stream (https://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx)
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1"); // (MF_MT_VIDEO_ROTATION)

        public event EventHandler<NewFrameEventArgs> NewFrameCaptured;

        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _externalCamera;
        private bool _mirroringPreview;

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
                var audioDevice = (await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture)).FirstOrDefault();
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
                        AudioDeviceId = audioDevice.Id,
                        StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo,
                        PhotoCaptureSource = PhotoCaptureSource.VideoPreview,
                    });

                    //_mediaCapture.AudioDeviceController.VolumePercent = 0;
                    _mediaCapture.AudioDeviceController.Muted = true;

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

            this.VideoProperties = this._mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;


            // Start preview
            await _mediaCapture.StartPreviewAsync();
            _isPreviewing = true;

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }

            

            var t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(1);
            t.Tick += (sender, o) =>
            {
                if (_mediaCapture != null)
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        const BitmapPixelFormat inputPixelFormat = BitmapPixelFormat.Bgra8;
                        using (
                            var previewFrame = new Windows.Media.VideoFrame(inputPixelFormat, (int)this.VideoProperties.Width,
                                (int)this.VideoProperties.Height))
                        {
                            await _mediaCapture.GetPreviewFrameAsync(previewFrame);
                            var imageBytes = await GetPixelBytesFromSoftwareBitmapAsync(previewFrame.SoftwareBitmap);
                            var imageStream = new MemoryStream(imageBytes);
                            NewFrameCaptured?.Invoke(this, new NewFrameEventArgs(new Models.VideoFrame()
                            {
                                ImageStream = imageStream,
                                Timestamp = DateTime.Now
                            }));

                            //await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), imageStream);
                            //var f = new Models.VideoFrame()
                            //{
                            //    Timestamp = DateTime.Now
                            //};
                            //f.ImageStream = imageStream.AsStreamForRead();
                            //NewFrameCaptured?.Invoke(this, new NewFrameEventArgs(f));

                            //var imageStream = new InMemoryRandomAccessStream();
                            //await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), imageStream);
                            //var f = new Models.VideoFrame()
                            //{
                            //    Timestamp = DateTime.Now
                            //};
                            //f.ImageStream = imageStream.AsStreamForRead();
                            //NewFrameCaptured?.Invoke(this, new NewFrameEventArgs(f));
                        }
                    });
                }
            };
            t.Start();
        }

        public VideoEncodingProperties VideoProperties { get; set; }

        private static async Task<byte[]> GetPixelBytesFromSoftwareBitmapAsync(SoftwareBitmap softwareBitmap)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
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
