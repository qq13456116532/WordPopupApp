using Newtonsoft.Json;
using System.Collections.Generic;

namespace WordPopupApp.Models
{
    public class YoudaoDefinition
    {
        [JsonProperty("part_of_speech")]
        public string PartOfSpeech { get; set; }

        [JsonProperty("definition")]
        public string Definition { get; set; }
    }

    public class YoudaoPhrase
    {
        [JsonProperty("phrase")]
        public string Phrase { get; set; }

        [JsonProperty("definition")]
        public string Definition { get; set; }
    }

    public class YoudaoSentence
    {
        [JsonProperty("example_en")]
        public string ExampleEn { get; set; }

        [JsonProperty("example_cn")]
        public string ExampleCn { get; set; }
    }

    public class YoudaoWordCard
    {
        [JsonProperty("word")]
        public string Word { get; set; }

        [JsonProperty("phonetic")]
        public string Phonetic { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("audio_url")]
        public string AudioUrl { get; set; }

        [JsonProperty("definitions")]
        public List<YoudaoDefinition> Definitions { get; set; } = new List<YoudaoDefinition>();

        [JsonProperty("phrases")]
        public List<YoudaoPhrase> Phrases { get; set; } = new List<YoudaoPhrase>();

        [JsonProperty("sentences")]
        public List<YoudaoSentence> Sentences { get; set; } = new List<YoudaoSentence>();
        
        public bool IsValid() => !string.IsNullOrEmpty(Word) && (Definitions.Count > 0 || Sentences.Count > 0);
    }
}