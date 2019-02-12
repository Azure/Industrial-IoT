# Azure Industrial IoT Services

## OPC Unified Architecture (OPC UA) Certificate Management Service

The certificate management service for OPC UA includes the **OPC Vault Microservice** to implement the CA certificate cloud service, a ASP.Net Core **Sample Certificate Management Web Application** front end and a **OPC Vault Edge Module** to implement a OPC UA GDS server for local connectivity.

#### A detailed overview of the OPC UA Certificate Management Service is [here](docs/opcvault-services-overview.md).

### This repository contains the following:

This repo contains all components required to run a CA in the Azure cloud for your OPC UA environment:

* ASP.Net Core **OPC Vault Microservice** to manage certificates with [Azure Key Vault][azure-keyvault] and [Azure Cosmos DB][azure-cosmosdb].
* ASP.Net Core **Sample Certificate Management Web Application** as user interface for the OPC Vault microservice.
* OPC UA .Net Standard **OPC Vault Edge Module**  as GDS server for local OPC UA device connectivity to the OPC Vault microservice.

A Powershell deployment script automatically builds and deploys the services to your subscription. By default, security is configured for a production system. 

[![Build status](https://msazure.visualstudio.com/One/_apis/build/status/Custom/Azure_IOT/Industrial/Components/ci-azure-iiot-opc-vault-service)](https://msazure.visualstudio.com/One/_build/latest?definitionId=44197)

### OPC Vault Microservice Features
- Production ready certificate microservice based on C# with ASP.Net Core 2.1.
- Uses Azure Key Vault as CA certificate store, key pair generator and certificate signer backed by FIPS 140-2 Level 2 validated HSMs.
- Uses Cosmos DB as application and certificate request database. Open database interface to integrate with other database services.
- Secured by AzureAD role based access with separation of Reader, Writer, Approver and Administrator roles.
- Exposes Rest API (with Swagger UI, development deployment only) to easily integrate OPC Vault microservice with other cloud services.
- Support for RSA certificates with a SHA256 signature and keys with a length of 2048, 3072 or 4096 bits.
- Support to sign application certificates created with new key pairs from Azure Key Vault or by using Certificate Signing Requests (CSR).
- Key Pairs and signed certificates with extensions follow requirements and guidelines as specified in the *OPC UA Architecture Specification Part 12: Discovery and Global Services Release V1.04* for OPC UA devices.
- The CA has full CRL support with revocation of unregistered OPC UA applications.
- Uses on behalf tokens to access Azure Key Vault to validate user permissions at Key Vault access level in addition to the validation at the microservice Rest API.
- Business logic ensures secure workflow with assigned user roles and the validation of certificate requests against the application database.
- Follows Microsoft SDL guidelines for public-key infrastructure.
- Leverages open source [OPC UA .NetStandard][opc-netstandard] GDS Server Common libraries.
- Uses Azure Key Vault versioning and auditing to track CA certificate access and CRL history.

### Web Certificate Management Sample Features
- Sample code is based on the OPC Vault microservice Rest API using C# with ASP.Net Core 2.1.
- Workflow to secure a OPC UA application with a CA signed certificate: Register an OPC UA application, request a certificate or key pair, generate the signed certificate and download it.
- Secure workflow to unregister and revoke a OPC UA application including CRL updates.
- Forms to manage OPC UA applications, certificate requests and certificate groups.
- CA certificate management for the Administrator role to configure CA cert lifetime and subject name.
- Renewal of a CA certificates.
- Create key pairs and sign certificates with a CSR validated with application database information.
- Upload CSR for signing requests as file or base64 PEM format.
- Binary and base64 download of certificates and keys as PFX, PEM and DER.
- Issues consolidated CRL updates for multiple unregistered applications in a single step, e.g. for weekly updates.
- Accesses the OPC Vault microservice on behalf of the user to be able to execute protected functions in Azure Key Vault (e.g. signing rights for Approver).

### On premise OPC Vault Edge Module as OPC UA Global Discovery Server (GDS) with cloud integration
- Based on the GDS server common library of the [OPC UA .NetStandard][opc-netstandard] Nuget packages.
- Implements the OPC UA Discovery and Certificate Management profile by connecting to the OPC Vault microservice.
- Executes in a docker container or as a .Net Core 2.0 application on Windows or Linux.
- Implements GDS namespace as specified in the *OPC UA Specification Part 12:  Discovery and Global Services V1.04*.

  **Known limitations:** At this time the GDS can only act in a reader role with limited functionality due to the lack of user OAuth2 authentication support in the OPC UA .NetStandard SDK. For development purposes and testing, the Azure AD registration can be enabled for a 'Writer' role to allow to create certificate requests and to update applications, 
  but this configuration is not recommended for use in production deployments.

## Documentation

### [OPC UA Certificate Management Service Overview](docs/opcvault-services-overview.md) 

### [How to Build and Deploy the service to Azure](docs/howto-deploy-services.md) 

### [How to Manage certificates with the Web Sample Application](docs/howto-use-cert-services.md)

### [How to run a Secure Certificate Service](docs/howto-secureca-services.md)

<!---

### [How to Build, Run and Debug the services locally](docs/howto-run-services-locally.md) 

-->

## [Contributing](CONTRIBUTING.md)

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

If you want/plan to contribute, we ask you to sign a [CLA](https://cla.microsoft.com/) (Contribution License Agreement) and follow the project 's [code submission guidelines](contributing.md). A friendly bot will remind you about it when you submit a pull-request. ​ 

## License

Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the [MIT](license.txt) License.  

[azure-free]:https://azure.microsoft.com/en-us/free/
[azure-keyvault]:https://azure.microsoft.com/services/key-vault/
[opc-netstandard]:https://github.com/OPCFoundation/UA-.NETStandard
[azure-cosmosdb]:https://azure.microsoft.com/services/cosmos-db/
[powershell-install]:https://azure.microsoft.com/en-us/downloads/#PowerShell
[docker-url]: https://www.docker.com/
[dotnet-install]: https://www.microsoft.com/net/learn/get-started
[vs-install-url]: https://www.visualstudio.com/downloads
[dotnetcore-tools-url]: https://www.microsoft.com/net/core#windowsvs2017


