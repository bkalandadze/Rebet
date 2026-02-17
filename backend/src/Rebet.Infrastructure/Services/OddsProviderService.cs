using Rebet.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net.Http.Json;

namespace Rebet.Infrastructure.Services;

public class OddsProviderService : IOddsProviderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OddsProviderService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public OddsProviderService(
        HttpClient httpClient,
        ILogger<OddsProviderService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;

        // Configure retry policy with Polly
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    if (outcome.Exception != null)
                    {
                        _logger.LogWarning(
                            "Retrying HTTP call. Attempt {RetryCount} after {Delay}ms. Exception: {ExceptionType}",
                            retryCount,
                            timespan.TotalMilliseconds,
                            outcome.Exception.GetType().Name);
                    }
                    else if (outcome.Result != null)
                    {
                        _logger.LogWarning(
                            "Retrying HTTP call. Attempt {RetryCount} after {Delay}ms. Status: {StatusCode}",
                            retryCount,
                            timespan.TotalMilliseconds,
                            outcome.Result.StatusCode);
                    }
                });

        // Configure HttpClient base address and headers
        var baseUrl = _configuration["OddsApi:BaseUrl"] ?? "https://api.example.com";
        var apiKey = _configuration["OddsApi:ApiKey"] ?? string.Empty;

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<OddsApiResponse> GetPrematchOddsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _configuration["OddsApi:PrematchEndpoint"] ?? "/api/v4/prematch";
            var brandId = _configuration["OddsApi:BrandId"] ?? "default";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var url = $"{endpoint}/brand/{brandId}/en/{timestamp}";

            _logger.LogInformation("Fetching prematch odds from {Url}", url);

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _httpClient.GetAsync(url, cancellationToken));

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<OddsApiResponse>(cancellationToken: cancellationToken);

            if (data == null)
            {
                _logger.LogWarning("Received null response from odds API");
                return new OddsApiResponse();
            }

            _logger.LogInformation("Successfully fetched {Count} events from odds API", data.Events.Count);
            return data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching odds from external API");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout while fetching odds from external API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching odds from external API");
            throw;
        }
    }
}

