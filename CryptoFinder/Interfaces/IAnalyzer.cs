using CryptoFinder.Models;

namespace CryptoFinder.Interfaces;

/// <summary>
/// Kripto para analizi ve skorlama için arayüz.
/// </summary>
public interface IAnalyzer
{
    /// <summary>
    /// Kripto para adaylarını analiz eder ve skorlanmış sonuçları döndürür.
    /// </summary>
    /// <param name="baseSymbols">Analiz edilecek temel semboller</param>
    /// <param name="interval">Analiz için zaman aralığı</param>
    /// <param name="limit">Geçmiş veri noktası sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Skora göre sıralanmış analiz adayları listesi</returns>
    Task<List<AnalysisCandidate>> AnalyzeCandidatesAsync(
        IEnumerable<string> baseSymbols,
        string interval,
        int limit,
        CancellationToken cancellationToken = default);
}
