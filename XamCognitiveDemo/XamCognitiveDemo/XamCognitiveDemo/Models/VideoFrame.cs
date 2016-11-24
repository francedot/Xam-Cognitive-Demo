using System;
using System.IO;

namespace XamCognitiveDemo.Models
{
    public class VideoFrame
    {
        public DateTime Timestamp { get; set; }
        public byte[] ImageBytes { get; set; }
        public Tuple<int, int> PixelDimension { get; set; }
    }
}