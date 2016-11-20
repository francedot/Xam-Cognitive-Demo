using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json;
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
        public bool IsRunning { get; set; }

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
            _faceClient = new FaceServiceClient("685d3b9d785f4015a234d2abaa81d035");
            _emotionClient = new EmotionServiceClient("d633f5f017d143cea1f01a630b4003a9");
        }

        public string EmotionBaseUri { get; set; } = "https://api.projectoxford.ai/emotion/v1.0/recognize";

        public Func<VideoFrame> ProducerDelegate { get; set; }
        public AnalysisResult LastAnalysisResult { get; set; }
        public Func<Task> AnalysisFunction { get; set; }
        public double FrameRate { get; } = 0.5;

        public async Task<Emotion[]> RecognizeEmotionsAsync(byte[] imageBytes)
        {
            // Request body

            using (var content = new ByteArrayContent(imageBytes))
            //using (var streamContent = new StreamContent(imageStream))
            //using (var stringContent = new StringContent(content, Encoding.UTF8, "application/octet-stream"))
            using (var httpClient = new HttpClient(new HttpClientHandler(), false))
            {
                await content.LoadIntoBufferAsync();
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "d633f5f017d143cea1f01a630b4003a9");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await httpClient.PostAsync(EmotionBaseUri, content);
                Debug.WriteLine("Response Is" + response);
                var resultContent = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                Debug.WriteLine("Content Is" + resultContent);

                var emotions = JsonConvert.DeserializeObject<Emotion[]>(resultContent);

                //content.Dispose();
                //httpClient.Dispose();

                return emotions;
            }
        }

        public void StartProcessingCamera()
        {
            // Start a timer that runs after 1 minute.
            Device.StartTimer(TimeSpan.FromSeconds(1.0 / FrameRate), () =>
            {
                var videoFrame = ProducerDelegate();
                if (videoFrame == null)
                {
                    return true;
                }
                IsRunning = true;

                Device.BeginInvokeOnMainThread(async () =>
                {
                    // Do the actual request and wait for it to finish.

                    var emotions = await RecognizeEmotionsAsync(videoFrame.ImageBytes);

                    //var emotions = await _emotionClient.RecognizeAsync(videoFrame.ImageStream);
                    //var faces = await _faceClient.DetectAsync(videoFrame.ImageStream);
                    LastAnalysisResult = new AnalysisResult()
                    {
                        //Faces = faces,
                        Emotions = emotions
                    };
                    OnNewResultAvailable(new NewResultEventArgs
                    {
                        AnalysisResult = LastAnalysisResult
                    });
                    IsRunning = false;
                    // Switch back to the UI thread to update the UI
                    if (!_stopTimer)
                    {
                        Device.BeginInvokeOnMainThread(StartProcessingCamera);
                    }
                });


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