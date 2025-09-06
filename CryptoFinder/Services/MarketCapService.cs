using CryptoFinder.Config;
using CryptoFinder.Data;
using CryptoFinder.Interfaces;
using CryptoFinder.Models;

namespace CryptoFinder.Services;

/// <summary>
/// Piyasa değeri verilerine filtreleme kurallarını uygulayan piyasa değeri servisi.
/// </summary>
public class MarketCapService
{
    private readonly IMarketCapProvider _marketCapProvider;
    private readonly IOrderBookProvider _orderBookProvider;

    public MarketCapService(IMarketCapProvider marketCapProvider, IOrderBookProvider orderBookProvider)
    {
        _marketCapProvider = marketCapProvider ?? throw new ArgumentNullException(nameof(marketCapProvider));
        _orderBookProvider = orderBookProvider ?? throw new ArgumentNullException(nameof(orderBookProvider));
    }

    /// <summary>
    /// Belirli bir sayfa için filtrelenmiş temel sembolleri alır.
    /// </summary>
    /// <param name="page">Alınacak sayfa numarası</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Filtrelenmiş temel semboller listesi</returns>
    public async Task<List<string>> GetFilteredSymbolsAsync(int page, CancellationToken cancellationToken = default)
    {
        try
        {
            // Piyasa değeri verilerini al
            var marketCapData = await _marketCapProvider.GetMarketCapDataAsync(page, cancellationToken);
            if (marketCapData.Count == 0)
                return new List<string>();

            // Binance verilerini al (verimlilik için tek çağrılar)
            var tradableSymbols = await _orderBookProvider.GetTradableSymbolsAsync(cancellationToken);
            var bookTickers = await _orderBookProvider.GetAllBookTickersAsync(cancellationToken);

            var filteredSymbols = new List<string>();
            var gmReader = new GetGlobalMetrics();
            var gm = await gmReader.FetchAsync(cancellationToken) ?? new GlobalMetrics();

            foreach (var item in marketCapData)
            {
                if (item == null) continue;

                // Stabil/sarılmış tokenları filtrele
                var name = (item.Name ?? "").ToUpperInvariant();
                if (name.Contains("WRAPPED") || name.Contains("PEG") || name.Contains("REBASE"))
                    continue;

                // Temel filtreleri uygula
                if (!PassesBasicFilters(item,gm))
                    continue;

                // Spread filtresini uygula
                var baseSymbol = (item.Symbol ?? "").ToUpperInvariant();
                var pair = baseSymbol + "USDT";

                // bookTicker yoksa ele
                if (!bookTickers.TryGetValue(pair, out var bidAsk))
                    continue;

                // spread > threshold ise ele
                var spread = _orderBookProvider.ComputeSpreadPercentage(bidAsk);
                if (spread == null || spread > Settings.SPREAD_MAX_PCT)
                    continue;

              
                // Derinlik filtresini uygula (sadece kısa listeye alınan semboller için)
                //var depth = await _orderBookProvider.GetOrderBookDepthAsync(pair, Settings.ORDERBOOK_LIMIT, cancellationToken);
                //if (depth == null || depth < Settings.DEPTH_MIN_USD)
                //    continue;

                filteredSymbols.Add(baseSymbol);
            }

            return filteredSymbols;
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Piyasa değeri verilerinin temel filtreleme kurallarını geçip geçmediğini kontrol eder.
    /// </summary>
    /// <param name="item">Piyasa değeri yanıt öğesi</param>
    /// <returns>Temel filtreleri geçerse true</returns>
    private static bool PassesBasicFilters(MarketCapResponse item,GlobalMetrics gm)
    {
        // Piyasa değeri ve hacim pozitif olmalı
        if (item.MarketCap <= 0 || item.TotalVolume <= 0)
            return false;

        // FDV mevcut olmalı
        var fdv = item.FullyDilutedValuation.GetValueOrDefault(0);
        if (fdv == 0)
            return false;

        // Oranları hesapla
        var fdvMc = (decimal)fdv / item.MarketCap;
        var turnover = (decimal)item.TotalVolume / item.MarketCap;
        var rank = item.MarketCapRank ?? int.MaxValue;

        decimal mcMin = Settings.MC_MIN;

        if (gm.BtcDominancePct.HasValue)
        {
            if (gm.BtcDominancePct.Value >= 50m)
                mcMin = 100_000_000m;   // ayı/temkin: sıkı
            else
                mcMin = 50_000_000m;    // boğa/risk-on: esnek
        }

        // Eşik değerlerini uygula
        return item.MarketCap >= mcMin &&
               item.TotalVolume >= Settings.VOL24H_MIN &&
               fdvMc <= Settings.FDV_MC_MAX &&
               turnover >= Settings.TURNOVER_MIN &&
               rank <= Settings.RANK_MAX;
    }
}
