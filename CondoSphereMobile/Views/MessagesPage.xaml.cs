using CondoSphereMobile.Models;

namespace CondoSphereMobile.Views;

public partial class MessagesPage : ContentPage
{
    public MessagesPage()
    {
        InitializeComponent();
        BindingContext = new MessagesViewModel(); 
        Appearing += async (_, __) =>
        {
            if (BindingContext is MessagesViewModel vm)
                await vm.LoadAsync();
        };
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//DashboardPage");

    private async void OnNewClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new MessageComposePage());

    private async void OnToggleScopeClicked(object sender, EventArgs e)
    {
        if (BindingContext is not MessagesViewModel vm) return;
        vm.OnlyMine = !vm.OnlyMine;
        (sender as ToolbarItem)!.Text = vm.OnlyMine ? "Só as minhas" : "Todas";
        await vm.LoadAsync();
    }
}