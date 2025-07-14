namespace SPA.Models
{
    public class WeatherModel
    {
        public DateTime Date { get; set; }
        public double RainfallLast7Days { get; set; }
        public double RainfallNext7Days { get; set; }
        public double MaxTempNext7DayAvg { get; set; }
        public double SoilTempAvgLast3Days { get; set; }
    }
}
