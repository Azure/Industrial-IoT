# Discover and register servers and browse their address space from the command line

This article will walk you through the steps to discover and register OPC UA servers using the Command line interface.  

## Prerequisites

You should have already successfully deployed all services and at least one IoT Edge Gateway with the Industrial IoT Modules.  If you have not, please follow the instructions in:

1. [Deploy the Industrial IoT services to Azure](../howto-deploy-services.md) and securely store the resulting `.env` file.

2. [Deploy Industrial IoT Edge Modules](../howto-deploy-modules.md)

To run the demo OPC UA server and the OPC Device Management Console Client you will also need Docker installed on your development PC.  If you have not, please follow the instructions for [Linux](https://docs.docker.com/install/linux/docker-ce/ubuntu/), [Mac](https://docs.docker.com/docker-for-mac/install/), or on [Windows](https://docs.docker.com/docker-for-windows/install/).

Also, make sure you have Git installed.  Otherwise follow the instructions for [Linux or Mac](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git), or [Windows](https://gitforwindows.org/) to install it.

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

## Build and run the Azure IoT Industrial IoT API CLI console

1. Ensure you have saved the `.env` file that was generated during deployment in the repo root folder.

2. Build and start the CLI in console mode by running

   ```bash
   docker build -t cli .
   docker run -it --env-file .env cli console 
   ```

   > If you are trying to access running services on your local development machine and your OS is Linux you must explicitly specify your host's host name using `-e _HOST=$(hostname)` argument.

3. You will now see a prompt and be able to enter commands, e.g. type 

   ```bash
   > help

   aziiotcli - Allows to script Industrial IoT Services api.
   usage:      aziiotcli command [options]

   Commands and Options

        console     Run in interactive mode. Enter commands after the >
        exit        Exit interactive mode and thus the cli.
        apps        Manage applications
        endpoints   Manage endpoints
        supervisors Manage supervisors
        nodes       Call nodes services on endpoint
        status      Print service status
        help, -h, -? --help
                    Prints out this help.

   >
   ```

4. Test your connectivity with the services by running

   ```bash
   > status
   ```

   If you have deployed with authentication you should see an output like this:

   ```bash
   To sign in, use a web browser to open the page https://microsoft.com/devicelogin and enter the code ABCDEFG to authenticate.
   ```

   Follow the prompt to authenticate and you should see output similar to

   ```bash
   Connecting to https://opctwintest.azurewebsites.net/twin...
   ==================
   {
     "Name": "IIoT-Opc-Twin-Service",
     "Status": "OK:Alive and well",
     "CurrentTime": "2019-01-10T18:42:42+00:00",
     "StartTime": "2019-01-10T18:42:18+00:00",
     "UpTime": 24
   }
   ==================
   Connecting to https://opctwintest.azurewebsites.net/registry...
   ==================
   {
     "Name": "IIoT-Opc-Twin-Registry",
     "Status": "OK:Alive and well",
     "CurrentTime": "2019-01-10T18:42:44+00:00",
     "StartTime": "2019-01-10T18:42:18+00:00",
     "UpTime": 26
   }
   ==================
   >
   ```

5. Next, make sure the IoT Edge Gateway with OPC Twin module is up and running by entering...

   ```bash
   > supervisors list
   ```

   You should see the OPC Twin Modules you deployed in the form of their supervisor identities, for example:

   ```bash
   ==================
   {
     "items": [
       {
         "id": "myhostname_module_opctwin"
       }
     ]
   }
   ==================
   >
   ```

## Exercise 1

### Register the demo OPC UA server  manually using its discovery URL

1. In the OPC Twin Console, run `apps list` and `endpoints list`, the result should be an empty list with no applications and endpoints registered.

2. Enter..

   ```bash
   > apps add --url opc.tcp://<hostname>:50000 -a
   ```

   - `–a` auto-activates all discovered twins automatically, a convenience shortcut. Normally an operator would manually activate/enable newly registered twins based on some form of security audit.

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

- Learn how to write an application that reads and writes variable values on an OPC UA server
- [Explore the OPC Device Management REST APIs](../api/readme.md)
