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
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Internals.Fibers;

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
            new string[] { "fll", "first lego league", "lego robotics"},
            new string[] {"ftc", "first tech challenge", "first tech competition"},
            new string[] {"frc", "first robotics competition", "first robotics challenge"},
            new string[] {"fll jr", "fll jr.", "fll junior", "first lego league jr", "first lego league jr.", "first lego league junior"}
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
            string message = $"I think you wanted to learn about this applicaication when you said: " + result.Query + $"Hello, I am The FIRST WA Bot! I can tell you about FIRST Washington Programs and opportunities";
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("EndContact")]
        public async Task EndContact(IDialogContext context, LuisResult result)
        {
            context.UserData.RemoveValue("Seen");
            string message = $"I think you wanted me to stop contacting you when you said: " + result.Query + ".\n";

            Volunteer volunteer = null;
            int volunteerId;
            if (context.UserData.TryGetValue<int>("VolunteerId", out volunteerId))
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(volunteerDataBaseUri);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.GetAsync($"api/volunteers/{volunteerId}");
                    if (response.IsSuccessStatusCode)
                    {
                        string responseJson = response.Content.ReadAsStringAsync().Result;
                        volunteer = JsonConvert.DeserializeObject<Volunteer>(responseJson);

                        if (volunteer != null)
                        {
                            PromptDialog.Confirm(context, DeleteContactInformation, message + $"Do you want your data for {volunteer.Email} removed from our system?");
                        }
                    }
                }
            }

            if (volunteer == null)
            {
                await context.PostAsync(message + "We'll still be here if you want to talk to us in the future, bye!");
                context.Wait(MessageReceived);
            }
        }

        private async Task DeleteContactInformation(IDialogContext context, IAwaitable<bool> deleteInfo)
        {
            int volunteerId;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(volunteerDataBaseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (context.UserData.TryGetValue<int>("VolunteerId", out volunteerId))
                {
                    if (await deleteInfo)
                    {
                        HttpResponseMessage response = await client.DeleteAsync($"api/volunteers/{volunteerId}");
                        if (response.IsSuccessStatusCode)
                        {
                            context.UserData.RemoveValue("VolunteerId");
                            await context.PostAsync($"We've removed your information from our system. Thanks for talking to us!");
                        }
                        else
                        {
                            await context.PostAsync($"I failed to remove your data from our servers! I'm sorry! Please try again later ...");
                        }
                    }
                    else
                    {
                        HttpResponseMessage response = await client.GetAsync($"api/volunteers/{volunteerId}");
                        if (response.IsSuccessStatusCode)
                        {
                            string responseJson = response.Content.ReadAsStringAsync().Result;
                            Volunteer volunteer = JsonConvert.DeserializeObject<Volunteer>(responseJson);
                            volunteer.CanMessage = false;
                            response = await client.PutAsJsonAsync($"api/volunteers/{volunteerId}", volunteer);
                            if (response.IsSuccessStatusCode)
                            {
                                await context.PostAsync($"Okay, we'll stop contacting you, but your information is still with us if you want to talk to us again in the future. Thank you!");
                            }
                        }
                        if (!response.IsSuccessStatusCode)
                        {
                            await context.PostAsync($"I'm sorry, I can't seem to update your preference not to be contacted right now! Please try again later ...");
                        }
                    }
                }
                else
                {
                    await context.PostAsync("It looks like your information has already been removed. Thanks for talking to us!");
                }
            }
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
                        message = $"I think that you wanted to learn more about all the FIRST WA programs when you said: " + result.Query;
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
                                    message = $"FIRST LEGO League teams (4th-8th grades) build and program a LEGO MINDSTORMS robot and research a real-world problem. More info:\nhttp://bit.ly/2aqDnAO";
                                    break;
                                case 1:
                                    message = $"FIRST Tech Challenge teams (grades 7-12) are challenged to design, build, program, and operate robots to play a floor game in an alliance format. More info:\nhttp://bit.ly/2axOiZ4";
                                    break;
                                case 2:
                                    message = $"FIRST Robotics Competition teams (grades 9-12) are challenged to build and program industrial-size robots to play a difficult field game against like-minded competitors. It’s as close to real-world engineering as a student can get. More info:\nhttp://bit.ly/29ZZO1B";
                                    break;
                                case 3:
                                    message = $"FIRST LEGO League Junior teams (ages 6-10) are introduced to STEM concepts using LEGO and simple machines. More info:\nhttp://bit.ly/2aaa6JT";
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

                    HttpResponseMessage response = await client.PostAsJsonAsync("api/volunteers", newVolunteer);
                    if (response.IsSuccessStatusCode)
                    {
                        Volunteer outVolunteer = await response.Content.ReadAsAsync<Volunteer>();
                        if (outVolunteer != null)
                        {
                            context.UserData.SetValue<int>("VolunteerId", outVolunteer.Id);
                            await context.PostAsync("Thank you for giving us your information. If you'd like more information, reply what you'd like to learn about.");
                        }
                        else
                        {
                            await context.PostAsync("Failed to get volunteer ID that was added. Oops.");
                        }
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

        private async Task UpdateContactInformation(IDialogContext context, IAwaitable<bool> isAlreadyRegistered)
        {
            if (await isAlreadyRegistered)
            {
                await context.PostAsync("Great! How can I help you? I can tell you more about FIRST WA robotics, robotics leagues or volunteer roles.");
                context.Wait(MessageReceived);
            }
            else
            {
                await context.PostAsync("I'm sorry, let me get your details first then.");
                AddContactInformation(context);
            }
        }

        private void AddContactInformation(IDialogContext context)
        {
            var volunteerForm = new FormDialog<VolunteerFormFlow>(new VolunteerFormFlow(), this.MakeVolunteerForm, FormOptions.PromptInStart);
            context.Call<VolunteerFormFlow>(volunteerForm, VolunteerFormComplete);
        }

        [LuisIntent("GetSignUp")]
        public async Task GetSignUp(IDialogContext context, LuisResult result)
        {
            context.UserData.SetValue<bool>("Seen", true);
            int volunteerId;
            string message = "I can help you learn more about volunteering.";
            if (context.UserData.TryGetValue<int>("VolunteerId", out volunteerId))
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(volunteerDataBaseUri);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.GetAsync($"api/volunteers/{volunteerId}");
                    if (response.IsSuccessStatusCode)
                    {
                        string responseJson = response.Content.ReadAsStringAsync().Result;
                        Volunteer volunteer = JsonConvert.DeserializeObject<Volunteer>(responseJson);
                        if (volunteer != null)
                        {
                            message = $" It looks like we've previously chatted about this. Are you {volunteer.Name} ({volunteer.Email}) at {volunteer.PostalCode}?";
                            PromptDialog.Confirm(context, UpdateContactInformation, message);
                        }
                        else
                        {
                            await context.PostAsync($"Received a response from api/volunteers/{volunteerId} that I could not parse. I'm confused! T_T");
                            context.Wait(MessageReceived);
                        }
                    }
                    else
                    {
                        await context.PostAsync("Sorry, I couldn't figure out if we'd chatted before, can I get your details again?");
                        AddContactInformation(context);
                    }
                }
            }
            else
            {
                message += " I'm going to be asking you a few quick questions.";
                await context.PostAsync(message);
                AddContactInformation(context);
            }
        }
    }
}
public class Awaiter : IAwaitable<bool>
{
    public IAwaiter<bool> GetAwaiter()
    {
        throw new NotImplementedException();
    }
}