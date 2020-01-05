# Deploying Dependencies for local Development

[Home](readme.md)

This article explains how to deploy only the Azure Platform Microservices need to do local development and debugging.   At the end you will have a resource group deployed that contains everything you need for local development and debugging.

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

2. Follow the prompts to assign a name to the resource group for your deployment  The script deploys only the [dependencies](services/dependencies.md) to this resource group in your Azure subscription, but not the Microservices .  The script also registers an Application in Azure Active Directory.  This is needed to support OAUTH based authentication.  Deployment can take several minutes.  In case you run into issues please follow the steps [below](#Troubleshooting-deployment-failures).

3. Once the script completes, you can select to save the .env file.  The .env environment file is the configuration file of all Microservices and tools you want to run on your development machine.  

## Troubleshooting deployment failures

### Resource group name

Ensure you use a short and simple resource group name.  The name is used also to name resources as such it must comply with resource naming requirements.  

### Azure Active Directory (AAD) Registration

The deployment script tries to register 2 AAD applications in Azure Active Directory.  Depending on your rights to the selected AAD tenant, this might fail.   

An administrator with the relevant rights to the tenant can create the AAD applications for you.  The `deploy/scripts` folder contains the `aad-register.ps1` script to perform the AAD registration separately from deploying.  The output of the script is an object containing the relevant information to be used as part of deployment and must be passed to the `deploy.ps1` script in the same folder using the `-aadConfig` argument.

## Next steps

Now that you have successfully deployed Azure Industrial IoT Microservices to an existing project, here are the suggested next steps:

* [Run the Industrial IoT modules locally](howto-deploy-modules.md)
* [Learn about the OPC Twin dependencies](services/dependencies.md)
