Pre-work:

Create one-time-setup Resource Group
    Create Azure Container Registry (Azure Portal)
        Navigate to Access Keys
        Check "Enable Admin user" - this is needed for the pipeline to push containers to the registry.
    Create User Assigned Managed Identity (Azure Portal)
         Assign User Assigned Managed Identity "AcrPull" role in the setup Resource Group

Create Azure DevOps Service Connection (Azure DevOps/Project Settings/Service Connections) using Azure Resource Manage/Service Principal - Automated. Check "Grant pipeline permissions" in the lower left corner.

Create Azure DevOps Service Connection (Azure DevOps/Project Settings/Service Connections) using Docker Registry - Other Registry - obtain relevant information from the Azure Portal page for the Azure Container Registry.

Replace 'onion8-clean-serviceconnection' with new Service Connection name in onion8-clean-pipeline.yml and deploy-env.yml (AzDo no longer supports variables as service connections, this has to be done manually)

Create UAT Resource Group
    Create loganalyticsworkspace
    Create appinsights pointed to loganalytics

Create Prod Resource Group
    Create loganalyticsworkspace
    Create appinsights pointed to loganalytics
    
Create Variable Groups for pipeline (one per desired environment - it's easiest to create tdd and clone it for successive environments)
    *appInsightsConnectionString         gathered from App Insights/Properties in the appsinsights instance in the environment's resource group
    AzureLocation                       appropriate Azure location for resources
    AzureFeedName                       projectname\name-of-nuget-feed-for-storing-and-retrieving-packages
    *containerAppEnvironmentName        arbitrary name for container app environment
    *containerAppLogAnalyticsName       gathered from the Overview page of the App Insights instance in the environment's resource group
    *containerAppName                   arbitrary name for the container app
    containerAppScaledUpCPU             .5
    containerAppScaledUpMem             1.0
    containerAppScaledUpReplicas        2
    containerImage                      $(registryLoginServer)/reponame:tag
    !databaseAction                     Update
    databaseEdition                     Basic
    *databaseName                       arbitrary name for Azure SQL database
    databasePassword                    arbitrary password to be set in Azure SQL (MS enforces complexity - lower, upper, number)
    databasePerformanceLevel            Basic
    databaseUser                        arbitrary user name to be set in Azure SQL 
    *databaseServerName                 arbitrary name for Azure SQL server
    *environment                        three/four-letter abbreviation for environment name
    httpPort                            8080
    !registryLoginServer                gathered from Azure Container Registry/Overview
    SubscriptionId                      gathered from Azure Portal on any resource in the subscription
    *resourceGroupName                  arbitrary name for given resource group, frequently constructed from text-$(environment)
    !uamiName                           arbitrary name for User Assigned Managed Identity
    !uamiRGName                         the name of the Resource Group in which the User Assigned Managed Identity exists

    The variables marked with * need to be customized per-environment in the Variable Group.
    Variables marked with ! should be consistent across all variable groups.
    Unmarked variables can be set per-environment without impact to other environments.

Update variable group references:
    Change line 21 in onion8-clean-pipeline to read  "- group: <name of tdd variable group>"
    Change line 21 in onion8-clean-pipeline to read  "- group: <name of tdd variable group>"
    Change line 112 in onion8-clean-pipeline to read "containerRegistry: 'name of Docker service connection created above'"
    Change line 127 in onion8-clean-pipeline to read "- group: <name of tdd variable group>"
    Change line 203 in onion8-clean-pipeline to read "- group: <name of tdd variable group>"
    Change line 244 in onion8-clean-pipeline to read "- group: <name of uat variable group>"
    Change line 322 in onion8-clean-pipeline to read "- group: <name of prod variable group>"

Increment minor version number by one. (This prevents pipeline failure in re-used Artifact feeds)

Commit pipeline changes and push.

Create Environments (one per desired Environment) - these will require authorization when invoked during the first run of the pipeline.

Create Pipeline (Azure DevOps Portal) - use /onion8-clean-pipeline.yml as the YAML source.

Run Pipeline.  This will require a number of one-time authorizations.
