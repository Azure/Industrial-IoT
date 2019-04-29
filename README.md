# Azure Industrial IoT Services

The micro services contained in this repository provide a REST like API on top of our [Azure Industrial IoT components](https://github.com/Azure/azure-iiot-components). Services include:

- The **OPC Device Registry micro service** provides access to registered OPC UA applications and their endpoints.  Operators and administrators can register and unregister new OPC UA applications and browse the existing ones, including their endpoints.  

  In addition to application and endpoint management, the registry service also catalogues registered [OPC Twin IoT Edge modules](https://github.com/Azure/azure-iiot-opc-twin-module).  The service API gives you control of edge module functionality, e.g. starting or stopping Server discovery (Scanning services), or activating new 'Endpoint twins' that can be accessed using the OPC Twin micro service.

- The **OPC Twin micro service** facilitates communication with factory floor edge OPC UA server devices via a [OPC Twin IoT Edge module](https://github.com/Azure/azure-iiot-opc-twin-module) and exposes OPC UA services (Browse, Read, Write, and Execute) via its REST API.  

- The **OPC UA Device onboarding agent** is a worker role that processes OPC UA server discovery events sent by the [OPC Twin IoT Edge module](https://github.com/Azure/azure-iiot-opc-twin-module) when in discovery (or scan) mode.  The discovery events result in application registration and updates in the OPC UA device registry.

Learn more about how to deploy and use these services by checking out the components [documentation](https://github.com/Azure/azure-iiot-components).

### Give Feedback

Please enter issues, bugs, or suggestions for any of the components and services as GitHub Issues [here](https://github.com/Azure/azure-iiot-components/issues).

### Contribute

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct).  For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement) and follow the project 's [code submission guidelines](docs/contributing.md). A friendly bot will remind you about it when you submit a pull-request. ​ 

## License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](LICENSE) License.  
