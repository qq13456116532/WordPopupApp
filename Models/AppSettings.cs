namespace WordPopupApp.Models
{
    public class AppSettings
    {
        public string AnkiDeckName { get; set; } = "Default";

        // 新增：是否启用 AI 补充
        public bool EnableAISupplement { get; set; } = true;
        
    }
}