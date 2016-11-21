﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;
using XamCognitiveDemo.Events;
using XamCognitiveDemo.Models;
using XamCognitiveDemo.Services;
using XamCognitiveDemo.ViewModels;

namespace XamCognitiveDemo.Views
{
    public partial class CameraPage : ContentPage
    {
        public CameraPage()
        {
            InitializeComponent();
            this.BindingContext = new CameraPageViewModel();
        }
    }
}
