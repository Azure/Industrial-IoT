This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments

# OPC Publisher Module for Azure IoT Edge
This reference implementation demonstrates how Azure IoT Edge can be used to connect to existing OPC UA servers and publishes JSON encoded telemetry data from these servers in OPC UA "Pub/Sub" format (using a JSON payload) to Azure IoT Hub. All transport protocols supported by Azure IoT Edge can be used, i.e. HTTPS, AMQP and MQTT. The transport is selected in the transport setting in the gatewayconfig.json file.

This module, apart from including an OPC UA *client* for connecting to existing OPC UA servers you have on your network, also includes an OPC UA *server* on port 62222 that can be used to manage the module.

This module uses the OPC Foundations's OPC UA reference stack and therefore licensing restrictions apply. Visit http://opcfoundation.github.io/UA-.NETStandardLibrary/ for OPC UA documentation and licensing terms.

|Branch|Status|
|------|-------------|
|master|[![Build status](https://ci.appveyor.com/api/projects/status/6t7ru6ow7t9uv74r/branch/master?svg=true)](https://ci.appveyor.com/project/marcschier/iot-gateway-opc-ua-r4ba5/branch/master) [![Build Status](https://travis-ci.org/Azure/iot-gateway-opc-ua.svg?branch=master)](https://travis-ci.org/Azure/iot-gateway-opc-ua)|

# Directory Structure

## /src
This folder contains the source code of the module, a managed gateway loader and a library to handle IoT Hub credentials.

# Building the Module

This module requires the .NET Core SDK V1.1. You can build the module from Visual Studio 2017 by opening the solution file, right clicking the GatewayApp.NetCore project and selecting "publish".

# Configuring the Module
The OPC UA nodes whose values should be published to Azure IoT Hub can be configured by creating a "publishednodes.json" file. This file is auto-generated and persisted by the module automatically when using the Publisher's OPC UA server interface from a client. If you want to create the file manually instead, below is the format of the SAMPLE publishednodes.json file:
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

# Configuring the Gateway
The ```Configuration``` Section must contain at a minimum all items shown in the provide file. The JSON type conforms to the OPC UA reference stack serialization of the ```ApplicationConfiguration``` type.  

You should pass your application name and the IoT Hub owner connection string (which can be read out for your IoT Hub from portal.azure.com) as command line arguments. The IoT Hub owner connection string is only required for device registration with IoT Hub on first run.

# Running the module

## From Visual Studio 2017
You can run the module through the supplied gateway app GatewayApp.NetCore on Windows along with the Gateway SDK and IoT Hub module directly via Visual Studio 2017 by hitting F5 (after publishing GatewayApp.NetCore). Don't forget your command line arguments, i.e. ```<yourApplicationName>``` (needs to be specified always) and the ```<IoTHubOwnerConnectionString>``` (needs to be specified on first run ONLY)!

## From Docker
You can also run the module in a Docker container using the Dockerfile provided. From the root of the repo, in a console, type:

```docker build -t gw .```

On first run, for one-time IoT Hub registration:

```docker run -it --rm gw <applicationName> <IoTHubOwnerConnectionString>```

From then on:

```docker run -it --rm gw <applicationName>```

For detailed instructions on using Docker with the OPC Publisher, see [here](https://docs.microsoft.com/en-us/azure/iot-suite/iot-suite-connected-factory-gateway-deployment).
