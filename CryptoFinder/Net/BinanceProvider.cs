using CryptoFinder.Config;
using CryptoFinder.Interfaces;
using CryptoFinder.Models;
using CryptoFinder.Util;
using System.Globalization;
using System.Text.Json;

namespace CryptoFinder.Net;

/// <summary>
/// Emir defteri ve işlem verileri için Binance API sağlayıcı uygulaması.
/// </summary>
public class BinanceProvider : IOrderBookProvider
{
    private readonly HttpService _httpService;

    public BinanceProvider(HttpService httpService)
    {
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
    }

    /// <inheritdoc />
    public async Task<HashSet<string>> GetTradableSymbolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{Settings.BINANCE_BASE_URL}/exchangeInfo";
            var json = await _httpService.GetStringWithRetryAsync(url, cancellationToken);
            var info = JsonSerializer.Deserialize<BinanceExchangeInfo>(json, GetJsonOptions());
            
            if (info?.Symbols == null)
                return new HashSet<string>();

            return info.Symbols
                .Where(s => string.Equals(s.Status, "TRADING", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Symbol.ToUpperInvariant())
                .ToHashSet();
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, (decimal bid, decimal ask)>> GetAllBookTickersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{Settings.BINANCE_BASE_URL}/ticker/bookTicker";
            var json = await _httpService.GetStringWithRetryAsync(url, cancellationToken);
            var tickers = JsonSerializer.Deserialize<List<BinanceBookTicker>>(json, GetJsonOptions());

            if (tickers == null)
                return new Dictionary<string, (decimal, decimal)>();

            var result = new Dictionary<string, (decimal, decimal)>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var ticker in tickers)
            {
                if (decimal.TryParse(ticker.BidPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var bid) &&
                    decimal.TryParse(ticker.AskPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var ask) &&
                    bid > 0 && ask > 0 && ask >= bid)
                {
                    result[ticker.Symbol.ToUpperInvariant()] = (bid, ask);
                }
            }
            
            return result;
        }
        catch
        {
            return new Dictionary<string, (decimal, decimal)>();
        }
    }

    /// <inheritdoc />
    public async Task<decimal?> GetOrderBookDepthAsync(string symbol, int limit = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{Settings.BINANCE_BASE_URL}/depth?symbol={symbol}&limit={limit}";
            var json = await _httpService.GetStringWithRetryAsync(url, cancellationToken);
            var orderBook = JsonSerializer.Deserialize<BinanceOrderBook>(json, GetJsonOptions());
            
            return ComputeDepthUsdWithin1Percent(orderBook);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string? PickPreferredPair(string baseSymbol, HashSet<string> tradableSymbols)
    {
        var candidates = new[]
        {
            (baseSymbol + "USDT").ToUpperInvariant(),
            (baseSymbol + "USD").ToUpperInvariant(),
            (baseSymbol + "USDC").ToUpperInvariant(),
            (baseSymbol + "TRY").ToUpperInvariant()
        };
        
        return candidates.FirstOrDefault(tradableSymbols.Contains);
    }

    /// <inheritdoc />
    public decimal? ComputeSpreadPercentage((decimal bid, decimal ask) bidAsk)
    {
        var (bid, ask) = bidAsk;
        var mid = (ask + bid) / 2m;
        
        if (mid <= 0) 
            return null;
            
        return (ask - bid) / mid * 100m; // Percentage
    }

    /// <summary>
    /// Emir defteri verilerinden orta fiyatın ±1% içindeki USD derinliğini hesaplar.
    /// </summary>
    /// <param name="orderBook">Emir defteri verisi</param>
    /// <returns>USD cinsinden toplam derinlik veya geçersizse null</returns>
    private static decimal? ComputeDepthUsdWithin1Percent(BinanceOrderBook? orderBook)
    {
        if (orderBook?.Bids == null || orderBook.Asks == null || 
            orderBook.Bids.Count == 0 || orderBook.Asks.Count == 0)
            return null;

        // En iyi bid/ask'i al
        if (!decimal.TryParse(orderBook.Bids[0][0], NumberStyles.Any, CultureInfo.InvariantCulture, out var bestBid))
            return null;
        if (!decimal.TryParse(orderBook.Asks[0][0], NumberStyles.Any, CultureInfo.InvariantCulture, out var bestAsk))
            return null;
        if (bestBid <= 0 || bestAsk <= 0 || bestAsk < bestBid)
            return null;

        var mid = (bestAsk + bestBid) / 2m;
        var lower = mid * (1m - (decimal)(Settings.DEPTH_PERCENTAGE / 100.0));
        var upper = mid * (1m + (decimal)(Settings.DEPTH_PERCENTAGE / 100.0));

        decimal bidsUsd = 0m, asksUsd = 0m;

        // BID'ler: fiyat >= alt && fiyat <= orta
        foreach (var level in orderBook.Bids)
        {
            if (!decimal.TryParse(level[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                continue;
            if (!decimal.TryParse(level[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
                continue;
            if (price < lower) break; // Bid'ler azalan fiyat sırasında gelir
            if (price <= mid) bidsUsd += price * quantity;
        }

        // ASK'ler: fiyat <= üst && fiyat >= orta
        foreach (var level in orderBook.Asks)
        {
            if (!decimal.TryParse(level[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                continue;
            if (!decimal.TryParse(level[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
                continue;
            if (price > upper) break; // Ask'ler artan fiyat sırasında gelir
            if (price >= mid) asksUsd += price * quantity;
        }

        return bidsUsd + asksUsd; // USD cinsinden toplam ±1% derinlik
    }

    /// <summary>
    /// Tutarlı ayrıştırma için JSON serileştirici seçeneklerini alır.
    /// </summary>
    /// <returns>Yapılandırılmış JsonSerializerOptions</returns>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
