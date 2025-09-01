using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class PaymentsPage : ContentPage
{
    public PaymentsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is PaymentsViewModel vm &&
            vm.LoadPaymentsCommand.CanExecute(null))
        {
            vm.LoadPaymentsCommand.Execute(null);
        }
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}