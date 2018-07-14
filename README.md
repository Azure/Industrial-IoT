[Build Status](https://msazure.visualstudio.com/_apis/public/build/definitions/b32aa71e-8ed2-41b2-9d77-5bc261222004/33979/badge)

# OPC Twin Module

The OPC Twin module runs on the edge and contains the OPC Twin supervisor which manages OPC UA endpoint Twins.  
These endpoint twins translate OPC UA Json received from the Twin service into OPC UA binary messages which are 
sent over a stateful secure channel to the server endpoint.  The supervisor also provides discovery services 
which upload discovered devices to the OPC Twin Onboarding service for processing, enabling plug and play 
factory.

* [Contributing and Development setup](CONTRIBUTING.md)
* [Development setup, scripts and tools](DEVELOPMENT.md)

