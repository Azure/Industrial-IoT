# Deploying Azure Resources for local Development

[Home](readme.md)

This article explains how to deploy only the Azure services needed to do local development and debugging.   At the end you will have a resource group deployed that contains everything you need for local development and debugging.

## Prerequisites

1. Make sure you have PowerShell and [Az PowerShell](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps) extensions installed.  If not, first install PowerShell, then open PowerShell as Administrator and run

   ```powershell
   Install-Module -Name Az -AllowClobber
   Install-Module -Name AzureAD -AllowClobber
   ```

2. If you have not done so yet, clone this GitHub repository.  Open a command prompt or terminal and run:

   ```bash
   git clone https://github.com/Azure/Industrial-IoT
   cd Industrial-IoT
   ```

## Deploy Azure Dependencies and generate .env file

1. Open a command prompt or terminal in the repository root and run:

   ```bash
   deploy -type local
   ```

2. Follow the prompts to assign a name to the resource group for your deployment  The script deploys only the [dependencies](services/dependencies.md) to this resource group in your Azure subscription, but not the Microservices .  The script also registers an Application in Azure Active Directory.  This is needed to support OAUTH based authentication.  
   Deployment can take several minutes.  In case you run into issues please follow the troubleshooting help [here](howto-deploy-all-in-one.md).

3. Once the script completes, you must select to save the `.env` file.  The `.env` environment file is the configuration file of all Microservices and tools you want to run on your development machine.  

## Next steps

Now that you have successfully deployed Azure Industrial IoT Microservices to an existing project, here are the suggested next steps:

* [Run the Industrial IoT modules locally](howto-install-iot-edge.md)
* [Learn about the OPC Twin dependencies](services/dependencies.md)
