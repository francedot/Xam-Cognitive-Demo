using System;
using System.IO;
using Android.App;
using Android.Graphics;
using Android.Hardware;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XamCognitiveDemo.Droid.CustomRenderers;
using XamCognitiveDemo.Views;
#pragma warning disable 618

[assembly: ExportRenderer(typeof(CameraPage), typeof(CameraPageRenderer))]
namespace XamCognitiveDemo.Droid.CustomRenderers
{
    public class CameraPageRenderer : PageRenderer, TextureView.ISurfaceTextureListener
    {
        Android.Hardware.Camera _camera;
        Android.Widget.Button _takePhotoButton;
        Android.Widget.Button _toggleFlashButton;
        Android.Widget.Button _switchCameraButton;
        Android.Views.View _view;

        Activity _activity;
        CameraFacing _cameraType;
        TextureView _textureView;
        SurfaceTexture _surfaceTexture;

        bool _flashOn;
        byte[] _imageBytes;

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            try
            {
                SetupUserInterface();
                SetupEventHandlers();
                AddView(_view);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"ERROR: ", ex.Message);
            }
        }

        void SetupUserInterface()
        {
            _activity = this.Context as Activity;
            _view = _activity.LayoutInflater.Inflate(Resource.Layout.CameraLayout, this, false);
            _cameraType = CameraFacing.Back;

            _textureView = _view.FindViewById<TextureView>(Resource.Id.textureView);
            _textureView.SurfaceTextureListener = this;
        }

        void SetupEventHandlers()
        {
            _takePhotoButton = _view.FindViewById<global::Android.Widget.Button>(Resource.Id.takePhotoButton);
            _takePhotoButton.Click += TakePhotoButtonTapped;

            _switchCameraButton = _view.FindViewById<global::Android.Widget.Button>(Resource.Id.switchCameraButton);
            _switchCameraButton.Click += SwitchCameraButtonTapped;

            _toggleFlashButton = _view.FindViewById<global::Android.Widget.Button>(Resource.Id.toggleFlashButton);
            _toggleFlashButton.Click += ToggleFlashButtonTapped;
        }

        //protected override void OnLayout(bool changed, int l, int t, int r, int b)
        //{
        //    base.OnLayout(changed, l, t, r, b);

        //    var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
        //    var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

        //    _view.Measure(msw, msh);
        //    _view.Layout(0, 0, r - l, b - t);
        //}

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {

        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            _camera = global::Android.Hardware.Camera.Open((int)_cameraType);
            _textureView.LayoutParameters = new FrameLayout.LayoutParams(width, height);
            _surfaceTexture = surface;

            _camera.SetPreviewTexture(surface);
            PrepareAndStartCamera();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            _camera.StopPreview();
            _camera.Release();
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            PrepareAndStartCamera();
        }

        void PrepareAndStartCamera()
        {
            _camera.StopPreview();

            var display = _activity.WindowManager.DefaultDisplay;
            if (display.Rotation == SurfaceOrientation.Rotation0)
            {
                _camera.SetDisplayOrientation(90);
            }

            if (display.Rotation == SurfaceOrientation.Rotation270)
            {
                _camera.SetDisplayOrientation(180);
            }

            _camera.StartPreview();
        }

        void ToggleFlashButtonTapped(object sender, EventArgs e)
        {
            _flashOn = !_flashOn;
            if (_flashOn)
            {
                if (_cameraType == CameraFacing.Back)
                {
                    _toggleFlashButton.SetBackgroundResource(Resource.Drawable.FlashButton);
                    _cameraType = CameraFacing.Back;

                    _camera.StopPreview();
                    _camera.Release();
                    _camera = Android.Hardware.Camera.Open((int)_cameraType);
                    var parameters = _camera.GetParameters();
                    parameters.FlashMode = global::Android.Hardware.Camera.Parameters.FlashModeTorch;
                    _camera.SetParameters(parameters);
                    _camera.SetPreviewTexture(_surfaceTexture);
                    PrepareAndStartCamera();
                }
            }
            else
            {
                _toggleFlashButton.SetBackgroundResource(Resource.Drawable.NoFlashButton);
                _camera.StopPreview();
                _camera.Release();

                _camera = global::Android.Hardware.Camera.Open((int)_cameraType);
                var parameters = _camera.GetParameters();
                parameters.FlashMode = global::Android.Hardware.Camera.Parameters.FlashModeOff;
                _camera.SetParameters(parameters);
                _camera.SetPreviewTexture(_surfaceTexture);
                PrepareAndStartCamera();
            }
        }

        void SwitchCameraButtonTapped(object sender, EventArgs e)
        {
            if (_cameraType == CameraFacing.Front)
            {
                _cameraType = CameraFacing.Back;

                _camera.StopPreview();
                _camera.Release();
                _camera = global::Android.Hardware.Camera.Open((int)_cameraType);
                _camera.SetPreviewTexture(_surfaceTexture);
                PrepareAndStartCamera();
            }
            else
            {
                _cameraType = CameraFacing.Front;

                _camera.StopPreview();
                _camera.Release();
                _camera = global::Android.Hardware.Camera.Open((int)_cameraType);
                _camera.SetPreviewTexture(_surfaceTexture);
                PrepareAndStartCamera();
            }
        }

        async void TakePhotoButtonTapped(object sender, EventArgs e)
        {
            _camera.StopPreview();

            var image = _textureView.Bitmap;

            try
            {
                var absolutePath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;
                var folderPath = absolutePath + "/Camera";
                var filePath = System.IO.Path.Combine(folderPath, string.Format("photo_{0}.jpg", Guid.NewGuid()));

                var fileStream = new FileStream(filePath, FileMode.Create);
                await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 50, fileStream);
                fileStream.Close();
                image.Recycle();

                var intent = new Android.Content.Intent(Android.Content.Intent.ActionMediaScannerScanFile);
                var file = new Java.IO.File(filePath);
                var uri = Android.Net.Uri.FromFile(file);
                intent.SetData(uri);
                Forms.Context.SendBroadcast(intent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"", ex.Message);
            }

            _camera.StartPreview();
        }
    }
}

