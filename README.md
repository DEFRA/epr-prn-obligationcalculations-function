# epr-prn-obligationcalculations-facade
The README should include the following (if they apply):

- **Description of the product** – what the service or product is, and what role this repo performs within it

- **Prerequisites** – what you need to install or configure before you can set up the repo

- **Setup process** - how to set up your local environment to work on the repo, including:

  - development tools

  - test tools

- **How to run in development** – how to locally run the application in development mode after setup

- **How to run tests** – how to run the test suite, broken into different categories if relevant (unit, integration, acceptance)

- **Contributing to the project** - what to know before you submit your first pull request (this could also be in the form of a CONTRIBUTING.md  file)

- **Licence information** – what licence the repo uses (in addition to your LICENSE file)
## Running on a developer machine
To run locally, create a file `local.settings.json`. This file is in `.gitignore`.

```
{
    "IsEncrypted": false,
    "Values": {
		"AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "CommonDataApi__BaseUrl": "",
        "CommonDataApi__SubmissionsEndPoint": "api/submissions/v1/pom/approved/",
        "CommonBackendApi__BaseUrl": "",
        "CommonBackendApi__PrnCalculateEndPoint": "api/v1/prn/organisation/{0}/calculate",
        "CommonBackendApi__LastSuccessfulRunDateEndPoint": "api/v1/prn/lastSuccessfulRunDate",
        "ServiceBus__Namespace": "",
        "ServiceBus__ObligationQueueName": "",
        "ServiceBus__ObligationLastSuccessfulRunQueueName": "",
        "ServiceBus__ConnectionString": "",
        "ApplicationConfig__DeveloperMode": true,
        "ApplicationConfig__DefaultRunDate": "2024-01-01",
        "StoreApprovedSubmissions__Schedule": "0 */30 * * * *"
    }
}
```
