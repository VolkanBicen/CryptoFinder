using CryptoFinder.Config;
using CryptoFinder.Interfaces;
using CryptoFinder.Models;
using CryptoFinder.Util;
using Skender.Stock.Indicators;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace CryptoFinder.Services;

/// <summary>
/// Teknik göstergeleri hesaplayan ve skorlayan kripto para analiz servisi.
/// </summary>
public class AnalyzerService : IAnalyzer
{
    private readonly IIndicatorService _indicatorService;
    private readonly HttpService _httpService;

    public AnalyzerService(IIndicatorService indicatorService, HttpService httpService)
    {
        _indicatorService = indicatorService ?? throw new ArgumentNullException(nameof(indicatorService));
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
    }

    /// <inheritdoc />
    public async Task<List<AnalysisCandidate>> AnalyzeCandidatesAsync(
        IEnumerable<string> baseSymbols,
        string interval,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<AnalysisCandidate>();

        // Göreceli güç hesaplaması için BTC referansını al
        var btcQuotes = await GetQuotesAsync("BTCUSDT", interval, limit, cancellationToken);
        if (btcQuotes == null || btcQuotes.Count == 0)
            return candidates;

        int lookbackBars = interval.Equals("4h", StringComparison.OrdinalIgnoreCase)
    ? Settings.RS_LOOKBACK_BARS_4H
    : Settings.RS_LOOKBACK_BARS_1D;

        var btcRet = _indicatorService.CalculateReturn(btcQuotes, Settings.RS_LOOKBACK_DAYS);

        int dropPair = 0, dropQuotes = 0, dropMinBars = 0, dropGuard = 0, dropGate = 0; int dropGateMacd = 0, dropGateSt = 0, dropGateDch = 0;


        foreach (var baseSymbol in baseSymbols.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var symbol = (baseSymbol ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(symbol))
                continue;

            var pair = symbol + "USDT";
            if (string.IsNullOrEmpty(pair)) { dropPair++; continue; }

            var quotes = await GetQuotesAsync(pair, interval, limit, cancellationToken);
            if (quotes == null || quotes.Count == 0) { dropQuotes++; continue; }

            // Göstergeler için yeterli veri olduğundan emin ol
            int minBars = Math.Max(35, Math.Max(Settings.DCH_LEN, Settings.ST_LEN) + 5);
            if (quotes.Count < minBars) { dropMinBars++; continue; }
            
            //var candidate = await AnalyzeSymbolAsync(symbol, quotes, btcRet, cancellationToken);
            
            // 4) Analiz — guard/gate nedenlerini öğrenmek için “with reason” çağır
            var (candidate, droppedByGuard, droppedByGate, gateFail) = await AnalyzeSymbolWithReasonAsync(symbol, quotes, btcRet, cancellationToken);

            if (droppedByGuard) { dropGuard++; continue; }
            if (droppedByGate)
            {
                switch (gateFail)
                {
                    case "MACD": dropGateMacd++; break;
                    case "ST": dropGateSt++; break;
                    case "DCH": dropGateDch++; break;
                }
                dropGate++;
                continue;
            }

            if (candidate != null)
                candidates.Add(candidate);
        }

        Console.WriteLine($"Drops → pair:{dropPair} quotes:{dropQuotes} minBars:{dropMinBars} guard:{dropGuard} gate:{dropGate} " + $"| gate(M:{dropGateMacd}, S:{dropGateSt}, D:{dropGateDch})");

   

        return candidates.OrderByDescending(x => x.Score).ToList();
    }

