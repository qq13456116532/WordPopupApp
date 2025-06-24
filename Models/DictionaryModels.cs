using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 我们将使用免费的 https://dictionaryapi.dev/
// 这个文件是根据其返回的JSON结构创建的
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WordPopupApp.Models
{
    public class Phonetic
    {
        public string Text { get; set; }
        public string Audio { get; set; }
    }

    public class Definition
    {
        public string PartOfSpeech { get; set; }
        public List<string> Synonyms { get; set; }
        public List<string> Antonyms { get; set; }

        // [修改] 添加JsonProperty属性以匹配API返回的JSON字段名
        [JsonProperty("definition")]
        public string DefinitionText { get; set; }
        public string Example { get; set; }
    }

    public class Meaning
    {
        public string PartOfSpeech { get; set; }
        public List<Definition> Definitions { get; set; }
    }

    public class DictionaryEntry
    {
        public string Word { get; set; }
        public List<Phonetic> Phonetics { get; set; }
        public List<Meaning> Meanings { get; set; }
    }
}