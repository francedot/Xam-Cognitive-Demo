using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace XamCognitiveDemo.ViewModels
{
    /// <summary>
    /// The VM guidelines suggest that the VM should expose a property for each value on the model that it wants to expose.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected bool Set<T>(ref T field, T value,
            [CallerMemberName] string name = null)
        {
            if (Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        #endregion

        #region Navigation

        public virtual void OnNavigatingFrom(object parameter = null)
        {
            return;
        }

        public virtual void OnNavigatingTo(object parameter = null)
        {
            return;
        }

        public virtual async Task OnNavigatingFromAsync(object parameter = null)
        {
            await Task.Yield();
        }

        public virtual async Task OnNavigatingToAsync(object parameter = null)
        {
            await Task.Yield();
        }

        #endregion

    }
}