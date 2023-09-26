# Azure IoT Hub Samples

This folder contains several samples that show how to interact with the OPC Publisher through Azure IoT Hub service and show for example how to 
read or write values. To run the samples, first start OPC Publisher and OPC PLC simulation server:

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

> Restart Visual Studio to pick up the environment variables.

> IMPORTANT: The connection strings contain secrets, ensure you clear them and don't share them outside of your environment.