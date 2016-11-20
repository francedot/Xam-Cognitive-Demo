using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Newtonsoft.Json;
using Xamarin.Forms;
using XamCognitiveDemo.Events;
using XamCognitiveDemo.Models;

namespace XamCognitiveDemo.Services
{
    public class RtVideoAnalyzer
    {
        private bool _stopTimer;
        private readonly double fps = 0.2;

        #region Events

        public event EventHandler ProcessingStarting;
        public event EventHandler ProcessingStarted;
        public event EventHandler ProcessingStopping;
        public event EventHandler ProcessingStopped;
        public event EventHandler<NewFrameEventArgs> NewFrameProvided;
        public event EventHandler<NewResultEventArgs> NewResultAvailable;

        #endregion Events

        public RtVideoAnalyzer()
        {
            EmotionServiceHttpClient = new HttpClient
            {
                BaseAddress = new Uri(EmotionBaseUri, UriKind.Absolute)
            };
            EmotionServiceHttpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", EmotionApiKey);
            EmotionServiceHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string EmotionBaseUri { get; } = "https://api.projectoxford.ai/emotion/v1.0/recognize";
        public string EmotionApiKey { get; } = "d633f5f017d143cea1f01a630b4003a9";

        public Func<VideoFrame> ProducerDelegate { get; set; }
        public AnalysisResult LastAnalysisResult { get; set; }
        public Func<Task> AnalysisFunction { get; set; }
        public HttpClient EmotionServiceHttpClient { get; set; }

        public async Task<Emotion[]> RecognizeEmotionsAsync(byte[] imageBytes)
        {
            var content = new ByteArrayContent(imageBytes);
            await content.LoadIntoBufferAsync();
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            
            var response = await EmotionServiceHttpClient.PostAsync("", content);
            Debug.WriteLine("Response Is" + response);
            var resultContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            Debug.WriteLine("Content Is" + resultContent);
            var emotions = JsonConvert.DeserializeObject<Emotion[]>(resultContent);

            content.Dispose();

            return emotions;
        }

        public void StartProcessingCamera()
        {
            // Start a timer that runs after 1 minute.
            Device.StartTimer(TimeSpan.FromSeconds(1.0 / fps), () =>
            {
                var videoFrame = ProducerDelegate();
                if (videoFrame == null)
                {
                    return true;
                }

                Device.BeginInvokeOnMainThread(async () =>
                {
                    // Do the actual request and wait for it to finish.
                    var emotions = await RecognizeEmotionsAsync(videoFrame.ImageBytes).ConfigureAwait(false);
                    LastAnalysisResult = new AnalysisResult
                    {
                        //Faces = faces,
                        Emotions = emotions
                    };

                    OnNewResultAvailable(new NewResultEventArgs
                    {
                        AnalysisResult = LastAnalysisResult
                    });
                });

                if (!_stopTimer)
                {
                    StartProcessingCamera();
                }

                return false;
            });
        }

        public void StopProcessingCamera()
        {
            _stopTimer = true;
        }

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

    }
}