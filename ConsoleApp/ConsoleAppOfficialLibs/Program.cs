using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        //this is C# 7.1 feature - refer to this link to set C# to 7.1
        //https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version
        //otherwise user other approaches to call async method in main method with static void signature
        static async Task Main(string[] args)
        {
            await GetDataFromAzureAnalysisService();
            Console.WriteLine("Done");
            Console.ReadKey();
        }

        private static async Task GetDataFromAzureAnalysisService()
        {
            //Grab the token
            //Get servername from Azure Analysis Service (Overview) resource 
            //Format: asazure://<region>.asazure.windows.net/<servername>
            var serverName = "asazure://australiasoutheast.asazure.windows.net/demoaas";
            var token = await GetAccessToken("https://westeurope.asazure.windows.net");
            var connectionString = $"Provider=MSOLAP;Data Source={serverName};Initial Catalog=adventureworks;User ID=;Password={token};Persist Security Info=True;Impersonation Level=Impersonate";

            try
            {
                //read data from AAS
                using (AdomdConnection connection = new AdomdConnection(connectionString))
                {
                    connection.Open();
                    var mdX = @"EVALUATE (
                      TOPN (
                        10,
                        SUMMARIZECOLUMNS (
                          'Customer'[First Name],
                          'Customer'[Last Name]
                        ),
                        'Customer'[Last Name],1
                      )
                    )";
                    using (AdomdCommand command = new AdomdCommand(mdX, connection))
                    {
                        var results = command.ExecuteReader();
                        foreach (var result in results)
                        {
                            Console.WriteLine($"FirstName: {result[0]?.ToString()}; LastName: {result[1]?.ToString()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private static async Task<string> GetAccessToken(string aasUrl)
        {
            var tenantId = "<Directory ID your your Azure Active Directory>";
            var appId = "<Application Id of the app you registered>";
            var appSecret = "<Secret key that you created for the app>";
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
