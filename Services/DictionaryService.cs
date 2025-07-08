using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WordPopupApp.Models;

namespace WordPopupApp.Services
{
    public class DictionaryService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrl = "https://api.dictionaryapi.dev/api/v2/entries/en/";

        public async Task<DictionaryEntry> LookupAsync(string word, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiUrl + word, cancellationToken);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                // API返回的是一个数组，我们只取第一个结果
                var entries = JsonConvert.DeserializeObject<List<DictionaryEntry>>(json);
                return entries?.FirstOrDefault();
            }
            catch (TaskCanceledException)
            {
                // 任务被取消时，我们不认为这是一个错误，而是正常操作
                return null;
            }
            catch
            {
                // 其他异常仍然可以被捕获，但对于这个应用来说，返回null是安全的
                return null;
            }
        }
    }
}