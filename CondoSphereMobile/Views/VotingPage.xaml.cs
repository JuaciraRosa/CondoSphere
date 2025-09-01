using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;


public partial class VotingPage : ContentPage
{
    private readonly ApiService _api = new();

    public VotingPage()
    {
        InitializeComponent();
        Appearing += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var polls = await _api.GetAsync<List<VoteDto>>("voting/list"); // <-- ajusta
            List.ItemsSource = polls;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    private async void OnReloadClicked(object sender, EventArgs e) => await LoadAsync();

    private async void OnVoteClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not VoteDto poll) return;
        if (string.IsNullOrWhiteSpace(poll.Selected))
        {
            await DisplayAlert("Atenção", "Escolhe uma opção.", "OK");
            return;
        }

        try
        {
            var payload = new { pollId = poll.PollId, option = poll.Selected };
            var _ = await _api.PostAsync<object, object>("voting/vote", payload); // <-- ajusta
            await DisplayAlert("Obrigado", "Voto registado.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }
}