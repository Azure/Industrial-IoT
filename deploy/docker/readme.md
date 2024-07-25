# Deploy in docker compose <!-- omit in toc -->

> IMPORTANT: Docker compose based hosting is experimental and only supported on a best effort basis.

## Table Of Contents <!-- omit in toc -->

- [General usage](#general-usage)
  - [Running with Open Telemetry stack](#running-with-open-telemetry-stack)
  - [Running with dotnet-monitor](#running-with-dotnet-monitor)
  - [Running with Mosquitto MQTT broker](#running-with-mosquitto-mqtt-broker)
- [Setting resource limits on OPC Publisher](#setting-resource-limits-on-opc-publisher)

## General usage

Ensure you have docker compose installed. Run

```bash
docker compose up -d
```

To start OPC Publisher and OPC PLC. If you want to build OPC Publisher images from the repo run

```cmd
build
```

To stop run

```bash
docker compose down -d
```

To connect to Azure IoT Hub set the `EdgeHubConnectionString` environment variable to a Azure IoT Hub device connection string. Such connection string can be obtained in the portal or through the AZ command line.

> If you use a `.env` file to set the variable, remember to delete the file since the connection string contains secrets!

You can run OPC Publisher with a couple of additional components to aid in development:

- Use [Open Telemetry](#running-with-open-telemetry-stack) and track metrics.
- Observe OPC Publisher under [dotnet monitor](#running-with-dotnet-monitor)
- Run with [Mosquitto MQTT broker](#running-with-mosquitto-mqtt-broker)

The different configurations can be mixed and matched.

### Running with Open Telemetry stack

To run OPC Publisher with the Open Telemetry collector connected to Loki, Tempo, Prometheus and Grafana and observe metrics run

```bash
docker compose -f docker-compose.yaml -f with-opentelemtry.yaml up -d
```

### Running with dotnet-monitor

To create dump files and performance traces of OPC Publisher you can attach dotnet monitor tool.

```bash
docker compose -f docker-compose.yaml -f with-monitor.yaml up -d
```

### Running with Mosquitto MQTT broker

You can connect OPC Publisher with MQTT output to Mosquitto. The included configuration includes the OPC Foundation UA Cloud dashboard.

```bash
docker compose -f docker-compose.yaml -f with-mosquitto.yaml up -d
```

Hint: Ensure that you keep the OPC PLC nodes configured to a minimum to show nice graphs.

## Setting resource limits on OPC Publisher

You should set limits that are not aggressive and take the following runtime characteristics into account:

- Number of sessions.  Each session requires significant memory (10-20 MB depending on the size of the server)
- Number of nodes. Each node has a cache of last value
- Number of messages queued up for batching.
- Size of the data per monitored item

It is recommended to set a limit of 4gb. But you should monitor runtime usage in production to determined exact numbers.

You can run the OPC Publisher with 50m limits under docker, which is not supported by either .net nor us. Nevertheless, the following docker compose configuration shows which settings you can set to run OPC Publisher container for several hours in this mode.

```bash
docker compose -f docker-compose.yaml -f with-limits.yaml up -d
```

We use this mode to test for leaks over time. You an use the `gcdump.ps1` powershell script to start the same together with dotnet-monitor to periodically dump the GC and observe differences in memory use over time.

This setup is certainly experimental because:

- Running the above command uses the null transport sink, so no data is ever buffered, messages are just thrown away. The more that is buffered before sending the more memory is required.
- Alpine images must be used if you want to run for more than 3 seconds with said limit. The official OPC Publisher images are based on Alpine, but the ones built from the repository do not.
- We disable meta data loading which has significant memory overhead. But you might need it.
- The publisher publishes 11 nodes with value changes every 1 second. There is no buffering enabled, the messages are immediately dropped after encoding.
- The setup is continuously under GC pressure and a good chunk of CPU is used for garbage collection and compression.
- Open Telemetry and Prometheus metrics are disabled as they consume a large amount of memory.
- The .net GC is configured to be over aggressive which are not ideal for production scenarios.
