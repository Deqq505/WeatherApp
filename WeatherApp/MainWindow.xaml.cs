using System;
using System.Configuration;
using System.Net.Http;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private const string ApiUrl = "http://api.openweathermap.org/data/2.5/weather?q={0}&appid={1}&units=metric&lang=pl";
        private string _apiKey;

        public MainWindow()
        {
            InitializeComponent();

            
            _apiKey = ConfigurationManager.AppSettings["ApiKey"] ?? throw new InvalidOperationException("Klucz API nie został znaleziony w pliku konfiguracyjnym.");
        }

        private async void GetWeatherButton_Click(object sender, RoutedEventArgs e)
        {
            string city = CityTextBox.Text;
            if (string.IsNullOrEmpty(city) || city == "Wpisz miasto")
            {
                MessageBox.Show("Proszę wpisać nazwę miasta.");
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = string.Format(ApiUrl, city, _apiKey);
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    JObject weatherData = JObject.Parse(responseBody);

                    
                    if (weatherData["weather"] == null || weatherData["main"] == null || weatherData["name"] == null)
                    {
                        MessageBox.Show("Nieprawidłowa odpowiedź z serwera pogodowego.");
                        return;
                    }

                    string weatherDescription = weatherData["weather"][0]?["description"]?.ToString() ?? "Brak opisu";
                    double temperature = weatherData["main"]?["temp"]?.ToObject<double>() ?? 0.0;
                    double humidity = weatherData["main"]?["humidity"]?.ToObject<double>() ?? 0.0;
                    string cityName = weatherData["name"]?.ToString() ?? "Nieznane miasto";

                    
                    CityText.Text = cityName;
                    TemperatureText.Text = $"Temperatura: {temperature}°C";
                    WeatherDescriptionText.Text = $"Opis: {weatherDescription}";
                    HumidityText.Text = $"Wilgotność: {humidity}%";

                    
                    string iconCode = weatherData["weather"][0]?["icon"]?.ToString();
                    if (!string.IsNullOrEmpty(iconCode))
                    {
                        string iconUrl = $"http://openweathermap.org/img/wn/{iconCode}@2x.png";
                        WeatherIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconUrl));
                    }

                    
                    double pressure = weatherData["main"]?["pressure"]?.ToObject<double>() ?? 0.0;
                    PressureText.Text = $"Ciśnienie: {pressure} hPa";

                    long sunrise = weatherData["sys"]?["sunrise"]?.ToObject<long>() ?? 0;
                    long sunset = weatherData["sys"]?["sunset"]?.ToObject<long>() ?? 0;
                    SunriseText.Text = $"Wschód słońca: {DateTimeOffset.FromUnixTimeSeconds(sunrise).ToLocalTime().ToString("HH:mm")}";
                    SunsetText.Text = $"Zachód słońca: {DateTimeOffset.FromUnixTimeSeconds(sunset).ToLocalTime().ToString("HH:mm")}";

                    
                    if (!HistoryListBox.Items.Contains(cityName))
                    {
                        HistoryListBox.Items.Add(cityName);
                    }
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Błąd podczas pobierania danych pogodowych: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wystąpił nieoczekiwany błąd: {ex.Message}");
                }
            }
        }

        private void CityTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (CityTextBox.Text == "Wpisz miasto")
            {
                CityTextBox.Text = "";
            }
        }

        private void CityTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CityTextBox.Text))
            {
                CityTextBox.Text = "Wpisz miasto";
            }
        }
    }
}