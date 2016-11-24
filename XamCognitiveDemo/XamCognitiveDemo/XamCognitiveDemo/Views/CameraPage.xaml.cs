using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face.Contract;
using Xamarin.Forms;
using XamCognitiveDemo.Events;
using XamCognitiveDemo.Models;
using XamCognitiveDemo.Services;
using XamCognitiveDemo.ViewModels;

namespace XamCognitiveDemo.Views
{
    public partial class CameraPage : ContentPage
    {
        public CameraPageViewModel ViewModel { get; }
        public CameraPage()
        {
            InitializeComponent();
            this.BindingContext = ViewModel = new CameraPageViewModel();
            //ViewModel.NewResultProvided += ViewModelOnNewResultProvided;
        }

        // TODO not aligned xaml!
        //private async void ViewModelOnNewResultProvided(object sender, NewResultEventArgs newResultEventArgs)
        //{
        //    var faceRectangle = newResultEventArgs.AnalysisResult.Face.FaceRectangle;
        //    await
        //        FaceRectangleBox.LayoutTo(new Rectangle(faceRectangle.Left, faceRectangle.Top, faceRectangle.Width,
        //            faceRectangle.Height));
        //}
    }
}
