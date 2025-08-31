using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.ViewModels
{
    public class BaseViewModel : BindableObject
    {
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        protected async Task ExecuteSafeAsync(Func<Task> work)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                await work();
            }
            catch (UnauthorizedAccessException)
            {
                await Application.Current.MainPage.DisplayAlert("Sessão expirada", "Faça login novamente.", "OK");
                await Shell.Current.GoToAsync("///LoginPage");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

}
