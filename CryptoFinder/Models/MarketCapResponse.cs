using System.Text.Json.Serialization;

namespace CryptoFinder.Models;

/// <summary>
/// CoinGecko piyasa değeri yanıt modeli.
/// </summary>
public record MarketCapResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("symbol")]
    public required string Symbol { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("circulating_supply")]
    public decimal CirculatingSupply { get; init; }
    
    [JsonPropertyName("total_supply")]
    public decimal TotalSupply { get; init; }
    
    [JsonPropertyName("max_supply")]
    public decimal? MaxSupply { get; init; }
    
    [JsonPropertyName("current_price")]
    public decimal CurrentPrice { get; init; }
    
    [JsonPropertyName("market_cap")]
    public decimal MarketCap { get; init; }
    
    [JsonPropertyName("market_cap_rank")]
    public decimal? MarketCapRank { get; init; }
    
    [JsonPropertyName("fully_diluted_valuation")]
    public decimal? FullyDilutedValuation { get; init; }
    
    [JsonPropertyName("total_volume")]
    public decimal TotalVolume { get; init; }
    
    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; init; }
}
