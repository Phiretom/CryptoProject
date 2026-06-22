using CryptoAnalysis.App.Domain;
using CryptoAnalysis.App.Services;
using ScottPlot;

namespace CryptoAnalysis.App.Presentation
{
    public static class VisualizationService
    {
        public static string GeneratePriceChart(List<DateTime> dates, List<double> prices, string assetName)
        {
            var plt = new Plot();
            plt.Title($"Динамика цены {assetName.ToUpper()}");
            double[] xs = [.. dates.Select(d => d.ToOADate())];
            double[] ys = [.. prices];

            var sig = plt.Add.Scatter(xs, ys);
            sig.LineWidth = 2;
            plt.Axes.DateTimeTicksBottom();

            string filePath = Path.Combine(Path.GetTempPath(), $"{assetName}_price.png");
            plt.SavePng(filePath, 600, 300);
            return filePath;
        }

        public static string GenerateVolumeChart(List<DateTime> dates, List<double> volumes, string assetName)
        {
            var plt = new Plot();
            plt.Title($"Объемы торгов {assetName.ToUpper()}");
            double[] xs = [.. dates.Select(d => d.ToOADate())];
            double[] ys = [.. volumes];

            var sig = plt.Add.Scatter(xs, ys);
            sig.Color = Colors.Green;
            plt.Axes.DateTimeTicksBottom();

            string filePath = Path.Combine(Path.GetTempPath(), $"{assetName}_volume.png");
            plt.SavePng(filePath, 600, 300);
            return filePath;
        }

        public static string GenerateNormalizedComparisonChart(Dictionary<string, List<MarketData>> comparisonData)
        {
            var plt = new Plot();
            plt.Title("Сравнение доходности активов");

            foreach (var kv in comparisonData)
            {
                var history = kv.Value;
                if (history.Count == 0) continue;
                double firstPrice = (double)history.First().ClosePrice;
                if (firstPrice == 0) continue;

                double[] xs = [.. history.Select(d => d.Timestamp.ToOADate())];
                double[] ys = [.. history.Select(d => (double)d.ClosePrice / firstPrice * 100)];

                var sp = plt.Add.Scatter(xs, ys);
                sp.LegendText = kv.Key.ToUpper();
            }

            plt.Axes.DateTimeTicksBottom();
            plt.ShowLegend(Alignment.UpperLeft);

            string filePath = Path.Combine(Path.GetTempPath(), "comparison_prices.png");
            plt.SavePng(filePath, 600, 300);
            return filePath;
        }

        public static string GenerateBarChart(List<(string Key, double Value)> data, string title)
        {
            var plt = new Plot();
            plt.Title(title);

            double[] values = [.. data.Select(d => d.Value)];
            var bars = plt.Add.Bars(values);

            for (int i = 0; i < data.Count; i++)
            {
                bars.Bars[i].Label = data[i].Key.ToUpper();
            }

            string filePath = Path.Combine(Path.GetTempPath(), "bar_chart.png");
            plt.SavePng(filePath, 600, 300);
            return filePath;
        }

        public static string GenerateCorrelationHeatmap(double[,] correlationMatrix, string[] labels)
        {
            var plt = new Plot();
            plt.Title("Матрица корреляции");

            var hm = plt.Add.Heatmap(correlationMatrix);
            hm.Colormap = new ScottPlot.Colormaps.Viridis();

            if (labels != null && labels.Length > 0)
            {
                Tick[] xTicks = labels.Select((lbl, i) => new Tick(i, lbl.ToUpper())).ToArray();
                plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(xTicks);

                Tick[] yTicks = labels.Select((lbl, i) => new Tick(i, lbl.ToUpper())).ToArray();
                plt.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual(yTicks);
            }
            plt.HideGrid();
            string filePath = Path.Combine(Path.GetTempPath(), "correlation_heatmap.png");
            plt.SavePng(filePath, 450, 450);
            return filePath;
        }

        public static string GenerateAnomalyChart(List<(DateTime Time, double Price)> history, List<AnomalyEvent> anomalies, string assetName)
        {
            var plt = new Plot();
            plt.Title($"Поиск аномалий: {assetName.ToUpper()}");

            double[] xs = [.. history.Select(h => h.Time.ToOADate())];
            double[] ys = [.. history.Select(h => h.Price)];
            plt.Add.Scatter(xs, ys);

            if (anomalies.Count != 0)
            {
                double[] axs = [.. anomalies.Select(a => a.Timestamp.ToOADate())];
                double[] ays = [.. anomalies.Select(a => a.Value)];
                var markers = plt.Add.Markers(axs, ays);
                markers.Color = Colors.Red;
                markers.MarkerStyle.Size = 10;
            }

            plt.Axes.DateTimeTicksBottom();
            string filePath = Path.Combine(Path.GetTempPath(), $"{assetName}_anomalies.png");
            plt.SavePng(filePath, 600, 300);
            return filePath;
        }
    }
}