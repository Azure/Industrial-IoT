# Microsoft.Azure.IIoT.Deployment (Preview)

## Table Of Contents

* [Running Microsoft.Azure.IIoT.Deployment](#running-microsoft.azure.iiot.deployment)
  * [Run Modes](#run-modes)
  * [Running with User Credentials](#running-with-user-credentials)
  * [Running with Service Principal Credentials](#running-with-service-principal-credentials)
  * [Sample Run](#sample-run)
  * [Granting Admin Consent](#granting-admin-consent)
  * [Resource Cleanup](#resource-cleanup)
* [Configuration](#configuration)
  * [JSON File Configuration Provider](#json-file-configuration-provider)
  * [Environment Variables Configuration Provider](#environment-variables-configuration-provider)
  * [Command-line Configuration Provider](#command-line-configuration-provider)
  * [Parameters](#parameters)
* [Deployed Resources](#deployed-resources)
  * [AKS](#aks)
* [Missing And Planned Features](#missing-and-planned-features)
  * [Configuring Azure Resources](#configuring-azure-resources)
  * [User Interface](#user-interface)
* [Resources](#resources)

`Microsoft.Azure.IIoT.Deployment` is a command line application for deploying Industrial IoT solution.
It takes care of deploying Azure infrastructure resources and microservices of Industrial IoT solution.

The main difference compared to the [script based deployment](howto-deploy-all-in-one.md) option is that
from an infrastructure perspective `Microsoft.Azure.IIoT.Deployment` deploys microservices to an Azure
Kubernetes Service (AKS) cluster, while `deploy.ps1` runs the entire platform as a web application.

## Running Microsoft.Azure.IIoT.Deployment

`Microsoft.Azure.IIoT.Deployment` application can be run with either user or Service Principal credentials.
And those two methods have different setup requirements before the application can run successfully.
The main difference is that when running with user credentials, the application will require onboarding of
`AzureIndustrialIoTDeployment` Enterprise Application into your Azure AD and that will require consent of
organization administrator.

Additionally, `Microsoft.Azure.IIoT.Deployment` has three distinct run modes which require different
permissions.

### Run Modes

`Microsoft.Azure.IIoT.Deployment` supports the following three run modes:

| RunMode                   | Description                                                                                                                                       |
|---------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|
| `Full`                    | Performs Applications registration, deployment of Azure resources and deployment of microservices into AKS cluster. This is the **default** mode. |
| `ApplicationRegistration` | Performs only Applications registration and outputs JSON definition of Applications and Service Principals created.                               |
| `ResourceDeployment`      | Performs deployment of Azure resources and deployment of microservices into AKS cluster.                                                          |

For `Full` and `ApplicationRegistration` modes you would require **Administrator** role.

For `ResourceDeployment` mode **Contributor** role on a subscription would suffice. But it requires
definitions of existing Applications and Service Principals. You can get those if you first run in
`ApplicationRegistration` mode, which would then output those definitions.

Note that `Full` will do full deployment of the Industrial IoT solution. And `ApplicationRegistration` and
`ResourceDeployment` modes represent separation of the full deployment into two steps where first one
requires Administrator role, but second one does not.

### Running with User Credentials

Execution of `Microsoft.Azure.IIoT.Deployment` application with user credentials will result in initiation of
onboarding process of `AzureIndustrialIoTDeployment` Enterprise Application into your Azure AD, if it is not
already present. If you have admin role, then first run of the application will ask you whether you want to
add `AzureIndustrialIoTDeployment` Enterprise Application in your Azure AD. This will happen immediately after
login prompt. If you are not an administrator, then you will have the opportunity to propagate the request
to your organization administrator. After this, an administrator should also consent to the permissions that
the application requires to run in your Azure AD. The steps to do so are described in
[Granting Admin Consent](#granting-admin-consent) bellow.

Successful execution of `Microsoft.Azure.IIoT.Deployment` application will be possible only after
`AzureIndustrialIoTDeployment` Enterprise Application has been added to your Azure AD and consent has been
granted.

### Running with Service Principal Credentials

To run `Microsoft.Azure.IIoT.Deployment` with Service Principal credentials, you would first need to
create a Service Principal with enough permissions to execute the application and then pass Service
Principal `ClientId` (also referred as `ApplicationId` or `AppId`) and `ClientSecret` (also referred as
password) to the application via configuration. Please note that if you do not pass those configuration
properties, then the application will fall back to user credentials authentication flow.

To create a Service Principal you can use [Azure CLI](https://docs.microsoft.com/cli/azure/?view=azure-cli-latest).
And you can use the command bellow to create a Service Principal for password-based authentication
([source](https://docs.microsoft.com/cli/azure/create-an-azure-service-principal-azure-cli?view=azure-cli-latest#password-based-authentication)):

``` bash
az ad sp create-for-rbac --name ServicePrincipalName
```

1. In `Full` and `ApplicationRegistration` run modes, the application will create three Application
    registrations (with corresponding Service Principals) and assign permissions to the Applications. This
    operation required administrator role in the AD. So you would need to assign one of the following roles
    to a Service Principal:

    * [Global administrator](https://docs.microsoft.com/azure/active-directory/users-groups-roles/directory-assign-admin-roles#global-administrator--company-administrator)
    * [Application Administrator](https://docs.microsoft.com/azure/active-directory/users-groups-roles/directory-assign-admin-roles#application-administrator)
    * [Cloud Application Administrator](https://docs.microsoft.com/azure/active-directory/users-groups-roles/directory-assign-admin-roles#cloud-application-administrator)

    You can read more about these roles in
    [Administrator role permissions in Azure Active Directory](https://docs.microsoft.com/azure/active-directory/users-groups-roles/directory-assign-admin-roles).
    And you can use this guide to
    [view and assign administrator roles in Azure Active Directory](https://docs.microsoft.com/azure/active-directory/users-groups-roles/directory-manage-roles-portal).

    Additionally you would also need to give permission to the Service Principal to register applications.
    To do that follow these steps:

    1. Go to Azure Portal
    2. Then go to **App registration** service
    3. Find application registration for the Service Principal that you've created. It will have the same
        name as Service Principal.
    4. On the left side find **API permissions** under **Manage** group and select it. It will show list of
        permissions currently granted to Service Principal.
    5. Click on **Add a permission** and choose **Microsoft APIs** tab.
    6. Find **Microsoft Graph** and select it.
    7. Select **Application permissions**.
    8. Find **Application** group and expand it.
    9. Select either **Application.ReadWrite.OwnedBy** or **Application.ReadWrite.All**.
    10. Push **Add permission** button on the bottom.
    11. Click on `Grant admin consent for <your-directory-name>` button to grant consent for the Service
        Principal to create applications.

2. In `ResourceDeployment` mode Service Principal needs only **Contributor** role on a subscription.
    Service Principals created through ```az ad sp create-for-rbac``` would already have it.

### Sample Run

Sample run of `Full` deployment without any configuration parameters is described below.
`Microsoft.Azure.IIoT.Deployment` would interactively prompt for all required parameters that are not
provided through configuration. And if no authentication configuration is provided, then authentication flow
with user credentials will be used.

> **Note:** **5**th step below describes the error that you might get if you are lacking admin consent
and [Granting Admin Consent](#granting-admin-consent) walks you through steps to mitigate that problem.
You will still need to run the application once before granting the consent and that first run will fail.

1. Run `Microsoft.Azure.IIoT.Deployment` application from command line:

    ``` bash
    ./Microsoft.Azure.IIoT.Deployment
    ```

    On Linux and MacOS you might need to assign execute permission to the application before you can run it.
    To do that run the following command first:

    ``` bash
    chmod +x Microsoft.Azure.IIoT.Deployment
    ```

2. The application will ask you to choose your Azure environment, please select one. Those are either Azure
    Global Cloud or government-specific independent deployments of Microsoft Azure.

    ``` bash
    [13:57:12 INF] Application RunMode is not configured, default will be used: Full
    [13:57:25 INF] Starting full deployment of Industrial IoT solution.
    Please select Azure environment to use:

    Available Azure environments:
    0: AzureGlobalCloud
    1: AzureChinaCloud
    2: AzureUSGovernment
    3: AzureGermanCloud
    Select Azure environment:
    0
    ```

3. The application will ask you to provide your Tenant Id. This is the same as Directory Id. To find out your
    Directory Id:

    * Go to Azure Portal
    * Then go to **Azure Active Directory** service
    * In Azure Active Directory service page, on the left side find **Properties** under **Manage** group.
        Copy Directory Id from `Properties` page.

    Then provide Tenant Id to the application:

    ``` bash
    Please provide your TenantId:
    XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    ```

4. Now the application will ask you to login to Azure. During the first run, login flow will also ask your
    consent to provide permissions to `AzureIndustrialIoTDeployment` application if you are an admin in
    Azure AD. If you are not an admin, you would be asked to request consent from your admin.

    * On Windows, an interactive flow will launch a web browser and ask you to login to Azure.

    * On Linux and MacOS, a device login flow will be used which will ask you to manually open a web browser,
        go to Azure device login page and login with the code provided by the application. This can be done on
        a different machine if the one in question does not have a web browser. It will look something like
        this:

        ``` bash
        To sign in, use a web browser to open the page https://microsoft.com/devicelogin and enter the code XXXXXXXXX to authenticate.
        ```

5. At this point you might receive an error indicating that your administrator has not consented to use of
    `AzureIndustrialIoTDeployment` Enterprise Application, it will look like this:

    ``` bash
    [10:46:35 ERR] Failed to deploy Industrial IoT solution.
    System.AggregateException: One or more errors occurred. (AADSTS65001: The user or administrator has not consented to use the application with ID 'fb2ca262-60d8-4167-ac33-1998d6d5c50b' named 'AzureIndustrialIoTDeployment'. Send an interactive authorization request for this user and resource.
    Trace ID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    Correlation ID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    Timestamp: 2019-11-11 09:46:35Z)
    ...
    ```

    If you encounter this go to [Granting Admin Consent](#granting-admin-consent) to resolve this issue and
    then try to run the application again.

6. Select a Subscription within your Azure Account that will be used for creating Azure resources. If just
    one Subscription exists in your Account, then it will be listed and application will proceed further,
    otherwise you have to select Subscription from the list of available Subscriptions:

    ``` bash
    The following subscription will be used:
    DisplayName: 'Visual Studio Enterprise', SubscriptionId: 'XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX'
    ```

7. Provide a name for the AAD applications that will be registered. You will be asked for one name, but 3
    applications will be registered:

    * one with `-services` suffix added to provided name
    * one with `-clients` suffix added to provided name
    * one with `-aks` suffix added to provided name

    ``` bash
    Please provide a name for the AAD application to register:
    kkTest00100
    ```

8. Select Resource Group. You would be presented with options of either using an existing Resource Group or
    creating a new one. If you choose to use an existing one, you will be presented with a list of all Resource
    Groups within the Subscription to choose from. Otherwise, you would be asked to select a Region for the new
    Resource Group and provide a name for it. Output for the latter case is below.

    > **Note:** If the application encounters an error during execution, it will ask you whether to perform a
    cleanup or not. Cleanup works by deleting registered Applications and the Resource Group. So
    if an existing Resource Group has been selected for the deployment and an error occurs then selecting to
    perform cleanup will also trigger deletion of all resources previously present in the Resource Group. More
    details can be found in [Resource Cleanup](#resource-cleanup).

    ``` bash
    Do you want to use existing Resource Group or create a new one ?
    Please select E[existing] or N[new]
    n
    Please select region where resource group will be created.

    Available regions:
    0: eastus2
    1: westus2
    2: northeurope
    3: westeurope
    4: southeastasia
    Select a region:
    2
    Select resource group name, press Enter to use 'kkTest00100':

    [13:58:58 INF] Creating Resource Group: kkTest00100 ...
    [13:58:59 INF] Created Resource Group: kkTest00100
    ```

9. Now the application will perform application registration, Azure resource deployment and deployment of
    microservices to AKS cluster. This will take around 15 to 20 minutes depending on Azure Region. Along
    the way, it will log what Azure resources are created. Sample log looks like this:

    ``` bash
    [13:58:59 INF] Registering resource providers ...
    [13:59:04 INF] Registered resource providers
    [13:59:17 INF] Creating service application registration ...
    [13:59:24 INF] Created service application registration.
    [13:59:25 INF] Creating client application registration ...
    [13:59:28 INF] Created client application registration.
    [13:59:33 INF] Creating AKS application registration ...
    [13:59:36 INF] Created AKS application registration.
    [13:59:37 DBG] Assigning NetworkContributor role to Service Principal: kkTest00100-aks ...
    [13:59:38 DBG] ServicePrincipal creation has not propagated correctly. Waiting for 5 seconds before retry.
    [13:59:46 DBG] Assigned NetworkContributor role to Service Principal: kkTest00100-aks
    [13:59:52 INF] Creating Azure Network Security Group: iiotservices-nsg ...
    [13:59:57 INF] Created Azure Network Security Group: iiotservices-nsg
    [13:59:57 INF] Creating Azure Virtual Network: iiotservices-vnet ...
    [14:00:02 INF] Created Azure Virtual Network: iiotservices-vnet
    [14:00:02 INF] Creating Azure KeyVault: keyvault-16598 ...
    [14:00:34 INF] Created Azure KeyVault: keyvault-16598
    [14:00:34 INF] Adding certificate to Azure KeyVault: webAppCert ...
    [14:00:35 INF] Added certificate to Azure KeyVault: webAppCert
    [14:00:50 INF] Adding certificate to Azure KeyVault: aksClusterCert ...
    [14:00:51 INF] Added certificate to Azure KeyVault: aksClusterCert
    [14:01:06 INF] Creating Azure Operational Insights Workspace: workspace-81485 ...
    [14:01:06 INF] Creating Azure Application Insights Component: appinsights-14867 ...
    [14:01:13 INF] Created Azure Application Insights Component: appinsights-14867
    [14:01:42 INF] Created Azure Operational Insights Workspace: workspace-81485
    [14:01:42 INF] Creating Azure AKS cluster: akscluster-85960 ...
    [14:01:42 INF] Creating Azure Storage Account: storage91176 ...
    [14:02:02 INF] Created Azure Storage Account: storage91176
    [14:02:02 INF] Creating Blob Container: iothub-default ...
    [14:02:03 INF] Created Blob Container: iothub-default
    [14:02:03 INF] Creating Azure IoT Hub: iothub-32072 ...
    [14:04:10 INF] Created Azure IoT Hub: iothub-32072
    [14:04:13 INF] Creating Azure CosmosDB Account: cosmosdb-55238 ...
    [14:04:13 INF] Creating Azure Service Bus Namespace: sb-86372 ...
    [14:04:13 INF] Creating Azure Event Hub Namespace: eventhubnamespace-13716 ...
    [14:05:17 INF] Created Azure Service Bus Namespace: sb-86372
    [14:05:17 INF] Created Azure Event Hub Namespace: eventhubnamespace-13716
    [14:05:17 INF] Creating Azure Event Hub: eventhub-95041 ...
    [14:05:23 INF] Created Azure Event Hub: eventhub-95041
    [14:05:23 INF] Creating Azure AppService Plan: kktest00100-60509 ...
    [14:05:30 INF] Created Azure AppService Plan: kktest00100-60509
    [14:05:30 INF] Creating Azure AppService: kkTest00100 ...
    [14:05:30 INF] Creating SignalR Service: signalr-78951 ...
    [14:05:50 INF] Created Azure AppService: kkTest00100
    [14:07:05 INF] Created SignalR Service: signalr-78951
    [14:07:52 INF] Created Azure AKS cluster: akscluster-85960
    [14:08:22 INF] Created Azure CosmosDB Account: cosmosdb-55238
    [14:08:31 INF] Deploying Industrial IoT services to Azure AKS cluster ...
    [14:08:33 INF] Deployed Industrial IoT services to Azure AKS cluster
    [14:11:06 INF] Deploying proxy service to AppService: kkTest00100 ...
    [14:15:16 INF] Deployed proxy service to AppService: kkTest00100
    ```

10. After that you would be provided with an option to save connection details of deployed resources to
    '.env' file. If you choose to do so, please store the file is a secure location afterwards since it
    contains sensitive data such as connection string to the resources and some secrets.

    ``` bash
    Do you want to save connection details of deployed resources to '.env' file ? Please select Y[yes] or N[no]
    y
    [14:30:45 INF] Writing environment to file: '.env' ...
    ```

11. At this point the application should have successfully deployed Industrial IoT solution.

    ``` bash
    [14:36:27 INF] Updating RedirectUris of client application to point to 'https://kktest00100.azurewebsites.net/'
    [14:36:30 INF] Done.
    ```

### Granting Admin Consent

To grant admin consent you have to be **admin** in the Azure account. Here are the steps to do this:

* Go to Azure Portal
* Then go to **Enterprise Applications** service
* Find `AzureIndustrialIoTDeployment` in the list of applications and select it
* On the left side find **Permissions** under **Security** group and select it. It will show list of
    permissions currently granted to `AzureIndustrialIoTDeployment`.
* Click on `Grant admin consent for <your-directory-name>` button to grant consent for the application to
    manage Azure resources. It will show you a prompt with some additional permissions that
    `AzureIndustrialIoTDeployment` has requested.

### Resource Cleanup

`Microsoft.Azure.IIoT.Deployment` will prompt for resource cleanup if it encounters an error during
deployment of Industrial IoT solution. Cleanup works by deleting registered Applications and the
Resource Group that has been selected for deployment. This means, that one should be cautious with
cleanup if an existing Resource Group has been selected for the deployment, since the cleanup will
trigger deletion of **all** resources within the Resource Group, even the ones that have been present
before execution of `Microsoft.Azure.IIoT.Deployment`. We will introduce more granular cleanup in the
next iterations of the application.

Cleanup prompt looks like this:

```bash
Do you want to delete registered Applications and the Resource Group ? Please select Y[yes] or N[no]
y
[11:24:48 INF] Initiated deletion of Resource Group: IAIDeployment
[11:24:48 INF] Deleting application: IAIDeployment-services ...
[11:24:57 INF] Deleted application: IAIDeployment-services
[11:24:58 INF] Deleting application: IAIDeployment-clients ...
[11:25:03 INF] Deleted application: IAIDeployment-clients
[11:25:03 INF] Deleting application: IAIDeployment-aks ...
[11:25:04 INF] Deleted application: IAIDeployment-aks
```

## Configuration

For configuration of `Microsoft.Azure.IIoT.Deployment` we are using the following .Net Core configuration
providers, in the order of increasing priority:

* [JSON File Configuration Provider](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#json-configuration-provider)
* [Environment Variables Configuration Provider](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#environment-variables-configuration-provider)
* [Command-line Configuration Provider](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#command-line-configuration-provider)

This means that all of the configuration parameters of the app can be passed from all of those three sources.

You can read more about configuration in .Net Core
[here](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1).

### JSON File Configuration Provider

Deployment application will be looking for `appsettings.json` file in the current working directory of the
application to load configuration from it.

### Environment Variables Configuration Provider

Deployment application will also load configuration from environment variable key-value pairs at runtime.
It will be expecting `AZURE_IIOT_` prefix before the names of actual keys.

When working with hierarchical keys the sections and keys are flattened with the use of a colon (:) to
maintain the original structure. But a colon separator (:) may not work on all platforms (for example,
Bash). A double underscore (__) is supported by all platforms and is automatically replaced by a colon.
So you can use either `AZURE_IIOT_AUTH:AZUREENVIRONMENT` as environment variable name on supported platforms
or `AZURE_IIOT_AUTH__AZUREENVIRONMENT` everywhere.

### Command-line Configuration Provider

Deployment application will also load configuration from command-line argument key-value pairs at runtime.

Similar to environment variables, hierarchical sections and keys should be flattened when passing configuration through command-line arguments with the use of a colon (:).

Command line argument key-value pairs can be specified with:

| Key prefix        | Example                                    |
|-------------------|--------------------------------------------|
| No prefix         | `RunMode=ApplicationRegistration`          |
| Two dashes (--)   | `--Auth:AzureEnvironment=AzureGlobalCloud` |
| Forward slash (/) | `/Auth:AzureEnvironment=AzureGlobalCloud`  |

### Parameters

| Key                       | Value details                                                                                   | Description                                                                                                                                                        |
|---------------------------|-------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| RunMode                   | Valid value are: `Full`, `ApplicationRegistration`, `ResourceDeployment`                        | Determines which steps of Industrial IoT solution deployment will be executed.                                                                                     |
| Auth:AzureEnvironment     | Valid value are: `AzureGlobalCloud`, `AzureChinaCloud`, `AzureUSGovernment`, `AzureGermanCloud` | Defines which Azure cloud to use.                                                                                                                                  |
| Auth:TenantId             | Should be Guid.                                                                                 | Id of the tenant to be used.                                                                                                                                       |
| Auth:ClientId             | Should be Guid.                                                                                 | ClientId of Service Principal.                                                                                                                                     |
| Auth:ClientSecret         | String                                                                                          | ClientSecret of Service Principal.                                                                                                                                 |
| SubscriptionId            | Should be Guid.                                                                                 | Id of Azure Subscription within tenant.                                                                                                                            |
| ApplicationName           | Should be globally unique name.                                                                 | Name of the application deployment.                                                                                                                                |
| ApplicationURL            | Used only in `ApplicationRegistration` run mode.                                                | Base URL that will be used for generating RedirectUris for client application. This is required for enabling client authentication to use externally exposed APIs. |
| ResourceGroup:Name        |                                                                                                 | Name of the Resource Group where Azure resources will be created.                                                                                                  |
| ResourceGroup:UseExisting | `true` or `false`                                                                               | Determines whether an existing Resource Group should be used or a new one should be created.                                                                       |
| ResourceGroup:Region      | Supported regions are: `USEast2`, `USWest2`, `EuropeNorth`, `EuropeWest`, `AsiaSouthEast`       | Region where new Resource Group should be created.                                                                                                                 |
| ApplicationRegistration   | Object, see [Special Notes](#special-notes) bellow.                                             | Provides definitions of existing Applications and Service Principals to be used.                                                                                   |

#### Special Notes

1. **ApplicationName**

    This name will be used as name of App Service resource, thus determining its URL as
    `<ApplicationName>.azurewebsites.net`. As a result, it should be a globally unique name.

2. **ApplicationURL**

    Base URL that will be used for generating RedirectUris for client application. This is required for
    enabling client authentication for use of exposed APIs (including access to Swagger). Usually it would
    look like:

    > `<ApplicationName>.azurewebsites.net`

    This parameter is used only in `ApplicationRegistration` run mode.

3. **ApplicationRegistration**

    Provides definitions of applications and Service Principals to be used. Those definitions will be used
    instead of creating new application registrations and Service Principals for deployment of Azure
    resources.
  
    This is particularly useful in `ResourceDeployment` mode, where `Microsoft.Azure.IIoT.Deployment` would
    rely on existing Application Registrations and Service Principals.

    > **Note**: Execution in `ApplicationRegistration` run mode will output JSON object for this property that should be used on consequent `ResourceDeployment`
    run.
  
    Properties correspond to that of application registration and Service Principal manifests. Definition
    of application properties can be found
    [here](https://docs.microsoft.com/azure/active-directory/develop/reference-app-manifest).
  
    Application objects should contain the following properties:

    ``` json
    {
      "Id": "<guid>",
      "DisplayName": "<application name string>",
      "IdentifierUris": [
        "<unique user-defined URI for the application>"
      ],
      "AppId": "<guid>"
    }
    ```

    Service Principal objects should contain the following properties:

    ```json
    {
      "Id": "<guid>",
      "DisplayName": "<service principal name string>",
    }
    ```

    ApplicationRegistration object should have the keys as below. Note that:
    * Values of `ServiceApplication`, `ClientApplication` and `AksApplication` keys should be Application
      objects as described above.
    * Values of `ServiceApplicationSP`, `ClientApplicationSP` and `AksApplicationSP` keys should be Service
      Principal objects as described above.
    * `AksApplicationRbacSecret` is client secret (password) of AksApplication.

    ``` json
    "ApplicationRegistration": {
      "ServiceApplication": "<Application object>",
      "ServiceApplicationSP": "<Service Principal object>",
      "ClientApplication": "<Application object>",
      "ClientApplicationSP": "<Service Principal object>",
      "AksApplication": "<Application object>",
      "AksApplicationSP": "<Service Principal object>",
      "AksApplicationRbacSecret": "<string>"
    },
    ```

## Deployed Resources

### AKS

All cloud microservices of Industrial IoT solution are deployed to an AKS Kubernetes cluster.

#### Resource Definitions

The deployment creates `industrial-iot` namespace where all microservices are running. To provide connection
details for all Azure resources created by `Microsoft.Azure.IIoT.Deployment`, we created
`industrial-iot-env` secret, which is then consumed by deployments.

To see YAML files of all Kubernetes resources that are created by the application, please check
[deploy/src/Microsoft.Azure.IIoT.Deployment/Resources/aks/](../../deploy/src/Microsoft.Azure.IIoT.Deployment/Resources/aks/)
directory.

#### Kubernetes Dashboard

To see state of microservices you can check Kubernetes dashboard.

You can follow this tutorial to do that: [Access the Kubernetes web dashboard in Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/kubernetes-dashboard).

Alternatively, you can run the following commands:

1. Make sure you have `kubectl` installed. If it is not already installed, run the following command to
    download and install `kubectl`:

    ```bash
    az aks install-cli
    ```

    More details about the command and its optional parameters can be found [here](https://docs.microsoft.com/cli/azure/aks?view=azure-cli-latest#az-aks-install-cli).

2. Get access credentials for a managed Kubernetes cluster. You would need the name of the resource group
    containing AKS resource and the name of the AKS resource:

    ```bash
    az aks get-credentials --resource-group myResourceGroup --name myAKSCluster
    ```

    More details about the command and its optional parameters can be found [here](https://docs.microsoft.com/cli/azure/aks?view=azure-cli-latest#az-aks-get-credentials).

3. Create `ClusterRoleBinding` that will bind `ServiceAccount` of Kubernetes dashboard to `cluster-admin`
    role. By default, the Kubernetes dashboard is deployed with minimal read access and displays RBAC access
    errors.

    ```bash
    kubectl create clusterrolebinding kubernetes-dashboard --clusterrole=cluster-admin --serviceaccount=kube-system:kubernetes-dashboard
    ```

4. Open the dashboard for a Kubernetes cluster in a web browser. You would need the name of the resource
    group containing AKS resource and the name of the AKS resource:

    ```bash
    az aks browse --resource-group myResourceGroup --name myAKSCluster
    ```

    More details about the command and its optional parameters can be found [here](https://docs.microsoft.com/cli/azure/aks?view=azure-cli-latest#az-aks-browse).

    This will open a Kubernetes dashboard in a web browser.

#### APIs of Microservices

You should also be able to access APIs of microservices through URL of App Service. The URL is available
in overview of App Service. For example, you can access OPC Registry Service by appending `/registry/` to
the URL. It should look something like the link bellow, where `kktest00100` is the name of App Service.

* `https://kktest00100.azurewebsites.net/registry/`

The following microservice endpoints are exposed:

| URL Suffix  | Service Name                                   |
|-------------|------------------------------------------------|
| `registry/` | [OPC Registry](../services/registry.md)        |
| `twin/`     | [OPC Twin](../services/twin.md)                |
| `history/`  | [OPC Historian Access](../services/history.md) |
| `ua/`       | [OPC Gateway](../services/gateway.md)          |
| `vault/`    | [OPC Vault](../services/vault.md)              |

## Missing And Planned Features

### Configuring Azure Resources

Currently `Microsoft.Azure.IIoT.Deployment` application deploys predefined Azure resources. In the next
iterations we will provide a way to re-use existing Azure resources and specifying you own definitions of
resources.

### User Interface

We plan to add an interactive UI experience for `Microsoft.Azure.IIoT.Deployment` that will guide you
through deployment steps. This will be available only on Windows.

## Resources

### Azure AD

* [Administrator role permissions in Azure Active Directory](https://docs.microsoft.com/azure/active-directory/users-groups-roles/directory-assign-admin-roles)
* [View and assign administrator roles in Azure Active Directory](https://docs.microsoft.com/azure/active-directory/users-groups-roles/directory-manage-roles-portal)

### AKS Docs

* [Access the Kubernetes web dashboard in Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/kubernetes-dashboard)
* [Install applications with Helm in Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/kubernetes-helm)
* [Create an HTTPS ingress controller and use your own TLS certificates on Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/ingress-own-tls)

### CosmosDB Docs

* [Request Units in Azure Cosmos DB](https://docs.microsoft.com/azure/cosmos-db/request-units)
