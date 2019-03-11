# Deploying Azure Industrial IoT services and dependencies

This article explains how to deploy the Industrial IoT Services in Azure.

## Prerequisites

Make sure you have PowerShell and [Azure PowerShell](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps?view=azps-1.1.0) extensions installed.   If you have not done so yet, clone this GitHub repository.  Open a command prompt or terminal and run:

```bash
git clone --recursive https://github.com/Azure/azure-iiot-components 
cd azure-iiot-components
```

## Deploy Industrial IoT Services to Azure

1. On the open command prompt or terminal run: 

   ```bash
   deploy
   ```

2. Follow the prompts to assign a name to the resource group of the deployment and a name to the website.   The script deploys the micro services and their Azure platform dependencies into the resource group in your Azure subscription.  The script also registers an Application in your Azure Active Directory (AAD) tenant to support OAUTH based authentication.  Deployment will take several minutes.  An example of what you'd see once the solution is successfully deployed:    

   ![Deployment Result](media/deployment1.png)

   The output includes the  URL of the public endpoint.  In case you run into issues please follow the steps [below](#Troubleshooting-deployment-failures).

3. Once the script completes successfully, select whether you want to save the .env file.  You need the .env environment file if you want to connect to the cloud endpoint using tools such as the [Console](twin/howto-use-cli.md) or [deploy modules](howto-deploy-modules.md) for development and debugging.   

## Troubleshooting deployment failures

### Resource group name

Ensure you use a short and simple resource group name.  The name is used also to name resources as such it must comply with resource naming requirements.  

### Website name already in use

It is possible that the name of the website is already in use.  If you run into this error, you need to use a different name.

### Azure Active Directory (AAD) Registration 

The deployment script tries to register 2 AAD applications in Azure Active Directory.  Depending on your rights to the selected AAD tenant, this might fail.   There are 2 options:

1. If you chose a AAD tenant from a list of tenants, restart the script and choose a different one from the list.
2. Alternatively, deploy a private AAD tenant in another subscription, restart the script and select to use it.

**WARNING**:  NEVER continue without Authentication.  If you choose to do so, anyone can access your OPC Device Management endpoints from the Internet unauthenticated.   You can always choose the ["local" deployment option](howto-deployment-dependencies.md) to kick the tires.

## Deploy an all-in-one Industrial IoT Services Demo

Instead of just the services and dependencies you can also deploy an all-in-one demo.  The all in one demo contains 3 OPC UA servers, the OPC Twin module, and all micro services as well as a sample Web Application.  It is intended for demonstration purposes.

1. Make sure you have a clone of the repository (see above). Open a command prompt or terminal in the  root of the repository and run:

   ```bash
   deploy -type demo
   ```

2. Follow the prompts to assign a new name to the resource group and a name to the website.  Once deployed successfully, the script will display the URL of the web application endpoint.  Follow the steps [above](#Troubleshooting-deployment-failures) if you run into any deployment failures.

## Deployment script options

The script takes the following parameters:

```
-type
```


The type of deployment (vm, local, demo)

```
-resourceGroupName
```

Can be the name of an existing or a new resource group.

```
-subscriptionId
```


Optional, the subscription id where resources will be deployed.

```
-subscriptionName
```


Or alternatively the subscription name.

```
-resourceGroupLocation
```


Optional, a resource group location. If specified, will try to create a new resource group in this location.

```
-aadApplicationName
```


A name for the AAD application to register under. 

```
-tenantId
```


AAD tenant to use. 

```
-credentials
```


To support non interactive usage of script.  (TODO)


## Next steps

Now that you have successfully deployed OPC Device Management services to an existing project, here is the suggested next step:

- [Deploy Industrial IoT modules to IoT Edge](howto-deployment-modules.md)
- [What is OPC Device Management?](twin/readme.md)
- [OPC Device Management Dependencies](/twin/dependencies.md)