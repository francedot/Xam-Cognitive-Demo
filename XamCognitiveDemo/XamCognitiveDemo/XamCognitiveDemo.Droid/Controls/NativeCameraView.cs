#pragma warning disable 618
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
using Microsoft.ProjectOxford.Vision.Contract;
using XamCognitiveDemo.Events;
using Camera = Android.Hardware.Camera;

namespace XamCognitiveDemo.Droid.Controls
{
    public class NativeCameraView : FrameLayout, TextureView.ISurfaceTextureListener
    {
        private Camera _camera;
        private View _view;
        private Activity _activity;
        private CameraFacing _cameraType;
        private TextureView _textureView;
        private SurfaceTexture _surfaceTexture;
        private Timer _timer;

        public event EventHandler<NewFrameEventArgs> NewFrameCaptured;

        #region Constructors Implementation

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

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            _camera = Camera.Open((int)_cameraType);
            _textureView.LayoutParameters = new LayoutParams(width, height);
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

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            _surfaceTexture = surface;
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

            _timer = new Timer(async state =>
            {
                var image = _textureView.Bitmap;
                var imageStream = new MemoryStream();
                await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 50, imageStream);
                image.Recycle();

                NewFrameCaptured?.Invoke(this, new NewFrameEventArgs(new Models.VideoFrame
                {
                    ImageBytes = imageStream.ToArray(),
                    Timestamp = DateTime.Now,
                    PixelDimension = new Tuple<int, int>(image.Width, image.Height)
                }));

                imageStream.Dispose();

            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(0.5));
        }
    }
}