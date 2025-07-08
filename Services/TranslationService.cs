using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Web; // 需要手动添加引用或使用 .NET 自动处理

namespace WordPopupApp.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// 将英文文本翻译成中文。
        /// </summary>
        /// <param name="text">要翻译的英文单词或短语</param>
        /// <returns>中文翻译结果，如果失败则返回提示信息</returns>
        public async Task<string> TranslateToChineseAsync(string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            try
            {
                // 使用一个免费的Google翻译API接口，注意这并非官方API，可能不稳定
                var urlEncodedText = HttpUtility.UrlEncode(text);
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=zh-CN&dt=t&q={urlEncodedText}";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                
                // 解析返回的JSON数组
                // 格式类似 [[["你好世界","Hello World",null,null,1]],null,"en",null,null,null,null,[]]
                var jArray = JArray.Parse(json);
                var translatedText = jArray[0][0][0].ToString();

                return translatedText;
            }
            catch (TaskCanceledException)
            {
                return "[取消]"; // 或者返回 string.Empty
            }
            catch
            {
                return "翻译失败";
            }
        }
    }
}