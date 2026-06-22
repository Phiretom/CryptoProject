using CryptoAnalysis.App.Infrastructure;
using CryptoAnalysis.App.Services;
using CryptoAnalysis.App.Presentation;

namespace CryptoAnalysis.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            using var context = new CryptoDbContext();
            await context.Database.EnsureCreatedAsync();

            using var httpClient = new HttpClient();
            var etl = new EtlService(httpClient, context);

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "--sync":
                        Console.WriteLine("Синхронизация базы данных");
                        await etl.LoadHistoricalDataAsync("bitcoin", 30);
                        await etl.LoadHistoricalDataAsync("ethereum", 30);
                        await etl.LoadCurrentDataAsync(["bitcoin", "ethereum"]);

                        Console.WriteLine("Расчет аналитических показателей и волатильности");
                        var analysis = new StatisticsSaver(context);
                        await analysis.SaveVolatilityAsync("bitcoin", 30);
                        await analysis.SaveVolatilityAsync("ethereum", 30);
                        await analysis.SaveCorrelationAsync("bitcoin", "ethereum", 30);
                        Console.WriteLine("Синхронизация завершена.");
                        break;

                    case "--report":
                        await HandleReport(args, context);
                        break;

                    default:
                        PrintHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в работе приложения: {ex.Message}");
            }
        }

        private static async Task HandleReport(string[] args, CryptoDbContext context)
        {
            string? reportType = GetArgValue(args, "--report");
            string? id = GetArgValue(args, "--id");
            string? ids = GetArgValue(args, "--ids");
            int days = int.TryParse(GetArgValue(args, "--days"), out var d) ? d : 30;
            string outputPath = GetArgValue(args, "--out") ?? "report.pdf";

            var reportService = new ReportService(context);

            if (string.IsNullOrEmpty(reportType))
            {
                Console.WriteLine("Не указан тип отчета.");
                return;
            }

            switch (reportType.ToLower())
            {
                case "asset":
                    if (string.IsNullOrEmpty(id)) throw new ArgumentException("Укажите --id bitcoin");
                    await reportService.GenerateAssetReportAsync(id, days, outputPath);
                    break;

                case "compare":
                    if (string.IsNullOrEmpty(ids)) throw new ArgumentException("Укажите --ids bitcoin,ethereum");
                    await reportService.GenerateComparativeReportAsync(ids.Split(',').ToList(), days, outputPath);
                    break;

                case "correlation":
                    if (string.IsNullOrEmpty(ids)) throw new ArgumentException("Укажите --ids bitcoin,ethereum");
                    await reportService.GenerateCorrelationReportAsync(ids.Split(',').ToList(), days, outputPath);
                    break;

                case "anomalies":
                    if (string.IsNullOrEmpty(id)) throw new ArgumentException("Укажите --id bitcoin");
                    await reportService.GenerateAnomaliesReportAsync(id, days, outputPath);
                    break;

                default:
                    Console.WriteLine($"Неподдерживаемый тип отчета: {reportType}");
                    break;
            }
        }

        private static string? GetArgValue(string[] args, string argName)
        {
            int index = Array.IndexOf(args, argName);
            if (index != -1 && index + 1 < args.Length)
            {
                return args[index + 1];
            }
            return null;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Доступные аргументы:");
            Console.WriteLine("  --sync (загружает данные в базу данных)");
            Console.WriteLine("  --report [тип] [параметры] (создаёт отчёт выбраного типа для выбранных активов за период,");
            Console.WriteLine("дополнительно можно указать путь сохранения файла)");
            Console.WriteLine("\nВарианты запуска:");
            Console.WriteLine("  --report asset --id bitcoin --days 30 --out r.pdf");
            Console.WriteLine("  --report compare --ids bitcoin,ethereum --days 30 --out r.pdf");
            Console.WriteLine("  --report correlation --ids bitcoin,ethereum --days 30");
            Console.WriteLine("  --report anomalies --id bitcoin --days 30");
        }
    }
}