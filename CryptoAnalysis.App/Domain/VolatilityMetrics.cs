namespace CryptoAnalysis.App.Domain
{
    public class VolatilityMetrics
    {
        public int Id { get; set; }
        public string AssetId { get; set; } = null!;
        public DateTime CalculatedAt { get; set; }
        public double DailyVolatility { get; set; }
        public double WeeklyVolatility { get; set; }
        public double MonthlyVolatility { get; set; }
    }
}