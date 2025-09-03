using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent(); // ? vai compilar quando x:Class e namespace estiverem certos
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LoginViewModel vm)
            await vm.InitAsync(); // auto-login + remember
    }
}