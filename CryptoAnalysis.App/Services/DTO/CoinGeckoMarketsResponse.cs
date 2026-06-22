using System.Text.Json.Serialization;

namespace CryptoAnalysis.App.Services
{
    public class CoinGeckoMarketsResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("current_price")]
        public double? CurrentPrice { get; set; }

        [JsonPropertyName("market_cap")]
        public double? MarketCap { get; set; }

        [JsonPropertyName("total_volume")]
        public double? TotalVolume { get; set; }

        [JsonPropertyName("high_24h")]
        public double? High24h { get; set; }

        [JsonPropertyName("low_24h")]
        public double? Low24h { get; set; }

        [JsonPropertyName("price_change_24h")]
        public double? PriceChange24h { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime? LastUpdated { get; set; }
    }
}