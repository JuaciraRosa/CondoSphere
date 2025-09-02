using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class OccurrenceCreatePage : ContentPage
{
    private readonly ApiService _api = new();
    public event Func<Task>? Created;

    public OccurrenceCreatePage()
    {
        InitializeComponent();
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text) ||
            string.IsNullOrWhiteSpace(DescEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            await DisplayAlert("Atenção", "Preenche Título, Descrição e Email.", "OK");
            return;
        }

        try
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

            var payload = new
            {
                Title = TitleEntry.Text?.Trim(),
                Description = DescEntry.Text?.Trim(),
                CreatedBy = EmailEntry.Text?.Trim()
            };

            await _api.PostAsync<object, object>("occurrences", payload); // ? POST
            await DisplayAlert("Sucesso", "Ocorrência criada.", "OK");

            if (Created != null) await Created();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }
}