namespace WordPopupApp.Models
{
    public class AnkiNote
    {
        public string DeckName { get; set; }
        public string ModelName { get; set; } = "WordPopUpNote"; // 固定 note type 名

        public Dictionary<string, string> Fields { get; set; }
        public List<string> Tags { get; set; }
        public AnkiAudio Audio { get; set; }
    }

    public class AnkiAction
    {
        public string Action { get; set; }
        public int Version { get; set; }
        public object Params { get; set; }
    }

    public class AnkiAddNoteAction : AnkiAction
    {
        public AnkiAddNoteAction(AnkiNote note)
        {
            Action = "addNote";
            Version = 6;
            Params = new { note };
        }
    }
}
