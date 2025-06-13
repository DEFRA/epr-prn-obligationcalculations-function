# epr-prn-obligationcalculations-function

## Overview

Functions to retrieve approved POM submissions and process them to complete obligation calculation.


## Environment Variables - deployed environments

The structure of the application settings can be found in the repository. Example configurations for the different environments can be found in [epr-app-config-settings](https://dev.azure.com/defragovuk/RWD-CPR-EPR4P-ADO/_git/epr-app-config-settings).

| Variable Name										| Description																								|
|---------------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| SubmissionsServiceApi__BaseUrl					| Common Data API base URL of POM submissions endpoint														|
| SubmissionsServiceApi__SubmissionsEndPoint		| Endpoint that retrieves approved submissions POM data from Common data API								|
| SubmissionsServiceApi__ClientId					| App regisration client id if available																	|
| SubmissionsServiceApi__LogPrefix					| Logger prefix to log information, warning, and error														|
| PrnServiceApi__BaseUrl							| Common Backend API base URL of PRN endpoint																|
| PrnServiceApi__PrnCalculateEndPoint				| Endpoint that process the retrieved submissions to complete obligation calculation via Common Backend API	|
| PrnServiceApi__ClientId							| App regisration client id																					|
| PrnServiceApi__LogPrefix							| Logger prefix to log information, warning, and error														|
| ServiceBus__FullyQualifiedNamespace				| Fully qualified namespace of a Service Bus																|
| ServiceBus__ObligationQueueName					| Queue to store retrived approved submissions POM data per organisation									|
| ServiceBus__ObligationLastSuccessfulRunQueueName	| Queue to store StoreApprovedSubmissionsFunctions's last successful run date								|
| ServiceBus__LogPrefix								| Logger prefix to log information, warning, and error														|
| ApplicationConfig__DefaultRunDate					| Date to be used when there is no date available in ObligationLastSuccessfulRunQueueName queue				|
| ApplicationConfig__LogPrefix						| Logger prefix to log information, warning, and error														|
| ApplicationConfig__FunctionIsEnabled				| Feature flag to enable or disable to execute business logic in StoreApprovedSubmissionsFunction			|
| StoreApprovedSubmissions__Schedule				| Timer trigger CRON expression to schedule StoreApprovedSubmissionsFunction								|

## Retry policy

Polly is used to retry http requests. Policies are added to http clients in the in the `ConfigurationExtensions` class.

## Running on a developer machine
To run locally, create a file `local.settings.json`. This file is in `.gitignore`.
Then, Replace service bus namespace with connection string in AddAzureClients method for ServiceBusClient in the `ConfigurationExtensions` class.

```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SubmissionsServiceApi__BaseUrl": "",
    "SubmissionsServiceApi__SubmissionsEndPoint": "api/submissions/v1/pom/approved/",
    "SubmissionsServiceApi__LogPrefix": "[EPR.PRN.ObligationCalculation]",
    "SubmissionsServiceApi__ClientId": "",
    "PrnServiceApi__LogPrefix": "[EPR.PRN.ObligationCalculation]",
    "PrnServiceApi__BaseUrl": "",
    "PrnServiceApi__PrnCalculateEndPoint": "api/v1/prn/organisation/{0}/calculate",
    "PrnServiceApi__ClientId": "",
    "ServiceBus__FullyQualifiedNamespace": "",
    "ServiceBus__ObligationQueueName": "defra.epr.obligation.dev10",
    "ServiceBus__ObligationLastSuccessfulRunQueueName": "defra.epr.obligationlastsuccessfulrun.dev10",
	"ServiceBus__LogPrefix": "[EPR.PRN.ObligationCalculation]",
    "ApplicationConfig__LogPrefix": "[EPR.PRN.ObligationCalculation]",
    "ApplicationConfig__DefaultRunDate": "2024-01-01",
    "ApplicationConfig__FunctionIsEnabled": "true",
    "StoreApprovedSubmissions__Schedule": "0 0 14 * * *"
  }
}
```
