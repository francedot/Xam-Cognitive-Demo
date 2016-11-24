using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision;
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

            FaceServiceHttpClient = new HttpClient
            {
                BaseAddress = new Uri(FaceBaseUri, UriKind.Absolute)
            };
            FaceServiceHttpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", FaceApiKey);
            FaceServiceHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            VisionServiceHttpClient = new HttpClient
            {
                BaseAddress = new Uri(VisionBaseUri, UriKind.Absolute)
            };
            VisionServiceHttpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", VisionApiKey);
            VisionServiceHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string EmotionBaseUri { get; } = "https://api.projectoxford.ai/emotion/v1.0/recognize";
        public string FaceBaseUri { get; } = "https://api.projectoxford.ai/face/v1.0/detect";
        public string VisionBaseUri { get; } = "https://api.projectoxford.ai/vision/v1.0/analyze";
        public string EmotionApiKey { get; } = "d633f5f017d143cea1f01a630b4003a9";
        public string FaceApiKey { get; } = "685d3b9d785f4015a234d2abaa81d035";
        public string VisionApiKey { get; } = "692a592800ea47d88b34aee6aadd6055";

        public Func<VideoFrame> ProducerDelegate { get; set; }
        public AnalysisResult LastAnalysisResult { get; set; }
        public Func<Task> AnalysisFunction { get; set; }
        public HttpClient EmotionServiceHttpClient { get; set; }
        public HttpClient FaceServiceHttpClient { get; set; }
        public HttpClient VisionServiceHttpClient { get; set; }

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

        public async Task<Face[]> RecognizeFacesAsync(byte[] imageBytes)
        {
            var content = new ByteArrayContent(imageBytes);
            await content.LoadIntoBufferAsync();
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var response = await FaceServiceHttpClient.PostAsync("?returnFaceId=true&returnFaceLandmarks=true&returnFaceAttributes=age,gender,smile,facialHair,headPose,glasses", content);
            Debug.WriteLine("Response Is" + response);
            var resultContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            Debug.WriteLine("Content Is" + resultContent);
            var faces = JsonConvert.DeserializeObject<Face[]>(resultContent);

            content.Dispose();

            return faces;
        }

        public async Task<Microsoft.ProjectOxford.Vision.Contract.AnalysisResult> RecognizeVisionAsync(byte[] imageBytes)
        {
            var content = new ByteArrayContent(imageBytes);
            await content.LoadIntoBufferAsync();
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var response = await VisionServiceHttpClient.PostAsync("?visualFeatures=Categories,Tags,Description,Faces,ImageType,Color,Adult&details=Celebrities&language=en", content);
            Debug.WriteLine("Response Is" + response);
            var resultContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            Debug.WriteLine("Content Is" + resultContent);
            var result = JsonConvert.DeserializeObject<Microsoft.ProjectOxford.Vision.Contract.AnalysisResult>(resultContent);

            content.Dispose();
            return result;
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
                    var faces = await RecognizeFacesAsync(videoFrame.ImageBytes).ConfigureAwait(false);
                    var result = await RecognizeVisionAsync(videoFrame.ImageBytes).ConfigureAwait(false);
                    LastAnalysisResult = new AnalysisResult
                    {
                        Face = faces?.FirstOrDefault() == null ? null : CalculateFaceRectangleForRendering(faces.FirstOrDefault(), videoFrame.PixelDimension),
                        Emotion = emotions?.FirstOrDefault()
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

        public static Face CalculateFaceRectangleForRendering(Face face, Tuple<int, int> imageInfo)
        {
            int maxSize = 300;
            var imageWidth = imageInfo.Item1;
            var imageHeight = imageInfo.Item2;
            float ratio = (float)imageWidth / imageHeight;
            int uiWidth = 0;
            int uiHeight = 0;
            if (ratio > 1.0)
            {
                uiWidth = maxSize;
                uiHeight = (int)(maxSize / ratio);
            }
            else
            {
                uiHeight = maxSize;
                uiWidth = (int)(ratio * uiHeight);
            }

            int uiXOffset = (maxSize - uiWidth) / 2;
            int uiYOffset = (maxSize - uiHeight) / 2;
            float scale = (float)uiWidth / imageWidth;

            var newFaceRectangle = new FaceRectangle()
            {
                Left = (int) ((face.FaceRectangle.Left*scale) + uiXOffset),
                Top = (int) ((face.FaceRectangle.Top*scale) + uiYOffset),
                Height = (int) (face.FaceRectangle.Height*scale),
                Width = (int) (face.FaceRectangle.Width*scale),
            };
            face.FaceRectangle = newFaceRectangle;

            return face;
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