    /// <summary>
    /// Analyzes a single symbol and returns analysis candidate if it passes filters.
    /// </summary>
    /// <param name="symbol">Base symbol to analyze</param>
    /// <param name="quotes">Price quotes</param>
    /// <param name="btcReturn30">BTC 30-day return for relative strength</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis candidate or null if doesn't pass filters</returns>
    /// 
    #region Old AnalyzeSymbol
    /*
    private async Task<AnalysisCandidate?> AnalyzeSymbolAsync(
        string symbol,
        List<Quote> quotes,
        double? btcReturn30,
        CancellationToken cancellationToken)
    {
        try
        {

            // İndikatörler için yeterli veri olduğundan emin ol
            // MACD(12,26,9) için ~35 bar; Donchian(ST) + güvenlik payı
            int minBars = Math.Max(35, Math.Max(Settings.DCH_LEN, Settings.ST_LEN) + 5);
            if (quotes.Count < minBars)
                return null;

            // === İndikatörleri hesapla (YENİ SET) ===
            var macdList = quotes.GetMacd(12, 26, 9).ToList();
            var stList = quotes.GetSuperTrend(Settings.ST_LEN, Settings.ST_MULT).ToList();
            var dchList = quotes.GetDonchian(Settings.DCH_LEN).ToList();
            var srsiList = quotes.GetStochRsi(14, 14, 3, 3).ToList();

            // Chop guard için BB(20,2)
            var bbList = quotes.GetBollingerBands(20, 2).ToList();

            var last = quotes[^1];
            var macd = macdList.FirstOrDefault(x => x.Date == last.Date);
            var st = stList.FirstOrDefault(x => x.Date == last.Date);
            var dch = dchList.FirstOrDefault(x => x.Date == last.Date);
            var srsi = srsiList.FirstOrDefault(x => x.Date == last.Date);

            if (macd == null || dch == null || st == null || srsi == null)
                return null;

            // === Chop Guard (opsiyonel) ===
            if (Settings.ENABLE_CHOP_GUARD)
            {
                int stFlips = CountStFlips(stList, Settings.CHOP_LOOKBACK);   // ST flip sayısı
                var bbwAvg = AvgBbWidth(bbList, Settings.CHOP_LOOKBACK);     // BB genişlik ortalaması
                var priceSlope = PriceSlope(quotes, Settings.CHOP_LOOKBACK);     // fiyattan eğim

                bool isChop =
                    (bbwAvg.HasValue && bbwAvg.Value <= Settings.BBWIDTH_CHOP_MAX) &&
                    (priceSlope.HasValue && Math.Abs(priceSlope.Value) <= Settings.PRICE_SLOPE_FLAT_MAX) &&
                    (stFlips >= Settings.ST_FLIP_MIN);

                if (isChop)
                {
                    // Range modunda: Donchian üst banda çok yakın değilse ele
                    decimal? upper = dch.UpperBand.HasValue ? (decimal?)dch.UpperBand.Value : null;
                    if (!upper.HasValue ||
                        last.Close < upper.Value * (1m - (decimal)(Settings.DCH_UPPER_SLACK / 100.0)))
                    {
                        return null;
                    }
                }
            }

            // === İş kurallarını uygula (MACD + ST + DCH + StochRSI + RS vs BTC) ===
            var rules = ApplyBusinessRules(last, macd, st, dch, srsi, quotes, btcReturn30);

            // Sert kapı (gating): MACD & SuperTrend & Donchian (alt banda yakın değil) & StochRSI
            // if (!(rules.Macd && rules.SuperTrend && rules.Donchian && rules.Stoch))
            //   return null;

            if (Settings.REQUIRE_STOCH_IN_GATE)
            {
                if (!(rules.Macd && rules.SuperTrend && rules.Donchian))
                    return null;
            }
         

            // Skor (yeni set)
            double score = CalculateScore(rules, last.Close);

            // === ÇIKTI (MODELINE GÖRE ALANLARI UYARLA) ===
            return new AnalysisCandidate
            {
                Symbol = symbol,
                Date = last.Date,
                Close = last.Close,
                PassedMacd = rules.Macd,
                PassedSuperTrend = rules.SuperTrend,
                PassedDonchian = rules.Donchian,
                NearUpperBand = rules.NearUpper,
                PassedStoch = rules.Stoch,
                PassedRs = rules.Rs,
                RsCoin = rules.CoinRet,
                RsDiff = rules.RsDiff,
                MacdDiff = rules.MacdDiff,
                StochK = rules.StochK,
                StochD = rules.StochD,

                // Donchian info (opsiyonel):
                // DonchianUpper = (decimal?) dch?.UpperBand,
                // DonchianLower = (decimal?) dch?.LowerBand,

                Score = Math.Round(score, 2)
            };
            #region Old = Calculate indicators Ema, Adx, Atr, Donchian

            /*
            var emaResults = _indicatorService.CalculateEma(quotes, Settings.EMA_LEN);
            var adxResults = _indicatorService.CalculateAdx(quotes, Settings.ADX_LEN);
            var atrResults = _indicatorService.CalculateAtr(quotes, 14);
            var donchianResults = _indicatorService.CalculateDonchian(quotes, Settings.DCH_LEN);

            var lastQuote = quotes[^1];
            var ema = emaResults.FirstOrDefault(x => x.Date == lastQuote.Date);
            var adx = adxResults.FirstOrDefault(x => x.Date == lastQuote.Date);
            var atr = atrResults.FirstOrDefault(x => x.Date == lastQuote.Date);
            var donchian = donchianResults.FirstOrDefault(x => x.Date == lastQuote.Date);
        



            if (ema?.Ema == null || adx?.Adx == null || atr?.Atr == null || donchian == null)
                return null;

            // Apply rules
            var rules = ApplyAnalysisRules(lastQuote, ema, adx, atr, donchian, quotes, btcReturn30);
            
            // Early exit if basic rules don't pass
            if (!(rules.PassedAboveEma && rules.PassedRs && rules.PassedAtr))
                return null;

            // Calculate score
            var score = CalculateScore(rules, adx.Adx.Value, atr.Atr.Value, lastQuote.Close);

            return new AnalysisCandidate
            {
                Symbol = symbol,
                Date = lastQuote.Date,
                Close = lastQuote.Close,
                Ema200 = ema.Ema,
                Adx = adx.Adx,
                AtrPct = rules.AtrPercentage,
                Rs30 = rules.CoinReturn30,
                Rs30VsBtc = rules.RsDifference,
                PassedAboveEma = rules.PassedAboveEma,
                PassedAdx = rules.PassedAdx,
                PassedRs = rules.PassedRs,
                PassedAtr = rules.PassedAtr,
                NearLowerBandBlocked = !rules.PassedDonchian,
                Score = Math.Round(score, 2)
            };
              
        }
        catch
        {
            return null;
        }
    }
    */
    #endregion

