using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;

namespace XamCognitiveDemo.Models
{
    public class AnalysisResult
    {
        public Face[] Faces { get; set; } = null;
        public Scores[] EmotionScores { get; set; }
        public Emotion[] Emotions { get; set; }
        public string[] CelebrityNames { get; set; } = null;
        //public Microsoft.ProjectOxford.Vision.Contract.Tag[] Tags { get; set; } = null;
    }
}