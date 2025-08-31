using CondoSphereMobile.Services;
using CondoSphereMobile.ViewModels;

namespace CondoSphereMobile.Views;


public partial class MeetingDocumentsPage : ContentPage
{
    public MeetingDocumentsViewModel VM { get; }
    public Command<string> DownloadCommand { get; }

    public int CondominiumId { get; set; }

    public MeetingDocumentsPage(int condominiumId)
    {
        InitializeComponent();
        BindingContext = VM = new MeetingDocumentsViewModel();
        DownloadCommand = new Command<string>(async (url) => await OnDownload(url));
        CondominiumId = condominiumId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await VM.LoadAsync(CondominiumId);
    }

    private async System.Threading.Tasks.Task OnDownload(string url)
    {
        var api = new ApiService();
        var token = await SecureStorage.GetAsync("jwt_token");
        var downloader = new FileDownloadService("http://condosphere.somee.com/", token);
        var relative = url.StartsWith("/") ? url.Substring(1) : url;
        var bytes = await downloader.GetBytesAsync(relative);

        var path = System.IO.Path.Combine(FileSystem.CacheDirectory, $"ata_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        System.IO.File.WriteAllBytes(path, bytes);
        await DisplayAlert("Download", $"Guardado em: {path}", "OK");
    }
}