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
        public Stream FrameStream { get; set; }

        public VideoAnalysisViewModel()
        {
            _videoAnalyzer = new RtVideoAnalyzer
            {
                ProducerDelegate = () => new VideoFrame
                {
                    ImageStream = FrameStream,
                    Timestamp = DateTime.Now
                },
            };
            _videoAnalyzer.NewResultAvailable += OnUpdateAnalysisResult;

            _videoAnalyzer.StartProcessingCamera();
        }

        private void OnUpdateAnalysisResult(object sender, NewResultEventArgs newResultEventArgs)
        {
        }

        public override void OnNavigatingTo(object parameter = null)
        {
        }
    }
}
