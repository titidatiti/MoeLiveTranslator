using System;
using System.IO;
using Newtonsoft.Json;

namespace LiveTranslator.Models
{
    public class AppSettings
    {
        public int InputDeviceIndex { get; set; } = 0;
        public string SourceLanguage { get; set; } = "en-US"; // Language for STT and Translation Source
        public string TargetLanguage { get; set; } = "zh-CN";
        public string GoogleApiKey { get; set; } = "";
        public string DeepLApiKey { get; set; } = ""; // DeepL API key
        public string TranslationProvider { get; set; } = "Google"; // "Google" or "DeepL"
        public bool IsTopmost { get; set; } = true;

        public int NoiseGateThreshold { get; set; } = 10; // 0-100%



        public string WhisperModel { get; set; } = "ggml-small.bin";

        // Visual Settings
        public string ThemeColor { get; set; } = "#00FFC3";
        public double WindowOpacity { get; set; } = 0.8; // 0-1, Background opacity of the main window container (if any)

        public string OriginalTextColor { get; set; } = "#D5D5D5";
        public string TranslatedTextColor { get; set; } = "#00FFC3";

        public string SubtitleBackgroundColor { get; set; } = "#000000";
        public double SubtitleOpacity { get; set; } = 0.6;
        public double SubtitleSpacing { get; set; } = 2.0;

        public string SubtitleShadowColor { get; set; } = "#000000";
        public double SubtitleShadowBlur { get; set; } = 4.0;
        public double SubtitleShadowDirection { get; set; } = 320;
        public double SubtitleShadowDepth { get; set; } = 2;

        public string SubtitleStrokeColor { get; set; } = "#000000";
        public double SubtitleStrokeThickness { get; set; } = 0.0;

        public double OriginalFontSize { get; set; } = 14.0;
        public double TranslatedFontSize { get; set; } = 18.0;
        public string OriginalFontFamily { get; set; } = "Segoe UI";
        public bool OriginalFontBold { get; set; } = false;
        public string TranslatedFontFamily { get; set; } = "Segoe UI";
        public bool TranslatedFontBold { get; set; } = true;
    }
}
