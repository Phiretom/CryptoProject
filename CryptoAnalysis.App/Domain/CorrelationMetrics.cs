namespace CryptoAnalysis.App.Domain
{
    public class CorrelationMetrics
    {
        public int Id { get; set; }
        public string AssetId1 { get; set; } = null!;
        public string AssetId2 { get; set; } = null!;
        public DateTime CalculatedAt { get; set; }
        public double CorrelationValue { get; set; }
        public int PeriodDays { get; set; }
    }
}