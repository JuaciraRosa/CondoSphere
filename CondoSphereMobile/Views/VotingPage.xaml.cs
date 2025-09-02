using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;


public partial class VotingPage : ContentPage
{
    private readonly ApiService _api = new();
    private List<VoteDto> _polls = new();

    public VotingPage()
    {
        InitializeComponent();
        Appearing += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _polls = await BuildPollsFromMeetingsAsync();
            // TODO: bind _polls ao teu UI (Picker/CollectionView/etc.)
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    // XAML: Clicked="OnReloadClicked"
    private async void OnReloadClicked(object sender, EventArgs e) => await LoadAsync();

    // Se teu XAML tiver um botão "Votar" com Clicked="OnVoteClicked"
    private async void OnVoteClicked(object sender, EventArgs e)
    {
        var poll = _polls.FirstOrDefault(p => !string.IsNullOrEmpty(p.Selected));
        if (poll == null)
        {
            await DisplayAlert("Atenção", "Escolhe uma opção.", "OK");
            return;
        }

        try
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

            var me = await _api.GetAsync<dynamic>("residents/me");
            var voterEmail = (string)(me?.email ?? me?.Email ?? "");

            var payload = new
            {
                MeetingId = poll.PollId,
                VoterEmail = voterEmail,
                UnitNumber = "",
                Choice = poll.Selected
            };

            await _api.PostAsync<object, object>("voting/cast", payload);
            await DisplayAlert("Ok", "Voto registado.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    // ---- helpers ----
    private record MeetingDto(int Id, DateTime ScheduledDate, string Agenda);

    private async Task<List<VoteDto>> BuildPollsFromMeetingsAsync()
    {
        var token = await SecureStorage.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

        var meetings = await _api.GetAsync<List<MeetingDto>>("meetings/list");
        var options = new List<string> { "A favor", "Contra", "Abstenção" };

        return meetings.Select(m => new VoteDto
        {
            PollId = m.Id,
            Question = m.Agenda,
            Options = new List<string>(options)
        }).ToList();
    }
}