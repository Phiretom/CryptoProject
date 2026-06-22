using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CryptoAnalysis.App.Infrastructure;
using CryptoAnalysis.App.Domain;
using CryptoAnalysis.App.Services;

namespace CryptoAnalysis.App.Presentation
{
    public class ReportService
    {
        private readonly CryptoDbContext _context;

        public ReportService(CryptoDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task GenerateAssetReportAsync(string assetId, int days, string outputPath)
        {
            var data = await _context.MarketDataPoints
                .Where(m => m.AssetId == assetId)
                .OrderByDescending(m => m.Timestamp)
                .Take(days)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (data.Count == 0) return;

            var dates = data.Select(d => d.Timestamp).ToList();
            var prices = data.Select(d => (double)d.ClosePrice).ToList();
            var volumes = data.Select(d => (double)d.Volume).ToList();

            string priceChart = VisualizationService.GeneratePriceChart(dates, prices, assetId);
            string volumeChart = VisualizationService.GenerateVolumeChart(dates, volumes, assetId);

            double mean = prices.Average();
            double median = StatisticsService.CalculateMedian(prices);
            var storedVolatility = await _context.VolatilityMetrics
                .Where(v => v.AssetId == assetId)
                .OrderByDescending(v => v.CalculatedAt)
                .FirstOrDefaultAsync();

            double dailyVolatility = storedVolatility?.DailyVolatility ?? 0.0;
            double weeklyVolatility = storedVolatility?.WeeklyVolatility ?? 0.0;
            double monthlyVolatility = storedVolatility?.MonthlyVolatility ?? 0.0;

            var priceHistory = data.Select(d => (d.Timestamp, (double)d.ClosePrice)).ToList();
            var volumeHistory = data.Select(d => (d.Timestamp, (double)d.Volume)).ToList();

            var priceForecast = ForecastingService.PredictLinearRegression(priceHistory, 5);
            var volumeForecast = ForecastingService.PredictLinearRegression(volumeHistory, 5);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Header().Text($"Отчет по активу {assetId.ToUpper()}").Bold().FontSize(18).FontColor(Colors.Black);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Период анализа: {days} дней. Последняя зарегистрированная цена в $: {prices.Last():F2}");
                        col.Item().Text($"Средняя цена в $: {mean:F2} | Медиана: {median:F2}");
                        col.Item().Text($"Дневная волатильность: {dailyVolatility * 100:F3}%");
                        col.Item().Text($"Недельная волатильность: {weeklyVolatility * 100:F3}%");
                        col.Item().Text($"Месячная волатильность: {monthlyVolatility * 100:F3}%");
                        col.Item().Text("Динамика цены:").Bold();
                        col.Item().Image(priceChart);
                        col.Item().PageBreak();
                        col.Item().Text("Объемы торгов:").Bold();
                        col.Item().Image(volumeChart);
                        col.Item().Text("Прогноз линейной регрессии на 5 дней вперед").Bold().FontSize(14).FontColor(Colors.Black);
                        
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Black).Text("Дата прогноза").Bold();
                                header.Cell().Background(Colors.Black).Text("Прогнозируемая цена").Bold();
                                header.Cell().Background(Colors.Black).Text("Прогнозируемый объем").Bold();
                            });

