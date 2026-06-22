namespace CryptoAnalysis.App.Services
{
    public class AnomalyEvent
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public double ExpectedValue { get; set; }
        public double Deviation { get; set; }
        public string Description { get; set; } = null!;
    }
}