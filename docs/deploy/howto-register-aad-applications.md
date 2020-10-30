# AAD App Registration  <!-- omit in toc -->

[Home](readme.md)

## Table Of Contents <!-- omit in toc -->

* [Introduction](#introduction)
* [PowerShell scripts](#powershell-scripts)
* [Microsoft.Azure.IIoT.Deployment](#microsoftazureiiotdeployment)

## Introduction

Components of the Azure Industrial IoT platform require several App Registrations in your Azure Active Directory
(AAD) to run. Those App Registrations are responsible for:

* providing identity for back-end microservices to run
* defining authentication methods and details for client applications

Both of our deployment methods (`deploy.ps1` script and the `Microsoft.Azure.IIoT.Deployment` application) create
those App Registrations for you by default. But they both require user to have `Administrator` role in the AAD
for deployment to succeed. In cases when the user does not have the `Administrator` role, the process of
deployment can be separated into 2 distinct states:

* creation and setup of the App Registrations in AAD tenant
* creation of Azure resources and deployment of the Azure Industrial IoT platform components

With this separation, the first step can be performed by an IT administrator. Deployments will output details
of App Registrations which can then be passed to a user for running the second step.

## PowerShell scripts

`deploy.ps1` PowerShell script calls `aad-register.ps1` for creating the following App Registrations:

* `<application-name>-service`: used for providing identity for back-end microservices
* `<application-name>-client`: used for authentication of native clients, such as CLI tool
* `<application-name>-web`: used for authentication of Web clients, such as Swagger

Use the following command to run `aad-register.ps1` script for creation of the App Registrations.
Specify desired application name instead of the `<application-name>` and your tenant id.
Follow the script commands and provide additional details where needed.

> Note: `ReplyUrl` has the following format `https://<application-name>.azurewebsites.net/`, as we are using
> an instance of App Service to host Engineering Tool.

```bash
cd deploy/scripts
./aad-register.ps1 -Name <application-name> -ReplyUrl https://<application-name>.azurewebsites.net/ -TenantId XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX -Output aad.json
```

The output of the script is a JSON file containing the relevant information to be used as part of the
deployment and must be passed to the `deploy.ps1` script in the same folder using `-aadConfig` argument.

Use the following command to deploy the Azure Industrial IoT platform with pre-created App Registrations.
Follow the script commands and provide additional details where needed.

```bash
./deploy.ps1 -aadConfig aad.json -applicationName <application-name>
```

You can find all configuration options for `deploy.ps` [here](./howto-deploy-all-in-one.md#deployment-script-options).

## Microsoft.Azure.IIoT.Deployment

Find links for downloading executables of the `Microsoft.Azure.IIoT.Deployment` application [here](./howto-deploy-aks.md#download-microsoftazureiiotdeployment-binaries).

`Microsoft.Azure.IIoT.Deployment` application creates the following App Registrations:

* `<application-name>-service`: used for providing identity for back-end microservices
* `<application-name>-client`: used for authentication of native and Web clients, such as CLI tool or Swagger
* `<application-name>-aks`: used for providing identity for AKS cluster

Use the following command to run the `Microsoft.Azure.IIoT.Deployment` application for App Registration creation
and setup only. Follow the commands and provide additional details where needed.

> Note: `ApplicationUrl` has the following format `<application-name>.<region>.cloudapp.azure.com`, as we are
> using a DNS entry for a Public IP Address resource to provide a URL for the Ingress. So please make sure to
> use correct format for the region that you want to deploy to.

```bash
.\Microsoft.Azure.IIoT.Deployment.exe RunMode=ApplicationRegistration Auth:AzureEnvironment=AzureGlobalCloud ApplicationName=<application-name> ApplicationUrl=<application-name>.northeurope.cloudapp.azure.com
```

The command will output a JSON object that contains details and credentials of created App Registrations and
associated Service Principals. Those should be used as an input for the next step. To do that, please create
a `appsettings.json` file in the same directory as the `Microsoft.Azure.IIoT.Deployment` application and use the
JSON object as value of the `ApplicationRegistration` key as shown bellow.

```json
{
    "ApplicationRegistration": {
        "ServiceApplication": {
            "Id": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
            "DisplayName": "<application-name>-service",
            "IdentifierUris": [
                "https://<tenant-id>/<application-name>-service"
            ],
            "AppId": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        },
        "ServiceApplicationSP": {
            "Id": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
            "DisplayName": "<application-name>-service"
        },
        "ServiceApplicationSecret": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
        "ClientApplication": {
            "Id": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
            "DisplayName": "<application-name>-client",
            "IdentifierUris": [
                "https://<tenant-id>/<application-name>-client"
            ],
            "AppId": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        },
        "ClientApplicationSP": {
            "Id": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
            "DisplayName": "<application-name>-client"
        },
        "ClientApplicationSecret": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
        "AksApplication": {
            "Id": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
            "DisplayName": "<application-name>-aks",
            "IdentifierUris": [
                "https://<tenant-id>/<application-name>-aks"
            ],
            "AppId": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
        },
        "AksApplicationSP": {
            "Id": "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
            "DisplayName": "<application-name>-aks"
        },
        "AksApplicationSecret": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
    }
}
```

After that, use the following command to deploy the Azure Industrial IoT platform with pre-created App
Registrations. Follow the commands and provide additional details where needed.

```bash
.\Microsoft.Azure.IIoT.Deployment.exe RunMode=ResourceDeployment Auth:AzureEnvironment=AzureGlobalCloud ApplicationName=<application-name> ResourceGroup:Region=EuropeNorth
```

You can find all configuration options for the `Microsoft.Azure.IIoT.Deployment` application [here](./howto-deploy-aks.md#configuration).
