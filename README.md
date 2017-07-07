This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments

# OPC Publisher for Azure IoT Edge
This reference implementation demonstrates how Azure IoT Edge can be used to connect to existing OPC UA servers and publishes JSON encoded telemetry data from these servers in OPC UA "Pub/Sub" format (using a JSON payload) to Azure IoT Hub. All transport protocols supported by Azure IoT Edge can be used, i.e. HTTPS, AMQP and MQTT (the default).

This application, apart from including an OPC UA *client* for connecting to existing OPC UA servers you have on your network, also includes an OPC UA *server* on port 62222 that can be used to manage what gets published.

This application uses the OPC Foundations's OPC UA reference stack and therefore licensing restrictions apply. Visit http://opcfoundation.github.io/UA-.NETStandardLibrary/ for OPC UA documentation and licensing terms.

|Branch|Status|
|------|-------------|
|master|[![Build status](https://ci.appveyor.com/api/projects/status/6t7ru6ow7t9uv74r/branch/master?svg=true)](https://ci.appveyor.com/project/marcschier/iot-gateway-opc-ua-r4ba5/branch/master) [![Build Status](https://travis-ci.org/Azure/iot-gateway-opc-ua.svg?branch=master)](https://travis-ci.org/Azure/iot-gateway-opc-ua)|

# Building the Application

This application requires the .NET Core SDK V1.1. You can build the application from Visual Studio 2017 by opening the solution file and hitting F7.

# Configuring the Application
The OPC UA nodes whose values should be published to Azure IoT Hub can be configured by creating a "publishednodes.json" file. This file is auto-generated and persisted by the application automatically when using it's OPC UA server interface from a client. If you want to create the file manually instead, below is the format of the SAMPLE publishednodes.json file:
```
[
  {
    "EndpointUrl": "opc.tcp://myopcservername:51210/UA/SampleServer",
    "NodeId": { "Identifier": "ns=1;i=123" }
  }
  {
    "EndpointUrl": "opc.tcp:// myopcservername:51210/UA/SampleServer",
    "NodeId": { "Identifier": "ns=2;i=456" }
  }
]
```
The "Identifier" tag follows the string representation of an OPC UA node ID as described in the OPC UA specifications.

# Running the Application

## From Visual Studio 2017
You can run the app directly via Visual Studio 2017 by hitting F5. Don't forget your command line arguments, i.e. ```<yourApplicationName>``` (needs to be specified always) and the ```<IoTHubOwnerConnectionString>``` (needs to be specified on first run ONLY)!

## From Docker
You can also run the application in a Docker container using the Dockerfile provided. From the root of the repo, in a console, type:

```docker build -t gw .```

On first run, for one-time IoT Hub registration:

```docker run -it --rm gw <applicationName> <IoTHubOwnerConnectionString>```

From then on:

```docker run -it --rm gw <applicationName>```

For detailed instructions on using Docker with the OPC Publisher, see [here](https://docs.microsoft.com/en-us/azure/iot-suite/iot-suite-connected-factory-gateway-deployment).
