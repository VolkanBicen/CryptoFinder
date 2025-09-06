using CryptoFinder.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoFinder.Data
{
    // Kullanışlı çıktı modeli
    public class GlobalMetrics
    {
        public decimal? TotalMarketCapUsd { get; set; }
        public decimal? TotalVolumeUsd { get; set; }
        public decimal? BtcDominancePct { get; set; }   // örn: 49.81
        public DateTime? UpdatedAtUtc { get; set; }
    }

    // CoinGecko /global reader
    public class GetGlobalMetrics
    {
        private readonly HttpClient _http;

        public GetGlobalMetrics(HttpClient httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
            if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
                _http.DefaultRequestHeaders.Add("User-Agent", Settings.USER_AGENT);

            _http.Timeout = TimeSpan.FromSeconds(Settings.HTTP_TIMEOUT_SECONDS);
        }

        public async Task<GlobalMetrics> FetchAsync(CancellationToken ct = default)
        {
            var url = $"{Settings.COINGECKO_BASE_URL}/global";
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(Settings.USER_AGENT);
            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                FloatParseHandling = FloatParseHandling.Decimal,
                NullValueHandling = NullValueHandling.Ignore
            };

            var root = JsonConvert.DeserializeObject<GlobalResponse>(json, settings);
            var data = root?.data;
            if (data == null)
                return new GlobalMetrics();

            // Total Market Cap (USD)
            decimal? totalMcapUsd = null;
            if (data.total_market_cap != null &&
                data.total_market_cap.TryGetValue("usd", out var mcapUsd))
                totalMcapUsd = mcapUsd;

            // Total Volume (USD)
            decimal? totalVolUsd = null;
            if (data.total_volume != null &&
                data.total_volume.TryGetValue("usd", out var volUsd))
                totalVolUsd = volUsd;

            // BTC Dominance (%)
            decimal? btcDom = null;
            if (data.market_cap_percentage != null &&
                data.market_cap_percentage.TryGetValue("btc", out var pct))
                btcDom = pct;

            // UpdatedAt (unix seconds → UTC)
            DateTime? updated = null;
            if (data.updated_at.HasValue)
                updated = DateTimeOffset.FromUnixTimeSeconds(data.updated_at.Value).UtcDateTime;

            return new GlobalMetrics
            {
                TotalMarketCapUsd = totalMcapUsd,
                TotalVolumeUsd = totalVolUsd,
                BtcDominancePct = btcDom,
                UpdatedAtUtc = updated
            };
        }
    }

    // ==== JSON modelleri ====

    public class GlobalResponse
    {
        public GlobalData data { get; set; }
    }

    public class GlobalData
    {
        public Dictionary<string, decimal> total_market_cap { get; set; }
        public Dictionary<string, decimal> total_volume { get; set; }
        public Dictionary<string, decimal> market_cap_percentage { get; set; }
        public long? updated_at { get; set; }
    }
}
