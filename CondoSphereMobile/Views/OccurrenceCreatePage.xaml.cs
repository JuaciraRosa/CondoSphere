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
        try
        {
            var payload = new
            {
                CondominiumId = int.TryParse(CondoId.Text, out var cid) ? cid : 0,
                UnitNumber = UnitNumber.Text?.Trim(),
                Title = TitleEntry.Text?.Trim(),
                Description = DescEntry.Text?.Trim(),
                CreatedBy = EmailEntry.Text?.Trim()
            };

            // endpoint do teu backend para criar ocorrência
            var _ = await _api.PostAsync<object, object>("occurrences", payload); // <-- ajusta

            if (Created != null) await Created();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }
}