using Microsoft.Bot.Connector;

namespace Hav.Bot.LanguageCop
{
    public static class ActivityExtensions
    {
        public static bool IsApplicableForLanguageEvaluation(this Activity activity)
        {
            return !string.IsNullOrEmpty(activity.Text) && activity.Text.Split(' ').Length > 3; // Only translate if more than 3 words
        }

        public static bool IsChannelActivity(this Activity activity)
        {
            return !string.IsNullOrEmpty(activity.Conversation.Name);
        }
    }
}