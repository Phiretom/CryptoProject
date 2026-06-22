using System.Text.Json.Serialization;

namespace CryptoAnalysis.App.Services
{
    public class CoinGeckoChartResponse
    {
        [JsonPropertyName("prices")]
        public List<List<double>> Prices { get; set; } = null!;

        [JsonPropertyName("market_caps")]
        public List<List<double>> MarketCaps { get; set; } = null!;

        [JsonPropertyName("total_volumes")]
        public List<List<double>> TotalVolumes { get; set; } = null!;
    }
}