using CondoSphereMobile.Models;
using CondoSphereMobile.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CondoSphereMobile.ViewModels
{
    public class ExpensesViewModel : BindableObject
    {
        private readonly ApiService _apiService;

        public ObservableCollection<Expense> Expenses { get; set; } = new();

        public ICommand LoadExpensesCommand { get; }

        public ExpensesViewModel()
        {
            _apiService = new ApiService();
            LoadExpensesCommand = new Command(async () => await LoadExpensesAsync());
        }

        private async Task LoadExpensesAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetAuthToken(token);

                var expenses = await _apiService.GetAsync<List<Expense>>("expenses");
                Expenses.Clear();
                foreach (var expense in expenses)
                    Expenses.Add(expense);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
