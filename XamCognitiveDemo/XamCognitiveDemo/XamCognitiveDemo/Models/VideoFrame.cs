using System;
using System.IO;

namespace XamCognitiveDemo.Models
{
    public class VideoFrame
    {
        public DateTime Timestamp { get; set; }
        public Stream ImageStream { get; set; }
    }
}