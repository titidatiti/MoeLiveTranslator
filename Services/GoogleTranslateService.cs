using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LiveTranslator.Services
{
    public class GoogleTranslateService
    {
        private readonly HttpClient _httpClient;

        public GoogleTranslateService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            try
            {
                // Use 'gtx' endpoint (No Key Required)
                // sl=auto (Source Auto), tl=target, dt=t (Return Translation)
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";

                var response = await _httpClient.GetStringAsync(url);

                // Parse GTX JSON: [[["Translated","Original",...]]]
                // Example: [[["Hello","Ola",null,null,1]],...]
                var array = JArray.Parse(response);

                // Combine multi-part sentences if needed, but usually index 0 is enough for short text
                string translated = "";
                foreach (var segment in array[0])
                {
                    translated += segment[0].ToString();
                }

                return translated;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GTX trans error: {ex.Message}");
                return "";
            }
        }
    }
}
