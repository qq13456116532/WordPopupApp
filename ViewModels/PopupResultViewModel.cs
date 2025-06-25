using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WordPopupApp.Services;
using WordPopupApp.Models;
using System.Windows;
using System.Collections.Generic; // [新增]

namespace WordPopupApp.ViewModels
{
    public partial class PopupResultViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isLoading = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddToAnkiCommand))] // 当此属性变化时，通知命令更新其可执行状态
        private bool isAddingToAnki; // 新增：用于跟踪添加状态

        private readonly Action _closeAction; // 新增：用于关闭窗口的回调

        [ObservableProperty]
        private string word;

        [ObservableProperty]
        private string phoneticText;

        [ObservableProperty]
        private string englishDefinitionText; // [修改] 名字改得更清晰

        [ObservableProperty]
        private string chineseDefinition; // <-- 新增：用于存储和显示中文翻译

        [ObservableProperty]
        private bool hasAudio;

        private string audioUrl;
        private DictionaryEntry _fullEntry;
        private readonly AnkiService _ankiService;
        private readonly AppSettings _settings;
        private readonly MainWindow _mainWindow;
        private readonly List<string> _phrases; // [新增] 用于存储词组


        // [修改] 构造函数签名，增加 chineseTranslation 参数
// [修改] 构造函数签名，增加 phrases 参数
        public PopupResultViewModel(DictionaryEntry entry, string chineseTranslation, List<string> phrases, AnkiService ankiService, AppSettings settings, MainWindow mainWindow, Action closeAction)        {
            // entry是单词的json
            _fullEntry = entry;
            _ankiService = ankiService;
            _settings = settings;
            _mainWindow = mainWindow;
            IsLoading = false;
            _closeAction = closeAction; // 新增：保存回调
            _phrases = phrases; // [新增] 保存词组

            ChineseDefinition = chineseTranslation ?? "翻译失败"; // 设置中文翻译

            if (entry == null)
            {
                Word = "未找到";
                EnglishDefinitionText = "无法查询到该单词或短语的英文释义。";
                return;
            }

            Word = entry.Word;

            var phonetic = entry.Phonetics?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text));
            PhoneticText = phonetic?.Text ?? "";

            // [修改] 查找音频并实现备用方案
            var audioPhonetic = entry.Phonetics?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Audio));
            audioUrl = audioPhonetic?.Audio;

            // 如果 API 没有提供音频链接，并且我们确实有一个单词
            if (string.IsNullOrEmpty(audioUrl) && !string.IsNullOrEmpty(entry.Word))
            {
                // 使用 Google TTS 作为备用方案
                // 注意：这是非官方接口，可能不稳定，但很常用
                audioUrl = $"https://translate.google.com/translate_tts?ie=UTF-8&q={Uri.EscapeDataString(entry.Word)}&tl=en&client=tw-ob";
            }

            HasAudio = !string.IsNullOrEmpty(audioUrl);
            // 生成英文释义
            var sb = new StringBuilder();
            foreach (var meaning in entry.Meanings)
            {
                sb.AppendLine($"▶ {meaning.PartOfSpeech}");
                foreach (var def in meaning.Definitions.Take(3))
                {
                    // 注意：这里用 DefinitionText
                    sb.AppendLine($"  - {def.DefinitionText}");
                    if (!string.IsNullOrEmpty(def.Example))
                    {
                        sb.AppendLine($"    e.g. {def.Example}");
                    }
                }
                sb.AppendLine();
            }
            EnglishDefinitionText = sb.ToString().Trim();
        }

        [RelayCommand]
        private void PlayAudio()
        {
            if (HasAudio)
            {
                // 使用一个临时的 MediaElement 播放音频
                var mediaElement = new MediaElement { Volume = 1 };
                mediaElement.LoadedBehavior = MediaState.Play;
                mediaElement.Source = new System.Uri(audioUrl, System.UriKind.Absolute);
            }
        }
        static bool _modelReady;
        // [修改] 重写 AddToAnki 命令
        [RelayCommand(CanExecute = nameof(CanAddToAnki))]
        private async Task AddToAnki()
        {
            if (_fullEntry == null) return;

            IsAddingToAnki = true; // 开始添加，设置状态
            try
            {
                if (!_modelReady)
                {
                    await _mainWindow.EnsureWordPopUpNoteModelAsync();
                    _modelReady = true;
                }
                // 1) 生成音频文件名
                var fileName = $"{Word}_{DateTimeOffset.Now.ToUnixTimeSeconds()}.mp3";
                // [新逻辑] 智能选择2-3个例句
                var selectedExamples = new List<string>();

                // 1. 优先从每个词性中各取一个例句
                var examplesFromEachPos = _fullEntry.Meanings
                    .Select(m => m.Definitions.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Example))?.Example)
                    .Where(ex => !string.IsNullOrEmpty(ex))
                    .ToList();
                
                selectedExamples.AddRange(examplesFromEachPos);

                // 2. 如果例句总数少于2个，并且还有更多例句可用，则进行补充
                if (selectedExamples.Count < 2)
                {
                    // 查找所有可用的例句，排除已经选过的
                    var allOtherExamples = _fullEntry.Meanings
                        .SelectMany(m => m.Definitions)
                        .Select(d => d.Example)
                        .Where(ex => !string.IsNullOrEmpty(ex) && !selectedExamples.Contains(ex))
                        .ToList();

                    // 需要补充的数量
                    int needed = 2 - selectedExamples.Count;
                    
                    // 从剩余的例句中补充
                    if (allOtherExamples.Any())
                    {
                        selectedExamples.AddRange(allOtherExamples.Take(needed));
                    }
                }
                
                // 3. 格式化并合并最终的例句列表
                var examples = string.Join("<br/>", 
                    selectedExamples.Select(e => e.Replace("\n", "<br/>"))
                );
                var notes = string.Join("<br/>",
                    _fullEntry.Meanings
                              .Select(m => $"◆ {m.PartOfSpeech}"));
                // [修改] 使用获取到的词组填充 "笔记" 字段
                notes = (_phrases != null && _phrases.Any())
                    ? string.Join("<br/>", _phrases) // 如果有词组，就用词组
                    : notes;                    // 如果没有，显示提示信息
                    
                // 2) 构造 note，字段与模板一一对应
                var note = new AnkiNote
                {
                    DeckName = _settings.AnkiDeckName,
                    Fields = new Dictionary<string, string>
                {
                    { "单词", $"{Word} {PhoneticText}<br>" },
                    { "释义", ChineseDefinition },
                    { "笔记", notes },
                    { "例句", examples }
                },
                    Tags = new List<string> { "WordPopupApp" },
                    Audio = HasAudio
                        ? new AnkiAudio
                        {
                            Url = audioUrl,
                            Filename = fileName,
                            Fields = new List<string> { "单词" }
                        }
                        : null
                };

                await _ankiService.AddNoteAsync(note);
            
            // 成功后，调用回调关闭窗口
                _closeAction?.Invoke();
            }
            catch (Exception ex)
            {
                // 捕获异常，并友好地提示用户，而不是让程序崩溃
                MessageBox.Show($"添加到 Anki 失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 无论成功还是失败，最后都将状态重置
                IsAddingToAnki = false;
            }
        }

        // 新增：为 RelayCommand 提供 CanExecute 的逻辑
        private bool CanAddToAnki()
        {
            return !IsAddingToAnki;
        }
        }
}
