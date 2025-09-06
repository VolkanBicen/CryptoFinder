namespace CryptoFinder.Interfaces;

/// <summary>
/// Emir defteri ve işlem veri sağlayıcıları için arayüz.
/// </summary>
public interface IOrderBookProvider
{
    /// <summary>
    /// Borsadan tüm işlem gören sembolleri alır.
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>İşlem gören semboller kümesi</returns>
    Task<HashSet<string>> GetTradableSymbolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm semboller için tüm book ticker'ları (bid/ask fiyatları) alır.
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Sembolden (bid, ask) fiyatlarına sözlük</returns>
    Task<Dictionary<string, (decimal bid, decimal ask)>> GetAllBookTickersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir sembol için emir defteri derinliğini alır.
    /// </summary>
    /// <param name="symbol">İşlem çifti sembolü</param>
    /// <param name="limit">Emir defteri derinlik limiti</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>USD cinsinden emir defteri derinliği</returns>
    Task<decimal?> GetOrderBookDepthAsync(string symbol, int limit = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir temel sembol için tercih edilen işlem çiftini seçer.
    /// </summary>
    /// <param name="baseSymbol">Temel sembol (örn. BTC)</param>
    /// <param name="tradableSymbols">Mevcut işlem gören semboller kümesi</param>
    /// <returns>Tercih edilen işlem çifti veya mevcut değilse null</returns>
    string? PickPreferredPair(string baseSymbol, HashSet<string> tradableSymbols);

    /// <summary>
    /// Bid/ask fiyatlarından spread yüzdesini hesaplar.
    /// </summary>
    /// <param name="bidAsk">Bid ve ask fiyatları</param>
    /// <returns>Spread yüzdesi veya geçersizse null</returns>
    decimal? ComputeSpreadPercentage((decimal bid, decimal ask) bidAsk);
}
