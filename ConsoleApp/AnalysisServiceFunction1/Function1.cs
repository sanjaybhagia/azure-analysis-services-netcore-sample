using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AnalysisServiceFunction1
{
    public static class Function1
    {
        [FunctionName("CallAzureAnalysisService")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;


            //Grab the token
            //Get servername from Azure Analysis Service (Overview) resource 
            //Format: asazure://<region>.asazure.windows.net/<servername>
            var serverName = "asazure://australiasoutheast.asazure.windows.net/demoaas1";
            var token = await Utils.GetAccessToken("https://westeurope.asazure.windows.net");
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
                            log.LogInformation($"FirstName: {result[0]?.ToString()}; LastName: {result[1]?.ToString()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
            }

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

    }
}
