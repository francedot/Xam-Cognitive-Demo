using System;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace XamCognitiveDemo.Services
{
    public class RtVideoAnalyzer
    {
        public string ApiKey { get; set; } // TODO

        public void Samplew()
        {
            // Create grabber, with analysis type Face[]. 
            //FrameGrabber<Face[]> grabber = new FrameGrabber<Face[]>();

            // Create Face API Client. Insert your Face API key here.
            FaceServiceClient faceClient = new FaceServiceClient("<subscription key>");

            // Set up our Face API call.
            //grabber.AnalysisFunction = async frame => return await faceClient.DetectAsync(frame.Image.ToMemoryStream(".jpg"));

            // Set up a listener for when we receive a new result from an API call. 
            //grabber.NewResultAvailable += (s, e) =>
            //{
            //    if (e.Analysis != null)
            //        Console.WriteLine("New result received for frame acquired at {0}. {1} faces detected", e.Frame.Metadata.Timestamp, e.Analysis.Length);
            //};

            //// Tell grabber to call the Face API every 3 seconds.
            //grabber.TriggerAnalysisOnInterval(TimeSpan.FromMilliseconds(3000));

            //// Start running.
            //grabber.StartProcessingCameraAsync().Wait();

            //// Wait for keypress to stop
            //Console.WriteLine("Press any key to stop...");
            //Console.ReadKey();

            //// Stop, blocking until done.
            //grabber.StopProcessingAsync().Wait();
        }

    }
}