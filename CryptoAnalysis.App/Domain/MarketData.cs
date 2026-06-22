namespace CryptoAnalysis.App.Domain
{
    public class MarketData
    {
        public int Id { get; set; }
        public string AssetId { get; set; } = null!;
        public Asset Asset { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal MaximumPrice { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal Volume { get; set; }
        public decimal MarketCap { get; set; }
    }
}