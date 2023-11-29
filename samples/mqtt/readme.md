# MQTT Samples

This folder contains several samples that show how to interact with the OPC Publisher over MQTT and show how to read values or subscribe to 
telemetry sent by the OPC Publisher. To run the samples, first start OPC Publisher and Mosquitto MQTT broker:

````bash
cd deploy
cd docker
docker compose -f docker-compose.yaml -f with-mosquitto.yaml up
```

The samples will communicate through the mosquitto broker which runs at localhost:1883.
> For simplicity of the same, the broker is listening on port 1883 and thus an unencrypted connection is used. In production, ensure to use TLS.

The sample uses the MQTT.net RPC library. The MQTT.net RPC libary does not support error conditions, and the ExecuteAsync call returns just the
response buffer and no additional context of the MQTT message. OPC Publisher will return a status code as part of the user properties of the 
response message which can be read via other MQTT libraries.