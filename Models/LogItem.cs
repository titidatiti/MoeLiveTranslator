namespace LiveTranslator.Models
{
    public class LogItem
    {
        public string Original { get; set; }
        public string Translated { get; set; }
        public bool IsError { get; set; }

        public string DisplayOriginal => IsError ? "Error" : Original;
        public string DisplayTranslated => Translated;
    }
}
