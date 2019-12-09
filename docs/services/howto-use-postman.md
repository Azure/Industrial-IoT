# Discover and register servers and browse their address space via REST API in Postman

[Home](../readme.md)

This article will walk you through the steps to discover and register OPC UA servers using the REST API, through Postman.  

## Prerequisites

You should have already successfully deployed all Microservices and at least one IoT Edge Gateway with the Industrial IoT Modules.  If you have not, please follow the instructions in:

1. [Deploy the Industrial IoT Microservices to Azure](../howto-deploy-microservices.md) and securely store the resulting `.env` file.

2. [Deploy Industrial IoT Edge Modules](../howto-deploy-modules.md)

To run the demo OPC UA server and the OPC Device Management Console Client you will also need Docker installed on your development PC.  If you have not, please follow the instructions for [Linux](https://docs.docker.com/install/linux/docker-ce/ubuntu/), [Mac](https://docs.docker.com/docker-for-mac/install/), or on [Windows](https://docs.docker.com/docker-for-windows/install/).

Also, make sure you have Git installed.  Otherwise follow the instructions for [Linux or Mac](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git), or [Windows](https://gitforwindows.org/) to install it.

3. Have Postman installed or download from [here](https://www.getpostman.com/).


## Start the demo OPC UA server

To make the demo deterministic we also start a demo OPC UA server.  

1. Open a terminal or command prompt and run

   ```bash
   hostname
   ```

   Remember the return host name of your development PC.  You will need it later when you specify the discovery URL of the server.  

2. Now run

   ```bash
   docker run -it -p 50000:50000 mcr.microsoft.com/iotedge/opc-plc -aa
   ```

   to start PLC demo server.  For simplicity we instruct the demo server to accept our certificate.  

## Configure Azure AD App permissions for using later with Postman
Note: the Postman requests will use an Authorization Code flow to authenticate to the service.

### Create AAD App secret

1. Go into the Azure Portal > Azure Active Directory > App Registrations.
1. During deployment of the services in the cloud, two application registrations will have been created. 
  - [yourprefex]-clients
  - [yourprefix]-services
1. Search for the '-clients' app registration created upon deployment of the solution. 
1. Go to the detail screen by clicking on it, and choose 'Clients & secrets'.
1. Under Client secrets, click 'New client secret'.
1. Provide a unique name and expiration date (as you prefer).
1. Click Add. Don't close the screen!
1. Before closing the screen, make sure you copy the Value of the new secret. You will not be able to see it in the future after you leave this screen. Take note of the secret and copy for later in this tutorial. This will be used for [YOUR CLIENT SECRET] in the Postman configuration.

### Take note of AAD app information

1. Within Azure Active Directory, browse to the '-clients' application if you are no longer in there. From the Overview screen, copy the and keep handy:
    - Application (client) ID (this will be used below for [YOUR AAD CLIENT ID])
    - Directory (tenant) ID (this will be used below for [YOUR TENANT ID])
1. From Azure Active Directory, back in App registrations, go to the details of the '-services' appl registration. You'll need to copy and keep handy the following data:
    - From the Expose an API screen, copy the default scope which will look like this: 'https://[yourtenant]].onmicrosoft.com/[yourprefix]-services/user_impersonation', you will need this under [YOUR SCOPE URI] below


## Download a sample set of API calls (Postman collection)
1. Download from ...
2. 


## Configure Postman environment

1. From within Postman at the top right, choose the 'OPC Twin' environment.
1. Click the Settings button.
1. Configure your own values for the properties:
    - OPC-SERVICEURL: typically this will be something like ''
1. Update and close the screen.

2. TODO (add image and full list of fields)

### Request a new OAuth 2.0 Token

1. On the right you will see your collections, click the '...' (three dots) next to the OPC Twin collection and choose 'Edit'.
1. Choose Authorization. ![auth](../media/3-auth-edit.png)
1. Choose Get New Access Token.
1. Fill in the following fields:
    - Token Name: your choice
    - Grant type: Authorization Code
    - Callback URL:  https://[yourserviceendpoint]/.auth/login/aad/callback, for example  https://myiiotserviceendpoint.azurewebsites.net/.auth/login/aad/callback
    - Auth URL: https://login.microsoftonline.com/[YOUR TENANT ID]/oauth2/v2.0/authorize
    - Access Token URL: https://login.microsoftonline.com/[YOUR TENANT ID]/oauth2/v2.0/token
    - Client ID: [YOUR AAD CLIENT ID]
    - Client Secret: [YOUR CLIENT SECRET]
    - Scope: [YOUR SCOPE URI]
    - Click Request Token.
    - You will be prompted to login.
    ![gettoken](../media/4-postmantoken.png)
    - A token is returned, scroll to the bottom and click 'Use Token'.
    Click Update to close the Edit screen.

1. Test your connectivity with the Microservices by running the request named 'GET Applications'.
    - This should fire a GET request with the inherited auth token. It should return a 200 OK with the list of applications.

## Exercise 1

### Register the demo OPC UA server manually using its discovery URL

1. Execute the 'GET endpoints' request. The result should be empty.

### BELOW HERE STILL TODO

2. Add an application

   ```bash
   > apps add --url opc.tcp://<hostname>:50000 -a
   ```

   * `–a` auto-activates all discovered twins automatically, a convenience shortcut. Normally an operator would manually activate/enable newly registered twins based on some form of security audit.

3. Run

   ```bash
   > apps list
   ```

   a couple of times until the application is discovered and registered, which should happen within seconds.

   ```bash
   ==================
   {
     "items": [
       {
         "applicationId": "uas0f2c131113668eb4c743dc7c46bcaaeb31902595",
         "applicationName": "OpcPlc",
         "locale": "en-US",
         "supervisorId": "myhostname_module_opctwin",
         "applicationUri": "urn:OpcPlc:plcdemo",
         "hostAddresses": [
           "[fe80::24ea:a4cd:bdf6:8fab%20]:50000",
           "192.168.0.161:51213"
         ],
         "productUri": "https://github.com/azure-samples/iot-edge-opc-plc",
         "applicationType": "Server",
         "discoveryUrls": [
           "opc.tcp://plcdemo:50000/"
         ],
         "certificate": "..."
       }
     ]
   }
   ==================
   >
   ```

   Note all the information about the application that the IoT Edge added during the registration, including public certificate, product URI and more. Copy the `applicationId`.

### Browse all root nodes of the activated endpoint

1. Run

   ```bash
   > apps get –i <applicationId>
   ```

   to see the application and its endpoints. Select one of the id under `endpoints` (endpointId).

2. Run

   ```bash
   > nodes browse –i <endpointId>
   ```

   to browse the root node of the endpoint.

   > If you fail to browse a real OPC UA server, it is because the server does not trust the module client certificate.  For now, please follow the server manual to move the rejected certificate into the trust list and try again.   We are working on it.

### Clean up

Note the `applicationId` in `apps list` and remove the application and related twins by running:

```bash
apps unregister -i <applicationId>
```

## Exercise 2

### Automatically discover all OPC UA servers running in the IoT Edge network

1. On the console prompt, run `apps list` and `endpoints list`, the result should be empty lists, meaning that there are no applications registered. If you see applications or endpoints, follow the cleanup steps below.

1. Run

   ```bash
   > supervisors list
   ```

   and note down the `supervisorId`.

1. Enable scanning in the supervisor by running

   ```bash
   > supervisors update –d Fast -i <supervisorId> –a
   ```

   `–a` is a shortcut that auto-activates all discovered twins. Normally an operator would manually activate/enable registered twins based on an enterprise workflow or audit.

   You can change the configuration to your needs. For more information, see the console help.

1. The discovery takes about a minute. Run

   ```bash
   > apps list
   ```

   a couple of times until the demo OPC UA server application is discovered and registered.

You can now [browse one of the discovered endpoints](#Browse-all-root-nodes-of-the-activated-endpoint).

### Clean up

1. Note each applicationId in `apps list` and remove them by running

   ```bash
   > apps unregister -i <applicationId>
   ```

   Disable scanning on the supervisor by running

   ```bash
   > supervisor update -d Off -i <supervisorId>
   ```

## Next steps

Now that you are done, try to run your own OPC UA server in the same network as your IoT Edge gateway and follow the instructions above with the relevant variations (e.g. discovery URL).

* Learn how to write an application that reads and writes variable values on an OPC UA server (COMING SOON)
* [Explore Microservices REST APIs](../api/readme.md)
