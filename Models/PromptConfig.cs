using System.Collections.Generic;

namespace LiveTranslator.Models
{
    public class PromptConfig
    {
        public Dictionary<string, LanguageConfig> Languages { get; set; } = new Dictionary<string, LanguageConfig>();
    }

    public class LanguageConfig
    {
        public MetaConfig Meta { get; set; }
        public Dictionary<string, SceneConfig> Scenes { get; set; }
    }

    public class MetaConfig
    {
        public string DisplayName { get; set; }
        public string WhisperLanguage { get; set; }
    }

    public class SceneConfig
    {
        public List<string> System { get; set; }
        public List<string> User { get; set; }
        public HotWordsData Hotwords { get; set; }
    }

    public class HotWordsData
    {
        public string Title { get; set; }
        public List<string> Items { get; set; }
    }
}
