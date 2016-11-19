using Xamarin.Forms;
using XamCognitiveDemo.Services;
using XamCognitiveDemo.ViewModels;

namespace XamCognitiveDemo.Views
{
    public partial class MainPage : ContentPage
    {

        public MainPageViewModel ViewModel { get; set; }

        public MainPage()
        {
            InitializeComponent();
            this.BindingContext = 
                ViewModel = ViewModelLocator.MainPageViewModel = new MainPageViewModel();
        }
    }
}