    private async Task<(AnalysisCandidate? candidate, bool droppedByGuard, bool droppedByGate, string? gateFail)>
      AnalyzeSymbolWithReasonAsync(string symbol, List<Quote> quotes, double? btcRet, CancellationToken ct)
    {
        try
        {
            // --- İndikatörler ---
            var macdList = quotes.GetMacd(12, 26, 9).ToList();
            var stList = quotes.GetSuperTrend(Settings.ST_LEN, Settings.ST_MULT).ToList();
            var dchList = quotes.GetDonchian(Settings.DCH_LEN).ToList();
            var srsiList = quotes.GetStochRsi(14, 14, 3, 3).ToList();
            var bbList = quotes.GetBollingerBands(20, 2).ToList();

            var last = quotes[^1];
            var macd = macdList.FirstOrDefault(x => x.Date == last.Date);
            var st = stList.FirstOrDefault(x => x.Date == last.Date);
            var dch = dchList.FirstOrDefault(x => x.Date == last.Date);
            var srsi = srsiList.FirstOrDefault(x => x.Date == last.Date);

            if (macd == null || dch == null || st == null || srsi == null)
                return (null, false, true, "INDCALC"); // indikatör yok → gate say

            // --- ChopGuard ---
            if (Settings.ENABLE_CHOP_GUARD)
            {
                int stFlips = CountStFlips(stList, Settings.CHOP_LOOKBACK);
                var bbwAvg = AvgBbWidth(bbList, Settings.CHOP_LOOKBACK);
                var slope = PriceSlope(quotes, Settings.CHOP_LOOKBACK);

                bool isChop =
                    (bbwAvg.HasValue && bbwAvg.Value <= Settings.BBWIDTH_CHOP_MAX) &&
                    (slope.HasValue && Math.Abs(slope.Value) <= Settings.PRICE_SLOPE_FLAT_MAX) &&
                    (stFlips >= Settings.ST_FLIP_MIN);

                if (isChop)
                {
                    decimal? upper = dch.UpperBand.HasValue ? (decimal?)dch.UpperBand.Value : null;
                    if (!upper.HasValue || last.Close < upper.Value * (1m - (decimal)(Settings.DCH_UPPER_SLACK / 100.0)))
                    {
                        return (null, true, false, null);
                    }
                }
            }

            // --- İş kuralları ---
            var rules = ApplyBusinessRules(last, macd, st, dch, srsi, quotes, btcRet);

            // === StochRSI K/D normalizasyon (0..1’e indir) ===
            if (rules.StochK.HasValue && rules.StochK.Value > 1.0) rules.StochK = rules.StochK.Value / 100.0;
            if (rules.StochD.HasValue && rules.StochD.Value > 1.0) rules.StochD = rules.StochD.Value / 100.0;

            // Stoch kuralını normalize değerlerle yeniden değerlendir
            rules.Stoch = rules.StochK.HasValue && rules.StochD.HasValue
                          && rules.StochK.Value > rules.StochD.Value
                          && rules.StochK.Value > 0.20;

            // --- TAZELİK KONTROLLERİ ---
            // MACD histogram listesi: (Macd - Signal)
            var macdHist = macdList
                .Select(x => (x.Macd ?? 0) - (x.Signal ?? 0))
                .ToList();

            // Son 2 barda histogram artıyor mu?
            bool macdFresh = macdHist.Count >= 2 && macdHist[^1] > macdHist[^2];

            // SuperTrend: son 2 barda fiyat ST üstünde mi?
            int lastIdx = quotes.Count - 1;
            bool stFresh = false;
            if (lastIdx >= 1)
            {
                var stNow = stList[lastIdx].SuperTrend;
                var stPrev = stList[lastIdx - 1].SuperTrend;

                if (stNow.HasValue && stPrev.HasValue)
                {
                    var closeNow = quotes[lastIdx].Close;
                    var closePrev = quotes[lastIdx - 1].Close;

                    stFresh = closeNow > (decimal)stNow.Value
                           && closePrev > (decimal)stPrev.Value;
                }
            }

            // --- Gate (yumuşatılmış) ---
            bool gatePass = rules.Macd && rules.SuperTrend && rules.Donchian
                            && (!Settings.REQUIRE_STOCH_IN_GATE || rules.Stoch);
            if (!gatePass)
            {
                string fail = !rules.Macd ? "MACD" : (!rules.SuperTrend ? "ST" : (!rules.Donchian ? "DCH" : "OTHER"));
                return (null, false, true, fail);
            }

            // --- Score (normalize K/D ile) ---
            double score = CalculateScore(rules, last.Close);

            // Tazelik bonusları (gate değil, sadece skor bonusu)
            if (macdFresh)
                score += 0.20 * Settings.W_MACD;   // MACD ağırlığının +%20’si

            if (stFresh)
                score += 0.20 * Settings.W_ST;     // ST ağırlığının +%20’si

            var candidate = new AnalysisCandidate
            {
                Symbol = symbol,
                Date = last.Date,
                Close = last.Close,

                PassedMacd = rules.Macd,
                PassedSuperTrend = rules.SuperTrend,
                PassedDonchian = rules.Donchian,
                NearUpperBand = rules.NearUpper,
                PassedStoch = rules.Stoch,
                PassedRs = rules.Rs,

                RsCoin = rules.CoinRet,
                RsDiff = rules.RsDiff,
                MacdDiff = rules.MacdDiff,
                StochK = rules.StochK,   // artık 0..1 aralığında
                StochD = rules.StochD,   // artık 0..1 aralığında

                Score = Math.Round(score, 2)
            };

            return (candidate, false, false, null);
        }
        catch
        {
            return (null, false, true, "EX");
        }
    }


