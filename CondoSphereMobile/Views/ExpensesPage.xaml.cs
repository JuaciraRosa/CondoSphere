using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class ExpensesPage : ContentPage
{
    public ExpensesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ExpensesViewModel vm &&
            vm.LoadExpensesCommand.CanExecute(null))
        {
            vm.LoadExpensesCommand.Execute(null);
        }
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}
