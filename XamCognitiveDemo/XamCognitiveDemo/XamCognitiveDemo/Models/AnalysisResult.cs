using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;

namespace XamCognitiveDemo.Models
{
    public class AnalysisResult
    {
        public Emotion Emotion { get; set; }
        public Face Face { get; set; }
    }
}