    /// <summary>
    /// Applies analysis rules to determine if symbol passes filters.
    /// </summary>
    /// <param name="lastQuote">Last price quote</param>
    /// <param name="ema">EMA result</param>
    /// <param name="adx">ADX result</param>
    /// <param name="atr">ATR result</param>
    /// <param name="donchian">Donchian result</param>
    /// <param name="quotes">All quotes for return calculation</param>
    /// <param name="btcReturn30">BTC 30-day return</param>
    /// <returns>Analysis rules result</returns>

    #region old Rules
    /*
    private AnalysisRules ApplyAnalysisRules(
        Quote lastQuote,
        EmaResult ema,
        AdxResult adx,
        AtrResult atr,
        DonchianResult donchian,
        List<Quote> quotes,
        double? btcReturn30)
    {
        // EMA rule
        var passedAboveEma = lastQuote.Close > (decimal)ema.Ema.Value;

        // ADX rule
        var passedAdx = adx.Adx.Value >= Settings.ADX_MIN;

        // Relative strength rule
        var coinReturn30 = _indicatorService.CalculateReturn(quotes, Settings.RS_LOOKBACK_DAYS);
        var rsDifference = (coinReturn30 ?? 0) - (btcReturn30 ?? 0);
        var passedRs = coinReturn30.HasValue && btcReturn30.HasValue && coinReturn30.Value > btcReturn30.Value;

        // ATR rule
        var atrPercentage = (double)((decimal)atr.Atr.Value / lastQuote.Close * 100m);
        var passedAtr = atrPercentage <= Settings.ATR_MAX_PCT;

        // Donchian rule
        var passedDonchian = true;
        if (donchian.LowerBand.HasValue)
        {
            var block = (decimal)donchian.LowerBand.Value * (1m + (decimal)(Settings.DCH_LOWER_BLOCK / 100.0));
            passedDonchian = lastQuote.Close > block;
        }

        return new AnalysisRules
        {
            PassedAboveEma = passedAboveEma,
            PassedAdx = passedAdx,
            PassedRs = passedRs,
            PassedAtr = passedAtr,
            PassedDonchian = passedDonchian,
            AtrPercentage = atrPercentage,
            CoinReturn30 = coinReturn30,
            RsDifference = rsDifference
        };
    }


    */
    #endregion

