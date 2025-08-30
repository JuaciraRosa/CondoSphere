using CondoSphereMobile.ViewModels; // <- importante

namespace CondoSphereMobile.Views
{
    public partial class CondominiumsPage : ContentPage
    {
        public CondominiumsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is CondominiumsViewModel vm &&
                vm.LoadCondominiumsCommand.CanExecute(null))
            {
                vm.LoadCondominiumsCommand.Execute(null);
            }
        }

        private async void OnBackToDashboardClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///DashboardPage");
        }
    }
}
