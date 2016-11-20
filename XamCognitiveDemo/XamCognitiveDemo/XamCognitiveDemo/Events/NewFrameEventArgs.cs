using System;
using XamCognitiveDemo.Models;

namespace XamCognitiveDemo.Events
{
    public class NewFrameEventArgs : EventArgs
    {
        public NewFrameEventArgs(VideoFrame frame)
        {
            Frame = frame;
        }
        public VideoFrame Frame { get; }
    }
}