    private AnalysisRules ApplyBusinessRules(
    Quote last,
    MacdResult macd,                 // MACD(12,26,9) son bar
    SuperTrendResult st,             // SuperTrend(ATR=10, mult=3.0) son bar
    DonchianResult dch,              // Donchian(20) son bar
    StochRsiResult srsi,             // StochRSI(14) son bar
    List<Quote> quotes,              // coin’in 4h serisi
    double? btcRetLookback)          // BTC’nin RS lookback getirisi (7g)
    {

        // --- 1) MACD kuralı ---
        // momentum yukarı: MACD > Signal (+ histogram > 0 ekstra teyit)
        bool macdUp = macd?.Macd.HasValue == true && macd?.Signal.HasValue == true
                      && macd.Macd.Value > macd.Signal.Value;
        double macdDiff = (macd?.Macd ?? 0) - (macd?.Signal ?? 0);   // skor için

        // --- 2) SuperTrend kuralı ---
        // up-trend kabul: LowerBand var & UpperBand yok  (Skender'de böyle yorumladık)
        bool stUp =
     (st?.SuperTrend.HasValue == true && last.Close > (decimal)st.SuperTrend.Value)
     || (st?.LowerBand.HasValue == true && !st.UpperBand.HasValue);

        // Alternatif (bazı sürümlerde): fiyat > SuperTrend çizgisi
        // bool stUp = st?.SuperTrend.HasValue == true && last.Close > (decimal)st.SuperTrend.Value;

        // --- 3) Donchian konum kuralları ---
        bool notNearLower = true;
        bool nearUpper = false;

        if (dch != null)
        {
            if (dch.LowerBand.HasValue)
            {
                decimal lower = (decimal)dch.LowerBand.Value;
                decimal block = lower * (1m + (decimal)(Settings.DCH_LOWER_BLOCK / 100.0));
                notNearLower = last.Close > block; // alt banda çok yakınsa elenecek
            }

            if (dch.UpperBand.HasValue)
            {
                decimal upper = (decimal)dch.UpperBand.Value;
                decimal thr = upper * (1m - (decimal)(Settings.DCH_UPPER_SLACK / 100.0));
                nearUpper = last.Close >= thr;     // breakout’a yakın mı?
            }
        }

        // --- 4) StochRSI kuralı ---
        // K’yi srsi.StochRsi, D’yi srsi.Signal olarak kullanıyoruz (Skender’de bu isimler var)
        double? k = srsi?.StochRsi;
        double? d = srsi?.Signal;

        // K > D ve K > 0.2 → yükseliş momentumunun başladığı kabul
        // K >= 0.8 aşırı alım → skorlamada tavan kırpılır, ama kuralı direkt bozmaz.
        bool stochOk = k.HasValue && d.HasValue && k.Value > d.Value && k.Value > 0.20;

        // --- 5) RS vs BTC (kısa vade 7 gün) ---
        var coinRet = _indicatorService.CalculateReturn(quotes, Settings.RS_LOOKBACK_DAYS);
        bool rsOk = coinRet.HasValue && btcRetLookback.HasValue && (coinRet.Value > btcRetLookback.Value);
        double rsDiff = (coinRet ?? 0) - (btcRetLookback ?? 0); // skor için

        return new AnalysisRules
        {
            // geçiş bayrakları
            Macd = macdUp,
            SuperTrend = stUp,
            Donchian = notNearLower,   // alt banda çok yakınsa false
            NearUpper = nearUpper,      // breakout yakınlığı (bonus/koşul)
            Stoch = stochOk,
            Rs = rsOk,

            // skor/rapor metrikleri
            MacdDiff = macdDiff,
            StochK = k,
            StochD = d,
            CoinRet = coinRet,
            RsDiff = rsDiff
        };
    }
    /// <summary>
    /// Calculates analysis score based on rules and indicators.
    /// </summary>
    /// <param name="rules">Analysis rules</param>
    /// <param name="adxValue">ADX value</param>
    /// <param name="atrValue">ATR value</param>
    /// <param name="closePrice">Close price</param>
    /// <returns>Score from 0-100</returns>


