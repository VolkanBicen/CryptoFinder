using System.Text.Json.Serialization;

namespace CryptoFinder.Models;

/// <summary>
/// Binance borsa bilgisi yanıtı.
/// </summary>
public record BinanceExchangeInfo
{
    [JsonPropertyName("symbols")]
    public required List<BinanceSymbolInfo> Symbols { get; init; }
}

/// <summary>
/// Binance'ten tek sembol bilgisi.
/// </summary>
public record BinanceSymbolInfo
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }
    
    [JsonPropertyName("status")]
    public required string Status { get; init; }
}

/// <summary>
/// Binance book ticker yanıtı.
/// </summary>
public record BinanceBookTicker
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }
    
    [JsonPropertyName("bidPrice")]
    public required string BidPrice { get; init; }
    
    [JsonPropertyName("askPrice")]
    public required string AskPrice { get; init; }
}

/// <summary>
/// Binance emir defteri yanıtı.
/// </summary>
public record BinanceOrderBook
{
    [JsonPropertyName("bids")]
    public required List<List<string>> Bids { get; init; } // [ [fiyat, miktar], ... ]
    
    [JsonPropertyName("asks")]
    public required List<List<string>> Asks { get; init; } // [ [fiyat, miktar], ... ]
}

/// <summary>
/// Binance kline (OHLCV) veri yanıtı.
/// </summary>
public record BinanceKline
{
    [JsonPropertyName("0")]
    public long OpenTime { get; init; }
    
    [JsonPropertyName("1")]
    public string Open { get; init; } = string.Empty;
    
    [JsonPropertyName("2")]
    public string High { get; init; } = string.Empty;
    
    [JsonPropertyName("3")]
    public string Low { get; init; } = string.Empty;
    
    [JsonPropertyName("4")]
    public string Close { get; init; } = string.Empty;
    
    [JsonPropertyName("5")]
    public string Volume { get; init; } = string.Empty;
    
    [JsonPropertyName("6")]
    public long CloseTime { get; init; }
    
    [JsonPropertyName("7")]
    public string QuoteAssetVolume { get; init; } = string.Empty;
    
    [JsonPropertyName("8")]
    public int NumberOfTrades { get; init; }
    
    [JsonPropertyName("9")]
    public string TakerBuyBaseAssetVolume { get; init; } = string.Empty;
    
    [JsonPropertyName("10")]
    public string TakerBuyQuoteAssetVolume { get; init; } = string.Empty;
    
    [JsonPropertyName("11")]
    public string Ignore { get; init; } = string.Empty;
}
