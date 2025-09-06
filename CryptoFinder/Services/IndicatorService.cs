using CryptoFinder.Interfaces;
using Skender.Stock.Indicators;

namespace CryptoFinder.Services;

/// <summary>
/// Skender.Stock.Indicators kullanarak teknik gösterge servisi uygulaması.
/// </summary>
public class IndicatorService : IIndicatorService
{
    /// <inheritdoc />
    public List<EmaResult> CalculateEma(List<Quote> quotes, int period)
    {
        if (quotes == null || quotes.Count == 0)
            return new List<EmaResult>();

        return quotes.GetEma(period).ToList();
    }

    /// <inheritdoc />
    public List<AdxResult> CalculateAdx(List<Quote> quotes, int period)
    {
        if (quotes == null || quotes.Count == 0)
            return new List<AdxResult>();

        return quotes.GetAdx(period).ToList();
    }

    /// <inheritdoc />
    public List<AtrResult> CalculateAtr(List<Quote> quotes, int period)
    {
        if (quotes == null || quotes.Count == 0)
            return new List<AtrResult>();

        return quotes.GetAtr(period).ToList();
    }

    /// <inheritdoc />
    public List<DonchianResult> CalculateDonchian(List<Quote> quotes, int period)
    {
        if (quotes == null || quotes.Count == 0)
            return new List<DonchianResult>();

        return quotes.GetDonchian(period).ToList();
    }

    /// <inheritdoc />
    public double? CalculateReturn(List<Quote> quotes, int lookbackDays)
    {
        if (quotes == null || quotes.Count <= lookbackDays)
            return null;

        var last = quotes[^1].Close;
        var pastIndex = quotes.Count - 1 - lookbackDays;
        
        if (pastIndex < 0)
            return null;

        var past = quotes[pastIndex].Close;
        if (past <= 0)
            return null;

        return (double)((last / past) - 1m);
    }
}
