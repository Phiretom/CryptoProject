namespace CryptoAnalysis.App.Services
{
    public static class AnomalyService
    {
        public static List<AnomalyEvent> DetectPriceAnomalies(List<(DateTime Time, double Price)> data, int windowSize = 7, double thresholdZ = 2.2)
        {
            var anomalies = new List<AnomalyEvent>();

            for (int i = windowSize; i < data.Count; i++)
            {
                var window = data.Skip(i - windowSize).Take(windowSize).Select(d => d.Price).ToList();
                double mean = window.Average();
                double stdDev = StatisticsService.CalculateStandardDeviation(window, mean);

                if (stdDev == 0) continue;

                double currentPrice = data[i].Price;
                double zScore = Math.Abs(currentPrice - mean) / stdDev;

                if (zScore > thresholdZ)
                {
                    anomalies.Add(new AnomalyEvent
                    {
                        Timestamp = data[i].Time,
                        Value = currentPrice,
                        ExpectedValue = mean,
                        Deviation = zScore,
                        Description = $"Резкое отклонение цены от скользящего среднего, равного {windowSize}. Текущая: {currentPrice:F2}, Ожидаемая: {mean:F2} (Z-Score: {zScore:F2})"
                    });
                }
            }
            return anomalies;
        }
    }
}