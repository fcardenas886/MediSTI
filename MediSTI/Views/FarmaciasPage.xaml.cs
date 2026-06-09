using MediSTI.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace MediSTI.Views;

public partial class FarmaciasPage : ContentPage
{
	public FarmaciasPage()
	{
		InitializeComponent();
		CargarFarmacias("LONCOCHE");
	}

    private async void CargarFarmacias(string comuna)
    {
        loadingIndicator.IsVisible = true;
        listFarmacias.IsVisible = false;

        // Configuración para omitir errores de certificado SSL (Común en sitios de gobierno)
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using HttpClient client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        try
        {
            string url = "https://midas.minsal.cl/farmacia_v2/WS/getLocalesTurnos.php";

            // Descargamos el JSON como cadena para asegurar que no haya errores de formato
            var jsonRaw = await client.GetStringAsync(url);

            if (!string.IsNullOrWhiteSpace(jsonRaw))
            {
                var respuesta = JsonSerializer.Deserialize<List<FarmaciaTurno>>(jsonRaw);

                if (respuesta != null)
                {
                    // Filtramos: Comuna en mayúsculas y quitamos espacios extra
                    var filtradas = respuesta
                        .Where(f => f.comuna_nombre.Trim().ToUpper() == comuna.Trim().ToUpper())
                        .ToList();

                    MainThread.BeginInvokeOnMainThread(() => {
                        listFarmacias.ItemsSource = filtradas;

                        if (filtradas.Count == 0)
                        {
                            DisplayAlert("Sistema STI", $"No se encontraron farmacias de turno en {comuna} para hoy.", "OK");
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de Conexión", $"No se pudo conectar con el MINSAL: {ex.Message}", "OK");
        }
        finally
        {
            loadingIndicator.IsVisible = false;
            listFarmacias.IsVisible = true;
        }
    }

    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(searchBar.Text))
            CargarFarmacias(searchBar.Text);
    }

    private async void OnVerMapaClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var farmacia = (FarmaciaTurno)button.CommandParameter;

        if (double.TryParse(farmacia.local_lat, out double lat) &&
            double.TryParse(farmacia.local_lng, out double lng))
        {
            // Abre Google Maps o Apple Maps según el dispositivo
            await Map.OpenAsync(lat, lng, new MapLaunchOptions
            {
                Name = farmacia.local_nombre,
                NavigationMode = NavigationMode.Driving
            });
        }
    }
}