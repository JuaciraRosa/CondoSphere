using CondoSphereMobile.Models;
using CondoSphereMobile.Services;

namespace CondoSphereMobile.Views;

public partial class MessageComposePage : ContentPage
{
    private readonly ApiService _api = new();

    public MessageComposePage()
    {
        InitializeComponent();
        Appearing += async (_, __) => await PrefillAsync();
    }

    private async Task PrefillAsync()
    {
        // opcional: preencher remetente/para conforme o perfil
        var token = await SecureStorage.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);
        try
        {
            var me = await _api.GetAsync<dynamic>("residents/me");
            // se quiseres usar o e-mail do morador de alguma forma…
        }
        catch { /* ignorar se não for Resident */ }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var subject = SubjectEntry.Text?.Trim();
        var body = BodyEditor.Text?.Trim();
        var to = ToEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
        {
            await DisplayAlert("Atenção", "Assunto e mensagem são obrigatórios.", "OK");
            return;
        }

        try
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);

            var payload = new CreateMessageDto
            {
                To = string.IsNullOrWhiteSpace(to) ? null : to,
                Subject = subject!,
                Body = body!
            };

            await _api.PostAsync<CreateMessageDto, MessageDto>("messages", payload);
            await DisplayAlert("Ok", "Mensagem enviada.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }
}