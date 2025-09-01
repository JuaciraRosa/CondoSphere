using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;

public partial class ReceiptsPage : ContentPage
{
    public ReceiptsViewModel VM { get; }

    public Command<int> DownloadReceiptCommand { get; }

    public ReceiptsPage()
    {
        InitializeComponent();
        BindingContext = VM = new ReceiptsViewModel();
        DownloadReceiptCommand = new Command<int>(async (id) => await OnDownloadReceipt(id));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await VM.LoadPaymentsAsync();
    }

    private async Task OnDownloadReceipt(int paymentId)
    {
        var path = await VM.DownloadReceiptAsync(paymentId);
        await DisplayAlert("Recibo guardado", $"Guardado em: {path}", "OK");
    }
}