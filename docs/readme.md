# Azure Industrial IoT

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Table Of Contents <!-- omit in toc -->

* [Features](#features)
* [Getting Started](#getting-started)
 * [Install IoT Edge](#install-iot-edge)
 * [Deploy OPC Publisher](#deploy-opc-publisher)
   * [Using Azure CLI](#using-azure-cli)
   * [Using the Azure Portal](#using-the-azure-portal)
   * [Using the Azure Industrial IoT service](#automatic-deployment-with-azure-industrial-iot-services)
   * [Troubleshooting](#troubleshooting)
  * [Deploy the Industrial IoT Platform service](#optional-deploy-the-industrial-iot-platform)
    * [Troubleshooting deployment failures](#troubleshooting-deployment-failures)
    * [Deployment script options](#deployment-script-options)
    * [Running the service locally](#running-the-industrial-iot-platform-opc-publisher-web-service-locally)
    * [Next steps](#next-steps)
* [OPC Publisher Operation](./opc-publisher/readme.md)

## Features

Azure [OPC Publisher](./opc-publisher/readme.md) and the optional [Industrial IoT Web service](./web-api/readme.md) allows plant operators to discover [OPC UA](opcua.md) enabled servers in a factory network and register them in Azure IoT Hub. Operations personnel can subscribe to and react to events on the factory floor from anywhere in the world. The APIs mirror the [OPC UA](opcua.md) services and are secured through IoT Hub or optionally using OAUTH authentication and authorization backed by Azure Active Directory (AAD). This enables your applications to browse server address spaces or read/write variables and execute methods using IoT Hub, MQTT or HTTPS with simple JSON payloads.  

The [OPC Publisher API](./opc-publisher/readme.md) and the optional [Industrial IoT Web service REST API](./api/readme.md) can be used with any programming language through its exposed Open API specification (Swagger). This means when integrating OPC UA into cloud management solutions, developers are free to choose technology that matches their skills, interests, and architecture choices. For example, a full stack web developer who develops an application for an alarm and event dashboard can write logic to respond to events in JavaScript or TypeScript without ramping up on a OPC UA SDK, C, C++, Java or C#.

## Getting Started

### Install IoT Edge

The industrial assets (machines and systems) are connected to Azure through modules running on an [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) industrial gateway.

You can purchase industrial gateways compatible with IoT Edge. Please see our [Azure Device Catalog](https://catalog.azureiotsolutions.com/alldevices?filters={"3":["2","9"],"18":["1"]}) for a selection of industrial-grade gateways. Alternatively, you can setup a local VM.

#### Create an IoT Edge Instance and Install the IoT Edge Runtime

You can also manually [create an IoT Edge instance for an IoT Hub](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-register-device) and install the IoT Edge runtime following the [IoT Edge setup documentation](https://docs.microsoft.com/en-us/azure/iot-edge/). The IoT Edge Runtime can be installed on [Linux](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux) or [Windows](https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-for-linux-on-windows).

For more information check out...
* [Deploy and monitor Edge modules at scale](https://docs.microsoft.com/azure/iot-edge/how-to-deploy-monitor)
* [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)

### Deploy OPC Publisher

This article explains how to deploy the OPC Publisher module to [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) using the Azure Portal and Marketplace.

Before you begin, make sure you followed the [instructions to set up a IoT Edge device](#install-iot-edge) and have a running IoT Edge Gateway.

#### Using Azure CLI

1. Obtain the IoT Hub name and device id of the [installed IoT Edge](#install-iot-edge) Gateway.

1. Install the [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli). You must have at least `v2.0.24`, which you can verify with `az --version`.

1. Add the [IoT Edge Extension](https://github.com/Azure/azure-iot-cli-extension/) with the following commands:

    ```bash
    az extension add --name azure-cli-iot-ext
    ```

To deploy all required modules using Az...  

1. Save the following content into a `deployment.json` file:

    ```json
    {
      "modulesContent": {
        "$edgeAgent": {
          "properties.desired": {
            "schemaVersion": "1.1",
            "runtime": {
              "type": "docker",
              "settings": {
                "minDockerVersion": "v1.25",
                "loggingOptions": "",
                "registryCredentials": {}
              }
            },
            "systemModules": {
              "edgeAgent": {
                "type": "docker",
                "settings": {
                  "image": "mcr.microsoft.com/azureiotedge-agent:1.4",
                  "createOptions": ""
                }
              },
              "edgeHub": {
                "type": "docker",
                "status": "running",
                "restartPolicy": "always",
                "settings": {
                  "image": "mcr.microsoft.com/azureiotedge-hub:1.4",
                  "createOptions": "{\"HostConfig\":{\"PortBindings\":{\"5671/tcp\":[{\"HostPort\":\"5671\"}], \"8883/tcp\":[{\"HostPort\":\"8883\"}],\"443/tcp\":[{\"HostPort\":\"443\"}]}}}"
                },
                "env": {
                  "SslProtocols": {
                    "value": "tls1.2"
                  }
                }
              }
            },
            "modules": {
              "publisher": {
                "version": "1.0",
                "type": "docker",
                "status": "running",
                "restartPolicy": "always",
                "settings": {
                  "image": "mcr.microsoft.com/iotedge/opc-publisher:latest",
                  "createOptions": "{\"Hostname\":\"publisher\",\"HostConfig\":{\"CapDrop\":[\"CHOWN\",\"SETUID\"]}}"
                }
              }
            }
          }
        },
        "$edgeHub": {
          "properties.desired": {
            "schemaVersion": "1.0",
            "routes": {
              "publisherToUpstream": "FROM /messages/modules/publisher/* INTO $upstream",
              "leafToUpstream": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO $upstream"
            },
            "storeAndForwardConfiguration": {
              "timeToLiveSecs": 7200
            }
          }
        }
      }
    }
    ```

1. Use the following command to apply the configuration to an IoT Edge device:

   ```bash
   az iot edge set-modules --device-id [device id] --hub-name [hub name] --content ./deployment.json
   ```

   The `device id` parameter is case-sensitive. The content parameter points to the deployment manifest file that you saved.
    ![az iot edge set-modules output](https://docs.microsoft.com/azure/iot-edge/media/how-to-deploy-cli/set-modules.png)

1. Once you've deployed modules to your device, you can view all of them with the following command:

   ```bash
   az iot hub module-identity list --device-id [device id] --hub-name [hub name]
   ```

   The device id parameter is case-sensitive. ![az iot hub module-identity list output](https://docs.microsoft.com/azure/iot-edge/media/how-to-deploy-cli/list-modules.png)

More information about az and IoT Edge can be found [here](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-monitor-cli).

#### Using the Azure Portal

To deploy all required modules to the Gateway using the Azure Portal...

1. Sign in to the [Azure portal](https://portal.azure.com/) and navigate to the IoT Hub deployed earlier.

   > If you deploy using [the deployment script](#optional-deploy-the-industrial-iot-platform) then a simple way to locate your IoT Hub is to find the resource group variable in your `.env` file.  This resource group contains the IoT Hub.

1. Select **IoT Edge** from the left-hand menu.

1. Click on the ID of the target device from the list of devices.

1. Select **Set Modules**.

1. In the **Deployment modules** section of the page, select **Add** and **IoT Edge Module.**

1. In the **IoT Edge Custom Module** dialog use `publisher` as name for the module, then specify the container *image URI* as

   ```bash
   mcr.microsoft.com/iotedge/opc-publisher:2.9.0
   ```

   On Linux use the following *create options* if you intend to use the network scanning capabilities of the module:

   ```json
   {"NetworkingConfig":{"EndpointsConfig":{"host":{}}},"HostConfig":{"NetworkMode":"host","CapAdd":["NET_ADMIN"],
   "CapDrop":["CHOWN", "SETUID"]}}
   ```

   Fill out the optional fields if necessary. For more information about container create options, restart policy, and desired status see [EdgeAgent desired properties](https://docs.microsoft.com/azure/iot-edge/module-edgeagent-edgehub#edgeagent-desired-properties). For more information about the module twin see [Define or update desired properties](https://docs.microsoft.com/azure/iot-edge/module-composition#define-or-update-desired-properties).

1. Select **Save** and then **Next** to continue to the routes section.

1. In the routes tab, paste the following

    ```json
    {
      "routes": {
        "publisherToUpstream": "FROM /messages/modules/publisher/* INTO $upstream",
        "leafToUpstream": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO $upstream"
      }
    }
    ```

    and select **Next**

1. Review your deployment information and manifest.  It should look like the deployment manifest found in the [previous section](#using-azure-cli).  Select **Submit**.

1. Once you've deployed modules to your device, you can view all of them in the **Device details** page of the portal. This page displays the name of each deployed module, as well as useful information like the deployment status and exit code.

1. Add your own or other modules from the Azure Marketplace using the steps above.

For more in depth information check out [the Azure IoT Edge Portal documentation](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal).

### Automatic deployment with Azure Industrial IoT services

> IMPORTANT: This deployment option is only available with the deployment of the [Azure Industrial IoT service](#optional-deploy-the-industrial-iot-platform)!

The [Azure Industrial IoT comnpanion service](#optional-deploy-the-industrial-iot-platform) creates IoT Edge Layered Deployments for OPC Publisher. These Layered Deployments will be automatically applied to any IoT Edge instance that contains the following Device Twin JSON tags.

1. Go to the [Azure Portal page](http://portal.azure.com) and select your IoT Hub

1. Open the Device Twin configuration JSON under IoT Edge -> [your IoT Edge device] -> Device Twin

1. Insert the following `tags`:

   * For Linux, set the "os" property to "Linux":

    ```json
    ...
    },
    "version": 1,
    "tags": {
        "__type__": "iiotedge",
        "os": "Linux"
    },
    "properties":
    ...
    ```

   * For Windows (EFLOW), set the "os" property to "Windows":

    ```json
    ...
    },
    "version": 1,
    "tags": {
        "__type__": "iiotedge",
        "os": "Windows"
    },
    "properties":
    ...
    ```

The tags can also be created as part of an Azure Device Provisioning (DPS) enrollment. An example of the latter can be found in `/deploy/scripts/dps-enroll.ps1`.

#### Temporarily continue deploying out of support 1.1 LTS modules to an 1.1 IoT Edge device

To continue deploying the 1.1 LTS modules to a 1.1 LTS IoT Edge gateway device until you are able to upgrade the device to 1.4, add a tag to your gateway device's twin with the name `use_1_1_LTS` and remove it once you have upgraded your edge gateway to 1.4 LTS. This operation can be automated using the az CLI. It should be done ahead of deploying the 2.8.4 release to Azure to avoid outages.

```json
...
"tags": {
    "__type__": "iiotedge",
    // ...
    "use_1_1_LTS": true
}
...
```

> IMPORTANT: Setting the tag to `false` or any other value has no effect.  Once you upgrade your IoT Edge device to 1.4 you must remove the tag to ensure the 1.4 modules are deployed to it. 

#### Module Versions

By default, the same Docker container image version tag from mcr.microsoft.com is deployed that corresponds to the corresponding micro-service's version.

If you need to point to a different Docker container registry or image version tag, you can configure the source using environment variables `PCS_DOCKER_SERVER`, `PCS_DOCKER_USER`, `PCS_DOCKER_PASSWORD`, `PCS_IMAGES_NAMESPACE` and `PCS_IMAGES_TAG`, for example in your .env file (which can also be set during deployment), then restart the edge management or all-in-one service.

### Troubleshooting

To troubleshoot your IoT Edge installation follow the official [IoT Edge troubleshooting guide](https://docs.microsoft.com/en-us/azure/iot-edge/troubleshoot)

#### Host network

When device discovery operations fail on Linux gateways (where the discovery module by default is attached to the host network) make sure to validate the host network is available:

```bash
docker network ls
    NETWORK ID          NAME                DRIVER              SCOPE
    beceb3bd61c4        azure-iot-edge      bridge              local
    97eccb2b9f82        bridge              bridge              local
    758d949c5343        host                host                local
    72fb3597ef74        none                null                local
```

### (Optional) Deploy the Industrial IoT Platform

The simplest way to get started is to deploy the Azure Industrial IoT OPC Publisher, Services and Simulation demonstrator using the deployment script:

1. If you have not done so yet, clone the GitHub repository. To clone the repository you need git. If you do not have git installed on your system, follow the instructions for [Linux or Mac](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git), or [Windows](https://gitforwindows.org/) to install it. Open a new command prompt or terminal and run:

   ```bash
   git clone https://github.com/Azure/Industrial-IoT
   cd Industrial-IoT
   ```

1. Open a command prompt or terminal to the repository root and start the guided deployment:

   * On Windows:

     ```pwsh
     .\deploy
     ```

   * On Linux:

     ```bash
     ./deploy.sh
     ```

   The deployment script allows to select which set of components to deploy using deployment types:

    * `local`: Just what is necessary to run the services locally
    * `services`: `local` and the service container
    * `simulation`: `local` and the simulation components
    * `app`: `services` and the engineering tool
    * `all` (default): all components

    Depending on the chosen deployment type the following services will be deployed:

    * Minimum dependencies:
      * 1 [IoT Hub](https://azure.microsoft.com/services/iot-hub/) to communicate with the edge and ingress raw OPC UA telemetry data
      * 1 [Key Vault](https://azure.microsoft.com/services/key-vault/), Premium SKU (to manage secrets and certificates)
      * 1 [Blob Storage](https://azure.microsoft.com/services/storage/) V2, Standard LRS SKU (for event hub checkpointing)
      * App Service Plan, 1 [App Service](https://azure.microsoft.com/services/app-service/), B1 SKU for hosting the cloud micro-services [all-in-one](../services/all-in-one.md)
      * App Service Plan (shared with microservices), 1 [App Service](https://azure.microsoft.com/services/app-service/) for hosting the Industrial IoT Engineering Tool cloud application
    * Simulation:
      * 1 [Device Provisioning Service](https://docs.microsoft.com/azure/iot-dps/), S1 SKU (used for deploying and provisioning the simulation gateways)
      * [Virtual machine](https://azure.microsoft.com/services/virtual-machines/), Virtual network, IoT Edge used for a factory simulation to show the capabilities of the platform and to generate sample telemetry. By default, 4 [Virtual Machines](https://azure.microsoft.com/services/virtual-machines/), 2 B2 SKU (1 Linux IoT Edge gateway and 1 Windows IoT Edge gateway) and 2 B1 SKU (factory simulation).

   > Additional supported parameters can be found [here](#deployment-script-options).

1. Follow the prompts to assign a name to the resource group of the deployment and a name to the website. The script deploys the Microservices and their Azure platform dependencies into the resource group in your Azure subscription. The script also registers an Application in your Azure Active Directory (AAD) tenant to support OAUTH based authentication.
   Deployment will take several minutes. An example of what you'd see once the solution is successfully deployed:

   ![Deployment Result](../media/deployment-succeeded.png)

   The output includes the URL of the public endpoint.

   In case you run into issues please follow the steps [below](#troubleshooting-deployment-failures).

1. Once the script completes successfully, select whether you want to save the `.env` file. You need the `.env` environment file if you want to connect to the cloud endpoint using tools such as the [Console](../tutorials/tut-use-cli.md) or for debugging.

#### Azure Active Directory application registrations

Components of the Azure Industrial IoT platform require several App Registrations in your Azure Active Directory
(AAD) to run. Those App Registrations are responsible for:

* providing identity for back-end microservices to run
* defining authentication methods and details for client applications

The `deploy.ps1` script creates those App Registrations for you by default. But they both require user to have `Administrator` role in the AAD for deployment to succeed. In cases when the user does not have the `Administrator` role, the process of
deployment can be separated into 2 distinct states:

* creation and setup of the App Registrations in AAD tenant
* creation of Azure resources and deployment of the Azure Industrial IoT platform components

With this separation, the first step can be performed by an IT administrator. Deployments will output details
of App Registrations which can then be passed to a user for running the second step.

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

#### Troubleshooting deployment failures

##### Execution Policy

If you receive a message that the execution policy not being set you can set the execution policy when starting the PowerShell session:

```pwsh
pwsh -ExecutionPolicy Unrestricted
```

To set the execution policy on your machine:

1. Search for Windows PowerShell in Start
2. Right click on result Windows PowerShell and choose Run as Administrator
3. In PowerShell (Administrator) run:

```pwsh
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Force
```

##### Security Warning

If you see a message in PowerShell

`Security warning
Run only scripts that you trust. While scripts from the internet can be useful, this script can potentially harm your computer. If you trust this script, use the Unblock-File cmdlet to allow the script to run without this warning message. Do you want to run <...> deploy.ps1?
[D] Do not run [R] Run once [S] Suspend [?] Help (default is "D"):
Do you want to run this script?`

Choose R to run once.

##### Resource group name

Ensure you use a short and simple resource group name. The name is used also to name resources as such it must comply with resource naming requirements.

##### Website name already in use

It is possible that the name of the website is already in use. If you run into this error, you need to use a different application name.

##### Azure Active Directory Permissions

The deployment script tries to [register three AAD applications](#azure-active-directory-application-registrations) representing the web app, the client and the platform (service). This requires [Global Administrator, Application Administrator or Cloud Application Administrator](https://docs.microsoft.com/azure/active-directory/manage-apps/grant-admin-consent) rights.

If the deployment fails or if you see the following error when trying to sign-in, see further below for options:

> **Need admin approval**  
> \<APPLICATION\> needs permission to access resources in your organization that only an admin can grant. Please ask an admin to grant permission to this app before you can use it.

**Option 1** (recommended for production): Ask your AAD admin to grant tenant-wide admin consent for your application, there might be a process or tool for this in your enterprise environment.

**Option 2** (recommended for production): An AAD admin can create the AAD applications for you. The `deploy/scripts` folder contains the `aad-register.ps1` script to perform the AAD registration separately from the deployment. The output of the script is a file containing the relevant information to be used as part of deployment and must be passed to the `deploy.ps1` script in the same folder using the `-aadConfig` argument.

   ```pwsh
   cd deploy/scripts
   ./aad-register.ps1 -Name <application-name> -ReplyUrl https://<application-name>.azurewebsites.net/ -Output aad.json
   ./deploy.ps1 -aadConfig aad.json
   ```

If you need additional reply URLs, you may add them manually as this does not require AAD admin rights. The script `aad-register.ps1` also supports the parameter `-TenantId`, which can be used to explicitly select an AAD tenant, and can be executed from the [Cloud Shell](https://docs.microsoft.com/azure/cloud-shell/overview).

**Option 3** (recommended as PoC): Create your [own AAD tenant](https://docs.microsoft.com/azure/active-directory/develop/quickstart-create-new-tenant), in which you are the admin

* Azure Portal: Create a resource -> Azure Active Directory
* After about 1 min, click the link to manage the directory, or click on your account profile at the top right of the Azure Portal -> Switch directory, then select it from *All Directories*
* Copy the Tenant ID

Start the deployment with as many details about the environment as you can provide. You can use the following template to provide targeted details about how you would like the deployment to occur, supplying at least the `-authTenantID` parameter ...

   ```pwsh
   ./deploy.cmd -authTenantId {tenant_id_for_custom_AAD_instance} -subscriptionName {subscription_name} -tenantId {subscription_tenant_id} -SubscriptionId {subscription_id} -type {e.g. 'all'} -version {e.g. 'latest'} -applicationName {application_name} -resourceGroupName {resource_group_name} -resourceGroupLocation {resource_group_location} 
   ```

##### Missing Script dependencies

On **Windows**, the script uses Powershell, which comes with Windows. The deploy batch file uses it to install all required modules.

In case you run into issues, e.g. because you want to use pscore, run the following two commands in PowerShell as Administrator. See more information about [AzureAD and Az modules](https://docs.microsoft.com/powershell/azure/install-az-ps).

   ```pwsh
   Install-Module -Name Az -AllowClobber
   Install-Module -Name AzureAD -AllowClobber
   ```

On non - **Ubuntu** Linux or in case you run into issues follow the guidance in the next section.

##### Deploy from Linux other than Ubuntu

To install all necessary requirements on other Linux distributions, follow these steps:

1. First [install PowerShell](https://docs.microsoft.com/powershell/scripting/install/installing-powershell-core-on-linux?view=powershell-7). Follow the instructions for your Linux distribution.

2. Open PowerShell using `sudo pwsh`.

3. Install the required Azure Az Powershell module:

   ```pwsh
   Set-psrepository -Name PSGallery -InstallationPolicy Trusted
   Install-Module -Repository PSGallery -Name Az -AllowClobber
   ```

4. To also have the installation script create AAD Application registrations (aad-register.ps1) install the preview Azure AD module:

   ```pwsh
   Register-PackageSource -ForceBootstrap -Force -Trusted -ProviderName 'PowerShellGet' -Name 'Posh Test Gallery' -Location https://www.poshtestgallery.com/api/v2/
   Install-Module -Repository 'Posh Test Gallery' -Name AzureAD.Standard.Preview -RequiredVersion 0.0.0.10 -AllowClobber
   ```

5. `exit`

#### Deployment script options

Using the `deploy/scripts/deploy.ps1` script you can deploy several configurations including deploying images from a private Azure Container Registry (ACR).

To support these scenarios, the `deploy.ps1` takes the following parameters:

```bash
 .PARAMETER type
    The type of deployment (minimum, local, services, simulation, app, all), defaults to all.

 .PARAMETER version
    Set to mcr image tag to deploy - if not set and version can not be parsed from branch name will deploy "latest".

 .PARAMETER branchName
    The branch name where to find the deployment templates - if not set, will try to use git.

 .PARAMETER repo
    The repository to find the deployment templates in - if not set will try to use git or set default.

 .PARAMETER resourceGroupName
    Can be the name of an existing or new resource group.

 .PARAMETER resourceGroupLocation
    Optional, a resource group location. If specified, will try to create a new resource group in this location.

 .PARAMETER subscriptionId
    Optional, the subscription id where resources will be deployed.

 .PARAMETER subscriptionName
    Or alternatively the subscription name.

 .PARAMETER tenantId
    The Azure Active Directory tenant tied to the subscription(s) that should be listed as options.

 .PARAMETER authTenantId
    Specifies an Azure Active Directory tenant for authentication that is different from the one tied to the subscription.

 .PARAMETER accountName
    The account name to use if not to use default.

 .PARAMETER applicationName
    The name of the application, if not local deployment.

 .PARAMETER aadConfig
    The aad configuration object (use aad-register.ps1 to create object). If not provided, calls aad-register.ps1.

 .PARAMETER context
    A previously created az context to be used for authentication.

 .PARAMETER aadApplicationName
    The application name to use when registering aad application. If not set, uses applicationName.

 .PARAMETER acrRegistryName
    An optional name of an Azure container registry to deploy containers from.

 .PARAMETER acrSubscriptionName
    The subscription of the container registry, if different from the specified subscription.

 .PARAMETER environmentName
    The cloud environment to use, defaults to AzureCloud.

 .PARAMETER simulationProfile
    If you are deploying a simulation, the simulation profile to use, if not default.

 .PARAMETER numberOfSimulationsPerEdge
    Number of simulations to deploy per edge.

 .PARAMETER numberOfLinuxGateways
    Number of Linux gateways to deploy into the simulation.

 .PARAMETER numberOfWindowsGateways
    Number of Windows gateways to deploy into the simulation.
```

#### Running the Industrial IoT platform (OPC Publisher) Web service locally

The OPC Publisher web service contained in this repository provides a RESTful API exposing the Azure Industrial IoT platform capabilities. This section explains how to build, run and debug the web service on your local machine.

If not done yet, clone this GitHub repository. To clone it, you need git. If you don't have git, follow the instructions for [Linux, Mac](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git), or [Windows](https://gitforwindows.org/) to install it.

To clone the repository, open a command prompt or terminal and run:

```bash
git clone https://github.com/Azure/Industrial-IoT
cd Industrial-IoT
```

Run the deployment script with the `local` deployment option.

Build and run the Industrial IoT Microservices with Visual Studio or VS Code

1. First, make sure your development tool chain is setup to build the Microservices. If not, [install .NET Core 3.1+](https://dotnet.microsoft.com/download/dotnet-core/3.1) and
   * [Visual Studio 2019 16.4+ for Windows](https://visualstudio.microsoft.com/vs/), [Visual Studio for MacOS 8.3+](https://visualstudio.microsoft.com/vs/mac/).
   * Or latest version of [Visual Studio Code](https://code.visualstudio.com/).
1. Ensure that the `.env` file previously generated by the deployment script is located in the repository's root.
1. Open the `Industrial-IoT.sln` solution file in Visual Studio or VS Code.
1. Right-click on the solution in the solution viewer and select `Properties`.
1. Set the startup project to be  `Azure.IIoT.OpcUa.Services.WebApi`.
1. Log into Azure using the Az CLI (az login) or Visual Studio using the credentials used to deploy the infrastructure.
1. Start debugging by pressing the "Start" button or hitting F5.

To ensure the service is running, open a browser to ```http://localhost:9045/swagger```, which will show you the service's swagger UI. If the service exits immediately after start, check that the `.env` file exists in the root of the repository.

All configuration is stored in Azure Key vault during deployment. Key vault also is used to provide scale out data protection. When running inside Azure Cloud, the platform uses a Managed Service Identity to access the Key vault and pull this information in a secure way. When you run outside of Azure, you need to log in using `az login` or Visual Studio Service Authentication (Tools -> Options -> Azure Service Authentication).

Double check the following if you encounter any Key vault access issues during startup.

* If you deployed yourself, make sure that your own user account has Key and Secret access rights to the deployed Key vault.
* If you did not, or you do not see your user account configured in the Key vault, add your identity and give it all Key and Secret management rights.
* Sometimes your token has expired. Re-add your account in Visual Studio, or use az login to login again.

#### Next steps

* [Learn more about the Platform services](../web.api/readme.md)
* [Discover a server and browse its address space using the CLI](./web.api/tut-use-cli.md)
* [Discover a server and browse its address space using Postman](./web.api/tut-use-postman.md)

