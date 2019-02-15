# Azure Industrial IoT Services

## How to build, run and debug the services locally.



## Prerequisites

### Install required software

Currently the build and deploy operation is documented only for Windows.
The samples are all written for .NetStandard, which is needed to build the service and samples for deployment.
All the tools you need for .Net Standard come with the .Net Core tools. See [here](https://docs.microsoft.com/en-us/dotnet/articles/core/getting-started) for what you need.

1. [Install .NET Core 2.1+][dotnet-install].
2. [Install Docker][docker-url] (optional, only if the local OPC Vault Edge module docker container is required).
3. Install Visual Studio 2017 with support for C# and ASP.NET Core.

### Clone the repository

If you have not done so yet, clone this Github repository.  Open a command prompt or terminal and run:

```bash
git clone https://github.com/Azure/azure-iiot-opc-vault-service
cd azure-iiot-opc-vault-service 
```

or clone the repo directly in Visual Studio 2017.

### Deploy the service

If you have not done so yet, deploy the services in Azure. The service needs Azure AD integration, Azure Key Vault and a Cosmos DB database to run.

#### [How to Build and Deploy the service to Azure](howto-deploy-services.md) 

**Important Note:** The settings files to run and debug the services locally are only created if the services are deployed with the `-development 1` option.

## Run and Debug with Visual Studio

Visual Studio lets you quickly open the application solution without using a command prompt. The OPC Vault microservice, the OPC Vault edge module and the Sample Web Application can run locally at the same time with debugging support.

### Prepare the service configurations

The service configuration is stored using ASP.NET Core configuration adapters, in `appsettings.json`. The json format allows to store values in a readable format, with comments. The application also supports inserting environment variables, such as credentials and networking details.

The services need a local configuration when run locally. In the cloud, these settings are stored in the Web App Application Settings and secrets are stored in Azure Key Vault. The access to Key Vault is granted by a Managed Service Identity of the app and the microservice.

During deployment, the user who deploys the service is added with read access to the Standard Key Vault. This read access allows the application developer to read the secrets directly from Key Vault during debugging, if he is logged in Visual Studio. This means no secrets need to be transferred from Key Vault or the Application Settings to a local configuration file. <br>However, all other settings, like application id's, tenant id's or service URLs must be copied and pasted from the appropriate places in the Azure portal or copied from configuration files saved during deployment.

### Configure the OPC Vault microservice

To configure the microservice locally, create a copy of `appsettings.json` in the `/src` project folder and call it `appsettings.development.json`.  The configuration values then need to be filled in the template. The values are found in the Azure portal within the Azure AD application configuration for the microservice and the application.<br>For simplicity, the configuration values are also saved during deployment in a file called: `resourcegroupname-service.appsettings.Development.json`. The configuration values can be either copied and pasted one by one or the file itself can be copied to the `/src` folder and be renamed to `appsettings.Development.json`.

### Configure the Web Sample Application

To configure the microservice locally, create a copy of `appsettings.json` in the `/app` project folder and call it `appsettings.Development.json`. Now the same instructions apply as for the service settings.<br>For simplicity, the configuration values are also saved during deployment in a file called: `resourcegroupname.appsettings.Development.json`. The configuration values can be either copied and pasted one by one or the file itself can be copied to the `/app` folder and be renamed to `appsettings.Development.json`.

### Configuration of the OPC Vault Edge Module

The OPC Vault edge module uses a different configuration mechanism than the ASP.NET applications. The configuration parameters are passed in the command line to the application or the docker container.

The startup batch files with the module configurations are created during deployment. In addition, the `/deploy` folder contains a `resourcegroup.module.config` file which contains the command line parameters  needed to start the module in Visual Studio. Copy the following command line parameters from the config file:

```
--vault="https://resourcegroup-service.azurewebsites.net" 
--resource="1234d010-0345-0201-1234-3dc9008ddea0" 
--clientid="1234b294-738b-0102-1234-2d3cdd49b307" 
--secret="1234JjxLw3g2Av70+TWfE9PQfj56787DNC51Kbrr+uY=" 
--tenantid="12341234-5678-431c-8b2e-1234f2121da5"
```

 (sample id Values above are only placeholders)

Right click the project and select `Properties`, then `Debug`. Paste the arguments in the `Application Arguments` field. This will start the OPC Vault edge module with correct parameters to connect to the cloud service. To connect to the OPC Vault microservice running locally replace the `--vault` parameter with the service address at the localhost as below <br>`--vault="http://localhost:58801`.

### Run the services in Visual Studio

Steps using Visual Studio 2017:

1. Open the solution using `azure-iiot-opc-vault-service.sln`.
2. When the solution is loaded, right click on the solution node,
   select `Properties` and go to the `Startup Project` section.
3. Choose `Multiple Startup Projects`. The OPC Vault microservice  project `...Services.Vault` is always required. The application project `...Services.Vault.App` and/or the OPC Vault edge module `...Modules.Vault` are optional to debug the application and the edge module. Set projects to `Start` for debugging.
4. Press F5, or the Run icon. Visual Studio opens your browser showing the Swagger UI for the OPC Vault microservice. The Web Sample App project is also started in a browser window. Only the OPC Vault edge module is started in a command line window.
5. Now all projects can be used and debugged at the same time.

## Publish the services from Visual Studio

After a successful run of  the deployment script for the services, a file with the extension `.publishsettings` is stored in the `/deploy` directory for the OPC Vault microservice and the Sample web application.

To deploy the services for each from Visual Studio:

1. Before you can publish an application directly, make sure that the `WEBSITE_RUN_FROM_PACKAGE` setting in the Azure Web App application settings is set to 0 or being removed, for both the app and the microservice. Otherwise publish will fail. By default the package setting is only enabled for production builds.
2. Right click the `...Services.Vault` project or the `...Services.Vault.App` project.
3. Select Publish.
4. In the `Publish` dialog select `New Profile`. 
5. In the next dialog press the `Import Profile ...` button.
6. Depending on the selected project, navigate to the `/deploy`  folder and chose the matching `yourresourcegroup.publishsettings` file for the app or `yourresourcegroup-service.publishsettings`  for the microservice and import it.
7. Now the app or the microservice can be deployed with a right click on `Publish`.





1. 
