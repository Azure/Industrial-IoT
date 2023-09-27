# HTTP REST Samples

This folder contains several samples that show how to interact with the OPC Publisher over HTTPS. The samples show how to read values or update configuration
using the HTTP REST API of OPC Publisher. To run the samples, first start OPC Publisher:

```bash
cd deploy
cd docker
set EdgeHubConnectionString=<iot edge connection string from Azure portal>
docker compose up
```

Another option is to deploy both into IoT Edge or run them inside the simulation environment as 
explained [here](../../docs/opc-publisher/opc-publisher.md#using-iot-edge-simulation-environment).

You also need to set both the `EdgeHubConnectionString` and the Azure IoT Hub connection string in your environment before running the samples:

```bash
set EdgeHubConnectionString=<iot edge connection string from Azure portal>
set IoTHubConnectionString=<iot hub connection string from Azure portal>
```

> IMPORTANT: The connection strings contain secrets, ensure you clear them and don't share them outside of your environment.

> Restart Visual Studio to pick up the environment variables.

The environment variables are used to access IoT Hub and pull the API key (used as bearer authentication) and the certificate used to secure the REST endpoint.
The certificate contains the private key. In production it should be downloaded and only the certificate itself should be made available to other applications.

> Note: The Environment variables are not required to run the samples. 
> The samples will fall back to use the unsecure HTTP (port 80) endpoint of OPC Publisher.
> However, this is not recommended for production. 
> In production scenarios the HTTP port (80) should not be exposed like it is done in the compose samples. 
