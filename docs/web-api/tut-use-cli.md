# Discover and register servers and browse their address space from the command line

[Home](./readme.md)

This article will walk you through the steps to discover and register OPC UA servers using the Command line interface.  The command line interface exercises almost the entire REST API and allows you to

* Discover and register applications and endpoints
* Read and write variable values
* Publish and unpublish variable changes to Azure (sampling)
* Browse applications, endpoints and server address space on an endpoint
* Monitor server tag changes, discovery progress and changes to the registry

## Prerequisites

You should have already deployed the companion [web api](./readme.md) and at least one IoT Edge Gateway with the [OPC Publisher module](../opc-publisher/readme.md).

To run the demo OPC UA server you will also need Docker installed on a machine that is visible to the IoT Edge from a network point of view. If you don't have it, follow the instructions for [Linux](https://docs.docker.com/install/linux/docker-ce/ubuntu/), [Mac](https://docs.docker.com/docker-for-mac/install/), or [Windows](https://docs.docker.com/docker-for-windows/install/).

Make sure you have Git installed on the same machine that will run the demo OPC UA server. Otherwise follow the instructions for [Linux, Mac](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git) or [Windows](https://gitforwindows.org/) to install it.

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
   cd ./src/Azure.IIoT.OpcUa.Publisher.Service.Sdk/cli
   dotnet run console
   ```

   > On Windows you can also run `cli.cmd` in the repository root.

3. You will now see a prompt and be able to enter commands, e.g. type

   ```bash
   > help

   aziiotcli - Allows to script Industrial IoT Services api.
   usage:      aziiotcli command [options]

   Commands and Options

        console     Run in interactive mode. Enter commands after the >
        exit        Exit interactive mode and thus the cli.
        status      Print service status

         ...

        help, -h, -? --help
                    Prints out this help.

   >
   ```

4. Test your connectivity with the web api by running

   ```bash
   > status
   ```

   If you have deployed with authentication you should be prompted to authenticate.
   Next you will see something similar to

   ```bash
   Connecting to https://opctwintest.azurewebsites.net/twin...
   ==================
   OK
   ==================
   Connecting to https://opctwintest.azurewebsites.net/registry...
   ==================
   OK
   ==================
   >
   ```

5. Next, make sure the IoT Edge Gateway with OPC Publisher module is up and running by entering...

   ```bash
   > publishers list
   ```

   You should see the OPC Publisher Modules you deployed in the form of their publisher identities, for example:

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
   > apps add --url opc.tcp://<hostname>:50000
   ```

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

   Note all the information about the application that the IoT Edge added during the registration, including public certificate, product URI and more.

### Browse all root nodes of the activated endpoint

> Make sure you have selected the endpoint you want to communicate with.  If you did not select one you must pass the endpoint id using the `-i` flag.

1. Run

   ```bash
   > nodes browse
   ```

   to browse the root node of the endpoint.

   > If you fail to browse a real OPC UA server, it is because the server does not trust the module client certificate.  For now, please follow the server manual to move the rejected certificate into the trust list and try again.   We are working on it.

2. Run

   ```bash
   > nodes publish -n i=2258
   ```

   to publish the system time changes to Azure.

3. Run

   ```bash
   > nodes monitor
   ```

   to monitor the values being published.

### Clean up

To remove all applications and related endpoints run:

```bash
apps unregister
```

## Exercise 2

### Automatically discover all OPC UA servers running in the IoT Edge network

1. On the console prompt, run `apps list` and `endpoints list`, the result should be empty lists, meaning that there are no applications registered. If you see applications or endpoints, follow the cleanup steps below.

2. Run

   ```bash
   > publishers list
   ```

   and note down the `publisherId`.

3. Scan network

   ```bash
   > app discover -m
   ```

   `–m` monitors the discovery process.

4. Run

   ```bash
   > apps list
   ```

   To list all found servers.

5. To get a list of endpoints run

   ```bash
   > endpoints list
   ```

   If you have endpoints and these do not show up after a couple seconds take a look at the state of the endpoint.

   Remember one of the endpoints id, e.g. run `endpoints select -i <id>` so you do not need to pass the identifier in further commands.

You can now [browse one the discovered endpoints](#browse-all-root-nodes-of-the-activated-endpoint).

### Clean up applications

To remove all applications and related endpoints run:

```bash
> apps unregister
```
