using CryptoFinder.Config;
using CryptoFinder.Data;
using CryptoFinder.Interfaces;
using CryptoFinder.Models;
using CryptoFinder.Net;
using CryptoFinder.Services;
using CryptoFinder.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace CryptoFinder;

/// <summary>
/// CryptoFinder uygulaması için ana program giriş noktası.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {

        // Bağımlılık enjeksiyonunu kur
        var host = CreateHostBuilder(args).Build();

        try
        {
            await RunAnalysisAsync(host.Services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Bağımlılık enjeksiyon yapılandırması ile host builder'ı oluşturur.
    /// </summary>
    /// <param name="args">Komut satırı argümanları</param>
    /// <returns>Yapılandırılmış host builder</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // HTTP istemci yapılandırması
                services.AddHttpClient("CryptoFinder", client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Settings.USER_AGENT);
                    client.Timeout = TimeSpan.FromSeconds(Settings.HTTP_TIMEOUT_SECONDS);
                });

                // Temel servisler
                services.AddSingleton<HttpService>();
                services.AddSingleton<IIndicatorService, IndicatorService>();
                services.AddSingleton<IMarketCapProvider, CoinGeckoProvider>();
                services.AddSingleton<IOrderBookProvider, BinanceProvider>();
                services.AddSingleton<MarketCapService>();
                services.AddSingleton<IAnalyzer, AnalyzerService>();
                // Günlükleme
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Warning);

                    // Ama kendi namespace’ini INFO’da tut
                    builder.AddFilter("CryptoFinder", LogLevel.Information);

                    // HttpClient & Microsoft loglarını bastır
                    builder.AddFilter("System.Net.Http", LogLevel.Warning);
                    builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
                    builder.AddFilter("Microsoft", LogLevel.Warning);
                });
            });

    
    /// <summary>
    /// Ana analiz iş akışını çalıştırır.
    /// </summary>
    /// <param name="services">Servis sağlayıcı</param>
    private static async Task RunAnalysisAsync(IServiceProvider services)
    {

        var marketCapService = services.GetRequiredService<MarketCapService>();
        var analyzer = services.GetRequiredService<IAnalyzer>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        var orderBookProvider = services.GetRequiredService<IOrderBookProvider>();
        var allCandidates = new List<AnalysisCandidate>();
        var noNewStreak = 0;

        logger.LogInformation("CryptoFinder analizi başlatılıyor...");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Settings.GLOBAL_TIMEOUT_SECONDS));
        var tradableSymbols = await orderBookProvider.GetTradableSymbolsAsync(cts.Token);

        // Erken durma ile sayfaları işle
        for (int page = 1; page <= Settings.MAX_PAGES; page++)
        {

            try
            {
                // Bu sayfa için filtrelenmiş sembolleri al
                var symbols = await marketCapService.GetFilteredSymbolsAsync(page, cts.Token);

                if (page >= 4 && symbols.Count == 0)
                {
                    logger.LogInformation("Sayfa {Page}: Sembol bulunamadı", page);
                    noNewStreak++;
                    if (noNewStreak >= Settings.EARLY_STOP_CONSECUTIVE_EMPTY)
                    {
                        logger.LogInformation("{Count} ardışık boş sayfa sonrası erken duruluyor", noNewStreak);
                        break;
                    }
                    continue;
                }

                // Adayları analiz et
                var candidates = await analyzer.AnalyzeCandidatesAsync(symbols, "4h", 400, cts.Token);
                var dedup = candidates
                            .GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
                            .Select(g => g.OrderByDescending(c => c.Score).First())
                            .OrderByDescending(c => c.Score)
                            .ToList();
                // 4) SADECE top-N için depth filtresi uygula
                int topNForDepth = 100; // ihtiyacına göre 50–150 arası


                var depthFiltered = await DepthFilterAsync(dedup, topNForDepth, orderBookProvider, cts.Token);


                // Yeni adayları bul (listemizde olmayan)
                var newCandidates = candidates
                    .Where(c => !allCandidates.Any(x => x.Symbol.Equals(c.Symbol, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                allCandidates.AddRange(newCandidates);

                logger.LogInformation("Sayfa {Page}: {TotalCandidates} aday, {NewCandidates} yeni, {TotalUnique} toplam benzersiz",
                    page, candidates.Count, newCandidates.Count, allCandidates.Count);

                // Yeni aday bulduysak seriyi sıfırla
                if (newCandidates.Count > 0)
                {
                    noNewStreak = 0;
                }
                else
                {
                    noNewStreak++;
                    if (noNewStreak >= Settings.EARLY_STOP_CONSECUTIVE_EMPTY)
                    {
                        logger.LogInformation("{Count} ardışık yeni aday olmayan sayfa sonrası erken duruluyor", noNewStreak);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Sayfa {Page} için işlem iptal edildi", page);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sayfa {Page} işlenirken hata", page);
                // Çökmek yerine bir sonraki sayfa ile devam et
            }
        }

        // Sonuçları tekrarları kaldır ve sırala
        var finalResults = allCandidates
            .GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(c => c.Score).First())
            .OrderByDescending(c => c.Score)
            .ToList();

        // Sonuçları göster
        DisplayResults(finalResults, logger);

        // İsteğe bağlı CSV dışa aktarma
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && args[1].Equals("--export-csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvPath = $"cryptofinder_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            await CsvExporter.ExportToCsvAsync(finalResults, csvPath, CancellationToken.None);
            logger.LogInformation("Sonuçlar {CsvPath} dosyasına dışa aktarıldı", csvPath);
        }
    }

    /// <summary>
    /// Analiz sonuçlarını biçimlendirilmiş tabloda gösterir.
    /// </summary>
    /// <param name="candidates">Gösterilecek analiz adayları</param>
    /// <param name="logger">Günlük örneği</param>
    private static void DisplayResults(List<AnalysisCandidate> candidates, ILogger logger)
    {
        if (candidates.Count == 0)
        {
            logger.LogInformation("Analiz adayı bulunamadı.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("=".PadRight(120, '='));
        Console.WriteLine("CRYPTOFINDER ANALİZ SONUÇLARI");
        Console.WriteLine("=".PadRight(120, '='));
        Console.WriteLine();

        // Başlık
        Console.WriteLine($"{"Symbol",-8} {"Score",6} {"Close",12} {"MACDdiff",10} {"StochK",8} {"StochD",8} {"RSdiff",8} {"Rules",-20}");

        Console.WriteLine("-".PadRight(120, '-'));

        // Veri satırları
        foreach (var candidate in candidates.Take(50))
        {
            var kPct = (candidate.StochK ?? 0) * 100.0;
            var dPct = (candidate.StochD ?? 0) * 100.0;


            var rules = string.Join("", new[]
            {
                candidate.PassedMacd       ? "M" : "",
                candidate.PassedSuperTrend ? "S" : "",
                candidate.PassedDonchian   ? "D" : "",
                candidate.NearUpperBand    ? "U" : "",
                candidate.PassedStoch      ? "K" : "",
                candidate.PassedRs         ? "R" : ""
            });
            Console.WriteLine(
                    $"{candidate.Symbol,-8} " +
                    $"{candidate.Score,6:F1} " +
                    $"{candidate.Close,12:F4} " +
                    $"{candidate.MacdDiff,10:F4} " +
                    $"{kPct,7:F1}% " +
                    $"{dPct,7:F1}% " +
                    $"{candidate.RsDiff,8:F3} " +
                    $"{rules,-8}"
                );
        }

        Console.WriteLine();
        Console.WriteLine($"Toplam aday: {candidates.Count}");
        Console.WriteLine($"Ortalama skor: {candidates.Average(c => c.Score):F1}");
        Console.WriteLine();
    }




    private static async Task<List<AnalysisCandidate>> DepthFilterAsync(
    List<AnalysisCandidate> rankedCandidates,
    int topN,
    IOrderBookProvider orderBookProvider,
    CancellationToken ct)
    {
        var top = rankedCandidates.Take(topN).ToList();
        var passed = new List<AnalysisCandidate>(top.Count);

        using var sem = new SemaphoreSlim(Settings.MAX_CONCURRENT_REQUESTS);

        var tasks = top.Select(async c =>
        {
            await sem.WaitAsync(ct);
            try
            {
                string pair = c.Symbol.ToUpperInvariant() + "USDT";

                var depthUsd = await orderBookProvider
                    .GetOrderBookDepthAsync(pair, Settings.ORDERBOOK_LIMIT, ct);

                if (depthUsd.HasValue && depthUsd.Value >= Settings.DEPTH_MIN_USD)
                {
                    lock (passed) passed.Add(c);
                }
            }
            catch { /* depth fail → ele */ }
            finally { sem.Release(); }
        }).ToList();

        await Task.WhenAll(tasks);
        return passed.OrderByDescending(x => x.Score).ToList();
    }



}