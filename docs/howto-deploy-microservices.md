# Deploying Azure Industrial IoT Microservices and dependencies

This article explains how to deploy the Azure Industrial IoT Microservices in Azure.

## Prerequisites

> [!NOTE]
> Note: Due to the dependency on the AzureRM module, deployment currently is only supported on Windows.  We will add support for Linux soon.

1. Make sure you have PowerShell and [AzureRM PowerShell](https://docs.microsoft.com/powershell/azure/azurerm/install-azurerm-ps) extensions installed.  If not, first install PowerShell, then open PowerShell as Administrator and run

   ```powershell
   Install-Module -Name AzureRM -AllowClobber
   Install-Module -Name AzureAD -AllowClobber
   ```

2. If you have not done so yet, clone this GitHub repository.  Open a command prompt or terminal and run:

   ```bash
   git clone --recursive https://github.com/Azure/Industrial-IoT
   cd Industrial-IoT
   ```

## Deploy Industrial IoT Microservices to Azure

1. Open a command prompt or terminal in the repository root and run:

   ```bash
   deploy
   ```
   The supported parameters can be found at [below options](#deployment-script-options).

2. Follow the prompts to assign a name to the resource group of the deployment and a name to the website. The script deploys the Microservices and their Azure platform dependencies into the resource group in your Azure subscription.  The script also registers an Application in your Azure Active Directory (AAD) tenant to support OAUTH based authentication.  Deployment will take several minutes.  An example of what you'd see once the solution is successfully deployed:

   ![Deployment Result](media/deployment1.png)

   The output includes the  URL of the public endpoint.  

   In case you run into issues please follow the steps [below](#troubleshooting-deployment-failures).

3. Once the script completes successfully, select whether you want to save the .env file.  You need the .env environment file if you want to connect to the cloud endpoint using tools such as the [Console](services/howto-use-cli.md) or [deploy modules](howto-deploy-modules.md) for development and debugging.

## Troubleshooting deployment failures

### Resource group name

Ensure you use a short and simple resource group name.  The name is used also to name resources as such it must comply with resource naming requirements.  

### Website name already in use

It is possible that the name of the website is already in use.  If you run into this error, you need to use a different application name.

### Azure Active Directory (AAD) Registration

The deployment script tries to register 2 AAD applications in Azure Active Directory.  Depending on your rights to the selected AAD tenant, this might fail.   There are 2 options:

1. If you chose a AAD tenant from a list of tenants, restart the script and choose a different one from the list.
2. Alternatively, deploy a private AAD tenant in another subscription, restart the script and select to use it.

**WARNING**:  NEVER continue without Authentication.  If you choose to do so, anyone can access your OPC Device Management endpoints from the Internet unauthenticated.   You can always choose the ["local" deployment option](howto-deploy-dependencies.md) to kick the tires.

## Deployment script options

To support automation scenarios, the script takes the following parameters:

```bash
-type
```

The type of deployment (vm, local)

```bash
-resourceGroupName
```

Can be the name of an existing or a new resource group.

```bash
-subscriptionId
```

Optional, the subscription id where resources will be deployed.

```bash
-subscriptionName
```

Or alternatively the subscription name.

```bash
-resourceGroupLocation
```

Optional, a resource group location. If specified, will try to create a new resource group in this location.

```bash
-aadApplicationName
```

A name for the AAD application to register under.

```bash
-tenantId
```

AAD tenant to use.

```bash
-credentials
```

## Next steps

Now that you have successfully deployed the Microservices to an existing project, here are the suggested next steps:

* [Deploy Industrial IoT modules to IoT Edge](howto-deploy-modules.md)
* [Learn more about OPC Twin](services/readme.md)
* [OPC Twin Dependencies](services/dependencies.md)
