[![Build][build-badge]][build-url]
[![Issues][issues-badge]][issues-url]
[![Gitter][gitter-badge]][gitter-url]

OPC UA Explorer
=====================

Handles communication with shop floor via OPC UA.

Overview
========

* WebService.csproj - C# web service exposing REST interface for IoT Hub
  management functionality
* WebService.Test.csproj - Unit tests for web services functionality
* Services.csproj - C# assembly containining business logic for interacting
  with Azure services (IoTHub, etc.) and OPC UA devices.
* Services.Test.csproj - Unit tests for services functionality
* Solution/scripts - contains build scripts, docker container creation
  scripts, and scripts for running the microservice from the command line

How to use it
=============

Running locally on your development machine:

1. Set your PCS_IOTHUB_CONNSTRING system environment variable for your
   IoT Hub connection, and PCS_IOTHUBMANAGER_WEBSERVICE_URL for the URL of the
   iot hub manager service.
2. Start an OPC UA server, e.g. the UA Sample server from here:  (<todo>)
3. Start the proxy and publisher edge modules (see here <todo> for how)
4. Set WebService.csproj as your startup project and run F5 from VS.
    * Alternatively you can start the service using "dotnet webservice.dll""
5. Hit the REST api for the web service using:
	* http://127.0.0.1:9042/v1/status (checks status of the web service)
	* http://127.0.0.1:9042/v1/swagger (for swagger UI, which acts as API help
      and allows you to interact with the service)

Running locally in a container:

1. <todo - container instructions>

Running on Azure in a container in ACS:

1. <todo - cloud environment container instructions>

Running Unit Tests:

1. There are two test projects:
   1. Services.Test - this contains tests for the Services project which
      interacts with Azure services through the Azure SDKs, e.g. the IoT Hub,
	  and
   2. WebService.Test - this contains tests for the WebService project which
      contains the webservices APIs (note these tests are also dependent on
	  Services code).
2. Open the desired test project, e.g. WebService.Test
3. Open the controller test file, e.g. DevicesControllerTest.cs
4. Right click on a test and run it, e.g. Right click on TestAllDevices and
   select Run Intellitest from the context menu.

Configuration
=============

1. webservice\hosting.json allows configuring the webservice hosting environment,
   including urls the service should listen on (See Asp.net core hosting.json).
2. webservice\appsettings.json allows configuring the microservice application, 
   including IoT Hub connection string. 
   The file references the environment variable below:
       1. PCS_IOTHUB_CONNSTRING is a system environment variable and should contain
          your IoT Hub connection string. Create this environment variable before
          running the microservice.
       2. PCS_IOTHUBMANAGER_WEBSERVICE_URL points to the IoTHubManager micro service
          endpoint.  This setting must be set for production use. If it is not set, 
          the service will inject a development-only implementation of the IoT Hub 
          twin services interface that interacts with IoT Hub directly.  This means
          for development purposes, the IoTHubManager micro service is not required.
       3. Also for development purposes, you can set the "NoOpcEdgeProxy" setting to 
          true, which will bypass the proxy and enable your service to talk to your 
          OPC UA server on the same machine.  
       4. <todo - logging/monitoring>
       5. <todo - auth>

Other documents
===============

* [Contributing and Development setup](CONTRIBUTING.md)
* [Development setup, scripts and tools](DEVELOPMENT.md)

[build-badge]: https://img.shields.io/travis/Azure/iot-pcs-cf-opc-ua-explorer.svg
[build-url]: https://travis-ci.org/Azure/iot-pcs-cf-opc-ua-explorer
[issues-badge]: https://img.shields.io/github/issues/azure/iot-pcs-cf-opc-ua-explorer.svg
[issues-url]: https://github.com/Azure/iot-pcs-cf-opc-ua-explorer/issues
[gitter-badge]: https://img.shields.io/gitter/room/azure/iot-solutions.js.svg
[gitter-url]: https://gitter.im/azure/iot-solutions
