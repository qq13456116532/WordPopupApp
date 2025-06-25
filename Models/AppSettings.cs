namespace WordPopupApp.Models
{
    public class AppSettings
    {
        public string AnkiDeckName { get; set; } = "Default";
        // [新增] 用于存放 WordsAPI 的密钥
        public string WordsApiKey { get; set; }
    }
}