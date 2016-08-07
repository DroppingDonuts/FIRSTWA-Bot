using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Configuration;
using System.Globalization;

namespace VolunteerBot
{
    public static partial class Authenticator
    {
        //
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The App Key is a credential used by the application to authenticate to Azure AD. 
        // The Tenant is the name of the Azure AD tenant in which this application is registered.
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The Authority is the sign-in URL of the tenant.
        //
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        public static void Configure()
        {
            // Create the authentication context to be used to acquire tokens.
            Authenticator.authContext = new AuthenticationContext(authority);
            Authenticator.clientCredential = new ClientCredential(clientId, appKey);
        }
    }
}
