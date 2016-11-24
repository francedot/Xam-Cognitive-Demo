using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
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
        private Face _face;

        public event EventHandler<NewResultEventArgs> NewResultProvided;

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

        public Face Face
        {
            get { return _face; }
            set { Set(ref _face, value); }
        }

        private void OnUpdateAnalysisResult(object sender, NewResultEventArgs newResultEventArgs)
        {
            var emotions = newResultEventArgs.AnalysisResult?.Emotion;
            if (emotions != null)
            {
                Scores = emotions.Scores;
            }
            var face = newResultEventArgs.AnalysisResult?.Face;
            if (face != null)
            {
                Face = face;
                NewResultProvided?.Invoke(this, new NewResultEventArgs()
                {
                    AnalysisResult = new AnalysisResult()
                    {
                        Face = face
                    }
                });
            }
        }
    }
}
