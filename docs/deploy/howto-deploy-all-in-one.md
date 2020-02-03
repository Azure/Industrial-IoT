# Deploying Azure Industrial IoT Platform and dependencies

[Home](readme.md)

This article explains how to deploy the Azure Industrial IoT Platform and Simulation in Azure using the deployment scripts.  
The ARM deployment templates included in the repository deploy the platform and an entire simulation environment consisting of

- Linux and Windows IoT Edge simulation running all required modules
- A PLC server simulation
- All required Azure infrastructure
- The Industrial IoT Platform
- The Industrial IoT Sample Engineering tool.

## Running the script

The platform and simulation can also be deployed using the deploy script.

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

3. Open a command prompt or terminal in the repository root and run:

   ```bash
   deploy
   ```

   The supported parameters can be found [below](#deployment-script-options).

4. Follow the prompts to assign a name to the resource group of the deployment and a name to the website. The script deploys the Microservices and their Azure platform dependencies into the resource group in your Azure subscription.  The script also registers an Application in your Azure Active Directory (AAD) tenant to support OAUTH based authentication.  
   Deployment will take several minutes.  An example of what you'd see once the solution is successfully deployed:

   ![Deployment Result](../media/deployment1.png)

   The output includes the  URL of the public endpoint.  

   In case you run into issues please follow the steps [below](#troubleshooting-deployment-failures).

5. Once the script completes successfully, select whether you want to save the `.env` file.  You need the `.env` environment file if you want to connect to the cloud endpoint using tools such as the [Console](howto-use-cli.md) or for debugging.

## Troubleshooting deployment failures

### Resource group name

Ensure you use a short and simple resource group name.  The name is used also to name resources as such it must comply with resource naming requirements.  

### Website name already in use

It is possible that the name of the website is already in use.  If you run into this error, you need to use a different application name.

### Azure Active Directory Registration

The deployment script tries to register 2 Azure Active Directory (AAD) applications representing the client and the platform (service).  Depending on your rights to the selected AAD tenant, this might fail.

An administrator with the relevant rights to the tenant can create the AAD applications for you.  The `deploy/scripts` folder contains the `aad-register.ps1` script to perform the AAD registration separately from deploying.  The output of the script is an object containing the relevant information to be used as part of deployment and must be passed to the `deploy.ps1` script in the same folder using the `-aadConfig` argument.

```pwsh
pwsh
cd deploy/scripts
./aad-register.ps1 -Name <application-name> -Output aad.json
./deploy.ps1 -aadConfig aad.json ...
```

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
    The aad configuration file or object (use aad-register.ps1 to create).  If not provided, calls aad-register.ps1.

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

- [Deploy Industrial IoT modules to IoT Edge](howto-install-iot-edge.md)
- [Learn more about OPC Twin](services/readme.md)
- [OPC Twin Dependencies](services/dependencies.md)
