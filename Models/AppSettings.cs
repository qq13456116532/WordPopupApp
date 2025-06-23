namespace WordPopupApp.Models
{
    public class AppSettings
    {
        public string AnkiDeckName { get; set; } = "Default";

        // 新增模板字段
        public string CardTemplate { get; set; } = @"
            <div style='font-family:Microsoft YaHei,Arial,sans-serif;font-size:18px;line-height:1.5;padding:10px;background:#f6f8fa;border-radius:10px;'>
                <div style='color:#222;font-weight:bold;font-size:22px;'>{{Word}}</div>
                <div style='margin:10px 0 6px 0;color:#0066cc;'>{{Phonetic}}</div>
                <div style='color:#444;'>{{Definition}}</div>
                <div style='margin-top:12px;color:#aaa;font-size:14px;'>{{Example}}</div>
            </div>
            ";
    }
}