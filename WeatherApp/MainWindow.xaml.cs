using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private const string ApiUrl = "http://api.openweathermap.org/data/2.5/weather?q={0}&appid={1}&units=metric&lang=pl";
        private const string ForecastUrl = "http://api.openweathermap.org/data/2.5/forecast?q={0}&appid={1}&units=metric&lang=pl";
        private string _apiKey;

        public SeriesCollection SeriesCollection { get; set; }
        public List<string> Dates { get; set; }
        public ChartValues<double> Temperatures { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            SeriesCollection = new SeriesCollection();
            Dates = new List<string>();
            Temperatures = new ChartValues<double>();

            DataContext = this;

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

            
            ClearWeatherData();

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    
                    string currentWeatherUrl = string.Format(ApiUrl, city, _apiKey);
                    HttpResponseMessage currentResponse = await client.GetAsync(currentWeatherUrl);
                    currentResponse.EnsureSuccessStatusCode();
                    string currentResponseBody = await currentResponse.Content.ReadAsStringAsync();
                    JObject currentWeatherData = JObject.Parse(currentResponseBody);

                    
                    string forecastUrl = string.Format(ForecastUrl, city, _apiKey);
                    HttpResponseMessage forecastResponse = await client.GetAsync(forecastUrl);
                    forecastResponse.EnsureSuccessStatusCode();
                    string forecastResponseBody = await forecastResponse.Content.ReadAsStringAsync();
                    JObject forecastData = JObject.Parse(forecastResponseBody);

                    
                    if (currentWeatherData["weather"] == null || currentWeatherData["main"] == null || currentWeatherData["name"] == null)
                    {
                        MessageBox.Show("Nieprawidłowa odpowiedź z serwera pogodowego.");
                        return;
                    }

                    
                    UpdateCurrentWeatherUI(currentWeatherData);
                    PrepareChartData(forecastData);
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

        private void ClearWeatherData()
        {
            
            CityText.Text = string.Empty;
            TemperatureText.Text = string.Empty;
            WeatherDescriptionText.Text = string.Empty;
            HumidityText.Text = string.Empty;
            PressureText.Text = string.Empty;
            SunriseText.Text = string.Empty;
            SunsetText.Text = string.Empty;
            WeatherIcon.Source = null;

            
            Dates.Clear();
            Temperatures.Clear();
            SeriesCollection.Clear();

            
            SeriesCollection = new SeriesCollection();
            TemperatureChart.Series = SeriesCollection;

            
            TemperatureChart.Update(true, true);
        }

        private void UpdateCurrentWeatherUI(JObject currentWeatherData)
        {
            string weatherDescription = currentWeatherData["weather"][0]?["description"]?.ToString() ?? "Brak opisu";
            double temperature = currentWeatherData["main"]?["temp"]?.ToObject<double>() ?? 0.0;
            double humidity = currentWeatherData["main"]?["humidity"]?.ToObject<double>() ?? 0.0;
            string cityName = currentWeatherData["name"]?.ToString() ?? "Nieznane miasto";

            CityText.Text = cityName;
            TemperatureText.Text = $"Temperatura: {temperature}°C";
            WeatherDescriptionText.Text = $"Opis: {weatherDescription}";
            HumidityText.Text = $"Wilgotność: {humidity}%";

            string iconCode = currentWeatherData["weather"][0]?["icon"]?.ToString();
            if (!string.IsNullOrEmpty(iconCode))
            {
                string iconUrl = $"http://openweathermap.org/img/wn/{iconCode}@2x.png";
                WeatherIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconUrl));
            }

            double pressure = currentWeatherData["main"]?["pressure"]?.ToObject<double>() ?? 0.0;
            PressureText.Text = $"Ciśnienie: {pressure} hPa";

            long sunrise = currentWeatherData["sys"]?["sunrise"]?.ToObject<long>() ?? 0;
            long sunset = currentWeatherData["sys"]?["sunset"]?.ToObject<long>() ?? 0;
            SunriseText.Text = $"Wschód słońca: {DateTimeOffset.FromUnixTimeSeconds(sunrise).ToLocalTime().ToString("HH:mm")}";
            SunsetText.Text = $"Zachód słońca: {DateTimeOffset.FromUnixTimeSeconds(sunset).ToLocalTime().ToString("HH:mm")}";
        }

        private void PrepareChartData(JObject forecastData)
        {
            
            Dates.Clear();
            Temperatures.Clear();
            SeriesCollection.Clear();

            
            SeriesCollection = new SeriesCollection();
            TemperatureChart.Series = SeriesCollection;

            
            foreach (var item in forecastData["list"])
            {
                string date = item["dt_txt"].ToString();
                double temperature = item["main"]["temp"].ToObject<double>();

                Dates.Add(date);
                Temperatures.Add(temperature);
            }

            
            SeriesCollection.Add(new LineSeries
            {
                Title = "Temperatura (°C)",
                Values = Temperatures,
                PointGeometrySize = 10,
                Stroke = System.Windows.Media.Brushes.DodgerBlue,
                Fill = System.Windows.Media.Brushes.Transparent
            });

            
            TemperatureChart.AxisX.Clear();
            TemperatureChart.AxisX.Add(new Axis
            {
                Title = "Data",
                Labels = Dates,
                LabelsRotation = 45,
                Foreground = System.Windows.Media.Brushes.White
            });

            
            TemperatureChart.Update(true, true);
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
    }
}