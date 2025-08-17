namespace CondoSphereMobile
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            // Sempre começa no login
            Shell.Current.GoToAsync("//LoginPage");
        }

    }
}
