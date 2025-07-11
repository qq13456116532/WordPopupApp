using AngleSharp;
using AngleSharp.Dom;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WordPopupApp.Models;

namespace WordPopupApp.Services
{
    public class YoudaoScraperService
    {
        private readonly HttpClient _httpClient;
        private readonly IBrowsingContext _browsingContext;

        public YoudaoScraperService()
        {
            _httpClient = new HttpClient();
            // 配置请求头，模拟浏览器
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://dict.youdao.com/");
            
            // 创建一个 AngleSharp 的浏览上下文，用于解析 HTML
            var config = Configuration.Default.WithDefaultLoader();
            _browsingContext = BrowsingContext.New(config);
        }

        public async Task<YoudaoWordCard> ScrapeWordAsync(string word, CancellationToken cancellationToken)
        {
            var url = $"https://dict.youdao.com/w/{word}";
            try
            {
                // 1. 发起 HTTP 请求获取 HTML
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var htmlContent = await response.Content.ReadAsStringAsync();
                
                // 2. 使用 AngleSharp 解析 HTML
                var document = await _browsingContext.OpenAsync(req => req.Content(htmlContent), cancellationToken);

                var wordCard = new YoudaoWordCard { Word = word };

                // --- 抓取基础释义和音标 (逻辑同 Python 脚本) ---
                var basicDefContainer = document.QuerySelector("#phrsListTab .trans-container");
                if (basicDefContainer != null)
                {
                    wordCard.Source = "Youdao Basic";
                    var phoneticNodes = document.QuerySelectorAll("#phrsListTab .pronounce");
                    wordCard.Phonetic = string.Join(" | ", phoneticNodes.Select(n => n.TextContent.Trim()));

                    var simpleDefs = basicDefContainer.QuerySelectorAll("ul li");
                    foreach (var li in simpleDefs.Take(5))
                    {
                        var fullText = li.TextContent.Trim();
                        var match = Regex.Match(fullText, @"^([a-z]+\.)\s*(.*)", RegexOptions.IgnoreCase);
                        var pos = match.Success ? match.Groups[1].Value : "N/A";
                        var defn = match.Success ? match.Groups[2].Value : fullText;

                        wordCard.Definitions.Add(new YoudaoDefinition
                        {
                            PartOfSpeech = pos,
                            Definition = defn
                        });
                    }
                }

                // --- 抓取发音音频 (逻辑同 Python 脚本) ---
                var audioUs = document.QuerySelector(".pronounce a[data-rel*='type=2']");
                if (audioUs != null && audioUs.HasAttribute("data-rel"))
                {
                    wordCard.AudioUrl = $"http://dict.youdao.com/dictvoice?audio={word}&type=2";
                }
                else
                {
                    var audioUk = document.QuerySelector(".pronounce a[data-rel*='type=1']");
                    if (audioUk != null && audioUk.HasAttribute("data-rel"))
                    {
                        wordCard.AudioUrl = $"http://dict.youdao.com/dictvoice?audio={word}&type=1";
                    }
                }

                // --- 抓取柯林斯例句 (逻辑同 Python 脚本) ---
                var collinsResult = document.QuerySelector("#collinsResult");
                if (collinsResult != null)
                {
                    wordCard.Word = collinsResult.QuerySelector("h4 .title")?.TextContent.Trim() ?? wordCard.Word;
                    wordCard.Phonetic = collinsResult.QuerySelector("h4 .phonetic")?.TextContent.Trim() ?? wordCard.Phonetic;
                    
                    var listItems = collinsResult.QuerySelectorAll(".ol li");
                    foreach (var li in listItems.Take(5))
                    {
                        var enNode = li.QuerySelector(".exampleLists .examples p:nth-of-type(1)");
                        var cnNode = li.QuerySelector(".exampleLists .examples p:nth-of-type(2)");
                        if (enNode != null && cnNode != null)
                        {
                            wordCard.Sentences.Add(new YoudaoSentence
                            {
                                ExampleEn = enNode.TextContent.Trim(),
                                ExampleCn = cnNode.TextContent.Trim()
                            });
                        }
                    }
                }

                // --- 抓取网络短语 (逻辑同 Python 脚本) ---
                var webPhraseContainer = document.QuerySelector("#webPhrase");
                if (webPhraseContainer != null)
                {
                    var phraseNodes = webPhraseContainer.QuerySelectorAll(".wordGroup");
                    foreach (var node in phraseNodes.Take(5))
                    {
                        var titleNode = node.QuerySelector(".contentTitle");
                        if (titleNode != null)
                        {
                            var phraseTitle = titleNode.TextContent.Trim();
                            var fullText = node.TextContent.Trim();
                            var rawDefinition = fullText.Replace(phraseTitle, "").Trim();
                            var cleanedDefinition = string.Join(" ; ", rawDefinition.Split(';').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)));
                            
                            wordCard.Phrases.Add(new YoudaoPhrase
                            {
                                Phrase = phraseTitle,
                                Definition = cleanedDefinition
                            });
                        }
                    }
                }

                // 如果没有任何有效信息，则认为查询失败
                if (!wordCard.IsValid())
                {
                    return null;
                }

                return wordCard;
            }
            catch (TaskCanceledException)
            {
                // 任务被取消是正常操作
                return null;
            }
            catch (HttpRequestException ex)
            {
                // 网络错误
                Debug.WriteLine($"Error scraping Youdao for '{word}': {ex.Message}");
                return null;
            }
        }
    }
}