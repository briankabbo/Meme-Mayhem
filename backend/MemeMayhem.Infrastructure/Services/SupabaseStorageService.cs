using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemeMayhem.Infrastructure.Services;

public class SupabaseStorageService
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;
    private readonly string _bucket = "meme-cards";
    private readonly ILogger<SupabaseStorageService> _logger;

    public SupabaseStorageService(
        HttpClient http,
        IConfiguration config,
        ILogger<SupabaseStorageService> logger)
    {
        _http = http;
        _logger = logger;
        _supabaseUrl = config["Supabase:Url"] ?? throw new Exception("Supabase:Url missing");
        _serviceRoleKey = config["Supabase:ServiceRoleKey"] ?? throw new Exception("Supabase:ServiceRoleKey missing");
    }

    public async Task<string> GetSignedUrlAsync(
        string storagePath, int expiresInSeconds = 3600)
    {
        var endpoint = $"{_supabaseUrl}/storage/v1/object/sign/{_bucket}/{storagePath}";

        _logger.LogInformation("Requesting signed URL for: {Path}", storagePath);
        _logger.LogInformation("Endpoint: {Endpoint}", endpoint);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Authorization", $"Bearer {_serviceRoleKey}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { expiresIn = expiresInSeconds }),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Supabase error {Status}: {Body}",
                response.StatusCode, errorBody);
            throw new Exception($"Supabase {response.StatusCode}: {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(json);
        var signedUrl = parsed.RootElement
            .GetProperty("signedURL")
            .GetString()
            ?? throw new Exception("Failed to get signed URL");

        return $"{_supabaseUrl}/storage/v1{signedUrl}";
    }
}