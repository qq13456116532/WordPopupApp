// Services/LangChainClient.cs
using System.Net.Http;
using System.Text;
using System.Text.Json;
using WordPopupApp.Models;

namespace WordPopupApp.Services
{
    public class LangChainClient
    {
        private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8040") };

        public async Task<YoudaoWordCard?> GenerateAsync(string query, CancellationToken ct)
        {
            var payload = JsonSerializer.Serialize(new { query });
            var resp = await _http.PostAsync("/generate_word_card",
                new StringContent(payload, Encoding.UTF8, "application/json"), ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            var card = JsonSerializer.Deserialize<YoudaoWordCard>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return card?.IsValid() == true ? card : null;
        }
    }
}
