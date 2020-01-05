# Deploying Azure Industrial IoT Platform and dependencies

[Home](readme.md)

This article explains how to deploy the Azure Industrial IoT Platform in Azure.  

## Deploy Industrial IoT Microservices to AKS using the deployment tool

To deploy the Industrial IoT platform to Azure Kubernetes Service (AKS) follow the steps outlined [here](industrial_iot_deployment.md).

## Deploy Industrial IoT Platform to Azure using the Azure Portal

You can deploy from the *master* branch using the Deploy to Azure button:

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Findustrial-iot%2Fmaster%2Fdeploy%2Fscripts%2Ftemplates%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/></a> <a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Findustrial-iot%2Fmaster%2Fdeploy%2Fscripts%2Ftemplates%2Fazuredeploy.json" target="_blank"> <img src="http://armviz.io/visualizebutton.png"/></a>

## Deploy Industrial IoT Platform using deployment script

The deployment script deploys an entire simulation environment consisting of

* Linux and Windows IoT Edge simulation running all required modules
* A PLC server simulation
* All required Azure infrastructure
* The Industrial IoT Platform
* The Industrial IoT Sample Engineering tool.

### Prerequisites

Make sure you have PowerShell and [Az PowerShell](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps) extensions installed.  If not, first install PowerShell, then open PowerShell as Administrator and run

1. ```powershell
Install-Module -Name Az -AllowClobber
   Install-Module -Name AzureAD -AllowClobber
   ```
   
2. If you have not done so yet, clone this GitHub repository.  Open a command prompt or terminal and run:

   ```bash
   git clone https://github.com/Azure/Industrial-IoT
   cd Industrial-IoT
   ```

### Deploy

1. Open a command prompt or terminal in the repository root and run:

   ```bash
   deploy
   ```

   The supported parameters can be found [below](#deployment-script-options).

2. Follow the prompts to assign a name to the resource group of the deployment and a name to the website. The script deploys the Microservices and their Azure platform dependencies into the resource group in your Azure subscription.  The script also registers an Application in your Azure Active Directory (AAD) tenant to support OAUTH based authentication.  Deployment will take several minutes.  An example of what you'd see once the solution is successfully deployed:

   ![Deployment Result](media/deployment1.png)

   The output includes the  URL of the public endpoint.  

   In case you run into issues please follow the steps [below](#troubleshooting-deployment-failures).

3. Once the script completes successfully, select whether you want to save the .env file.  You need the .env environment file if you want to connect to the cloud endpoint using tools such as the [Console](howto-use-cli.md) or [deploy modules](howto-deploy-modules.md) for development and debugging.

## Troubleshooting deployment failures

### Resource group name

Ensure you use a short and simple resource group name.  The name is used also to name resources as such it must comply with resource naming requirements.  

### Website name already in use

It is possible that the name of the website is already in use.  If you run into this error, you need to use a different application name.

### Azure Active Directory (AAD) Registration

The deployment script tries to register 2 AAD applications in Azure Active Directory.  Depending on your rights to the selected AAD tenant, this might fail.   

An administrator with the relevant rights to the tenant can create the AAD applications for you.  The `deploy/scripts` folder contains the `aad-register.ps1` script to perform the AAD registration separately from deploying.  The output of the script is an object containing the relevant information to be used as part of deployment and must be passed to the `deploy.ps1` script in the same folder using the `-aadConfig` argument.

## Deployment script options

Using the  `deploy/scripts/deploy.ps1`  script you can deploy several configurations including deploying images from your private Azure Container Registry (ACR).

To support these scenarios, the `deploy.ps1` takes the following parameters:

```bash

 .PARAMETER type
    The type of deployment (local, services, app, all)

 .PARAMETER resourceGroupName
    Can be the name of an existing or a new resource group

 .PARAMETER resourceGroupLocation
    Optional, a resource group location. If specified, will try to create a new resource group in this location.

 .PARAMETER subscriptionId
    Optional, the subscription id where resources will be deployed.

 .PARAMETER subscriptionName
    Or alternatively the subscription name.

 .PARAMETER accountName
    The account name to use if not to use default.

 .PARAMETER applicationName
    The name of the application if not local deployment. 

 .PARAMETER aadConfig
    The aad configuration object (use aad-register.ps1 to create object).  If not provides calls aad-register.ps1.

 .PARAMETER context
    A previously created az context to be used as authentication.

 .PARAMETER aadApplicationName
    The application name to use when registering aad application.  If not set, uses applicationName

 .PARAMETER acrRegistryName
    An optional name of a Azure container registry to deploy containers from.

 .PARAMETER acrSubscriptionName
    The subscription of the container registry if differemt from the specified subscription.
```

## Next steps

Now that you have successfully deployed the Microservices to an existing project, here are the suggested next steps:

* [Deploy Industrial IoT modules to IoT Edge](howto-deploy-modules.md)
* [Learn more about OPC Twin](services/readme.md)
* [OPC Twin Dependencies](services/dependencies.md)
