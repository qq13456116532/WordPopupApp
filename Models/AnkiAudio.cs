namespace WordPopupApp.Models
{
    /// <summary>
    /// 与 AnkiConnect “audio” 对象格式对应的简单 DTO。
    /// </summary>
    public class AnkiAudio
    {
        /// <summary>要下载的音频地址（或本地文件绝对路径）。</summary>
        public string Url { get; set; }

        /// <summary>保存进 Anki 的文件名，形如 “hello_1721041830.mp3”。</summary>
        public string Filename { get; set; }

        /// <summary>要把音频标签插入到哪些字段中；通常就是 “单词”。</summary>
        public List<string> Fields { get; set; }
    }
}
