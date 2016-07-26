using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Configuration;

// PROTOTYPE: For convenience, this reuses the Entity Framework definitions for the Volunteer data model.
using VolunteerDataWebApi.Models;

namespace VolunteerBot
{
    [LuisModel("ec28aeb2-bf7b-40ce-84bd-21e3b706838c", "a9b2b734425a4594a7159349612bc36a")]
    [Serializable]
    public class VolunteerOutreachDialog : LuisDialog<object>
    {
        private readonly string volunteerDataBaseUri;

        public VolunteerOutreachDialog()
        {
            var rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~/");
            this.volunteerDataBaseUri = rootWebConfig.AppSettings.Settings["VolunteerDataBaseUri"].Value;
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry I did not understand: " + result.Query + "\n It resulted in intents: " + string.Join(", ", result.Intents.Select(i => i.Intent));
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("HelpBotInformation")]
        public async Task HelpBotInformation(IDialogContext context, LuisResult result)
        {
            string message = $"I think you wanted to meet me when you said: " + result.Query + $" So Hello, I am The FIRST Washington Bot 3000! I can tell you about FIRST Washington Programs and opportunities";
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("StopContact")]
        public async Task StopContact(IDialogContext context, LuisResult result)
        {
            string message = $"I think you wanted me to stop contacting when you said: " + result.Query;
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("InformUser")]
        public async Task InformUser(IDialogContext context, LuisResult result)
        {
            string message = $"I think you wanted to learn more about FIRST Washington Programs when you said: " + result.Query;
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }
        
        [LuisIntent("SignUpInformation")]
        public async Task SignUpInformation(IDialogContext context, LuisResult result)
        {
            string message = $"I think you wanted to learn about FIRST Washington's sign-up process when you said: " + result.Query;
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }
    }
}