using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Views
{
    public class WebViewPage : ContentPage
    {
        public WebViewPage(string url)
        {
            Title = "Pagamento";
            Content = new Grid
            {
                Children =
            {
                new WebView { Source = url }
            }
            };
            ToolbarItems.Add(new ToolbarItem("Fechar", null, async () => await Navigation.PopModalAsync()));
        }
    }
}
