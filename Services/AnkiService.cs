
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using WordPopupApp.Models;
using Newtonsoft.Json.Serialization;   // 需要引入

namespace WordPopupApp.Services
{
    public class AnkiService
    {
        private readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8765") };

        private async Task<T> InvokeAsync<T>(string action, object parameters = null)
        {
            var payload = new
            {
                action,
                version = 6,
                @params = parameters ?? new { }
            };


            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var json = JsonConvert.SerializeObject(payload, settings);

            var resp = await _httpClient.PostAsync("/", new StringContent(json, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();

            var respJson = await resp.Content.ReadAsStringAsync();
            var wrapper = JsonConvert.DeserializeObject<AnkiResponse<T>>(respJson);

            if (wrapper.Error != null)
                throw new InvalidOperationException(wrapper.Error);

            return wrapper.Result;
        }

        private class AnkiResponse<T>
        {
            [JsonProperty("result")] public T Result { get; set; }
            [JsonProperty("error")] public string Error { get; set; }
        }

        public Task<List<string>> GetDeckNamesAsync() => InvokeAsync<List<string>>("deckNames");


        public async Task AddNoteAsync(AnkiNote note)
        {
            var result = await InvokeAsync<long?>("addNote", new { note });
            if (result == null)
                throw new InvalidOperationException("添加笔记失败，Anki 返回 null。");
        }
    }
}
