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

    }
}