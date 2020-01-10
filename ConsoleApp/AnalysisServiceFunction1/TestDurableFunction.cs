using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AnalysisServiceFunction1
{
    public static class TestDurableFunction
    {
        [FunctionName("TestDurableFunction")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            //Call the function to execute
            outputs.Add(await context.CallActivityAsync<string>("TestDurableFunction_CallAzureAnalysisService", "test"));

            return outputs;
        }

        [FunctionName("TestDurableFunction_CallAzureAnalysisService")]
        public static async Task<string> CallAzureAnalysisService([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");

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

            return $"Hello {name}!";
        }

        [FunctionName("TestDurableFunction_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("TestDurableFunction", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}