using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UpdateSecureConfiguration
{
    class Program
    {
        static void Main(string[] args)
        {
            //var values = new string[,] { { "ID1","Val1" },{ "ID2","Val2" },{ "ID3","Val3" } };
            //var output = JsonConvert.SerializeObject(values);
            var connstring = args[0];
            var configValues = JsonConvert.DeserializeObject<List<string[]>>(args[1]);

            var crmSvc = new CrmServiceClient(connstring);
            if (crmSvc != null && crmSvc.IsReady)
            {
                //Display the CRM version number and org name that you are connected to
                Console.WriteLine("Connected to CRM! (Version: {0}; Org: {1}",
                crmSvc.ConnectedOrgVersion, crmSvc.ConnectedOrgUniqueName);

                var orgService = crmSvc.OrganizationServiceProxy;
                foreach (var item in configValues)
                {
                    var secureConfigGuid = item[0];
                    var secureConfig = item[1];

                    var stepQuery = new QueryExpression();

                    stepQuery.EntityName = "sdkmessageprocessingstep";
                    stepQuery.ColumnSet = new ColumnSet(new[] { "sdkmessageprocessingstepid", "sdkmessageprocessingstepsecureconfigid", "plugintypeid" });
                    stepQuery.Criteria.AddCondition(new ConditionExpression("sdkmessageprocessingstepid", ConditionOperator.Equal, new Guid(secureConfigGuid)));

                    var step = orgService.RetrieveMultiple(stepQuery)[0];

                    // Test if plugin already has a Secure Configuration:
                    if (!step.Attributes.Contains("sdkmessageprocessingstepsecureconfigid"))
                    {
                        Console.WriteLine("Plugin has no Secure Configuration yet, so create one.");

                        // create new secure configuration record
                        var processingStepSecureConfiguration = new Entity("sdkmessageprocessingstepsecureconfig");
                        processingStepSecureConfiguration.Attributes.Add("secureconfig", secureConfig);
                        processingStepSecureConfiguration.Id = orgService.Create(processingStepSecureConfiguration);

                        // now attach secure configuration record to processing step
                        step.Attributes["sdkmessageprocessingstepsecureconfigid"] = processingStepSecureConfiguration.ToEntityReference();
                        orgService.Update(step);
                    }
                    else
                    {
                        Console.WriteLine("Plug-in step already has a Secure Configuration entity, so retrieve that entity and update the configuration.");

                        // Retrieve and update secure configuration record
                        var secureConfigurationReference = step.Attributes["sdkmessageprocessingstepsecureconfigid"] as EntityReference;
                        var secureConfiguration = orgService.Retrieve(secureConfigurationReference.LogicalName, secureConfigurationReference.Id, new ColumnSet(new[] { "sdkmessageprocessingstepsecureconfigid", "secureconfig" }));
                        secureConfiguration.Attributes["secureconfig"] = secureConfig;
                        orgService.Update(secureConfiguration);
                    }
                }
            }
            else
            {
                // Display the last error.
                Console.WriteLine("An error occurred: {0}", crmSvc.LastCrmError);

                // Display the last exception message if any.
                Console.WriteLine(crmSvc.LastCrmException.Message);
                Console.WriteLine(crmSvc.LastCrmException.Source);
                Console.WriteLine(crmSvc.LastCrmException.StackTrace);

                return;
            }
        }
    }
}
