# Discover and register servers and browse their address space, read and publish nodes via REST API in Postman

[Home](readme.md)

This article will walk you through the steps to discover and register OPC UA servers, browse through the endpoints, read node values and publish these using the [REST API](https://azure.github.io/Industrial-IoT/api/), through Postman.  

## Prerequisites

You should have already successfully deployed all Microservices and at least one IoT Edge Gateway with the Industrial IoT Modules.  If you have not, please follow the instructions in:

1. [Deploy the Industrial IoT Microservices to Azure](deploy/howto-deploy-microservices.md).

2. [Install Industrial IoT Edge](deploy/howto-install-iot-edge.md)

    To run the demo OPC UA server you will also need Docker installed on a machine that is visible to the IoT Edge from a network point of view).  If you have not, please follow the instructions for [Linux](https://docs.docker.com/install/linux/docker-ce/ubuntu/), [Mac](https://docs.docker.com/docker-for-mac/install/), or on [Windows](https://docs.docker.com/docker-for-windows/install/).

    Also, make sure you have Git installed on the same machine that will run the demo OPC UA server.  Otherwise follow the instructions for [Linux or Mac](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git), or [Windows](https://gitforwindows.org/) to install it.

3. [Have Postman installed](https://www.getpostman.com/).

## Start the demo OPC UA server

To make the demo deterministic we also start a demo OPC UA server.  

1. Open a terminal or command prompt and run

   ```bash
   hostname
   ```

   Remember the return host name of the machine running the OPC server.  You will need it later when you specify the discovery URL of the server.  

2. Now run

   ```bash
   docker run -it -p 50000:50000 mcr.microsoft.com/iotedge/opc-plc -aa
   ```

   to start PLC demo server.  For simplicity we instruct the demo server to accept our certificate.  

## Configure Azure AD App permissions for using later with Postman

Note: the Postman requests will use an Authorization Code flow to authenticate to the service.

### Create AAD App secret

1. Go into the Azure Portal > Azure Active Directory > App Registrations.
2. During deployment of the services in the cloud, two application registrations will have been created. 
  - [yourprefix]-clients
  - [yourprefix]-services
3. Search for the '-clients' app registration created upon deployment of the solution. 
4. Go to the detail screen by clicking on it, and choose 'Clients & secrets'.
5. Under Client secrets, click 'New client secret'.
6. Provide a unique name and expiration date (as you prefer).
7. Click Add. Don't close the screen!
8. Before closing the screen, make sure you copy the Value of the new secret. You will not be able to see it in the future after you leave this screen. Take note of the secret and copy for later in this tutorial. This will be used for [YOUR CLIENT SECRET] in the Postman configuration.

### Take note of AAD app information

1. Within Azure Active Directory, browse to the '-clients' application if you are no longer in there. From the Overview screen, copy the and keep handy:
    - Application (client) ID (this will be used below for [YOUR AAD CLIENT ID])
    - Directory (tenant) ID (this will be used below for [YOUR TENANT ID])
2. From Azure Active Directory, back in App registrations, go to the details of the '-services' appl registration. You'll need to copy and keep handy the following data:
    - From the Expose an API screen, copy the default scope which will look like this: 'https://[yourtenant]].onmicrosoft.com/[yourprefix]-services/user_impersonation', you will need this under [YOUR SCOPE URI] below

## Download a sample set of API calls (Postman collection)

1. Download the collection [here](media/OPCTwin.postman_collection.1.0.json)
2. Import the collection by choosing 'Import' button at the top left of Postman.
3. Create a new Environment named OPC Twin: within Postman at the top right click Manage Environments.
4. Click Add.
5. Add a new variable:
    - OPC-SERVICEURL: typically this will be something like '[yourprefix].azurewebsites.net'
6. Click Add and close the screen. ![Environment](media/2-postmanenv.png)

### Request a new OAuth 2.0 Token

1. In Postman, on the left you will see your collections, click the '...' (three dots) next to the OPC Twin collection and choose 'Edit'. 
![edit](media/postman-edit-collection.png)
2. Choose Authorization. ![auth](media/3-auth-edit.png)
3. Choose Get New Access Token.
4. Fill in the following fields:
    - Token Name: your choice
    - Grant type: Authorization Code
    - Callback URL:  https://[yourserviceendpoint]/.auth/login/aad/callback, for example  https://[yourprefix].azurewebsites.net/.auth/login/aad/callback
    - Auth URL: https://login.microsoftonline.com/[YOUR TENANT ID]/oauth2/v2.0/authorize
    - Access Token URL: https://login.microsoftonline.com/[YOUR TENANT ID]/oauth2/v2.0/token
    - Client ID: [YOUR AAD CLIENT ID]
    - Client Secret: [YOUR CLIENT SECRET]
    - Scope: [YOUR SCOPE URI]
    - Click Request Token.
    - You will be prompted to login.
    ![gettoken](media/4-postmantoken.png)
    - A token is returned, scroll to the bottom and click 'Use Token'.
    Click Update to close the Edit screen.

5. Test your connectivity with the Microservices by running the request named 'GET Applications'.
    - This should fire a GET request with the inherited auth token. It should return a 200 OK with the list of applications.

  > Note your token will expire regularly. If you get an authentication error during any of the calls, just repeat the process to refresh your token. All of the requests in the Postman collection inherit the same Auth. 

## Exercise 1

### Register the demo OPC UA server manually using its discovery URL

1. Execute the 'Registry: Get Applications' request. The result should be empty.
1. Locate the 'Registry: Add application' request. Edit the request body to point to your hostname:
    ```
    {
      "discoveryUrl": "opc.tcp://[yourhostname]:50000"
    }
    ```
1. Execute the 'Registry: Get Applications' request a few times until you get the result and have the application discovered and registered.

   Note all the information about the application that the IoT Edge added during the registration, including public certificate, product URI and more. Copy the `applicationId` for the entry that has `productUri` set to `https://github.com/azure-samples/iot-edge-opc-plc`.

### Browse all root nodes of endpoint

1. Execute the request 'Registry: browse endpoints' to see the application and its endpoints. Select one of the id under `endpoints` (`endpointId`). Before executing, make sure you update the applicationId copied above.

2. Activate endpoint: run the request 'Registry: Activate endpoint' and replace the endpointId in the URL with your above `endpointId`.

3. Run 'Registry: browse endpoints' again and you should see your endpoint as acvite and ready.
    ```
     [ommitted]...
      "applicationId": "............................",
      "activationState": "ActivatedAndConnected",
      "endpointState": "Ready"
      ...
    ```

4. Execute the request 'Twin: browse endpoint for ID x' and replace the `endpointId` to browse the root node of the endpoint.

   > If you fail to browse a real OPC UA server, it is because the server does not trust the module client certificate.  For now, please follow the server manual to move the rejected certificate into the trust list and try again.   We are working on it.

## Exercise 2

### Browse nodes and read telemetry value

1. Once you have application and endpoints, execute the 'Registry - get endpoints' Postmann request to get your base endpoint ID for the OPC PLC. This will be one where the `endpointUrl` is on port 50000. Copy `id` (endpoint id).
2. Execute the request 'Twin: browse endpoint for ID x', using the `id` copied in the URL.
![img](media/ex3-twinbrowsebyid.png)
3. Copy the `nodeId` of the Objects (displayName: Objects) node. In our case this was `i=85`.
4. Execute the request 'Twin: browse target node', replace the ID in the URL with your `id` from above, and add the node ID to the query parameter. If the node id includes special characters such as a '/' or ':' then you should URL Encode the parameter by right-clicking the selection and choosing the Postman option 'EncodeUriComponent'.
![img](media/ex3-uriencode.png)
5. From the result, copy `nodeId` of the OpcPlc target node.
6. Execute the same request, but now use the above `nodeId` value in the request parameter. Remember to URI encode.
7. From the result copy the `nodeId` for Telemetry target node. Repeat above with this `nodeId`.  This should give you a set of target nodes with variables and their data and values.

### Read a node value

1. From the above last step, copy the `nodeId` of the variable `SpikeData`.
2. Execute the request 'Twin: read value'. Replace the ID in the URI with the `endpoint id`. 
3. You should now read the current value of the variable.

### Publish a node (OPC Publisher)

1. The last step in this exercise is looking at the published nodes, and add a variable to be published. The step of publishing something applies to the configuration of the OPC Publisher module. So in essence this step is about remotely configuring the OPC Publisher.
2. Execute the request 'Twin: Get currently published nodes', replace the endpointId in the URL with your `applicationId`.
3. This will show an empty list if you have not yet published any nodes.
4. Now execute the request 'Twin: publish node', make sure the URI has your `applicationId`, and update the request's Body to reflect the node you wish to publish.
5. Re-execute the request 'Twin: Get currently published nodes'.

## Clean up

Note the `applicationId` by executing the request `Registry: get applications. 
Execute the request 'Registry: remove applicaton', make sure to change the applicaitonId in the Uri.

Note this step does not remove any configuration for published nodes in OPC Publisher.

## Next steps

Now that you are done, try to run your own OPC UA server in the same network as your IoT Edge gateway and follow the instructions above with the relevant variations (e.g. discovery URL).

- See the [architectural flow diagrams](architecture-flow.md) for more information.
- [Explore Microservices REST APIs](api/readme.md)
