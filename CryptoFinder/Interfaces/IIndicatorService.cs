using Skender.Stock.Indicators;

namespace CryptoFinder.Interfaces;

/// <summary>
/// Teknik gösterge hesaplamaları için arayüz.
/// </summary>
public interface IIndicatorService
{
    /// <summary>
    /// Verilen fiyat verileri için EMA (Üstel Hareketli Ortalama) hesaplar.
    /// </summary>
    /// <param name="quotes">Fiyat verileri</param>
    /// <param name="period">EMA periyodu</param>
    /// <returns>EMA sonuçları listesi</returns>
    List<EmaResult> CalculateEma(List<Quote> quotes, int period);

    /// <summary>
    /// Verilen fiyat verileri için ADX (Ortalama Yönsel İndeks) hesaplar.
    /// </summary>
    /// <param name="quotes">Fiyat verileri</param>
    /// <param name="period">ADX periyodu</param>
    /// <returns>ADX sonuçları listesi</returns>
    List<AdxResult> CalculateAdx(List<Quote> quotes, int period);

    /// <summary>
    /// Verilen fiyat verileri için ATR (Ortalama Gerçek Aralık) hesaplar.
    /// </summary>
    /// <param name="quotes">Fiyat verileri</param>
    /// <param name="period">ATR periyodu</param>
    /// <returns>ATR sonuçları listesi</returns>
    List<AtrResult> CalculateAtr(List<Quote> quotes, int period);

    /// <summary>
    /// Verilen fiyat verileri için Donchian Kanalı hesaplar.
    /// </summary>
    /// <param name="quotes">Fiyat verileri</param>
    /// <param name="period">Donchian periyodu</param>
    /// <returns>Donchian sonuçları listesi</returns>
    List<DonchianResult> CalculateDonchian(List<Quote> quotes, int period);

    /// <summary>
    /// Verilen geriye bakış periyodu için getiri yüzdesini hesaplar.
    /// </summary>
    /// <param name="quotes">Fiyat verileri</param>
    /// <param name="lookbackDays">Geriye bakılacak gün sayısı</param>
    /// <returns>Getiri yüzdesi veya yetersiz veri varsa null</returns>
    double? CalculateReturn(List<Quote> quotes, int lookbackDays);
}
