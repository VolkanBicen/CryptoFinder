namespace CryptoFinder.Models;

/// <summary>
/// Teknik göstergeler ve skorlama ile kripto para analiz adayını temsil eder.
/// </summary>
public record AnalysisCandidate
{
    // --- Kimlik / zaman ---
    public string Symbol { get; set; } = "";
    public DateTime Date { get; set; }

    // --- Fiyat / temel ---
    public decimal Close { get; set; }

    // --- Kuralların geçiş bayrakları ---
    public bool PassedMacd { get; set; }          // MACD line > Signal
    public bool PassedSuperTrend { get; set; }    // SuperTrend yukarı (veya fiyat ST üstünde)
    public bool PassedDonchian { get; set; }      // Alt banda çok yakın değil
    public bool NearUpperBand { get; set; }       // Üst banda yakın (breakout potansiyeli)
    public bool PassedStoch { get; set; }         // StochRSI K>D ve K>0.2
    public bool PassedRs { get; set; }            // RS vs BTC (coin getirisi > BTC getirisi)

    // --- RS metrikleri (raporlama) ---
    public double? RsCoin { get; set; }           // Coin getirisi (RS_LOOKBACK_DAYS)
    public double RsDiff { get; set; }            // Coin - BTC farkı (pozitif iyi)

    // --- İndikatörlerden faydalı metrikler (opsiyonel rapor) ---
    public double MacdDiff { get; set; }          // MACD - Signal (pozitif iyi)
    public double? StochK { get; set; }           // StochRSI K
    public double? StochD { get; set; }           // StochRSI D

    // --- Donchian yakınlık bilgisi (opsiyonel rapor) ---
    public decimal? DonchianUpper { get; set; }
    public decimal? DonchianLower { get; set; }

    // --- Nihai skor ---
    public double Score { get; set; }

    // Çıktı modeli: analize değer aday + metrikler
    
    public double? Ema200 { get; set; }
    public double? Adx { get; set; }
    public double? AtrPct { get; set; }     // % cinsinden
    public double? Rs30 { get; set; }     // 0.12 => %12 30g getirisi
    public double Rs30VsBtc { get; set; }     // coin - BTC farkı
    public bool PassedAboveEma { get; set; }
    public bool PassedAdx { get; set; }
    public bool PassedAtr { get; set; }
    public bool NearLowerBandBlocked { get; set; }


    //public required string Symbol { get; init; }
    //public required DateTime Date { get; init; }
    //public required decimal Close { get; init; }
    //public double? Ema200 { get; init; }
    //public double? Adx { get; init; }
    //public double? AtrPct { get; init; }           // Yüzde
    //public double? Rs30 { get; init; }            // 30 günlük getiri (0.12 = %12)
    //public double Rs30VsBtc { get; init; }        // BTC'ye göre fark
    //public bool PassedAboveEma { get; init; }
    //public bool PassedAdx { get; init; }
    //public bool PassedRs { get; init; }
    //public bool PassedAtr { get; init; }
    //public bool NearLowerBandBlocked { get; init; }
    //public double Score { get; init; }             // 0-100
}
