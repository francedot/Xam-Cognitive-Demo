using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.ProjectOxford.Emotion.Contract;
using Xamarin.Forms;
using XamCognitiveDemo.Events;
using XamCognitiveDemo.Models;
using XamCognitiveDemo.Services;

namespace XamCognitiveDemo.ViewModels
{
    public class CameraPageViewModel : ViewModelBase
    {
        private readonly RtVideoAnalyzer _videoAnalyzer;
        private VideoFrame _videoFrame;
        private Scores _scores;

        public CameraPageViewModel()
        {
            _videoAnalyzer = new RtVideoAnalyzer
            {
                ProducerDelegate = () => VideoFrame
            };
            _videoAnalyzer.NewResultAvailable += OnUpdateAnalysisResult;

            _videoAnalyzer.StartProcessingCamera();
        }

        public VideoFrame VideoFrame
        {
            get { return _videoFrame; }
            set { Set(ref _videoFrame, value); }
        }

        public Scores Scores
        {
            get { return _scores; }
            set { Set(ref _scores, value); }
        }

        private void OnUpdateAnalysisResult(object sender, NewResultEventArgs newResultEventArgs)
        {
            var result = newResultEventArgs.AnalysisResult?.Emotions?.FirstOrDefault();
            if (result == null)
            {
                return;
            }
            Scores = result.Scores;
        }
    }
}
