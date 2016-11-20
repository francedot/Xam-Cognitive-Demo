using System;
using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Face.Contract;
using XamCognitiveDemo.Models;

namespace XamCognitiveDemo.Events
{
    public class FaceRectangleEventArgs : EventArgs
    {
        public FaceRectangleEventArgs(Rectangle faceRectangle)
        {
            FaceRectangle = faceRectangle;
        }
        public Rectangle FaceRectangle { get; }
    }
}