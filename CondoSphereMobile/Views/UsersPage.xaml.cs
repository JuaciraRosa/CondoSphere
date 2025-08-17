using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class UsersPage : ContentPage
{
    private readonly UsersViewModel _viewModel;

    public UsersPage()
    {
        InitializeComponent();
        _viewModel = new UsersViewModel();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadUsersCommand.Execute(null);
    }
}