    #region OLD CalculateScore
    /*
    private static double CalculateScore(AnalysisRules rules, double adxValue, double atrValue, decimal closePrice)
    {
        double score = 0;

        // EMA rule
        score += rules.PassedAboveEma ? Settings.W_EMA : 0;

        // ADX: linear scale from 15 to 35
        if (adxValue >= Settings.ADX_MIN)
        {
            var adxScore = Math.Min(1.0, (adxValue - Settings.ADX_MIN) / 20.0) * Settings.W_ADX;
            score += adxScore;
        }

        // Relative strength: scale positive difference
        var rsScore = Math.Max(0, Math.Min(1.0, rules.RsDifference / 0.15)) * Settings.W_RS;
        score += rsScore;

        // ATR: lower is better
        var atrScore = Math.Max(0, (Settings.ATR_MAX_PCT - rules.AtrPercentage) / Settings.ATR_MAX_PCT) * Settings.W_ATR;
        score += atrScore;

        // Donchian rule
        score += rules.PassedDonchian ? Settings.W_DCH : 0;

        return score;
    }


    */
    #endregion
    private static double CalculateScore(AnalysisRules r, decimal closePrice)
    {
        double score = 0;

        // --- MACD (0–25) ---
        // macdDiff’i normalize edelim: 0.00–0.01 aralığını 0–1’e sıkıştır (cap 1.0)
        // Çok küçük farklarda 0’a düşmesin diye max ile kırp.
        // Yenisi: MACD histogram farkını fiyata oranla normalize et (yüzde)
        double macdPct = (double)(r.MacdDiff / (double)closePrice);       // ≈ histogram / price
        double macdNorm = Math.Max(0.0, Math.Min(1.0, macdPct / 0.002));  // ~%0.2 → tam puana yaklaşıyor

        score += (r.Macd ? 0.5 * Settings.W_MACD : 0)
               + macdNorm * (0.5 * Settings.W_MACD);

        // --- SuperTrend (0–25) ---
        score += r.SuperTrend ? Settings.W_ST : 0;

        // --- Donchian (0–20) ---
        // Alt banda yakın değilse taban puan; üst banda yakınsa ekstra bonus
        if (r.Donchian)
        {
            score += Settings.W_DCH * (r.NearUpper ? 1.0 : 0.6);  // nearUpper yoksa %60’ını ver
        }
        // (İstersen nearUpper şartını “guard range” içinde zorunlu kılmaya devam edebilirsin; o kısım ChopGuard’da.)

        // --- StochRSI (0–20) ---
        // K>D ve K>0.2 ise baz puan; K 0.2–0.8 arasında yükseldikçe bonus;
        // K>=0.8 aşırı alım → bonusu tavanla (ör. max 80%)
        if (r.Stoch && r.StochK.HasValue)
        {
            double k = r.StochK.Value; // 0..1
            double bandNorm;
            if (k <= 0.20) bandNorm = 0.0;
            else if (k < 0.80) bandNorm = (k - 0.20) / 0.60;   // 0..1
            else if (k < 0.90) bandNorm = 0.8;                 // 0.80..0.90
            else bandNorm = 0.6;                                // 0.90+ → daha sert kırp

            score += Settings.W_STOCH * bandNorm;
        }
        // --- RS vs BTC (0–10) ---
        // Pozitif RS farkını 0..10’a ölçekle; 10% farkta (0.10) tavana vur.
        double rsDiffEff = r.RsDiff < 0.01 ? 0.0 : r.RsDiff; // <1% fark → skor yok
        double rsNorm = Math.Max(0.0, Math.Min(1.0, rsDiffEff / 0.10));
        score += rsNorm * Settings.W_RS;

       
        //double rsNorm = Math.Max(0.0, Math.Min(1.0, r.RsDiff / 0.10));
        //score += rsNorm * Settings.W_RS;

        return Math.Round(score, 2);
    }


