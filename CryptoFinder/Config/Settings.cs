namespace CryptoFinder.Config;

/// <summary>
/// CryptoFinder uygulaması için yapılandırma sabitleri.
/// Tüm eşik değerleri ve parametreler kolay bakım için burada merkezi olarak tutulur.
/// </summary>
public static class Settings
{
    public const bool REQUIRE_STOCH_IN_GATE = false;

    public const int RS_LOOKBACK_BARS_4H = 42; // ~7 gün = 7*24/4
    public const int RS_LOOKBACK_BARS_1D = 7;  // 7 gün

    // Piyasa Değeri Filtreleme Sabitleri
    public const decimal MC_MIN = 100_000_000m;           // Minimum piyasa değeri
    public const decimal VOL24H_MIN = 10_000_000m;        // Minimum 24 saatlik hacim
    public const decimal FDV_MC_MAX = 10m;               // Maksimum FDV/MC oranı
    public const decimal TURNOVER_MIN = 0.03m;            // Minimum cirolama oranı
    public const int RANK_MAX = 500;                      // Maksimum piyasa değeri sıralaması
    public const int AGE_MIN_DAYS = 0;                    // Minimum yaş (gün) (şu anda kullanılmıyor)
    public const decimal SPREAD_MAX_PCT = 0.5m;           // Maksimum spread yüzdesi
    public const decimal DEPTH_MIN_USD = 100_000m;        // Minimum derinlik (USD)
    public const double DCH_UPPER_SLACK = 1.0;         // Üst banda yakınlık toleransı (%)

    // Teknik Analiz Sabitleri
    public const int EMA_LEN = 200;                       // EMA periyodu
    public const int ST_LEN = 10;                         // Stochastic periyodu (yardımcı)
    public const double ST_MULT = 2.0;                    // Stochastic çarpanı
    public const int ADX_LEN = 14;                        // ADX periyodu
    public const double ADX_MIN = 15.0;                  // Minimum ADX değeri
    public const int RS_LOOKBACK_DAYS = 7;              // Göreceli güç geriye bakış
    public const double ATR_MAX_PCT = 25.0;              // Maksimum ATR yüzdesi
    public const int DCH_LEN = 20;                       // Donchian kanal uzunluğu
    public const double DCH_LOWER_BLOCK = 3.0;           // Donchian alt bant blok yüzdesi
    public const double ATH_NEAR_PCT = 0.0;              // ATH yakınlık yüzdesi (kullanılmıyor)

    // Skorlama Ağırlıkları (toplam ~100 olmalı)
    public const double W_EMA = 35.0;                    // EMA kuralı ağırlığı
    public const double W_ADX = 15.0;                    // ADX gücü ağırlığı
    public const double W_RS = 25.0;                      // Göreceli güç ağırlığı
    public const double W_ATR = 15.0;                    // ATR yüzdesi ağırlığı
    public const double W_DCH = 10.0;                     // Donchian kuralı ağırlığı
    public const double W_MACD = 25.0;                  // MACD kuralı için ağırlık
    public const double W_ST = 25.0;                    // SuperTrend kuralı için ağırlık
    public const double W_STOCH = 20.0;                 // Stoch RSI kuralı için ağırlık
 


    // HTTP Yapılandırması
    public const int HTTP_TIMEOUT_SECONDS = 15;           // İstek başına zaman aşımı
    public const int GLOBAL_TIMEOUT_SECONDS = 300;        // Genel zaman aşımı (5 dakika)
    public const int MAX_RETRY_ATTEMPTS = 3;              // Maksimum yeniden deneme sayısı
    public const int RETRY_DELAY_MS = 1000;               // Temel yeniden deneme gecikmesi
    public const int MAX_CONCURRENT_REQUESTS = 6;         // Maksimum eşzamanlı istek sayısı

    // API Yapılandırması
    public const string COINGECKO_BASE_URL = "https://api.coingecko.com/api/v3";
    public const string BINANCE_BASE_URL = "https://api.binance.com/api/v3";
    public const string USER_AGENT = "CryptoFinder/1.0";

    // Sayfalama
    public const int COINS_PER_PAGE = 250;                // CoinGecko'dan sayfa başına coin sayısı
    public const int MAX_PAGES = 10;                      // İşlenecek maksimum sayfa sayısı
    public const int EARLY_STOP_CONSECUTIVE_EMPTY = 2;    // N ardışık boş sayfa sonrası dur

    // ===== Chop Guard (optional; mitigate ST flips in ranges) =====
    public const bool ENABLE_CHOP_GUARD = true;   // toggle
    public const int CHOP_LOOKBACK = 20;     // last N bars
    public const double ADX_CHOP_MAX = 18.0;   // avg ADX <= 18 → range
    public const double BBWIDTH_CHOP_MAX = 0.10;   // avg BB width <= 10%
    public const int ST_FLIP_MIN = 3;      // min ST flips in range
    public const double PRICE_SLOPE_FLAT_MAX = 0.02;   // EMA_SLOPE_FLAT_MAX → PRICE_...


    // Emir Defteri Yapılandırması
    public const int ORDERBOOK_LIMIT = 1000;              // Emir defteri derinlik limiti
    public const double DEPTH_PERCENTAGE = 1.0;           // Derinlik hesaplama yüzdesi (±1%)


   
   
 


    // Sıkı Rejim Yapılandırması (alternatif ayarlar)
    public static class TightRegime
    {
        public const decimal MC_MIN = 500_000_000m;       // Daha yüksek minimum piyasa değeri
        public const decimal VOL24H_MIN = 50_000_000m;    // Daha yüksek minimum hacim
        public const decimal FDV_MC_MAX = 5m;             // Daha düşük FDV/MC oranı
        public const decimal TURNOVER_MIN = 0.05m;       // Daha yüksek cirolama gereksinimi
        public const int RANK_MAX = 200;                  // Daha düşük sıralama eşiği
        public const decimal SPREAD_MAX_PCT = 0.3m;       // Daha sıkı spread gereksinimi
        public const decimal DEPTH_MIN_USD = 500_000m;    // Daha yüksek derinlik gereksinimi
        public const double ADX_MIN = 20.0;              // Daha yüksek ADX gereksinimi
        public const double ATR_MAX_PCT = 20.0;           // Daha düşük ATR toleransı
    }
}
