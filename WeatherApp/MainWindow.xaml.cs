using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;
using Microsoft.Web.WebView2.Core;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private const string ApiUrl = "http://api.openweathermap.org/data/2.5/weather?q={0}&appid={1}&units=metric&lang=pl";
        private const string ForecastUrl = "http://api.openweathermap.org/data/2.5/forecast?q={0}&appid={1}&units=metric&lang=pl";
        private readonly string _apiKey;
        private double _latitude;
        private double _longitude;

        public SeriesCollection SeriesCollection { get; set; } = new();
        public List<string> Dates { get; set; } = new();
        public ChartValues<double> Temperatures { get; set; } = new();

        private readonly Dictionary<string, string> MapLayers = new()
        {
            {"Temperatura", "temperature"},
            {"Wiatr", "wind"},
            {"Opady", "precipitation"},
            {"Ciśnienie", "pressure"},
            {"Chmury", "clouds"}
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeChart();
            _apiKey = ConfigurationManager.AppSettings["ApiKey"] ?? throw new InvalidOperationException("Klucz API nie został znaleziony w pliku konfiguracyjnym.");
            this.StateChanged += MainWindow_StateChanged;
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await WeatherMapView.EnsureCoreWebView2Async();
            WeatherMapView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            WeatherMapView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        }

        private void UpdateWeatherMap()
        {
            string selectedLayer = (MapLayerComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Wiatr";
            string layer = MapLayers.ContainsKey(selectedLayer) ? MapLayers[selectedLayer] : "wind";

            string mapUrl = $"https://openweathermap.org/weathermap?basemap=map&cities=true&layer={layer}&lat={_latitude}&lon={_longitude}&zoom=10";
            WeatherMapView.Source = new Uri(mapUrl);
        }

        private void MapLayerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_latitude != 0 && _longitude != 0)
            {
                UpdateWeatherMap();
            }
        }

        private async void GetWeatherButton_Click(object sender, RoutedEventArgs e)
        {
            string city = CityTextBox.Text;
            if (string.IsNullOrEmpty(city) || city == "Wpisz miasto")
            {
                ShowErrorMessage("Proszę wpisać poprawną nazwę miasta");
                return;
            }

            ClearWeatherData();
            ShowLoadingIndicator();

            using (HttpClient client = new())
            {
                try
                {
                    string currentWeatherUrl = string.Format(ApiUrl, city, _apiKey);
                    HttpResponseMessage currentResponse = await client.GetAsync(currentWeatherUrl);

                    if (!currentResponse.IsSuccessStatusCode)
                    {
                        ShowErrorMessage("Nie znaleziono podanej miejscowości");
                        return;
                    }

                    string currentResponseBody = await currentResponse.Content.ReadAsStringAsync();
                    JObject currentWeatherData = JObject.Parse(currentResponseBody);

                    string forecastUrl = string.Format(ForecastUrl, city, _apiKey);
                    HttpResponseMessage forecastResponse = await client.GetAsync(forecastUrl);
                    forecastResponse.EnsureSuccessStatusCode();
                    string forecastResponseBody = await forecastResponse.Content.ReadAsStringAsync();
                    JObject forecastData = JObject.Parse(forecastResponseBody);

                    if (currentWeatherData["weather"] == null || currentWeatherData["main"] == null || currentWeatherData["name"] == null)
                    {
                        ShowErrorMessage("Błąd w danych pogodowych");
                        return;
                    }

                    UpdateCurrentWeatherUI(currentWeatherData);
                    UpdateChartData(forecastData);
                    UpdateMapCoordinates(currentWeatherData);
                    UpdateWeatherMap();
                    HideErrorMessage();
                }
                catch (HttpRequestException)
                {
                    ShowErrorMessage("Problem z połączeniem internetowym");
                }
                catch (Exception)
                {
                    ShowErrorMessage("Wystąpił nieoczekiwany błąd");
                }
                finally
                {
                    HideLoadingIndicator();
                }
            }
        }

        private void UpdateMapCoordinates(JObject weatherData)
        {
            _latitude = weatherData["coord"]?["lat"]?.ToObject<double>() ?? 0;
            _longitude = weatherData["coord"]?["lon"]?.ToObject<double>() ?? 0;
        }

        private void ShowErrorMessage(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void HideErrorMessage()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }

        private void ShowLoadingIndicator()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
        }

        private void HideLoadingIndicator()
        {
            LoadingIndicator.Visibility = Visibility.Collapsed;
        }

        private void ClearWeatherData()
        {
            CityText.Text = string.Empty;
            TemperatureText.Text = string.Empty;
            WeatherDescriptionText.Text = string.Empty;
            HumidityText.Text = string.Empty;
            WindText.Text = string.Empty;
            PressureText.Text = string.Empty;
            SunriseText.Text = string.Empty;
            SunsetText.Text = string.Empty;
            VisibilityText.Text = string.Empty;
            CloudsText.Text = string.Empty;
            WeatherIcon.Source = null;

            Dates.Clear();
            Temperatures.Clear();
        }

        private void UpdateCurrentWeatherUI(JObject currentWeatherData)
        {
            string weatherDescription = currentWeatherData["weather"]?[0]?["description"]?.ToString() ?? "Brak opisu";
            double temperature = currentWeatherData["main"]?["temp"]?.ToObject<double>() ?? 0.0;
            double humidity = currentWeatherData["main"]?["humidity"]?.ToObject<double>() ?? 0.0;
            string cityName = currentWeatherData["name"]?.ToString() ?? "Nieznane miasto";

            CityText.Text = cityName;
            TemperatureText.Text = $"Temperatura: {temperature}°C";
            WeatherDescriptionText.Text = $"Opis: {weatherDescription}";
            HumidityText.Text = $"Wilgotność: {humidity}%";

            double windSpeed = currentWeatherData["wind"]?["speed"]?.ToObject<double>() ?? 0.0;
            double windDeg = currentWeatherData["wind"]?["deg"]?.ToObject<double>() ?? 0.0;
            string windDirection = GetWindDirection(windDeg);
            WindText.Text = $"Wiatr: {windSpeed} m/s ({windDirection})";

            string iconCode = currentWeatherData["weather"]?[0]?["icon"]?.ToString();
            if (!string.IsNullOrEmpty(iconCode))
            {
                string iconUrl = $"http://openweathermap.org/img/wn/{iconCode}@2x.png";
                WeatherIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconUrl));
            }

            double pressure = currentWeatherData["main"]?["pressure"]?.ToObject<double>() ?? 0.0;
            PressureText.Text = $"Ciśnienie: {pressure} hPa";

            long sunrise = currentWeatherData["sys"]?["sunrise"]?.ToObject<long>() ?? 0;
            long sunset = currentWeatherData["sys"]?["sunset"]?.ToObject<long>() ?? 0;
            SunriseText.Text = $"Wschód słońca: {DateTimeOffset.FromUnixTimeSeconds(sunrise).ToLocalTime():HH:mm}";
            SunsetText.Text = $"Zachód słońca: {DateTimeOffset.FromUnixTimeSeconds(sunset).ToLocalTime():HH:mm}";

            int visibility = currentWeatherData["visibility"]?.ToObject<int>() ?? 0;
            VisibilityText.Text = $"Widoczność: {(visibility / 1000.0):0.0} km";

            int clouds = currentWeatherData["clouds"]?["all"]?.ToObject<int>() ?? 0;
            CloudsText.Text = $"Zachmurzenie: {clouds}%";
        }

        private string GetWindDirection(double degrees)
        {
            if (degrees >= 337.5 || degrees < 22.5) return "N";
            if (degrees >= 22.5 && degrees < 67.5) return "NE";
            if (degrees >= 67.5 && degrees < 112.5) return "E";
            if (degrees >= 112.5 && degrees < 157.5) return "SE";
            if (degrees >= 157.5 && degrees < 202.5) return "S";
            if (degrees >= 202.5 && degrees < 247.5) return "SW";
            if (degrees >= 247.5 && degrees < 292.5) return "W";
            return "NW";
        }

        private void UpdateChartData(JObject forecastData)
        {
            Dates.Clear();
            Temperatures.Clear();

            if (forecastData["list"] == null) return;

            foreach (var item in forecastData["list"]!)
            {
                string? date = item["dt_txt"]?.ToString();
                double temperature = item["main"]?["temp"]?.ToObject<double>() ?? 0.0;

                if (date != null)
                {
                    Dates.Add(date);
                    Temperatures.Add(temperature);
                }
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                var rect = new RectangleGeometry();
                rect.Rect = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
                this.Clip = rect;
            }
            else
            {
                var rect = new RectangleGeometry();
                rect.Rect = new Rect(0, 0, 1200, 900);
                rect.RadiusX = 15;
                rect.RadiusY = 15;
                this.Clip = rect;
            }
        }

        private void InitializeChart()
        {
            DataContext = this;
            SeriesCollection.Add(new LineSeries
            {
                Title = "Temperatura (°C)",
                Values = Temperatures,
                PointGeometrySize = 10,
                Stroke = Brushes.DodgerBlue,
                Fill = Brushes.Transparent
            });
        }
    }
}