using CryptoAnalysis.App.Domain;
using CryptoAnalysis.App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CryptoAnalysis.App.Services
{
    public class StatisticsSaver
    {
        private readonly CryptoDbContext _context;

        public StatisticsSaver(CryptoDbContext context)
        {
            _context = context;
        }

        public async Task SaveVolatilityAsync(string assetId, int days)
        {
            var data = await _context.MarketDataPoints
                .Where(m => m.AssetId == assetId)
                .OrderByDescending(m => m.Timestamp)
                .Take(days)
                .OrderBy(m => m.Timestamp)
                .Select(m => (double)m.ClosePrice)
                .ToListAsync();

            if (data.Count < 2) return;

            var returns = StatisticsService.CalculateDailyReturns(data);
            double dailyVol = StatisticsService.CalculateStandardDeviation(returns, returns.Average());

            double weeklyVol = dailyVol * Math.Sqrt(7);
            double monthlyVol = dailyVol * Math.Sqrt(30);

            var metric = new VolatilityMetrics
            {
                AssetId = assetId,
                CalculatedAt = DateTime.UtcNow,
                DailyVolatility = dailyVol,
                WeeklyVolatility = weeklyVol,
                MonthlyVolatility = monthlyVol
            };

            _context.VolatilityMetrics.Add(metric);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Метрика волатильности для {assetId} записана в БД.");
        }

        public async Task SaveCorrelationAsync(string assetId1, string assetId2, int days)
        {
            var prices1 = await _context.MarketDataPoints
                .Where(m => m.AssetId == assetId1)
                .OrderByDescending(m => m.Timestamp)
                .Take(days)
                .OrderBy(m => m.Timestamp)
                .Select(m => (double)m.ClosePrice)
                .ToListAsync();

            var prices2 = await _context.MarketDataPoints
                .Where(m => m.AssetId == assetId2)
                .OrderByDescending(m => m.Timestamp)
                .Take(days)
                .OrderBy(m => m.Timestamp)
                .Select(m => (double)m.ClosePrice)
                .ToListAsync();

            if (prices1.Count < 2 || prices2.Count < 2) return;

            var r1 = StatisticsService.CalculateDailyReturns(prices1);
            var r2 = StatisticsService.CalculateDailyReturns(prices2);

            double correlation = StatisticsService.CalculatePearsonCorrelation(r1, r2);

            var metric = new CorrelationMetrics
            {
                AssetId1 = assetId1,
                AssetId2 = assetId2,
                CalculatedAt = DateTime.UtcNow,
                CorrelationValue = correlation,
                PeriodDays = days
            };

            _context.CorrelationMetrics.Add(metric);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Метрика корреляции {assetId1}-{assetId2} записана в БД: {correlation:F4}");
        }
    }
}