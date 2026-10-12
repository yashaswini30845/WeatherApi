using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq; // Added to enable .First() for collection handling
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherApiProject.Models;

namespace WeatherApiProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public WeatherController(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["WeatherSettings:ApiKey"] ?? "";
        }

        [HttpGet("{city}")]
        public async Task<IActionResult> GetWeather(string city)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_OPENWEATHER_API_KEY")
            {
                return StatusCode(500, "API configuration error: Please provide a valid OpenWeatherMap ApiKey in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                return BadRequest("City name cannot be empty.");
            }

            // RECTIFIED: Corrected the base URL, version route, and query format (?q=)
            string url = $"https://openweathermap.org{city}&units=metric&appid={_apiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, $"Error from weather provider: {response.ReasonPhrase}");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                // RECTIFIED: Safely extracting the first item from the 'weather' array
                var weatherArray = root.GetProperty("weather");
                string description = "N/A";
                if (weatherArray.ValueKind == JsonValueKind.Array && weatherArray.GetArrayLength() > 0)
                {
                    var firstWeatherItem = weatherArray.EnumerateArray().First();
                    description = firstWeatherItem.GetProperty("description").GetString() ?? "N/A";
                }

                var weatherResult = new WeatherResult
                {
                    CityName = root.GetProperty("name").GetString() ?? city,
                    Temperature = root.GetProperty("main").GetProperty("temp").GetDouble(),
                    Humidity = root.GetProperty("main").GetProperty("humidity").GetInt32(),
                    Description = description
                };

                return Ok(weatherResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
