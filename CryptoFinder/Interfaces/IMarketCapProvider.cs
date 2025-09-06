using CryptoFinder.Models;

namespace CryptoFinder.Interfaces;

/// <summary>
/// Piyasa değeri veri sağlayıcıları için arayüz.
/// </summary>
public interface IMarketCapProvider
{
    /// <summary>
    /// Belirli bir sayfa için piyasa değeri verilerini alır.
    /// </summary>
    /// <param name="page">Alınacak sayfa numarası</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Piyasa değeri yanıtları listesi</returns>
    Task<List<MarketCapResponse>> GetMarketCapDataAsync(int page, CancellationToken cancellationToken = default);
}
