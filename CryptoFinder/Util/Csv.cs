using CryptoFinder.Models;

namespace CryptoFinder.Util;

/// <summary>
/// Analiz adayları için CSV dışa aktarma yardımcı sınıfı.
/// </summary>
public static class CsvExporter
{
    /// <summary>
    /// Analiz adaylarını CSV formatında dışa aktarır.
    /// </summary>
    /// <param name="candidates">Dışa aktarılacak analiz adayları</param>
    /// <param name="filePath">Çıktı dosya yolu</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    public static async Task ExportToCsvAsync(
        IEnumerable<AnalysisCandidate> candidates,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var lines = new List<string>();
        
        // Başlık ekle
        lines.Add("Symbol,Date,Close,EMA200,ADX,ATR%,RS30,RS30VsBTC,PassedAboveEMA,PassedADX,PassedRS,PassedATR,NearLowerBandBlocked,Score");
        
        // Veri satırlarını ekle
        foreach (var candidate in candidates)
        {
            var line = string.Join(",",
                EscapeCsvField(candidate.Symbol),
                candidate.Date.ToString("yyyy-MM-dd"),
                candidate.Close.ToString("F8"),
                candidate.Ema200?.ToString("F8") ?? "",
                candidate.Adx?.ToString("F2") ?? "",
                candidate.AtrPct?.ToString("F2") ?? "",
                candidate.Rs30?.ToString("F4") ?? "",
                candidate.Rs30VsBtc.ToString("F4"),
                candidate.PassedAboveEma.ToString().ToLower(),
                candidate.PassedAdx.ToString().ToLower(),
                candidate.PassedRs.ToString().ToLower(),
                candidate.PassedAtr.ToString().ToLower(),
                candidate.NearLowerBandBlocked.ToString().ToLower(),
                candidate.Score.ToString("F2")
            );
            lines.Add(line);
        }
        
        await File.WriteAllLinesAsync(filePath, lines, cancellationToken);
    }

    /// <summary>
    /// CSV alan değerlerini virgül, tırnak ve yeni satırları işlemek için kaçış karakterleri ekler.
    /// </summary>
    /// <param name="field">Kaçış karakterleri eklenecek alan değeri</param>
    /// <returns>Kaçış karakterleri eklenmiş alan değeri</returns>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // Alan virgül, tırnak veya yeni satır içeriyorsa, tırnak içine al ve iç tırnakları kaçış karakteri ile değiştir
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }
}
