using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XamCognitiveDemo.Controls;
using XamCognitiveDemo.Droid.Controls;
using XamCognitiveDemo.Droid.CustomRenderers;
using XamCognitiveDemo.Events;
using XamCognitiveDemo.Models;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewRenderer))]
namespace XamCognitiveDemo.Droid.CustomRenderers
{
    public class CameraViewRenderer : Xamarin.Forms.Platform.Android.AppCompat.ViewRenderer<CameraView, NativeCameraView>
    {
        private CameraView _cameraView;
        private VideoFrame _latestVideoFrame;

        protected override void OnElementChanged(ElementChangedEventArgs<CameraView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            _cameraView = e.NewElement;

            var nativeCameraView = new NativeCameraView(Forms.Context);
            nativeCameraView.NewFrameCaptured += NativeCameraViewOnNewFrameCaptured;

            nativeCameraView.SetupUserInterface((int) Element.HeightRequest, (int) Element.WidthRequest);
            nativeCameraView.SetupEventHandlers();

            SetNativeControl(nativeCameraView);
        }

        private void NativeCameraViewOnNewFrameCaptured(object sender, NewFrameEventArgs e)
        {
            _latestVideoFrame = _cameraView.VideoFrame = e.Frame;
        }

        protected override NativeCameraView CreateNativeControl()
        {
            return new NativeCameraView(Forms.Context);
        }
    }
}