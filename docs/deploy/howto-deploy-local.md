# Deploying Azure Resources for local Development

[Home](readme.md)

This article explains how to deploy only the Azure services needed to do local development and debugging.   At the end you will have a resource group deployed that contains everything you need for local development and debugging.

## Deploy Azure Dependencies and generate .env file

1. If you have not done so yet, clone the GitHub repository.  To clone the repository you need git.  If you do not have git installed on your system, follow the instructions for [Linux or Mac](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git), or [Windows](https://gitforwindows.org/) to install it.  Open a new command prompt or terminal and run:

   ```bash
   git clone https://github.com/Azure/Industrial-IoT
   cd Industrial-IoT
   ```

2. Open a command prompt or terminal to the repository root and run:

   - On Windows:

     ```pwsh
     .\deploy -type local
     ```

   - On Linux:

     ```bash
     ./deploy.sh -type local
     ```

3. Follow the prompts to assign a name to the resource group for your deployment  The script deploys only the [dependencies](../services/dependencies.md) to this resource group in your Azure subscription, but not the Microservices .  The script also registers an Application in Azure Active Directory.  This is needed to support OAUTH based authentication.
   Deployment can take several minutes.  In case you run into issues please follow the troubleshooting help [here](howto-deploy-all-in-one.md).

4. Once the script completes, you must select to save the `.env` file.  The `.env` environment file is the configuration file of all Microservices and tools you want to run on your development machine.  

## Next steps

Now that you have successfully deployed Azure Industrial IoT Microservices to an existing project, here are the suggested next steps:

- [Run the Industrial IoT components locally](howto-run-microservices-locally.md)
- [Learn more about the deployed Azure Service dependencies](../services/dependencies.md)
