using System;
using System.ComponentModel;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XamCognitiveDemo.Controls;
using XamCognitiveDemo.Droid.Controls;
using XamCognitiveDemo.Droid.CustomRenderers;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewRenderer))]
namespace XamCognitiveDemo.Droid.CustomRenderers
{
    public class CameraViewRenderer : Xamarin.Forms.Platform.Android.AppCompat.ViewRenderer<CameraView, NativeCameraView>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<CameraView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            var nativeCameraView = new NativeCameraView(Forms.Context);
            nativeCameraView.SetupUserInterface((int) Element.HeightRequest, (int) Element.WidthRequest);
            nativeCameraView.SetupEventHandlers();

            SetNativeControl(nativeCameraView);
        }
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            //switch (e.PropertyName)
            //{
            //    case "HeightRequest":
            //    {
            //        Control.LayoutParameters = new 
            //    }
            //}
        }

        protected override NativeCameraView CreateNativeControl()
        {

            return new NativeCameraView(Forms.Context);
        }
    }
}