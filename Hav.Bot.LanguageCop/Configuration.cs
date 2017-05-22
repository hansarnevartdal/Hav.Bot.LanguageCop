using System;

namespace Hav.Bot.LanguageCop
{
    public class Configuration
    {
        public string LanguageApiKey { get; set; }
        public string MicrosoftAppId { get; set; }
        public string MicrosoftAppPassword { get; set; }
        public string PreferredLanguage { get; set; }
        public bool GreetNewUsers { get; set; }
        public double MinimumLanguageConfidence { get; set; }

        public Configuration()
        {
            LanguageApiKey = Environment.GetEnvironmentVariable("LanguageApiKey", EnvironmentVariableTarget.Process);
            MicrosoftAppId = Environment.GetEnvironmentVariable("MicrosoftAppId", EnvironmentVariableTarget.Process);
            MicrosoftAppPassword = Environment.GetEnvironmentVariable("MicrosoftAppPassword", EnvironmentVariableTarget.Process);

            // Default preferred language: English
            PreferredLanguage = Environment.GetEnvironmentVariable("PreferredLanguage", EnvironmentVariableTarget.Process) ?? "English";

            // Default greet new users: true
            var greetNewUsers = Environment.GetEnvironmentVariable("GreetNewUsers", EnvironmentVariableTarget.Process);
            GreetNewUsers =  string.IsNullOrEmpty(greetNewUsers)? true : Convert.ToBoolean(greetNewUsers);
            
            // Default minimum language confidence: 0.8
            var minimumLanguageConfidence = Environment.GetEnvironmentVariable("MinimumLanguageConfidence", EnvironmentVariableTarget.Process);
            MinimumLanguageConfidence = string.IsNullOrEmpty(minimumLanguageConfidence) ? 0.8 : Convert.ToDouble(minimumLanguageConfidence);
        }
    }
}
