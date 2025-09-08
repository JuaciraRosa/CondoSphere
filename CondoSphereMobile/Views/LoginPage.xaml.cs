using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;
public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        if (BindingContext is not LoginViewModel)
            BindingContext = new LoginViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is LoginViewModel vm &&
            vm.InitCommand?.CanExecute(null) == true)
        {
            vm.InitCommand.Execute(null);   // carrega remember/email e tenta auto-login
        }
    }
}
