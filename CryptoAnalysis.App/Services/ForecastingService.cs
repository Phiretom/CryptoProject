namespace CryptoAnalysis.App.Services
{
    public static class ForecastingService
    {
        public static List<(DateTime Time, double Price)> PredictLinearRegression(List<(DateTime Time, double Price)> history, int stepsForward)
        {
            int n = history.Count;
            if (n < 2) return [];

            double[] x = [.. Enumerable.Range(0, n).Select(i => (double)i)];
            double[] y = [.. history.Select(h => h.Price)];

            double sumX = x.Sum();
            double sumY = y.Sum();
            double sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
            double sumX2 = x.Sum(xi => xi * xi);

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;

            var predictions = new List<(DateTime Time, double Price)>();
            DateTime lastTime = history.Last().Time;

            for (int i = 1; i <= stepsForward; i++)
            {
                double nextIndex = n + i - 1;
                double predictedValue = slope * nextIndex + intercept;
                DateTime nextTime = lastTime.AddDays(i);

                predictions.Add((nextTime, Math.Max(0, predictedValue)));
            }

            return predictions;
        }
    }
}