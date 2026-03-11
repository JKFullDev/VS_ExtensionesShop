using System.Text.Json.Serialization;

namespace ExtensionesShop.Server.Services;

public interface IRecaptchaService
{
    Task<RecaptchaResponse> VerifyTokenAsync(string token);
}

public class RecaptchaService : IRecaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecaptchaService> _logger;

    public RecaptchaService(HttpClient httpClient, IConfiguration configuration, ILogger<RecaptchaService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RecaptchaResponse> VerifyTokenAsync(string token)
    {
        var secretKey = _configuration["Recaptcha:SecretKey"];
        
        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogError("Recaptcha SecretKey no está configurada");
            return new RecaptchaResponse { Success = false, ErrorCodes = new[] { "missing-secret-key" } };
        }

        try
        {
            var response = await _httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>();
                return result ?? new RecaptchaResponse { Success = false };
            }

            _logger.LogError("Error en la verificación de reCAPTCHA: {StatusCode}", response.StatusCode);
            return new RecaptchaResponse { Success = false, ErrorCodes = new[] { "api-error" } };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al verificar reCAPTCHA");
            return new RecaptchaResponse { Success = false, ErrorCodes = new[] { "exception" } };
        }
    }
}

public class RecaptchaResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("challenge_ts")]
    public DateTime ChallengeTimestamp { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("error-codes")]
    public string[]? ErrorCodes { get; set; }
}
