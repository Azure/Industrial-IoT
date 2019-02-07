## Azure Industrial IoT Services

[![Build status](https://msazure.visualstudio.com/One/_apis/build/status/Custom/Azure_IOT/Industrial/Components/ci-azure-iiot-opc-vault-service)](https://msazure.visualstudio.com/One/_build/latest?definitionId=44197)

### OPC Unified Architecture (OPC UA) Certificate Management Service

An overview about the OPC Vault certificate microservice is [here](docs/opcvault-services-overview.md).

The certificate management service for OPC UA facilitates a CA certificate cloud service for OPC UA devices
based on Azure Key Vault and CosmosDB, a ASP.Net Core web application front end and a OPC UA GDS server based on .Net Standard.

The implementation follows the GDS Certificate Management Services as described in the OPC UA specification Part 12.

The CA certificates are stored in a HSM backed Azure Key Vault, which is also used to sign issued certificates. 

A web management application front end and a local OPC UA GDS server allow for easy connection to the services secured by Azure AD.

### This repository contains the following:

This repo contains all components required to run a CA in the Azure cloud for your OPC UA environment:

* **ASP.Net Core Certificate Management Microservice** to manage certificates with Azure Key Vault and CosmosDB.
* **ASP.Net Core Sample Application** as user interface for the Certificate Management Service.
* **OPC UA .Net Standard GDS Server** for local OPC UA device connectivity to the cloud Certificate Management Service.

A Powershell deployment script automatically builds and deploys the services to your subscription. By default, security is configured for a production system. 

### Certificate Management Microservice Features
- Production ready certificate microservice based on C# with ASP.Net Core 2.1.
- Uses Azure Key Vault as CA certificate store, key pair generator and certificate signer backed by FIPS 140-2 Level 2 validated HSMs.
- Uses Cosmos DB as application and certificate request database. Open database interface to integrate with other database services.
- Secured by AzureAD role based access with separation of Reader, Writer, Approver and Administrator roles.
- Exposes Rest API (with Swagger UI) to easily integrate certificate microservice in other cloud services.
- Support for RSA certificates with a SHA256 signature and keys with a length of 2048, 3072 or 4096 bits.
- Support to sign certificates created with new key pairs from Azure Key Vault or by using Certificate Signing Requests (CSR).
- Key Pairs and signed certificates with extensions follow requirements and guidelines as specified in the OPC UA GDS Certificate Management Services, Part 12.
- The CA has full CRL support with revocation of unregistered OPC UA applications.
- Uses on behalf tokens to access Azure Key Vault to validate user permissions at KeyVault level in addition to the validation at the microservice Rest API.
- Busines logic ensures secure workflow with assigned user roles and the validation of certificate requests against the application database.
- Follows Microsoft SDL guidelines for public-key infrastructure.
- Leverages OPC UA .NetStandard GDS Server Common libraries.
- Uses Azure Key Vault versioning and auditing to track CA certificate access and CRL history.

### Web Certificate Management Sample Features
- Sample code is based on the certificate management microservice Rest API using C# with ASP.Net Core 2.1.
- Workflow to secure a OPC UA application with a CA signed certificate: Register an OPC UA application, request a certificate or key pair, generate the signed certificate and download it.
- Secure workflow to unregister and revoke a OPC UA application including CRL updates.
- Forms to manage OPC UA applications and certificate requests.
- CA certificate management for the Administrator role to configure CA cert lifetime and subject name.
- Renewal of a CA certificates.
- Create key pairs and sign certificates with a CSR validated with application database information.
- Upload CSR for signing requests as file or base64 string.
- Binary and base64 download of certificates and keys as PFX, PEM and DER.
- Issues consolidated CRL updates for multiple unregistered applications in a single step.
- Accesses the microservice on behalf of the user to be able to execute protected functions in Azure Key Vault (e.g. signing rights for Approver).

### On premise Global Discovery Server (GDS) with cloud integration
- Based on the GDS server common library of the OPC UA .NetStandard SDK.
- Implements OPC UA Discovery and Certificate management services by connecting to the microservice.
- Executes in a docker container or as a .Net Core 2.0 application on Windows or Linux.
- Implements namespace of OPC UA GDS Discovery and Certificate Management Services V1.04, Part 12.
- **Note:** At this time the server can only act in a reader role with limited functionality due to the lack of user OAuth2 authentication support in the .NetStandard SDK. 
- For development purposes and testing, the AzureAD registration can be enabled for a 'Writer' role to allow to create certificate requests and to update applications, 
  but this configuration is not recommended for use in production deployments.

## [Overview](docs/opcvault-services-overview.md) on the OPC Vault microservice

An overview about the service is [here](docs/opcvault-services-overview.md).

## [Build and Deploy](docs/howto-deploy-services.md) the service to Azure

The documentation how to build and deploy the service is [here](docs/howto-deploy-services.md).

## [Manage certificates](docs/howto-use-cert-services.md) with the Web Sample Application

The documentation how to manage certificates with the Web sample application is [here](docs/howto-use-cert-services.md).

## [Secure](docs/howto-secureca-services.md) the Certificate service

Guidelines how to run a secure certificate service are [here](docs/howto-secureca-services.md).

<!---
## [Build and Run](docs/howto-run-services-locally.md) the services locally

The documentation how to build and run the service is [here](docs/howto-run-services-locally.md).
-->

# [Contributing](CONTRIBUTING.md)

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### Give Feedback

Please enter issues, bugs, or suggestions for any of the components and services as GitHub Issues [here](https://github.com/Azure/azure-iiot-opcvault-service/issues).

### Contribute

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct).  For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement) and follow the project 's [code submission guidelines](docs/contributing.md). A friendly bot will remind you about it when you submit a pull-request. ​ 

## License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](license.txt) License.  

[azure-free]:https://azure.microsoft.com/en-us/free/
[powershell-install]:https://azure.microsoft.com/en-us/downloads/#PowerShell
[run-with-docker-url]: https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-deploy-local#run-the-microservices-in-docker
[rm-arch-url]: https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-sample-walkthrough
[postman-url]: https://www.getpostman.com
[iotedge-url]: https://github.com/Azure/iotedge
[docker-url]: https://www.docker.com/
[dotnet-install]: https://www.microsoft.com/net/learn/get-started
[vs-install-url]: https://www.visualstudio.com/downloads
[dotnetcore-tools-url]: https://www.microsoft.com/net/core#windowsvs2017


