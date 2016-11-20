using System;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using XamCognitiveDemo.Events;
using View = Android.Views.View;

#pragma warning disable 618

namespace XamCognitiveDemo.Droid.Controls
{
    public class NativeCameraView : FrameLayout, TextureView.ISurfaceTextureListener
    {
        Android.Hardware.Camera _camera;
        Android.Widget.Button _takePhotoButton;
        Android.Widget.Button _toggleFlashButton;
        Android.Widget.Button _switchCameraButton;
        Android.Views.View _view;

        public event EventHandler<NewFrameEventArgs> NewFrameCaptured;


        Activity _activity;
        CameraFacing _cameraType;
        TextureView _textureView;
        SurfaceTexture _surfaceTexture;

        bool _flashOn;
        byte[] _imageBytes;


        #region Constructrors Implementation

        public NativeCameraView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public NativeCameraView(Context context) : base(context)
        {
        }

        public NativeCameraView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public NativeCameraView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public NativeCameraView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        #endregion

        public void SetupUserInterface(int height, int width)
        {
            _activity = this.Context as Activity;
            if ((_view = _activity?.LayoutInflater.Inflate(Resource.Layout.CameraLayout, this, false)) == null)
            {
                return;
            }
            var cameraView = _view.FindViewById<FrameLayout>(Resource.Id.cameraView);
            var parameters = cameraView.LayoutParameters;
            parameters.Height = height;
            parameters.Height = width;
            _cameraType = CameraFacing.Front;

           this.AddView(_view);

            _textureView = _view.FindViewById<TextureView>(Resource.Id.textureView);
            _textureView.SurfaceTextureListener = this;
        }

        public void SetupEventHandlers()
        {
            _takePhotoButton = _view.FindViewById<Android.Widget.Button>(Resource.Id.takePhotoButton);
            _takePhotoButton.Click += TakePhotoButtonTapped;

            _switchCameraButton = _view.FindViewById<Android.Widget.Button>(Resource.Id.switchCameraButton);
            _switchCameraButton.Click += SwitchCameraButtonTapped;

            _toggleFlashButton = _view.FindViewById<Android.Widget.Button>(Resource.Id.toggleFlashButton);
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
            _camera = Android.Hardware.Camera.Open((int)_cameraType);
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

        private void PrepareAndStartCamera()
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

            var timer = new Timer(async state =>
            {
                var image = _textureView.Bitmap;
                var imageStream = new MemoryStream();
                await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 50, imageStream);
                image.Recycle();

                NewFrameCaptured?.Invoke(this, new NewFrameEventArgs(new Models.VideoFrame()
                {
                    ImageBytes = imageStream.ToArray(),
                    Timestamp = DateTime.Now
                }));

            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(0.4));
        }

        private void ToggleFlashButtonTapped(object sender, EventArgs e)
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

        private void SwitchCameraButtonTapped(object sender, EventArgs e)
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

        private async void TakePhotoButtonTapped(object sender, EventArgs e)
        {
            _camera.StopPreview();

            var image = _textureView.Bitmap;

            try
            {
                var absolutePath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;
                var folderPath = absolutePath + "/Camera";
                var filePath = System.IO.Path.Combine(folderPath, $"photo_{Guid.NewGuid()}.jpg");

                var fileStream = new FileStream(filePath, FileMode.Create);
                await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 50, fileStream);
                fileStream.Close();
                image.Recycle();

                var intent = new Intent(Intent.ActionMediaScannerScanFile);
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