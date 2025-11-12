using Favi_BE.Data;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Favi_BE.Services
{
    public class SupabaseAuthService : ISupabaseAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseOptions _options;

        public SupabaseAuthService(HttpClient httpClient, IOptions<SupabaseOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;

            /*_httpClient.BaseAddress = new Uri(_options.Url);
            _httpClient.DefaultRequestHeaders.Add("apikey", _options.ApiKey);*/
        }

        public async Task<SupabaseAuthResponse?> RegisterAsync(string email, string password, string username)
        {
            var body = new
            {
                email,
                password,
                data = new { username }
            };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/auth/v1/signup", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SupabaseAuthResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<SupabaseAuthResponse?> LoginAsync(string email, string password)
        {
            var body = new { email, password };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/auth/v1/token?grant_type=password", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SupabaseAuthResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<SupabaseAuthResponse?> RefreshAsync(string refreshToken)
        {
            var body = new { refresh_token = refreshToken };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/auth/v1/token?grant_type=refresh_token", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SupabaseAuthResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

    }
}
