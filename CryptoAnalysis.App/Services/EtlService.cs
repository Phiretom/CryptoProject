using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using CryptoAnalysis.App.Domain;
using CryptoAnalysis.App.Infrastructure;

namespace CryptoAnalysis.App.Services
{
    public class EtlService
    {
        private readonly HttpClient _httpClient;
        private readonly CryptoDbContext _context;

        public EtlService(HttpClient httpClient, CryptoDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoMarketAnalyzer/1.0");
        }

        public async Task LoadHistoricalDataAsync(string assetId, int days)
        {
            try
            {
                string ohlcUrl = $"https://api.coingecko.com/api/v3/coins/{assetId}/ohlc?vs_currency=usd&days={days}";
                var ohlcResponse = await _httpClient.GetFromJsonAsync<List<List<double>>>(ohlcUrl);
                if (ohlcResponse == null || ohlcResponse.Count == 0)
                {
                    Console.WriteLine($"Не удалось получить исторические котировки для актива - {assetId}");
                    return;
                }

                string chartUrl = $"https://api.coingecko.com/api/v3/coins/{assetId}/market_chart?vs_currency=usd&days={days}";
                var chartResponse = await _httpClient.GetFromJsonAsync<CoinGeckoChartResponse>(chartUrl);
                if (chartResponse == null)
                {
                    Console.WriteLine($"Не удалось получить исторические статистики объёмов и капитализации для актива - {assetId}");
                    return;
                }

                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == assetId);
                if (asset == null)
                {
                    asset = new Asset { Id = assetId, Symbol = assetId[..Math.Min(3, assetId.Length)], Name = assetId };
                    _context.Assets.Add(asset);
                    await _context.SaveChangesAsync();
                }

                var volumeMap = new Dictionary<DateTime, decimal>();
                var capMap = new Dictionary<DateTime, decimal>();

                if (chartResponse.TotalVolumes != null)
                {
                    foreach (var point in chartResponse.TotalVolumes)
                    {
                        var time = DateTimeOffset.FromUnixTimeMilliseconds((long)point[0]).UtcDateTime.Date;
                        volumeMap[time] = (decimal)point[1];
                    }
                }

                if (chartResponse.MarketCaps != null)
                {
                    foreach (var point in chartResponse.MarketCaps)
                    {
                        var time = DateTimeOffset.FromUnixTimeMilliseconds((long)point[0]).UtcDateTime.Date;
                        capMap[time] = (decimal)point[1];
                    }
                }

                foreach (var candle in ohlcResponse)
                {
                    long unixMs = (long)candle[0];
                    decimal open = (decimal)candle[1];
                    decimal max = (decimal)candle[2];
                    decimal min = (decimal)candle[3];
                    decimal close = (decimal)candle[4];

                    DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime;
                    DateTime dateKey = dateTime.Date;
                    decimal volume = volumeMap.GetValueOrDefault(dateKey, 0);
                    decimal marketCap = capMap.GetValueOrDefault(dateKey, 0);

                    if (close <= 0) continue;

                    var existing = await _context.MarketDataPoints
                        .FirstOrDefaultAsync(m => m.AssetId == assetId && m.Timestamp == dateTime);

                    if (existing == null)
                    {
                        _context.MarketDataPoints.Add(new MarketData
                        {
                            AssetId = assetId,
                            Timestamp = dateTime,
                            OpenPrice = open,
                            MaximumPrice = max,
                            MinimumPrice = min,
                            ClosePrice = close,
                            Volume = volume,
                            MarketCap = marketCap
                        });
                    }
                    else
                    {
                        existing.OpenPrice = open;
                        existing.MaximumPrice = max;
                        existing.MinimumPrice = min;
                        existing.ClosePrice = close;
                        if (volume > 0) existing.Volume = volume;
                        if (marketCap > 0) existing.MarketCap = marketCap;
                    }
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"Исторические данные, объемы и капитализация для {assetId} успешно загружены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Сбой обработки {assetId}: {ex.Message}");
            }
        }

        public async Task LoadCurrentDataAsync(List<string> assetIds)
        {
            string idsParam = string.Join(",", assetIds);
            string url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&ids={idsParam}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<CoinGeckoMarketsResponse>>(url);
                if (response == null || response.Count == 0)
                {
                    Console.WriteLine($"Не удалось получить актуальные данные по активам: {assetIds}.");
                    return;
                }

                foreach (var item in response)
                {
                    decimal currentPrice = (decimal)(item.CurrentPrice ?? 0);
                    decimal priceChange24h = (decimal)(item.PriceChange24h ?? 0);

                    decimal open = currentPrice - priceChange24h;
                    decimal max = (decimal)(item.High24h ?? item.CurrentPrice ?? 0);
                    decimal min = (decimal)(item.Low24h ?? item.CurrentPrice ?? 0);
                    decimal close = currentPrice;

                    decimal volume = (decimal)(item.TotalVolume ?? 0);
                    decimal marketCap = (decimal)(item.MarketCap ?? 0);

                    DateTime timestamp = item.LastUpdated ?? DateTime.UtcNow;

                    var actualRecord = new MarketData
                    {
                        AssetId = item.Id,
                        Timestamp = timestamp,
                        OpenPrice = open,
                        MaximumPrice = max,
                        MinimumPrice = min,
                        ClosePrice = close,
                        Volume = volume,
                        MarketCap = marketCap
                    };

                    var existing = await _context.MarketDataPoints
                        .FirstOrDefaultAsync(m => m.AssetId == item.Id && m.Timestamp == timestamp);

                    if (existing == null)
                    {
                        _context.MarketDataPoints.Add(actualRecord);
                    }
                    else
                    {
                        existing.OpenPrice = open;
                        existing.MaximumPrice = max;
                        existing.MinimumPrice = min;
                        existing.ClosePrice = close;
                        existing.Volume = volume;
                        existing.MarketCap = marketCap;
                    }
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("Актуальные котировки и рыночные метрики успешно обновлены в БД.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Сбой обновления текущих котировок: {ex.Message}");
            }
        }
    }
}