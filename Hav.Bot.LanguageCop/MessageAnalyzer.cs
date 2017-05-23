using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System;
using Microsoft.ProjectOxford.Text.Language;
using Microsoft.ProjectOxford.Text.Core;
using System.Collections.Generic;

namespace Hav.Bot.LanguageCop
{
    public static class MessageAnalyzer
    {
        private static Configuration _configuration = new Configuration();
        private static BotAuthenticator _botAuthenticator = new BotAuthenticator(_configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword);

        [FunctionName("MessageAnalyzer")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("Message analyzer triggered by HTTP request.");

            var activity = await req.Content.ReadAsAsync<Activity>();

            if (!await ValidateBotAuthentication(req, activity))
            {
                log.Warning("Invalid bot credentials.");
                return req.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid bot credentials.");
            }

            if (!activity.IsChannelActivity())
            {
                log.Info("Non-channel activity.");
                return req.CreateResponse(HttpStatusCode.OK);
            }

            if (activity.Type == ActivityTypes.Message && activity.IsApplicableForLanguageEvaluation())
            {
                log.Info("Handleing language evaluation.");
                await HandleLanguageEvaluation(activity);
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                log.Info("Handeling new members in channel.");
                await HandleNewMembers(activity);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        private static async Task<bool> ValidateBotAuthentication(HttpRequestMessage req, Activity activity)
        {
            if (string.IsNullOrEmpty(_configuration.MicrosoftAppId) || string.IsNullOrEmpty(_configuration.MicrosoftAppPassword))
            {
                return false;
            }

            return await _botAuthenticator.TryAuthenticateAsync(req, new List<IActivity> { activity }, new System.Threading.CancellationToken());
        }

        private static async Task HandleLanguageEvaluation(Activity activity)
        {
            var document = new Document()
            {
                Id = Guid.NewGuid().ToString(),
                Text = activity.Text
            };

            var languageRequest = new LanguageRequest();
            languageRequest.Documents.Add(document);

            var languageClient = new LanguageClient(_configuration.LanguageApiKey);
            var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

            try
            {
                var languageResponse = await languageClient.GetLanguagesAsync(languageRequest);

                foreach (var doc in languageResponse.Documents)
                {
                    var lang = doc.DetectedLanguages
                        .Where(l => l.Score > _configuration.MinimumLanguageConfidence)
                        .Where(l => !l.Name.Equals(_configuration.PreferredLanguage, StringComparison.InvariantCultureIgnoreCase))
                        .Select(l => l.Name.Replace('_', ' '))
                        .ToList();

                    if (lang != null && lang.Any())
                    {
                        var candidates = String.Join(", or ", lang);
                        var reply = activity.CreateReply($"That looks like {candidates}, please use {_configuration.PreferredLanguage} in this channel.");

                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                }
            }
            catch (Exception ex)
            {
                await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"{ex.Message}"));
            }
        }

        private static async Task HandleNewMembers(Activity activity)
        {
            if(!_configuration.GreetNewUsers)
            {
                return;
            }

            var newMembers = activity.MembersAdded;
            if (newMembers != null && newMembers.Any())
            {
                var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                foreach (var member in newMembers)
                {
                    await connector.Conversations.CreateDirectConversationAsync(activity.Recipient, member, new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = $"Hi {member.Name}! The {activity.Conversation.Name} channel has language monitoring. Please use {_configuration.PreferredLanguage} in this channel."
                    });
                }
            }
        }
    }
}