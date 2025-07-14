using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace WeatherApp
{
    public class RainfallData
    {
        public string time { get; set; }
        public string rain { get; set; }
    }

    public class SoilData
    {
        public string time { get; set; }
        public string temp { get; set; }
    }

    public class TempForecastData
    {
        public string time { get; set; }
        public string temp { get; set; }
    }

    public static class GetWeatherData
    {
        // Start Date (previous 7 days)
        // End Dat (today)
        // API to get soil temperatures

        private static readonly HttpClient httpClient = new HttpClient();
        //private static readonly string apiUrl1 = "https://api.open-meteo.com/v1/forecast?latitude=38.6&longitude=77.34&hourly=precipitation&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch";
        //private static readonly string rainfallHistory_URL = "https://archive-api.open-meteo.com/v1/archive?latitude=38.6&longitude=77.34&start_date=2024-07-13&end_date=2024-07-27&hourly=rain&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch";
        
        [FunctionName("TestFunction01")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string startDate = DateTime.Now.AddDays(-8).ToString("yyyy-MM-dd");
            log.LogInformation($"Test function started: {startDate}");
            return new OkObjectResult($"TestFunction01 executed successfully: {startDate}");
        }

            [FunctionName("GetWeatherData")]
        public static async Task<IActionResult> RunGetWeatherData(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string endDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            string startDate = DateTime.Now.AddDays(-8).ToString("yyyy-MM-dd");
            
            // Calculate start and end dates for the next 7 days
            string forecastStartDate = DateTime.Now.ToString("yyyy-MM-dd");
            string forecastEndDate = DateTime.Now.AddDays(6).ToString("yyyy-MM-dd");

            double RainfallPast7Days = 0;
            log.LogInformation($"startDate: {startDate}");
            log.LogInformation($"endDate: {endDate}");

            string rainfallHistory_URL       = $"https://api.open-meteo.com/v1/forecast?latitude=38.6056429&longitude=-77.3449054&daily=rain_sum&timezone=America%2FNew_York&past_days=7&forecast_days=1&wind_speed_unit=mph&temperature_unit=fahrenheit&precipitation_unit=inch";
            string rainfallForecast          = $"https://api.open-meteo.com/v1/forecast?latitude=38.6056429&longitude=-77.3449054&daily=precipitation_sum&start_date={forecastStartDate}&end_date={forecastEndDate}&wind_speed_unit=mph&temperature_unit=fahrenheit&precipitation_unit=inch";
            string soiltempHistory_api       = $"https://api.open-meteo.com/v1/forecast?latitude=38.6&longitude=77.34&hourly=soil_temperature_6cm&daily=precipitation_sum,rain_sum&temperature_unit=fahrenheit&precipitation_unit=inch&timezone=America%2FNew_York";
            string airTempMaxforecast7days   = $"https://api.open-meteo.com/v1/forecast?latitude=38.6&longitude=77.34&daily=temperature_2m_max&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch&timezone=America%2FNew_York";

            // Rainfall History Last 7 Days
            var response = await httpClient.GetAsync(rainfallHistory_URL);
            var content = await response.Content.ReadAsStringAsync();

            dynamic jsonData = JsonConvert.DeserializeObject(content);

            List<RainfallData> rainfallDataList = new List<RainfallData>();

            if (jsonData != null && jsonData.daily != null && jsonData.daily.time != null && jsonData.daily.rain_sum != null)
            {
                log.LogInformation("Starting Rainfall loop");

                for (int i = 0; i < jsonData.daily.time.Count; i++)
                {
                    //Console.WriteLine($"hourly time object type: {jsonData.hourly.time.GetType().ToString()}");
                    //rainAmount = jsonData.hourly.rain[i] ?? new JValue((object)null);

                    RainfallPast7Days = jsonData.daily.rain_sum[i] != null ? RainfallPast7Days + jsonData.daily.rain_sum[i].ToObject<double>() : RainfallPast7Days + 0;

                    rainfallDataList.Add(new RainfallData
                    {
                        time = jsonData.daily.time[i],
                        rain = jsonData.daily.rain_sum[i] != null ? jsonData.daily.rain_sum[i].ToObject<double>().ToString() : "No Data Available"
                    });
                }
            }

            else
            {
                Console.WriteLine($"json data null: {jsonData.hourly.time}");
            }

            log.LogInformation("The type of rainfallDataList is: " + rainfallDataList.GetType().ToString()); // Log the type of content


            // Rainfall Forecast
            var response2 = await httpClient.GetAsync(rainfallForecast);
            var content2 = await response2.Content.ReadAsStringAsync();
            dynamic jsonData2 = JsonConvert.DeserializeObject(content2);
            double forecastedRainAmount = 0;

                //if (jsonData2 != null && jsonData2.hourly != null && jsonData2.hourly.time != null && jsonData2.hourly.rain != null)
                if (jsonData2 != null)
                      {
                          log.LogInformation("Averaging forecasted precipitation for next 7 days");
            
                          for (int i = 0; i < jsonData2.daily.time.Count; i++)
                          {
                                log.LogInformation($"Adding {jsonData2.daily.time[i]} - rainfall amount {jsonData2.daily.precipitation_sum[i]} to list");
                                forecastedRainAmount = jsonData2.daily.precipitation_sum[i] != null ? forecastedRainAmount + jsonData2.daily.precipitation_sum[i].ToObject<double>() : forecastedRainAmount + 0;
                          }
            
                          log.LogInformation("Total precipitation forecasted for next 7 days: " + forecastedRainAmount);
                      }
            
                      else
                      {
                          log.LogWarning("API data was null! for the rainfall forecast");
                      }

            // Soil Temps
            var response3 = await httpClient.GetAsync(soiltempHistory_api);
            var content3 = await response3.Content.ReadAsStringAsync();
            double soilTempSum = 0;
            double soilTempAvg = 0;

            dynamic jsonData3 = JsonConvert.DeserializeObject(content3);
            //Console.WriteLine("jsonData3:"); // Log the type of content
            //Console.WriteLine(jsonData3);

            List<SoilData> soilTempsList = new List<SoilData>();

            if (jsonData3 != null && jsonData3.hourly != null && jsonData3.hourly.time != null && jsonData3.hourly.soil_temperature_6cm != null)
            {
                log.LogInformation("Starting Soil temp loop");

                for (int i = 0; i < jsonData3.hourly.time.Count; i++)
                {
                    soilTempSum = jsonData3.hourly.soil_temperature_6cm[i] != null ? soilTempSum + jsonData3.hourly.soil_temperature_6cm[i].ToObject<double>() : soilTempSum + 0;

                    soilTempsList.Add(new SoilData
                    {
                        time = jsonData3.hourly.time[i],
                        temp = jsonData3.hourly.soil_temperature_6cm[i] != null ? jsonData3.hourly.soil_temperature_6cm[i].ToObject<double>().ToString() : "No Data Available"
                    });
                }

                soilTempAvg = soilTempSum / soilTempsList.Count;
            }

            else
            {
                Console.WriteLine($"json data null: {jsonData3.hourly.soil_temperature_6cm}");
            }

            // Max Temp forecast Next 7 Days
            var response4 = await httpClient.GetAsync(airTempMaxforecast7days);
            var content4 = await response4.Content.ReadAsStringAsync();
            dynamic jsonData4 = JsonConvert.DeserializeObject(content4);
            double ForecastedTempSum = 0;
            double ForecastedTempAvg = 0;


            log.LogInformation("Checking for forecasted Temp data...");

            List<TempForecastData> TempForecastList = new List<TempForecastData>();
            if (jsonData4 != null && jsonData4.daily != null && jsonData4.daily.time != null && jsonData4.daily.temperature_2m_max != null)
            {
                log.LogInformation("Starting Temperature forecast Loop");

                for (int i = 0; i < jsonData4.daily.time.Count; i++)
                {
                    log.LogInformation($"Checking {jsonData4.daily.temperature_2m_max[i]}");
                    ForecastedTempSum = jsonData4.daily.temperature_2m_max[i] != null ? ForecastedTempSum + jsonData4.daily.temperature_2m_max[i].ToObject<double>() : ForecastedTempSum + 0;

                    log.LogInformation($"Adding {jsonData4.daily.temperature_2m_max[i]} to list");
                    TempForecastList.Add(new TempForecastData
                    {
                        time = jsonData4.daily.time[i],
                        temp = jsonData4.daily.temperature_2m_max[i] != null ? jsonData4.daily.temperature_2m_max[i].ToObject<double>().ToString() : "No Data Available"
                    });
                }

                ForecastedTempAvg = ForecastedTempSum / TempForecastList.Count;
            }

            else
            {
                Console.WriteLine($"json data null: {jsonData4.daily.temperature_2m_max}");
            }

            // Daily Rain Amount - past 7 days and next 7 days
            // Combine Rainfall History and Forecast Per Day
            var dailyRainfall = new List<object>();

            // Add history data (past 7 days, skipping today)
            if (jsonData != null && jsonData.daily != null && jsonData.daily.time != null && jsonData.daily.rain_sum != null)
            {
                for (int i = 0; i < jsonData.daily.time.Count - 1; i++) // Exclude the last entry (today)
                {
                    dailyRainfall.Add(new
                    {
                        date = jsonData.daily.time[i] != null ? DateTime.Parse(jsonData.daily.time[i].ToString()).ToString("M-d-yy") : "No Date",
                        rain = jsonData.daily.rain_sum[i] != null ? jsonData.daily.rain_sum[i].ToObject<double>() : 0.0,
                        type = "history"
                    });
                }
            }

            // Add forecast data (next 7 days, including today)
            if (jsonData2 != null && jsonData2.daily != null && jsonData2.daily.time != null && jsonData2.daily.precipitation_sum != null)
            {
                for (int i = 0; i < jsonData2.daily.time.Count; i++) // Start at 0 to include today
                {
                    dailyRainfall.Add(new
                    {
                        date = jsonData2.daily.time[i] != null ? DateTime.Parse(jsonData2.daily.time[i].ToString()).ToString("M-d-yy") : "No Date",
                        rain = jsonData2.daily.precipitation_sum[i] != null ? jsonData2.daily.precipitation_sum[i].ToObject<double>() : 0.0,
                        type = "forecast"
                    });
                }
            }

            // Output

            DateTime currentDate = DateTime.Now;

            // Initialize the dictionary before using it
            var dict = new Dictionary<string, object>
            {
                {"Date", DateTime.Now},
                {"RainfallLast7Days", Math.Round(RainfallPast7Days, 2)},
                {"RainfallNext7Days", Math.Truncate(forecastedRainAmount * 100) / 100},
                {"MaxTempNext7DayAvg", Math.Truncate(ForecastedTempAvg * 100) / 100},
                {"SoilTempAvgLast3Days", Math.Truncate(soilTempAvg * 100) / 100}
            };

            // Add daily rainfall data to the dictionary
            dict["DailyRainfall"] = dailyRainfall;

            if ( dict == null)
            {
                log.LogWarning("dict is null!");
            }

            var json = System.Text.Json.JsonSerializer.Serialize(dict);
            Console.WriteLine(json);

            // Output
            String output = $@"
            Date                                         {DateTime.Now}
            Rainfall amount last 7 days:                 {Math.Truncate(RainfallPast7Days * 100) / 100}""
            Expected rainfall in the next 7 days:        {Math.Truncate(forecastedRainAmount * 100) / 100}
            Max Temp Next 7 Day Average                  {Math.Truncate(ForecastedTempAvg * 100) / 100}
            Soil Temperatures Averages the last 3 days:  {Math.Truncate(soilTempAvg * 100) / 100}
            ";

            Console.WriteLine(json);

            return new OkObjectResult(json);

        }
    }
}