using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
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

        private readonly string[][] leagueWords = new string[4][]
        {
            new string[] { "fll", "first lego league", "lego robotics", "lego"},
            new string[] {"ftc", "first tech challenge", "first tech competition"},
            new string[] {"frc", "first robotics competition", "first robotics challenge"},
            new string[] {"fll jr", "fll jr.", "fll junior", "first lego league jr", "first lego league jr.", "first lego league junior", "junior", "jr", "jr."}
        };

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
            string message;
            bool holder;
            if (!context.UserData.TryGetValue<bool>("Seen", out holder))
            {
                message = $"Hello! I am the FIRST WA Bot! I can help you volunteer or learn about FIRST!";
            } else
            {
                message = $"Sorry I did not understand: " + result.Query + "\n It resulted in intents: " + string.Join(", ", result.Intents.Select(i => i.Intent));
            }
            context.UserData.SetValue<bool>("Seen", true);
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("GetHelp")]
        public async Task GetHelp(IDialogContext context, LuisResult result)
        {
            context.UserData.SetValue<bool>("Seen", true);         
            string message = $"I think you wanted to learn about this applicaication when you said: " + result.Query + $"Hello, I am The FIRST WA Bot! I can tell you about FIRST WA Programs and opportunities";
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        //enum ChoiceOptions {  yes, no, };

        [LuisIntent("EndContact")]
        public async Task EndContact(IDialogContext context, LuisResult result)
        {
            string response = $"I think you wanted me to stop contacting when you said: " + result.Query + "  Do you want your data removed from our system?";
            await context.PostAsync(response);
            PromptDialog.Confirm(context, DeleteContactInformation, response);
        }

        private async Task DeleteContactInformation(IDialogContext context, IAwaitable<bool> options) {
            var response = string.Empty;
            switch (await options)
            {
                case true:
                    response = "Your information has been removed from the FIRST WA system";
                    break;
                default:
                    response = "Your information remains in the FIRST WA system";
                    break;

            }
            await context.PostAsync(response);

            context.Wait(MessageReceived);
        }

        [LuisIntent("GetInformation")]  
        public async Task GetInformation(IDialogContext context, LuisResult result)
        {
            context.UserData.SetValue<bool>("Seen", true);
            string message;

            var entities = new List<EntityRecommendation>(result.Entities);
            if (entities.Count > 1)
            {
                message = $"I'm sorry, I can only give you information on one thing at a time.";
            }
            else
            {
                var entity = entities.ElementAt(0);
                if (entity.Type.Equals("League"))
                {
                    if(entity.Entity.Equals("leagues") || entity.Entity.Equals("programs"))
                    {
                        message = $"FIRST has 4 programs for students (K-12).\nFIRST Robotics Competition(FRC) for grades 9 - 12.\nFIRST Tech Challenge(FTC) for grades 7 - 12.\nFIRST LEGO League(FLL) for grades 4 - 8. FIRST LEGO League Jr(FLLJr) for grades K-4) More info: http://www.firstinspires.org/";
                    } else 
                    {
                        bool found = false;
                        int i;
                        for(i = 0;  i < leagueWords.Length; i++)
                        {
                            for(int j = 0; j < leagueWords[i].Length; j++)
                            {
                                if(entity.Entity.Equals(leagueWords[i][j]))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                break;
                            }
                        }
                        if(found)
                        {
                            switch(i)
                            {
                                case 0:
                                    message = $"FIRST LEGO League teams (4th-8th grades) build and program a LEGO MINDSTORMS robot and research a real-world problem. More info: http://bit.ly/2aqDnAO";
                                    break;
                                case 1:
                                    message = $"FIRST Tech Challenge teams (grades 7-12) are challenged to design, build, program, and operate robots to play a floor game in an alliance format. More info: http://bit.ly/2axOiZ4";
                                    break;
                                case 2:
                                    message = $"FIRST Robotics Competition teams (grades 9-12) are challenged to build and program industrial-size robots to play a difficult field game against like-minded competitors. It’s as close to real-world engineering as a student can get. More info: http://bit.ly/29ZZO1B";
                                    break;
                                case 3:
                                    message = $"FIRST LEGO League Junior teams (ages 6-10) are introduced to STEM concepts using LEGO and simple machines. More info: http://bit.ly/2aaa6JT";
                                    break;
                                default:
                                    message = $"Internal code error occured. Try again.";
                                    break;
                            }
                        } else
                        {
                            message = $"I think that you wanted to learn more about " + entity.Entity + " when you said: " + result.Query;
                        }
                    }
                } else if(entity.Type.Equals("Event"))
                {
                    message = $"Competition events for FIRST Teams are found on the Calendar. Link: http://firstwa.org/Calendar";
                } else
                {
                    message = $"I think you wanted to learn more about FIRST WA Programs when you said: " + result.Query;
                }
            }

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
                string eventStrings = String.Empty;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(volunteerDataBaseUri);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    Volunteer newVolunteer = new Volunteer();
                    newVolunteer.Email = form.EmailAddress;
                    newVolunteer.Name = form.FullName;
                    newVolunteer.PostalCode = form.ZipCode;
                    newVolunteer.CanMessage = true;

                    // Get all events between now and the next 90 days
                    HttpResponseMessage response = await client.PostAsJsonAsync("api/volunteers", newVolunteer);
                    if (response.IsSuccessStatusCode)
                    {
                        await context.PostAsync("Thank you for giving us your information. If you'd like more information, reply what you'd like to learn about.");
                    }
                    else
                    {
                        await context.PostAsync($"Sorry, we seem to have some problem saving your details right now. Could you try again later? (Error:{response.StatusCode})");
                    }
                }
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
            context.UserData.SetValue<bool>("Seen", true);
            var entities = new List<EntityRecommendation>(result.Entities);
            if(entities.Count > 0 && entities.ElementAt(0).Entity.Equals("tribute"))
            {
                string message = $"The capital thanks you. May the odds be ever in your favor. But first, you must complete your interview. Here it comes.";
                await context.PostAsync(message);
            } else
            {
                string message = $"I can help you learn more about volunteering. Just a moment. I'm going to be asking you a few quick questions.";
                await context.PostAsync(message);
            }
            var volunteerForm = new FormDialog<VolunteerFormFlow>(new VolunteerFormFlow(), this.MakeVolunteerForm, FormOptions.PromptInStart);
            context.Call<VolunteerFormFlow>(volunteerForm, VolunteerFormComplete);
        }
    }
}