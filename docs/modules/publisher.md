# OPC Publisher Edge Module

[Home](../readme.md)

The OPC Publisher, just like the OPC Twin module also runs inside IoT Edge.  The Publisher module connects to OPC UA servers and publishes OPC UA telemetry data from these servers to Azure IoT Hub.  

The publisher is able to subscribe to Variable changes (sampling) and Events and publish these in the form of

* OPC UA Network Messages (UADP and JSON as per Part 14). 
* Simplified monitored item notification messages (JSON - non standardized) 

The publisher module can be configured using Writer Group jobs through the Industrial IoT Job Micro services.   These Writer Group jobs represent a JSON version of the OPC UA Part 14 configuration model.   

## Running OPC Publisher module 

OPC Publisher runs in [Azure IoT Edge](https://docs.microsoft.com/azure/iot-edge).  To add the PC Publisher as module to your IoT Edge deployment, go to the Azure portal and navigate to your IoT Hub and:

* Go to IoT Edge and create or select your IoT Edge device.

* Select `Set Modules`.

* Select `Add`under `Deployment Modules`and then `IoT Edge Module`.

* In the `Name` field, enter `publisher`.

* In the `Image URI` field, enter `mcr.microsoft.com/iotedge/opc-publisher:<tag>` To use the latest image use "latest" as tag, otherwise the released version, for example 2.6.

* Paste the following into the `Container Create Options` field:

        {
            "Hostname": "publisher",
            "Cmd": [
                "--aa"
            ]
        }
    This configuration will configure IoT Edge to start a container named `publisher` with OPC Publisher as the image.  The hostname of the container's system will be set to `publisher`.  OPC Publisher is called with the following command line: `--aa`.   With this option OPC Publisher trusts those OPC UA server's certificates it is connecting to.  You can use any options in the `Cmd` array that OPC Publisher supports. The only limitation is the total size of the `Container Create Options` field supported by IoT Edge.

* If you need to connect to an OPC UA server using the hostname of the OPC UA server and have no DNS configured, you can enable resolving the hostname by adding an `ExtraHosts` configuration to the `HostConfig` object:

                "HostConfig": {
                    "ExtraHosts": [
                        "opctestsvr:192.168.178.26"
                    ]
                }

    This setting will allow OPC Publisher running in the IoT Edge module to resolve the hostname `opctestsvr` to the IP Address `192.168.178.26`.  This is equivalent to using `--add-host opctestsvr:192.168.178.26` as parameter to a`docker run` command.

* Leave the other settings unchanged and select `Save`.

* If you want to process the output of the OPC Publisher locally with another Edge module, go back to the `Set Modules` page, and go to the `Specify Routes` tab and add a new route looking like (Notice the usage of the output for the OPC publisher):

        {
          "routes": {
            "processingModuleToIoTHub": "FROM /messages/modules/processingModule/outputs/* INTO $upstream",
            "opcPublisherToProcessingModule": "FROM /messages/modules/publisher INTO BrokeredEndpoint(\"/modules/processingModule/inputs/input1\")"
        }

* Back in the `Set Modules` page, select `Next`, till you reach the last page of the configuration.

* Select `Submit` to send your configuration down to IoT Edge

### Building and running Publisher from source and outside of IoT Edge

Make sure you cloned the repo into a local folder and deployed at least the dependencies in Azure and saved the .env file in the root of your repo.  On the command line ...

```
cd modules/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher/cli
dotnet run
```

This will start the publisher module debug wrapper which will create an identity for the publisher in IoT Hub.

## Additional information

### OPC UA X.509 certificates

The default store type for all cert stores is the file system. You can change that via command line options. Especially when you run OPC Publisher in a container, then the persistency of the certificates is important, since the container does not provide persistency. You need to use docker's `-v` option to persist the certificate stores in the host file system or a docker volume. If you are use a docker volume, you can pass in certificate relevant data via base64 encoded strings.

You need to take special care how certificate stores are handled. Especially for the application certificate the runtime environment has impact and you want to make sure that it is persisted and not created new on each start:

* Running on Windows natively, you can not use an application certificate store of type `Directory`, since the access to the private key fails. Please use the option `--at X509Store` in this case.
* Running as Linux docker container, you can map the certificate stores to the host file system by using the docker run option `-v <hostdirectory>:/appdata`. This will make the certificate persistent over starts.
* Running as Linux docker container and want to use an X509Store for the application certificate, you need to use the docker run option `-v x509certstores:/root/.dotnet/corefx/cryptography/x509stores` and the application option `--at X509Store`

### Legacy configuration using configuration file

To support backwards compatibility with previous versions of the publisher, the publisher also supports a legacy mode where the publishing configuration can be specified through a local configuration file

An example for the format of the configuration file is:

        [
          {
            "EndpointUrl": "opc.tcp://testserver:62541/Quickstarts/ReferenceServer",
            "UseSecurity": false,
            "OpcNodes": [
              {
                "Id": "i=2258",
                "OpcSamplingInterval": 2000,
                "OpcPublishingInterval": 5000,
                "DisplayName": "Current time"
              }
            ]
          }
        ]

### Breaking changes in 2.6.x from 2.5.x and lower versions

While we attempt to maintain backwards compatibility as much as possible.  This means you should use any publisher module as a drop in replacement for a previous version.   However - to support new functionality from 2.6.x forward we removed previously (up to version 2.5.x) available functionality, including

- OPC UA server and configuration support via device method interface.   The old model has been replaced with the new and richer Writer Group configuration surface of the [Publisher API](../api/publisher/readme.md).
- Message shaping using the `--tc` option and telemetry configuration file.
- Message batching (array of messages).  This has been replaced with the new and standardized Network Message format, which can also be emitted as binary (UADP) encoded messages.
- Several command line options (see `--help` for more details) which were only listed in previous documentation.

We did not make these decisions lightly but believe the new functionality is worth these tradeoffs.  If you feel functionality has been removed that you are relying on, please open a GitHub issue providing the use case.

### Licensing

All OPC UA components use the OPC Foundation's OPC UA reference stack as nuget packages and therefore licensing of their nuget packages apply. Visit https://opcfoundation.org/license/redistributables/1.3/ for the licensing terms.

## Next steps

- [Learn how to deploy OPC Publisher and Twin Modules](../howto-deploy-modules.md)
- [Learn about the OPC Twin Microservice](../services/twin.md)