﻿using System;
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
    [LuisModel("6a090036-7b6b-44dd-b506-3ec7fa450897", "2086c732bb1340d9b9845d0c265fbf78")]
    [Serializable]
    public class VolunteerOutreachDialog : LuisDialog<object>
    {
        private readonly BuildFormDelegate<VolunteerFormFlow> MakeVolunteerForm;
        private readonly string volunteerDataBaseUri;

        internal VolunteerOutreachDialog(BuildFormDelegate<VolunteerFormFlow> makeVolunteerForm)
        { 
             this.MakeVolunteerForm = makeVolunteerForm; 
        }


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
            string message = $"I think you wanted to learn about this applicaication when you said: " + result.Query + $"Hello, I am The FIRST Washington Bot! I can tell you about FIRST Washington Programs and opportunities";
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


        private async Task VolunteerFormComplete(IDialogContext context, IAwaitable<VolunteerFormFlow> result)
        {
            VolunteerFormFlow form = null;
            try
            {
                form = await result;
            }
            catch (OperationCanceledException)
            {
                await context.PostAsync("Your response was canceled. If you'd like more information, reply what you'd like to learn about.");
                return;

            }
            if (form != null)
            {
                await context.PostAsync("Thank you for giving us your information. If you'd like more information, reply what you'd like to learn about.");
            }
            else 
            {
                await context.PostAsync("The form returned empty response!");
            }

            context.Wait(MessageReceived);

        }

        [LuisIntent("GetSignUp")]
        public async Task GetSignUp(IDialogContext context, LuisResult result)
        {
            string message = $"I can help you learn more about volunteering. I'm going to be asking you a few quick questions.";
            await context.PostAsync(message);
            var volunteerForm = new FormDialog<VolunteerFormFlow>(new VolunteerFormFlow(), this.MakeVolunteerForm, FormOptions.PromptInStart);
            context.Call<VolunteerFormFlow>(volunteerForm, VolunteerFormComplete);
        }
    }
}