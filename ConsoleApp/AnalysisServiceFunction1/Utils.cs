using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisServiceFunction1
{
    public static class Utils
    {
        public static async Task<string> GetAccessToken(string aasUrl)
        {
            var tenantId = "<tenantId>";
            var appId = "<appId>";
            var appSecret = "<secret>";
            string authorityUrl = $"https://login.microsoftonline.com/{tenantId}";
            var authContext = new AuthenticationContext(authorityUrl);

            // Config for OAuth client credentials 
            var clientCred = new ClientCredential(appId, appSecret);
            AuthenticationResult authenticationResult = await authContext.AcquireTokenAsync(aasUrl, clientCred);

            //get access token
            return authenticationResult.AccessToken;
        }
    }
}
