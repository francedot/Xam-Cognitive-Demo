using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;
using XamCognitiveDemo.Controls;
using XamCognitiveDemo.Events;
using XamCognitiveDemo.Models;
using XamCognitiveDemo.UWP.Controls;
using XamCognitiveDemo.UWP.CustomRenderers;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewRenderer))]
namespace XamCognitiveDemo.UWP.CustomRenderers
{
    public class CameraViewRenderer : ViewRenderer<CameraView, NativeCameraView>
    {
        private CameraView _cameraView;
        private VideoFrame _latestVideoFrame;

        protected override async void OnElementChanged(ElementChangedEventArgs<CameraView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            _cameraView = e.NewElement;

            var nativeCameraView = new NativeCameraView();
            nativeCameraView.NewFrameCaptured += NativeCameraViewOnNewFrameCaptured;
            await nativeCameraView.InitializeCameraAsync();

            SetNativeControl(nativeCameraView);
        }

        private void NativeCameraViewOnNewFrameCaptured(object sender, NewFrameEventArgs e)
        {
            //_latestVideoFrame?.ImageStream?.Dispose();

            _latestVideoFrame = _cameraView.VideoFrame = e.Frame;
        }
    }
}
