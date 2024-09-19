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
        "Submissions__BaseUrl": "http://localhost:5001/api/submissions",
        "Submissions__EndPoint": "/v1/pom/approved/",
        "AppInsights__ClientId": "",
        "AppInsights__TenantId": "",
        "AppInsights__ClientSecret": "",
        "AppInsights__WorkspaceId": "",
        "ServiceBus__Namespace": "DEVRWDINFSB1402.servicebus.windows.net",
        "ServiceBus__QueueName": "defra.epr.obligation",
        "ServiceBus__ConnectionString": "",
        "ApiConfig__DeveloperMode": true,
        "StoreApprovedSubmissions__Schedule": "*/10 * * * * *"
    }
}
```
