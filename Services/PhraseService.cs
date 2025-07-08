using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using WordPopupApp.Models;

namespace WordPopupApp.Services
{
    // 用于反序列化 WordsAPI 返回的 JSON
    public class WordsApiResponse
    {
        public string Word { get; set; }
        public List<string> Phrases { get; set; }
    }

    public class PhraseService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly AppSettings _settings;

        public PhraseService(AppSettings settings)
        {
            _settings = settings;
            _httpClient.BaseAddress = new System.Uri("https://wordsapiv1.p.rapidapi.com/");
            // 设置固定的请求头
            _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", "wordsapiv1.p.rapidapi.com");
        }

        public async Task<List<string>> GetPhrasesAsync(string word, CancellationToken cancellationToken)
        {
            // 如果没有配置 API Key，直接返回空列表，避免出错
            if (string.IsNullOrWhiteSpace(_settings.WordsApiKey))
            {
                // 可以考虑在这里加日志，但为了简单起见，我们直接返回
                return new List<string>();
            }

            try
            {
                // 每次请求时都带上最新的 API Key
                var request = new HttpRequestMessage(HttpMethod.Get, $"words/{word}/phrases");
                request.Headers.Add("X-RapidAPI-Key", _settings.WordsApiKey);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                // 如果单词没有对应的词组，API会返回404，这是正常情况
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<string>();
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<WordsApiResponse>(json);

                return result?.Phrases ?? new List<string>();
            }
            catch (TaskCanceledException)
            {
                return new List<string>(); // 任务取消时返回空列表
            }
            catch
            {
                // 发生任何网络或解析错误时，返回空列表，确保程序稳定
                return new List<string>();
            }
        }
    }
}