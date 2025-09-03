using CondoSphereMobile.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CondoSphereMobile.Models
{
    public class MessagesViewModel : BindableObject
    {
        private readonly ApiService _api = new();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); (LoadCommand as Command)?.ChangeCanExecute(); }
        }

        public ObservableCollection<MessageDto> Messages { get; } = new();

        // true = /messages/mine; false = /messages (só Manager/Admin têm acesso)
        private bool _onlyMine = true;
        public bool OnlyMine
        {
            get => _onlyMine;
            set { if (_onlyMine == value) return; _onlyMine = value; OnPropertyChanged(); }
        }

        public ICommand LoadCommand { get; }
        public MessagesViewModel()
        {
            LoadCommand = new Command(async () => await LoadAsync(), () => !IsBusy);
        }

        private async Task EnsureAuthAsync()
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token)) _api.SetAuthToken(token);
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                await EnsureAuthAsync();

                var endpoint = OnlyMine ? "messages/mine" : "messages";
                List<MessageDto> items = new();

                try
                {
                    // tenta o endpoint pedido
                    items = await _api.GetAsync<List<MessageDto>>(endpoint);
                }
                catch (Exception)
                {
                    // se falhou tentando "todas", cai para "minhas"
                    if (!OnlyMine)
                    {
                        try
                        {
                            items = await _api.GetAsync<List<MessageDto>>("messages/mine");
                            OnlyMine = true; // reflete no UI
                        }
                        catch (Exception ex2)
                        {
                            await Application.Current.MainPage.DisplayAlert("Erro", ex2.Message, "OK");
                            return;
                        }
                    }
                    else
                    {
                        // já era "minhas" e mesmo assim falhou
                        throw;
                    }
                }

                Messages.Clear();
                foreach (var m in items.OrderByDescending(x => x.CreatedAt))
                    Messages.Add(m);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
