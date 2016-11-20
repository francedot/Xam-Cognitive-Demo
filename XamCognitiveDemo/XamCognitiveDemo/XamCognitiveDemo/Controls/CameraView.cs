using Xamarin.Forms;
using XamCognitiveDemo.Models;

namespace XamCognitiveDemo.Controls
{
    public class CameraView : ContentView
    {
        public static readonly BindableProperty VideoFrameProperty =
            BindableProperty.Create(nameof(VideoFrame), typeof(VideoFrame), typeof(CameraView),
                default(VideoFrame), BindingMode.TwoWay);


        public VideoFrame VideoFrame
        {
            get { return (VideoFrame)GetValue(VideoFrameProperty); }
            set { SetValue(VideoFrameProperty, value); }
        }

        public static readonly BindableProperty FrameRateProperty =
            BindableProperty.Create(nameof(VideoFrame), typeof(double), typeof(CameraView),
                default(double), BindingMode.TwoWay, null, FrameRateChanged);


        public double FrameRate
        {
            get { return (double)GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
        }

        private static void FrameRateChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
        }
    }
}