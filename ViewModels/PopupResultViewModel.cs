using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WordPopupApp.Services;
using WordPopupApp.Models;
using System.Windows;
using System.Collections.Generic;

namespace WordPopupApp.ViewModels
{
    public partial class PopupResultViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddToAnkiCommand))]
        private bool isAddingToAnki;

        private readonly Action _closeAction;

        [ObservableProperty] private string word;
        [ObservableProperty] private string phoneticText;
        [ObservableProperty] private string definitionsText;
        [ObservableProperty] private string phrasesText;
        [ObservableProperty] private string sentencesText;
        [ObservableProperty] private bool hasAudio;
        [ObservableProperty] private bool hasDefinitions;
        [ObservableProperty] private bool hasPhrases;
        [ObservableProperty] private bool hasSentences;
        [ObservableProperty] private bool searchSuccess;

        private string audioUrl;
        private YoudaoWordCard _card;
        private readonly AnkiService _ankiService;
        private readonly AppSettings _settings;
        private readonly MainWindow _mainWindow;
        static bool _modelReady;

        // [修改] 构造函数签名，接收 YoudaoWordCard
        public PopupResultViewModel(YoudaoWordCard? card, AnkiService ankiService, AppSettings settings, MainWindow mainWindow, Action closeAction)
        {
            _ankiService = ankiService;
            _settings = settings;
            _mainWindow = mainWindow;
            _closeAction = closeAction;
            _card = card;

            if (card == null || !card.IsValid())
            {
                IsLoading = card == null; // 如果card是null，说明还在加载
                SearchSuccess = false;    // 标记查询失败
                Word = card?.Word ?? "未找到";
                DefinitionsText = "无法查询到该单词或短语的释义。";
                return;
            }

            IsLoading = false;
            SearchSuccess = true;
            Word = card.Word;
            PhoneticText = card.Phonetic;
            audioUrl = card.AudioUrl;
            HasAudio = !string.IsNullOrEmpty(audioUrl);

            // --- 格式化释义 ---
            var defBuilder = new StringBuilder();
            foreach(var def in card.Definitions)
            {
                defBuilder.AppendLine($"{def.PartOfSpeech} {def.Definition}");
            }
            DefinitionsText = defBuilder.ToString().Trim();
            HasDefinitions = !string.IsNullOrEmpty(DefinitionsText);

            // --- 格式化词组 ---
            var phraseBuilder = new StringBuilder();
            foreach(var phrase in card.Phrases)
            {
                phraseBuilder.AppendLine($"{phrase.Phrase}\n  {phrase.Definition}");
            }
            PhrasesText = phraseBuilder.ToString().Trim();
            HasPhrases = !string.IsNullOrEmpty(PhrasesText);

            // --- 格式化例句 ---
            var sentenceBuilder = new StringBuilder();
            foreach(var sentence in card.Sentences)
            {
                sentenceBuilder.AppendLine($"{sentence.ExampleEn}\n{sentence.ExampleCn}\n");
            }
            SentencesText = sentenceBuilder.ToString().Trim();
            HasSentences = !string.IsNullOrEmpty(SentencesText);
        }

        [RelayCommand]
        private void PlayAudio()
        {
            if (HasAudio)
            {
                var mediaElement = new MediaElement { Volume = 1, LoadedBehavior = MediaState.Play };
                mediaElement.Source = new System.Uri(audioUrl, System.UriKind.Absolute);
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddToAnki))]
        private async Task AddToAnki()
        {
            if (_card == null) return;

            IsAddingToAnki = true;
            try
            {
                if (!_modelReady)
                {
                    await _mainWindow.EnsureWordPopUpNoteModelAsync();
                    _modelReady = true;
                }
                
                // --- 格式化 Anki 字段 ---
                var ankiDefinitions = string.Join("<br>", _card.Definitions.Select(d => $"{d.PartOfSpeech} {d.Definition}"));
                var ankiPhrases = string.Join("<br>", _card.Phrases.Select(p => $"<b>{p.Phrase}</b><br>{p.Definition}"));
                var ankiSentences = string.Join("<br>", _card.Sentences.Select(s => $"{s.ExampleEn}<br>{s.ExampleCn}"));
                
                var note = new AnkiNote
                {
                    DeckName = _settings.AnkiDeckName,
                    Fields = new Dictionary<string, string>
                    {
                        { "单词", $"{_card.Word}<br>{_card.Phonetic}" },
                        { "释义", ankiDefinitions },
                        { "笔记", ankiPhrases },
                        { "例句", ankiSentences }
                    },
                    Tags = new List<string> { "WordPopupApp", _card.Source.Replace(" ", "-") },
                    Audio = HasAudio
                        ? new AnkiAudio
                        {
                            Url = audioUrl,
                            Filename = $"{_card.Word.Replace(" ", "_")}_{DateTimeOffset.Now.ToUnixTimeSeconds()}.mp3",
                            Fields = new List<string> { "单词" }
                        }
                        : null
                };

                await _ankiService.AddNoteAsync(note);
                _closeAction?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加到 Anki 失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsAddingToAnki = false;
            }
        }

        private bool CanAddToAnki()
        {
            return SearchSuccess && !IsAddingToAnki;
        }
    }
}