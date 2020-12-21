# Release announcement
 
## Azure Industrial IoT Platform 2.7.20x 
 
The Azure Industrial IoT Platform is a Microsoft suite of modules and services that make it easier to connect your industrial assets to Azure. The Azure Industrial IoT Platform, consisting of edge and cloud services, allows you to discover industrial assets on-site and automatically register them in the cloud for easy access to your plant floor telemetry wherever you happen to be in the world.  We have worked to establish a large and vibrant partner network in order to support all types of industrial interfaces through the use of adapters, fully integrated with our platform – while natively supporting OPC UA by default. 
 
We are pleased to announce that we are releasing the next update, 2.7.20x of the Azure Industrial IoT Platform. We are planning to release it on January 12th. 
 
One of the changes that will be delivered with the update are the removal of Bouncy Castle, a collection of APIs used in cryptography. Based on the new system.formats. asn1 library released with .NET Core 5 many ASN.1 encoding and decoding operations have been reimplemented to be able to retire the dependency on the external crypto library, Bouncy Castle.  
 
The Bouncy Castle dependency retirement also leads to the removal of the OPC Vault microservice. It provided an API over a Certificate Authority to manage and fulfill certificate requests and provision OPC UA servers with the latest certificates. However, the OPC Vault microservice v2 had a dependency on Bouncy Castle, which is one of the reasons it’s no longer part of the Industrial IoT Platform offering. The Industrial IoT Platform will continue to be available on GitHub. 
 
## Coming up: 
The release of version 2.8 will follow in spring 2021 containing the removal of experimental code, and numerous bug fixes. The experimental code was removed to simplify the user experience. That will allow the focus to remain on areas that are highly needed and requested. A 3.0 release with a major update of the OPC Components will follow in summer 2021. It will include a reduction of surface area and long-term support. Backwards compatibility with 2.5 and 2.7 will be enabled and the support continued until 3.0 is in General Availability. Additionally, highly requested features, like alarms and events, that have not been part of the Industrial IoT Platform will be added to the offering.  
 
Please add your feature requests to the Industrial IoT GitHub repository. Our goal is to focus on our customer’s needs. Therefore, we appreciate any feedback and input from you to plan our roadmap accordingly.  
