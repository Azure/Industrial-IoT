This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments

# OPC UA Client Module for the Azure IoT Gateway SDK
This reference implementation demonstrates how the Azure IoT Gateway SDK can be used to connect to existing OPC UA servers and send JSON encoded telemetry data from these servers in OPC UA "Pub/Sub" format (using a JSON payload) to Azure IoT Hub. All transport protocols supported by the Gateway SDK can be used, i.e. HTTPS, AMQP and MQTT. The transport is selected in the transport setting in gateway_config.json.

This module uses the OPC Foundations's OPC UA reference stack and therefore licensing restrictions apply. Visit http://opcfoundation.github.io/UA-.NETStandardLibrary/ for OPC UA documentation and licensing terms.

|Branch|Status|
|------|-------------|
|master|[![Build status](https://ci.appveyor.com/api/projects/status/6t7ru6ow7t9uv74r/branch/master?svg=true)](https://ci.appveyor.com/project/marcschier/iot-gateway-opc-ua-r4ba5/branch/master) [![Build Status](https://travis-ci.org/Azure/iot-gateway-opc-ua.svg?branch=master)](https://travis-ci.org/Azure/iot-gateway-opc-ua)|

# Azure IoT Gateway SDK compatibility
The current version of the Proxy module is targeted at the Azure IoT Gateway SDK at commit 09bbcb7feaf5acc3913abd722b96e993238edd0c.

Use the following command line to clone the compatible version Azure IoT Gateway SDK, then follow the build instructions included:

```
git clone --recursive https://github.com/Azure/azure-iot-gateway-sdk.git
git checkout 09bbcb7feaf5acc3913abd722b96e993238edd0c
```

The gateway needs to be build with the ```--enable_dotnetcore_binding``` flag to enable it to run this module.
# Directory Structure

## /samples
This folder contains a sample configuration that instructs a vanilla gateway host load the module and IoT Hub proxy module and configures the module to create a 
subscription on a standard server which publishes the current server time to Azure IoT Hub.

## /src
This folder contains the C# OPC UA module source file (Module.cs).

## /bld
This folder contains build scripts for Windows and Linux.  

# Building the Module

Run ```bld/build``` to build the module both Debug and Release.  The published module can be found in ```build/release``` folder.  Run the build script with the ```--help``` command line argument to see all build options.  

# Configuring the Module
OPC UA nodes whose values should be published to Azure IoT Hub can be configured in the module JSON configuration.  A sample template configuration file can be found in ```samples/gateway_config.json```.  The configuration consists of a OPC-UA Application Configuration and Subscriptions section.  

## Application Configuration section
The ```Configuration``` Section must contain at a minimum all items shown in the sample template.  The JSON type conforms to the OPC UA reference stack serialization of the ```ApplicationConfiguration``` type.  

E.g. to enable automatic certificate accept (discouraged), set ```"AutoAcceptUntrustedCertificates"``` to true (default is false) inside the ```SecurityConfiguration``` section:

``` JSON
           "args": {
                "Configuration": {
                    "ApplicationName": "Opc.Ua.Client.SampleModule",
                    "ApplicationType": "Client",
                    "ApplicationUri": "urn:localhost:OPCFoundation:SampleModule",
                    "SecurityConfiguration": {
                        "ApplicationCertificate": {},
                        "AutoAcceptUntrustedCertificates": true
                    },
```

## Subscriptions section
The ```Subscriptions``` Section contains an array of OPC-UA sessions that the module should establish at startup, with each specifying a list of items to monitor.  The ```MonitoredItems``` array type in the JSON configuration conforms to the OPC UA reference stack serialization specification of the same type. 

E.g. the sample template shows how to monitor the **Current Server Time** node (node ID 2258):

``` JSON
                },
                "Subscriptions": [
                    {
                        "Id": "<DeviceID>",
                        "SharedAccessKey": "<SharedAccessKey>",
                        "ServerUrl": "opc.tcp://<hostname>:51210/UA/SampleServer",
						"MinimumSecurityLevel": 0,
						"MinimumSecurityMode": "SignAndEncrypt",
                        "PublishingInterval": 400,
                        "MonitoredItems": [
                            {
                                "StartNodeId": "i=2258",
                                "NodeClass": 2,
                                "DisplayName": "ServerStatusCurrentTime",
                                "DiscardOldest": false
                            }
                        ]
                    }
                ]
            },
```

The JSON snippet above shows the default security settings, which can thus be ommitted.  By default the session is created on the endpoint that supports ```SignAndEncrypt``` message mode, regardless of the security level advertised by the server (0).  You can adjust these settings to customize the endpoint selection process, e.g. setting the Security mode to ```"Sign"``` will select all ```Sign``` and ```SignAndEncrypt``` endpoints, ```"None"``` will select all endpoints.

# Running the module

To run the module and have it publish to IoT Hub, configure the name of your Hub (JSON field ```"IoTHubName"```) and the IoT Hub device ID and shared access key to use (JSON fields ```"Id"``` and ```"SharedAccessKey"```) in your version of ```gateway_config.json```.  Note that if you use a "Mapping" module in your configuration, you can omit the ```"SharedAccessKey"``` field.  Finally, ensure that the right native module is configured, based on your platform (i.e. iothub.dll for Windows, libiothub.so for Linux, etc.).

You can build a sample gateway host as part of the Azure IoT Gateway SDK build system.  Ensure that you pass the ```--enable_dotnetcore_binding``` to the build script and use one of the resulting sample gateway hosts.   

To simplify this step, and to build a sample gateway to host the OPC-UA module - along with the module itself - clone the gateway SDK repo to your device and run the build script with the ```-i <root-of-sdk>```.  The resulting release folder will contain not just the module and managed dependencies, but also a native IoT Hub proxy module as well as a ```sample_gateway``` executable that you can pass the updated JSON configuration to.  

