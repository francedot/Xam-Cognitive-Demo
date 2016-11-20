using Xamarin.Forms;
using XamCognitiveDemo.Services;
using XamCognitiveDemo.ViewModels;

namespace XamCognitiveDemo.Views
{
    public partial class VideoAnalysisPage : ContentPage
    {
        public VideoAnalysisViewModel ViewModel => ViewModelLocator.VideoAnalysisViewModel;

        public VideoAnalysisPage()
        {
            InitializeComponent();
            this.BindingContext = ViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            //ViewModel.OnNavigatingTo();
        }
    }
}
