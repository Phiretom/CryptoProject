namespace CryptoAnalysis.App.Services
{
    public static class StatisticsService
    {
        public static double CalculateMedian(List<double> values)
        {
            if (values == null || values.Count == 0) return 0;
            var sorted = values.OrderBy(v => v).ToList();
            int count = sorted.Count;
            if (count % 2 == 0)
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
            return sorted[count / 2];
        }

        public static double CalculateStandardDeviation(List<double> values, double mean)
        {
            if (values.Count <= 1) return 0;
            double sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumOfSquares / (values.Count - 1));
        }

        public static List<double> CalculateDailyReturns(List<double> prices)
        {
            var returns = new List<double>();
            for (int i = 1; i < prices.Count; i++)
            {
                if (prices[i - 1] == 0) continue;
                returns.Add(Math.Log(prices[i] / prices[i - 1]));
            }
            return returns;
        }

        public static double CalculatePearsonCorrelation(List<double> x, List<double> y)
        {
            int commonCount = Math.Min(x.Count, y.Count);
            if (commonCount == 0) return 0;

            var valX = x.Take(commonCount).ToList();
            var valY = y.Take(commonCount).ToList();

            double meanX = valX.Average();
            double meanY = valY.Average();

            double numerator = valX.Zip(valY, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
            double denominatorX = valX.Sum(xi => Math.Pow(xi - meanX, 2));
            double denominatorY = valY.Sum(yi => Math.Pow(yi - meanY, 2));

            if (denominatorX == 0 || denominatorY == 0) return 0;

            return numerator / Math.Sqrt(denominatorX * denominatorY);
        }
    }
}