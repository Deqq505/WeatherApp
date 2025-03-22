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

        private void PrepareChartData(JObject forecastData)
        {
            
            Dates.Clear();
            Temperatures.Clear();

            
            foreach (var item in forecastData["list"])
            {
                string date = item["dt_txt"].ToString();
                double temperature = item["main"]["temp"].ToObject<double>();

                Dates.Add(date);
                Temperatures.Add(temperature);
            }

           
            SeriesCollection.Clear();
            SeriesCollection.Add(new LineSeries
            {
                Title = "Temperatura (°C)",
                Values = Temperatures,
                PointGeometrySize = 10,
                Stroke = System.Windows.Media.Brushes.DodgerBlue,
                Fill = System.Windows.Media.Brushes.Transparent
            });

            
            TemperatureChart.AxisX[0].Labels = Dates;
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
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
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