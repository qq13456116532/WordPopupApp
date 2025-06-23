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

        public async Task<DictionaryEntry> LookupAsync(string word)
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiUrl + word);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                // API返回的是一个数组，我们只取第一个结果
                var entries = JsonConvert.DeserializeObject<List<DictionaryEntry>>(json);
                return entries?.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}