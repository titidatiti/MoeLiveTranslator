using System;
using System.IO;
using LiveTranslator.Models;
using Newtonsoft.Json;

namespace LiveTranslator.Services
{
    public class ConfigService
    {
        private string ConfigFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user.config.json");

        public AppSettings Load()
        {
            if (File.Exists(ConfigFile))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFile);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    return new AppSettings();
                }
            }
            return new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
