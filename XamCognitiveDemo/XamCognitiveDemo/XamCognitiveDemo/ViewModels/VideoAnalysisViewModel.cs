using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using XamCognitiveDemo.Events;
using XamCognitiveDemo.Models;
using XamCognitiveDemo.Services;

namespace XamCognitiveDemo.ViewModels
{
    public class VideoAnalysisViewModel : ViewModelBase
    {
        private readonly RtVideoAnalyzer _videoAnalyzer;
        private VideoFrame _videoFrame;

        public VideoAnalysisViewModel()
        {
            _videoAnalyzer = new RtVideoAnalyzer
            {
                ProducerDelegate = () =>
                {
                    return VideoFrame;
                }
            };
            _videoAnalyzer.NewResultAvailable += OnUpdateAnalysisResult;

            _videoAnalyzer.StartProcessingCamera();
        }

        public VideoFrame VideoFrame
        {
            get { return _videoFrame; }
            set
            {
                if (_videoAnalyzer.IsRunning)
                {
                    return;
                }
                Set(ref _videoFrame, value);
            }
        }

        private void OnUpdateAnalysisResult(object sender, NewResultEventArgs newResultEventArgs)
        {
            var result = newResultEventArgs.AnalysisResult;
        }

        public override void OnNavigatingTo(object parameter = null)
        {
        }
    }
}
