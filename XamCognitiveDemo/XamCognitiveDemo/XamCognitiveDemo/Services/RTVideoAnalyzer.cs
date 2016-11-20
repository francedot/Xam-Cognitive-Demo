using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Xamarin.Forms;
using XamCognitiveDemo.Events;
using XamCognitiveDemo.Models;

namespace XamCognitiveDemo.Services
{
    public class RtVideoAnalyzer
    {
        private readonly FaceServiceClient _faceClient;
        private readonly EmotionServiceClient _emotionClient;
        private bool _stopTimer = false;

        #region Events

        public event EventHandler ProcessingStarting;
        public event EventHandler ProcessingStarted;
        public event EventHandler ProcessingStopping;
        public event EventHandler ProcessingStopped;
        public event EventHandler<NewFrameEventArgs> NewFrameProvided;
        public event EventHandler<NewResultEventArgs> NewResultAvailable;

        #endregion Events

        #region Event Raisers

        /// <summary> Raises the processing starting event. </summary>
        protected void OnProcessingStarting()
        {
            ProcessingStarting?.Invoke(this, null);
        }

        /// <summary> Raises the processing started event. </summary>
        protected void OnProcessingStarted()
        {
            ProcessingStarted?.Invoke(this, null);
        }

        /// <summary> Raises the processing stopping event. </summary>
        protected void OnProcessingStopping()
        {
            ProcessingStopping?.Invoke(this, null);
        }

        /// <summary> Raises the processing stopped event. </summary>
        protected void OnProcessingStopped()
        {
            ProcessingStopped?.Invoke(this, null);
        }

        /// <summary> Raises the new frame provided event. </summary>
        /// <param name="frame"> The frame. </param>
        protected void OnNewFrameProvided(VideoFrame frame)
        {
            NewFrameProvided?.Invoke(this, new NewFrameEventArgs(frame));
        }

        /// <summary> Raises the new result event. </summary>
        /// <param name="args"> Event information to send to registered event handlers. </param>
        protected void OnNewResultAvailable(NewResultEventArgs args)
        {
            NewResultAvailable?.Invoke(this, args);
        }

        #endregion

        public RtVideoAnalyzer()
        {
            _faceClient = new FaceServiceClient("TODO");
            _emotionClient = new EmotionServiceClient("TODO");
        }

        public Func<VideoFrame> ProducerDelegate { get; set; }
        public AnalysisResult LastAnalysisResult { get; set; }
        public Func<Task> AnalysisFunction { get; set; }
        public TimeSpan AnalysisTimeout { get; set; } = TimeSpan.FromMilliseconds(5000);
        public double FrameRate { get; } = 0.5;

        public void StartProcessingCamera()
        {
            // Start a timer that runs after 1 minute.
            Device.StartTimer(TimeSpan.FromSeconds(1.0 / FrameRate), () =>
            {
                Task.Factory.StartNew(async () =>
                {
                    var videoFrame = ProducerDelegate();
                    // Do the actual request and wait for it to finish.
                    var emotions = await _emotionClient.RecognizeAsync(videoFrame.ImageStream);
                    var faces = await _faceClient.DetectAsync(videoFrame.ImageStream);
                    LastAnalysisResult = new AnalysisResult()
                    {
                        Faces = faces,
                        Emotions = emotions
                    };
                    // Switch back to the UI thread to update the UI
                    if (!_stopTimer)
                    {
                        Device.BeginInvokeOnMainThread(StartProcessingCamera);
                    }

                }, TaskCreationOptions.LongRunning);

                // Don't repeat the timer (we will start a new timer when the request is finished)
                return false;
            });
        }

        public void StopProcessingCamera()
        {
            _stopTimer = true;
        }
    }
}