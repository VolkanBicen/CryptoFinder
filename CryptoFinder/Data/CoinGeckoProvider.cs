using CryptoFinder.Config;
using CryptoFinder.Interfaces;
using CryptoFinder.Models;
using CryptoFinder.Util;
using System.Text.Json;

namespace CryptoFinder.Data;

/// <summary>
/// Piyasa değeri verileri için CoinGecko API sağlayıcı uygulaması.
/// </summary>
public class CoinGeckoProvider : IMarketCapProvider
{
    private readonly HttpService _httpService;

    public CoinGeckoProvider(HttpService httpService)
    {
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
    }

    /// <inheritdoc />
    public async Task<List<MarketCapResponse>> GetMarketCapDataAsync(int page, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{Settings.COINGECKO_BASE_URL}/coins/markets?vs_currency=usd&order=market_cap_desc&per_page={Settings.COINS_PER_PAGE}&page={page}&sparkline=false";
            var json = await _httpService.GetStringWithRetryAsync(url, cancellationToken);
            var responses = JsonSerializer.Deserialize<List<MarketCapResponse>>(json, GetJsonOptions());
            
            return responses ?? new List<MarketCapResponse>();
        }
        catch
        {
            return new List<MarketCapResponse>();
        }
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
