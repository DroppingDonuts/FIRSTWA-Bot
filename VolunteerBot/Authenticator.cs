using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace VolunteerBot
{
    public static partial class Authenticator
    {
        //
        // To authenticate to the VolunteerDataWebApi, the client needs to know the service's App ID URI.
        //
        private static string volunteerDataAppIdUri = ConfigurationManager.AppSettings["app:VolunteerDataAppIdUri"];

        private static AuthenticationContext authContext = null;
        private static ClientCredential clientCredential = null;

        public static async Task AddAccessTokenToRequest(HttpClient httpClient)
        {
            //
            // Get an access token from Azure AD using client credentials.
            // If the attempt to get a token fails because the server is unavailable, retry twice after 3 seconds each.
            //
            const int maxRetries = 2;
            const int retryIntervalMs = 3000;

            AuthenticationResult result = null;
            int retryCount = 0;
            bool retry = false;

            do
            {
                retry = false;
                try
                {
                    // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
                    result = await authContext.AcquireTokenAsync(volunteerDataAppIdUri, clientCredential);
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable" && retryCount < maxRetries)
                    {
                        retry = true;
                        retryCount++;
                        Thread.Sleep(retryIntervalMs);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            } while (retry == true);
            
            // Add the access token to the authorization header of the request.
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        }
    }
}