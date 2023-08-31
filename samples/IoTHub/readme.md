# Azure IoT Hub Device Method Samples

This folder contains several samples that show how to interact with the OPC Publisher through Azure IoT Hub service and show for example how to 
read or write values. To run the samples, first start OPC Publisher and OPC PLC simulation server:

````bash
cd deploy
cd docker
docker compose -f docker-compose.yaml -f with-mosquitto.yaml up
```

Another option is to deploy both into IoT Edge or run them inside the simulation environment as 
explained [here](../../docs/opc-publisher/opc-publisher.md#using-iot-edge-simulation-environment).
