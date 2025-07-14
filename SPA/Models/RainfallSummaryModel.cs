using SPA.Models;

public class RainfallSummaryModel
{
    public List<RainfallModel> History { get; set; } = new();
    public List<RainfallModel> Forecast { get; set; } = new();
}