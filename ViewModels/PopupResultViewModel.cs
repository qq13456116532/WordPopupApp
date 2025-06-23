using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WordPopupApp.Services;
using WordPopupApp.Models;
using WordPopupApp.Services;

namespace WordPopupApp.ViewModels
{
    public partial class PopupResultViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isLoading = true;

        [ObservableProperty]
        private string word;

        [ObservableProperty]
        private string phoneticText;

        [ObservableProperty]
        private string definitionText;

        [ObservableProperty]
        private bool hasAudio;

        private string audioUrl;
        private DictionaryEntry _fullEntry;
        private readonly AnkiService _ankiService;
        private readonly AppSettings _settings;

        public PopupResultViewModel(DictionaryEntry entry, AnkiService ankiService, AppSettings settings)
        {
            _fullEntry = entry;
            _ankiService = ankiService;
            _settings = settings;   

            IsLoading = false;

            if (entry == null)
            {
                Word = "未找到";
                DefinitionText = "无法查询到该单词或短语的释义。";
                return;
            }

            Word = entry.Word;

            var phonetic = entry.Phonetics?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text));
            PhoneticText = phonetic?.Text ?? "";

            var audio = entry.Phonetics?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Audio));
            audioUrl = audio?.Audio;
            HasAudio = !string.IsNullOrEmpty(audioUrl);

            var sb = new StringBuilder();
            foreach (var meaning in entry.Meanings)
            {
                sb.AppendLine($"▶ {meaning.PartOfSpeech}");
                foreach (var def in meaning.Definitions.Take(3)) // 最多显示3条释义
                {
                    sb.AppendLine($"  - {def.DefinitionText}");
                    if (!string.IsNullOrEmpty(def.Example))
                    {
                        sb.AppendLine($"    e.g. {def.Example}");
                    }
                }
                sb.AppendLine();
            }
            DefinitionText = sb.ToString().Trim();
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

        [RelayCommand]
        private async Task AddToAnki()
        {
            if (_fullEntry == null) return;

            var note = new AnkiNote
            {
                DeckName = _settings.AnkiDeckName,
                ModelName = _settings.AnkiModelName,
                Fields = new System.Collections.Generic.Dictionary<string, string>
                {
                    // 假设你的Anki模板字段是 "Front" 和 "Back"
                    { "Front", Word },
                    { "Back", DefinitionText.Replace("\n", "<br/>") } // Anki中换行用<br/>
                },
                Tags = new System.Collections.Generic.List<string> { "WordPopupApp" }
            };

            await _ankiService.AddNoteAsync(note);
        }
    }
}