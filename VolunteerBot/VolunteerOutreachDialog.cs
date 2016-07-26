using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
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
    [LuisModel("0c22b84c-43f7-43f4-bd85-9adbb79b0c5e", "2086c732bb1340d9b9845d0c265fbf78")]
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

        [LuisIntent("GetHelp")]
        public async Task GetHelp(IDialogContext context, LuisResult result)
        {
            string message = $"I think you wanted to learn about this applicaication when you said: " + result.Query + $"Hello, I am The FIRST Washington Bot 3000! I can tell you about FIRST Washington Programs and opportunities";
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("EndContact")]
        public async Task EndContact(IDialogContext context, LuisResult result)
        {
            string message = $"I think you wanted me to stop contacting when you said: " + result.Query;
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("GetInformation")]
        public async Task GetInformation(IDialogContext context, LuisResult result)
        {
            string message = $"I think you wanted to learn more about FIRST Washington Programs when you said: " + result.Query;
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        internal static IDialog<VolunteerFormFlow> MakeRootDialog()
        {
            return Chain.From(() => FormDialog.FromForm(VolunteerFormFlow.BuildForm));
        }

        [LuisIntent("GetSignUpInformation")]
        public async Task GetSignUpInformation(IDialogContext context, LuisResult result)
        {
            string message = $"I think you wanted to learn about FIRST Washington's sign-up process when you said: " + result.Query;
            //await context.PostAsync(message);
            MakeRootDialog().PostToUser();
            context.Wait(MessageReceived);
        }
    }
}