using System.Net;
using CryptoFinder.Config;

namespace CryptoFinder.Util;

/// <summary>
/// Yeniden deneme mantığı ve geri çekilme stratejileri ile HTTP yardımcı sınıfı.
/// </summary>
public class HttpService
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore;

    public HttpService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _semaphore = new SemaphoreSlim(Settings.MAX_CONCURRENT_REQUESTS, Settings.MAX_CONCURRENT_REQUESTS);
    }

    /// <summary>
    /// Yeniden deneme mantığı ve üstel geri çekilme ile HTTP GET isteği gerçekleştirir.
    /// </summary>
    /// <param name="url">İstek URL'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yanıt içeriği string olarak</returns>
    public async Task<string> GetStringWithRetryAsync(string url, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            for (int attempt = 1; attempt <= Settings.MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(Settings.HTTP_TIMEOUT_SECONDS));
                    _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Settings.USER_AGENT);
                    var response = await _httpClient.GetAsync(url, cts.Token);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync(cts.Token);
                    }

                    // Hız sınırlaması (429) ve sunucu hatalarını (5xx) işle
                    if (response.StatusCode == HttpStatusCode.TooManyRequests || 
                        (int)response.StatusCode >= 500)
                    {
                        if (attempt < Settings.MAX_RETRY_ATTEMPTS)
                        {
                            var delay = CalculateBackoffDelay(attempt);
                            await Task.Delay(delay, cancellationToken);
                            continue;
                        }
                    }

                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync(cts.Token);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (HttpRequestException) when (attempt < Settings.MAX_RETRY_ATTEMPTS)
                {
                    var delay = CalculateBackoffDelay(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw new HttpRequestException($"Failed to get response after {Settings.MAX_RETRY_ATTEMPTS} attempts");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Titreşim ile üstel geri çekilme gecikmesini hesaplar.
    /// </summary>
    /// <param name="attempt">Mevcut deneme numarası</param>
    /// <returns>Milisaniye cinsinden gecikme</returns>
    private static int CalculateBackoffDelay(int attempt)
    {
        var baseDelay = Settings.RETRY_DELAY_MS * Math.Pow(2, attempt - 1);
        var jitter = Random.Shared.Next(0, (int)(baseDelay * 0.1)); // %10 titreşim
        return (int)baseDelay + jitter;
    }
}