    /// <summary>
    /// Gets OHLCV quotes from Binance API.
    /// </summary>
    /// <param name="symbol">Trading pair symbol</param>
    /// <param name="interval">Time interval</param>
    /// <param name="limit">Number of candles</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of quotes</returns>
    private async Task<List<Quote>> GetQuotesAsync(string symbol, string interval, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{Settings.BINANCE_BASE_URL}/klines?symbol={symbol}&interval={interval}&limit={limit}";
            var json = await _httpService.GetStringWithRetryAsync(url, cancellationToken);
            var klines = JsonSerializer.Deserialize<List<List<JsonElement>>>(json);

            if (klines == null || klines.Count == 0)
                return new List<Quote>();

            var quotes = new List<Quote>(klines.Count);
            foreach (var kline in klines)
            {
                quotes.Add(new Quote
                {
                    Date = DateTimeOffset.FromUnixTimeMilliseconds(kline[0].GetInt64()).UtcDateTime,
                    Open = decimal.Parse(kline[1].GetString()!, CultureInfo.InvariantCulture),
                    High = decimal.Parse(kline[2].GetString()!, CultureInfo.InvariantCulture),
                    Low = decimal.Parse(kline[3].GetString()!, CultureInfo.InvariantCulture),
                    Close = decimal.Parse(kline[4].GetString()!, CultureInfo.InvariantCulture),
                    Volume = decimal.Parse(kline[5].GetString()!, CultureInfo.InvariantCulture)
                });
            }

            quotes.Sort((a, b) => a.Date.CompareTo(b.Date));
            return quotes;
        }
        catch
        {
            return new List<Quote>();
        }
    }
    private int CountStFlips(List<SuperTrendResult> st, int bars)
    {
        int flips = 0;
        for (int i = Math.Max(1, st.Count - bars); i < st.Count; i++)
        {
            bool prevUp = st[i - 1].LowerBand.HasValue && !st[i - 1].UpperBand.HasValue;
            bool nowUp = st[i].LowerBand.HasValue && !st[i].UpperBand.HasValue;
            if (prevUp != nowUp) flips++;
        }
        return flips;
    }

    private double? AvgBbWidth(List<BollingerBandsResult> bb, int bars)
    {
        var take = bb.Skip(Math.Max(0, bb.Count - bars))
                     .Where(x => x.UpperBand.HasValue && x.LowerBand.HasValue && x.Sma.HasValue && x.Sma.Value != 0)
                     .Select(x => (x.UpperBand!.Value - x.LowerBand!.Value) / x.Sma!.Value)
                     .ToList();
        return take.Count == 0 ? (double?)null : take.Average();
    }

    private double? PriceSlope(List<Quote> quotes, int bars)
    {
        if (quotes == null || quotes.Count < bars + 1) return null;
        int lastIndex = quotes.Count - 1;
        int prevIndex = lastIndex - bars;
        if (prevIndex < 0) return null;

        decimal last = quotes[lastIndex].Close;
        decimal prev = quotes[prevIndex].Close;
        if (prev == 0) return null;

        // oransal eğim (EMA eğimi yerine fiyatın kendi eğimi)
        return (double)((last - prev) / prev);
    }

    /// <summary>
    /// Internal class for analysis rules result.
    /// </summary>
    private class AnalysisRules
    {
        public bool Macd { get; set; }
        public bool SuperTrend { get; set; }
        public bool Donchian { get; set; }   // alt banda çok yakın değil
        public bool NearUpper { get; set; }  // üst banda yakın (breakout)
        public bool Stoch { get; set; }
        public bool Rs { get; set; }

        // metrics for scoring/reporting
        public double MacdDiff { get; set; }
        public double? StochK { get; set; }
        public double? StochD { get; set; }
        public double? CoinRet { get; set; }
        public double RsDiff { get; set; }

        //public bool PassedAboveEma { get; set; }
        //public bool PassedAdx { get; set; }
        //public bool PassedRs { get; set; }
        //public bool PassedAtr { get; set; }
        //public bool PassedDonchian { get; set; }
        //public double AtrPercentage { get; set; }
        //public double? CoinReturn30 { get; set; }
        //public double RsDifference { get; set; }
    }
}