                            for (int i = 0; i < priceForecast.Count; i++)
                            {
                                table.Cell().Text(priceForecast[i].Time.ToShortDateString());
                                table.Cell().Text($"{priceForecast[i].Price:F2} USD");
                                table.Cell().Text($"{volumeForecast[i].Price:N0} USD");
                            }
                        });
                    });
                });
            }).GeneratePdf(outputPath);
            Console.WriteLine($"Отчет по активу успешно сгенерирован: {outputPath}");
        }

        public async Task GenerateComparativeReportAsync(List<string> assetIds, int days, string outputPath)
        {
            var comparisonData = new Dictionary<string, List<MarketData>>();
            foreach (var id in assetIds)
            {
                var data = await _context.MarketDataPoints
                    .Where(m => m.AssetId == id)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(days)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();
                comparisonData[id] = data;
            }

            string compChart = VisualizationService.GenerateNormalizedComparisonChart(comparisonData);
            var latestCap = comparisonData.Select(kv => (kv.Key, (double)(kv.Value.LastOrDefault()?.MarketCap ?? 0))).ToList();
            string capChart = VisualizationService.GenerateBarChart(latestCap, "Сравнение капитализаций");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Header().Text("Сравнительный отчет").Bold().FontSize(18).FontColor(Colors.Black);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Сравнительный анализ за последние {days} дней для: {string.Join(", ", assetIds.Select(x => x.ToUpper()))}");
                        col.Item().Image(compChart);
                        col.Item().PageBreak();
                        col.Item().Image(capChart);
                    });
                });
            }).GeneratePdf(outputPath);
            Console.WriteLine($"Сравнительный отчет успешно сгенерирован: {outputPath}");
        }

        public async Task GenerateCorrelationReportAsync(List<string> assetIds, int days, string outputPath)
        {
            int n = assetIds.Count;
            double[,] correlationMatrix = new double[n, n];
            for (int i = 0; i < n; i++) correlationMatrix[i, i] = 1.0;

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    string id1 = assetIds[i];
                    string id2 = assetIds[j];

                    var metric = await _context.CorrelationMetrics
                        .Where(c => ((c.AssetId1 == id1 && c.AssetId2 == id2) || (c.AssetId1 == id2 && c.AssetId2 == id1))
                                    && c.PeriodDays == days)
                        .OrderByDescending(c => c.CalculatedAt)
                        .FirstOrDefaultAsync();

                    double correlationValue = metric?.CorrelationValue ?? 0.0;

                    correlationMatrix[i, j] = correlationValue;
                    correlationMatrix[j, i] = correlationValue;
                }
            }

            string heatmap = VisualizationService.GenerateCorrelationHeatmap(correlationMatrix, [.. assetIds]);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Header().Text("Анализ корреляции активов").Bold().FontSize(18).FontColor(Colors.Black);
                    page.Content().Column(col =>
                    {
                        col.Item().Text("Тепловая карта взаимосвязей:");
                        col.Item().Image(heatmap);
                    });
                });
            }).GeneratePdf(outputPath);
            Console.WriteLine($"Отчет по корреляциям успешно сгенерирован: {outputPath}");
        }

        public async Task GenerateAnomaliesReportAsync(string assetId, int days, string outputPath)
        {
            var data = await _context.MarketDataPoints
                .Where(m => m.AssetId == assetId)
                .OrderByDescending(m => m.Timestamp)
                .Take(days)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            var history = data.Select(d => (d.Timestamp, (double)d.ClosePrice)).ToList();
            var anomalies = AnomalyService.DetectPriceAnomalies(history, 7, 2.2);

            string anomalyChart = VisualizationService.GenerateAnomalyChart(history, anomalies, assetId);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Header().Text($"Реестр аномалий рынка: {assetId.ToUpper()}").Bold().FontSize(18).FontColor(Colors.Black);
                    page.Content().Column(col =>
                    {
                        col.Item().Image(anomalyChart);
                        col.Item().PageBreak();
                        col.Item().Text("Перечень обнаруженных аномальных отклонений:").Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(4);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Black).Text("Дата").Bold();
                                header.Cell().Background(Colors.Black).Text("Значение").Bold();
                                header.Cell().Background(Colors.Black).Text("Характеристики").Bold();
                            });

                            foreach (var anomaly in anomalies)
                            {
                                table.Cell().Text(anomaly.Timestamp.ToShortDateString());
                                table.Cell().Text($"{anomaly.Value:F2}");
                                table.Cell().Text(anomaly.Description);
                            }
                        });
                    });
                });
            }).GeneratePdf(outputPath);
            Console.WriteLine($"Отчет по аномалиям успешно сгенерирован: {outputPath}");
        }
    }
}