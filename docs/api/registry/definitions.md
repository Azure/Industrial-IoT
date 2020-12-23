
<a name="definitions"></a>
## Definitions

<a name="applicationinfoapimodel"></a>
### ApplicationInfoApiModel
Application info model


|Name|Description|Schema|
|---|---|---|
|**applicationId**  <br>*optional*|Unique application id|string|
|**applicationName**  <br>*optional*|Default name of application|string|
|**applicationType**  <br>*optional*||[ApplicationType](definitions.md#applicationtype)|
|**applicationUri**  <br>*optional*|Unique application uri|string|
|**capabilities**  <br>*optional*|The capabilities advertised by the server.|< string > array|
|**created**  <br>*optional*||[RegistryOperationApiModel](definitions.md#registryoperationapimodel)|
|**discovererId**  <br>*optional*|Discoverer that registered the application|string|
|**discoveryProfileUri**  <br>*optional*|Discovery profile uri|string|
|**discoveryUrls**  <br>*optional*|Discovery urls of the server|< string > array|
|**gatewayServerUri**  <br>*optional*|Gateway server uri|string|
|**hostAddresses**  <br>*optional*|Host addresses of server application or null|< string > array|
|**locale**  <br>*optional*|Locale of default name - defaults to "en"|string|
|**localizedNames**  <br>*optional*|Localized Names of application keyed on locale|< string, string > map|
|**notSeenSince**  <br>*optional*|Last time application was seen|string (date-time)|
|**productUri**  <br>*optional*|Product uri|string|
|**siteId**  <br>*optional*|Site of the application  <br>**Example** : `"productionlineA"`|string|
|**updated**  <br>*optional*||[RegistryOperationApiModel](definitions.md#registryoperationapimodel)|


<a name="applicationinfolistapimodel"></a>
### ApplicationInfoListApiModel
List of registered applications


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Application infos|< [ApplicationInfoApiModel](definitions.md#applicationinfoapimodel) > array|


<a name="applicationregistrationapimodel"></a>
### ApplicationRegistrationApiModel
Application with optional list of endpoints


|Name|Description|Schema|
|---|---|---|
|**application**  <br>*required*||[ApplicationInfoApiModel](definitions.md#applicationinfoapimodel)|
|**endpoints**  <br>*optional*|List of endpoint twins|< [EndpointRegistrationApiModel](definitions.md#endpointregistrationapimodel) > array|


<a name="applicationregistrationqueryapimodel"></a>
### ApplicationRegistrationQueryApiModel
Application information


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Name of application|string|
|**applicationType**  <br>*optional*||[ApplicationType](definitions.md#applicationtype)|
|**applicationUri**  <br>*optional*|Application uri|string|
|**capability**  <br>*optional*|Application capability to query with|string|
|**discovererId**  <br>*optional*|Discoverer id to filter with|string|
|**discoveryProfileUri**  <br>*optional*|Discovery profile uri|string|
|**gatewayServerUri**  <br>*optional*|Gateway server uri|string|
|**includeNotSeenSince**  <br>*optional*|Whether to include apps that were soft deleted|boolean|
|**locale**  <br>*optional*|Locale of application name - default is "en"|string|
|**productUri**  <br>*optional*|Product uri|string|
|**siteOrGatewayId**  <br>*optional*|Supervisor or site the application belongs to.|string|


<a name="applicationregistrationrequestapimodel"></a>
### ApplicationRegistrationRequestApiModel
Application information


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Default name of the server or client.|string|
|**applicationType**  <br>*optional*||[ApplicationType](definitions.md#applicationtype)|
|**applicationUri**  <br>*required*|Unique application uri|string|
|**capabilities**  <br>*optional*|The OPC UA defined capabilities of the server.|< string > array|
|**discoveryProfileUri**  <br>*optional*|The discovery profile uri of the server.|string|
|**discoveryUrls**  <br>*optional*|Discovery urls of the server.|< string > array|
|**gatewayServerUri**  <br>*optional*|Gateway server uri|string|
|**locale**  <br>*optional*|Locale of default name|string|
|**localizedNames**  <br>*optional*|Localized names key off locale id.|< string, string > map|
|**productUri**  <br>*optional*|Product uri of the application.  <br>**Example** : `"http://contoso.com/fridge/1.0"`|string|
|**siteId**  <br>*optional*|Site of the application|string|


<a name="applicationregistrationresponseapimodel"></a>
### ApplicationRegistrationResponseApiModel
Result of an application registration


|Name|Description|Schema|
|---|---|---|
|**id**  <br>*optional*|New id application was registered under|string|


<a name="applicationregistrationupdateapimodel"></a>
### ApplicationRegistrationUpdateApiModel
Application registration update request


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Default name of the server or client.|string|
|**capabilities**  <br>*optional*|Capabilities of the application|< string > array|
|**discoveryProfileUri**  <br>*optional*|Discovery profile uri|string|
|**discoveryUrls**  <br>*optional*|Discovery urls of the application|< string > array|
|**gatewayServerUri**  <br>*optional*|Gateway server uri|string|
|**locale**  <br>*optional*|Locale of default name - defaults to "en"|string|
|**localizedNames**  <br>*optional*|Localized names keyed off locale id.<br>To remove entry, set value for locale id to null.|< string, string > map|
|**productUri**  <br>*optional*|Product uri|string|


<a name="applicationsitelistapimodel"></a>
### ApplicationSiteListApiModel
List of application sites


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**sites**  <br>*optional*|Sites|< string > array|


<a name="applicationtype"></a>
### ApplicationType
Application type

*Type* : enum (Server, Client, ClientAndServer, DiscoveryServer)


<a name="authenticationmethodapimodel"></a>
### AuthenticationMethodApiModel
Authentication Method model


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*|Method specific configuration|string|
|**credentialType**  <br>*optional*||[CredentialType](definitions.md#credentialtype)|
|**id**  <br>*required*|Method id|string|
|**securityPolicy**  <br>*optional*|Security policy to use when passing credential.|string|


<a name="credentialtype"></a>
### CredentialType
Type of credentials to use for authentication

*Type* : enum (None, UserName, X509Certificate, JwtToken)


<a name="discovererapimodel"></a>
### DiscovererApiModel
Discoverer registration model


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Whether discoverer is connected on this registration|boolean|
|**discovery**  <br>*optional*||[DiscoveryMode](definitions.md#discoverymode)|
|**discoveryConfig**  <br>*optional*||[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**id**  <br>*required*|Discoverer id|string|
|**logLevel**  <br>*optional*||[TraceLogLevel](definitions.md#traceloglevel)|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync between<br>client (module) and server (service) (default: false).|boolean|
|**requestedConfig**  <br>*optional*||[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**requestedMode**  <br>*optional*||[DiscoveryMode](definitions.md#discoverymode)|
|**siteId**  <br>*optional*|Site of the discoverer|string|


<a name="discovererlistapimodel"></a>
### DiscovererListApiModel
Discoverer registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Registrations|< [DiscovererApiModel](definitions.md#discovererapimodel) > array|


<a name="discovererqueryapimodel"></a>
### DiscovererQueryApiModel
Discoverer registration query


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**discovery**  <br>*optional*||[DiscoveryMode](definitions.md#discoverymode)|
|**siteId**  <br>*optional*|Site of the discoverer|string|


<a name="discovererupdateapimodel"></a>
### DiscovererUpdateApiModel
Discoverer update request


|Name|Description|Schema|
|---|---|---|
|**discovery**  <br>*optional*||[DiscoveryMode](definitions.md#discoverymode)|
|**discoveryConfig**  <br>*optional*||[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**logLevel**  <br>*optional*||[TraceLogLevel](definitions.md#traceloglevel)|
|**siteId**  <br>*optional*|Site the discoverer is part of|string|


<a name="discoveryconfigapimodel"></a>
### DiscoveryConfigApiModel
Discovery configuration api model


|Name|Description|Schema|
|---|---|---|
|**activationFilter**  <br>*optional*||[EndpointActivationFilterApiModel](definitions.md#endpointactivationfilterapimodel)|
|**addressRangesToScan**  <br>*optional*|Address ranges to scan (null == all wired nics)|string|
|**discoveryUrls**  <br>*optional*|List of preset discovery urls to use|< string > array|
|**idleTimeBetweenScans**  <br>*optional*|Delay time between discovery sweeps|string (date-span)|
|**locales**  <br>*optional*|List of locales to filter with during discovery|< string > array|
|**maxNetworkProbes**  <br>*optional*|Max network probes that should ever run.|integer (int32)|
|**maxPortProbes**  <br>*optional*|Max port probes that should ever run.|integer (int32)|
|**minPortProbesPercent**  <br>*optional*|Probes that must always be there as percent of max.|integer (int32)|
|**networkProbeTimeout**  <br>*optional*|Network probe timeout|string (date-span)|
|**portProbeTimeout**  <br>*optional*|Port probe timeout|string (date-span)|
|**portRangesToScan**  <br>*optional*|Port ranges to scan (null == all unassigned)|string|


<a name="discoverymode"></a>
### DiscoveryMode
Discovery mode to use

*Type* : enum (Off, Local, Network, Fast, Scan)


<a name="discoveryrequestapimodel"></a>
### DiscoveryRequestApiModel
Discovery request


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*||[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**discovery**  <br>*optional*||[DiscoveryMode](definitions.md#discoverymode)|
|**id**  <br>*optional*|Id of discovery request|string|


<a name="endpointactivationfilterapimodel"></a>
### EndpointActivationFilterApiModel
Endpoint Activation Filter model


|Name|Description|Schema|
|---|---|---|
|**securityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**securityPolicies**  <br>*optional*|Endpoint security policies to filter against.<br>If set to null, all policies are in scope.|< string > array|
|**trustLists**  <br>*optional*|Certificate trust list identifiers to use for<br>activation, if null, all certificates are<br>trusted.  If empty list, no certificates are<br>trusted which is equal to no filter.|< string > array|


<a name="endpointactivationstate"></a>
### EndpointActivationState
Activation state of the endpoint twin

*Type* : enum (Deactivated, Activated, ActivatedAndConnected)


<a name="endpointactivationstatusapimodel"></a>
### EndpointActivationStatusApiModel
Endpoint Activation status model


|Name|Description|Schema|
|---|---|---|
|**activationState**  <br>*optional*||[EndpointActivationState](definitions.md#endpointactivationstate)|
|**id**  <br>*required*|Identifier of the endoint|string|


<a name="endpointapimodel"></a>
### EndpointApiModel
Endpoint model


|Name|Description|Schema|
|---|---|---|
|**alternativeUrls**  <br>*optional*|Alternative endpoint urls that can be used for<br>accessing and validating the server|< string > array|
|**certificate**  <br>*optional*|Endpoint certificate thumbprint|string|
|**securityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**securityPolicy**  <br>*optional*|Security policy uri to use for communication.<br>default to best.|string|
|**url**  <br>*optional*|Endpoint url to use to connect with|string|


<a name="endpointconnectivitystate"></a>
### EndpointConnectivityState
State of the endpoint after activation

*Type* : enum (Connecting, NotReachable, Busy, NoTrust, CertificateInvalid, Ready, Error, Disconnected, Unauthorized)


<a name="endpointinfoapimodel"></a>
### EndpointInfoApiModel
Endpoint registration model


|Name|Description|Schema|
|---|---|---|
|**activationState**  <br>*optional*||[EndpointActivationState](definitions.md#endpointactivationstate)|
|**applicationId**  <br>*required*|Application id endpoint is registered under.|string|
|**endpointState**  <br>*optional*||[EndpointConnectivityState](definitions.md#endpointconnectivitystate)|
|**notSeenSince**  <br>*optional*|Last time endpoint was seen|string (date-time)|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync|boolean|
|**registration**  <br>*required*||[EndpointRegistrationApiModel](definitions.md#endpointregistrationapimodel)|


<a name="endpointinfolistapimodel"></a>
### EndpointInfoListApiModel
Endpoint registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Endpoint registrations|< [EndpointInfoApiModel](definitions.md#endpointinfoapimodel) > array|


<a name="endpointregistrationapimodel"></a>
### EndpointRegistrationApiModel
Endpoint registration model


|Name|Description|Schema|
|---|---|---|
|**authenticationMethods**  <br>*optional*|Supported authentication methods that can be selected to<br>obtain a credential and used to interact with the endpoint.|< [AuthenticationMethodApiModel](definitions.md#authenticationmethodapimodel) > array|
|**discovererId**  <br>*optional*|Discoverer that registered the endpoint|string|
|**endpoint**  <br>*required*||[EndpointApiModel](definitions.md#endpointapimodel)|
|**endpointUrl**  <br>*optional*|Original endpoint url of the endpoint|string|
|**id**  <br>*required*|Registered identifier of the endpoint|string|
|**securityLevel**  <br>*optional*|Security level of the endpoint|integer (int32)|
|**siteId**  <br>*optional*|Registered site of the endpoint|string|
|**supervisorId**  <br>*optional*|Supervisor that manages the endpoint.|string|


<a name="endpointregistrationqueryapimodel"></a>
### EndpointRegistrationQueryApiModel
Endpoint query


|Name|Description|Schema|
|---|---|---|
|**activated**  <br>*optional*|Whether the endpoint was activated|boolean|
|**applicationId**  <br>*optional*|Application id to filter|string|
|**certificate**  <br>*optional*|Endpoint certificate thumbprint|string|
|**connected**  <br>*optional*|Whether the endpoint is connected on supervisor.|boolean|
|**discovererId**  <br>*optional*|Discoverer id to filter with|string|
|**endpointState**  <br>*optional*||[EndpointConnectivityState](definitions.md#endpointconnectivitystate)|
|**includeNotSeenSince**  <br>*optional*|Whether to include endpoints that were soft deleted|boolean|
|**securityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**securityPolicy**  <br>*optional*|Security policy uri|string|
|**siteOrGatewayId**  <br>*optional*|Site or gateway id to filter with|string|
|**supervisorId**  <br>*optional*|Supervisor id to filter with|string|
|**url**  <br>*optional*|Endoint url for direct server access|string|


<a name="gatewayapimodel"></a>
### GatewayApiModel
Gateway registration model


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Whether Gateway is connected on this registration|boolean|
|**id**  <br>*required*|Gateway id|string|
|**siteId**  <br>*optional*|Site of the Gateway|string|


<a name="gatewayinfoapimodel"></a>
### GatewayInfoApiModel
Gateway info model


|Name|Schema|
|---|---|
|**gateway**  <br>*required*|[GatewayApiModel](definitions.md#gatewayapimodel)|
|**modules**  <br>*optional*|[GatewayModulesApiModel](definitions.md#gatewaymodulesapimodel)|


<a name="gatewaylistapimodel"></a>
### GatewayListApiModel
Gateway registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Registrations|< [GatewayApiModel](definitions.md#gatewayapimodel) > array|


<a name="gatewaymodulesapimodel"></a>
### GatewayModulesApiModel
Gateway modules model


|Name|Schema|
|---|---|
|**discoverer**  <br>*optional*|[DiscovererApiModel](definitions.md#discovererapimodel)|
|**publisher**  <br>*optional*|[PublisherApiModel](definitions.md#publisherapimodel)|
|**supervisor**  <br>*optional*|[SupervisorApiModel](definitions.md#supervisorapimodel)|


<a name="gatewayqueryapimodel"></a>
### GatewayQueryApiModel
Gateway registration query


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**siteId**  <br>*optional*|Site of the Gateway|string|


<a name="gatewayupdateapimodel"></a>
### GatewayUpdateApiModel
Gateway registration update request


|Name|Description|Schema|
|---|---|---|
|**siteId**  <br>*optional*|Site of the Gateway|string|


<a name="publisherapimodel"></a>
### PublisherApiModel
Publisher registration model


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*||[PublisherConfigApiModel](definitions.md#publisherconfigapimodel)|
|**connected**  <br>*optional*|Whether publisher is connected on this registration|boolean|
|**id**  <br>*required*|Publisher id|string|
|**logLevel**  <br>*optional*||[TraceLogLevel](definitions.md#traceloglevel)|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync between<br>client (module) and server (service) (default: false).|boolean|
|**siteId**  <br>*optional*|Site of the publisher|string|


<a name="publisherconfigapimodel"></a>
### PublisherConfigApiModel
Default publisher agent configuration


|Name|Description|Schema|
|---|---|---|
|**capabilities**  <br>*optional*|Capabilities|< string, string > map|
|**heartbeatInterval**  <br>*optional*|Heartbeat interval|string (date-span)|
|**jobCheckInterval**  <br>*optional*|Interval to check job|string (date-span)|
|**jobOrchestratorUrl**  <br>*optional*|Job orchestrator endpoint url|string|
|**maxWorkers**  <br>*optional*|Parallel jobs|integer (int32)|


<a name="publisherlistapimodel"></a>
### PublisherListApiModel
Publisher registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Registrations|< [PublisherApiModel](definitions.md#publisherapimodel) > array|


<a name="publisherqueryapimodel"></a>
### PublisherQueryApiModel
Publisher registration query


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**siteId**  <br>*optional*|Site for the publishers|string|


<a name="publisherupdateapimodel"></a>
### PublisherUpdateApiModel
Publisher registration update request


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*||[PublisherConfigApiModel](definitions.md#publisherconfigapimodel)|
|**logLevel**  <br>*optional*||[TraceLogLevel](definitions.md#traceloglevel)|
|**siteId**  <br>*optional*|Site of the publisher|string|


<a name="registryoperationapimodel"></a>
### RegistryOperationApiModel
Registry operation log model


|Name|Description|Schema|
|---|---|---|
|**authorityId**  <br>*required*|Operation User|string|
|**time**  <br>*required*|Operation time|string (date-time)|


<a name="securitymode"></a>
### SecurityMode
Security mode of endpoint

*Type* : enum (Best, Sign, SignAndEncrypt, None)


<a name="serverregistrationrequestapimodel"></a>
### ServerRegistrationRequestApiModel
Server registration request


|Name|Description|Schema|
|---|---|---|
|**activationFilter**  <br>*optional*||[EndpointActivationFilterApiModel](definitions.md#endpointactivationfilterapimodel)|
|**discoveryUrl**  <br>*required*|Discovery url to use for registration|string|
|**id**  <br>*optional*|Registration id|string|


<a name="supervisorapimodel"></a>
### SupervisorApiModel
Supervisor registration model


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Whether supervisor is connected on this registration|boolean|
|**id**  <br>*required*|Supervisor id|string|
|**logLevel**  <br>*optional*||[TraceLogLevel](definitions.md#traceloglevel)|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync between<br>client (module) and server (service) (default: false).|boolean|
|**siteId**  <br>*optional*|Site of the supervisor|string|


<a name="supervisorlistapimodel"></a>
### SupervisorListApiModel
Supervisor registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Registrations|< [SupervisorApiModel](definitions.md#supervisorapimodel) > array|


<a name="supervisorqueryapimodel"></a>
### SupervisorQueryApiModel
Supervisor registration query


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Included connected or disconnected|boolean|
|**siteId**  <br>*optional*|Site for the supervisors|string|


<a name="supervisorstatusapimodel"></a>
### SupervisorStatusApiModel
Supervisor runtime status


|Name|Description|Schema|
|---|---|---|
|**deviceId**  <br>*required*|Edge device id|string|
|**endpoints**  <br>*optional*|Endpoint activation status|< [EndpointActivationStatusApiModel](definitions.md#endpointactivationstatusapimodel) > array|
|**moduleId**  <br>*optional*|Module id|string|
|**siteId**  <br>*optional*|Site id|string|


<a name="supervisorupdateapimodel"></a>
### SupervisorUpdateApiModel
Supervisor update request


|Name|Description|Schema|
|---|---|---|
|**logLevel**  <br>*optional*||[TraceLogLevel](definitions.md#traceloglevel)|
|**siteId**  <br>*optional*|Site the supervisor is part of|string|


<a name="traceloglevel"></a>
### TraceLogLevel
Log level

*Type* : enum (Error, Information, Debug, Verbose)


<a name="x509certificateapimodel"></a>
### X509CertificateApiModel
Certificate model


|Name|Description|Schema|
|---|---|---|
|**certificate**  <br>*optional*|Raw data  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**notAfterUtc**  <br>*optional*|Not after validity|string (date-time)|
|**notBeforeUtc**  <br>*optional*|Not before validity|string (date-time)|
|**selfSigned**  <br>*optional*|Self signed|boolean|
|**serialNumber**  <br>*optional*|Serial number|string|
|**subject**  <br>*optional*|Subject|string|
|**thumbprint**  <br>*optional*|Thumbprint|string|


<a name="x509certificatechainapimodel"></a>
### X509CertificateChainApiModel
Certificate chain


|Name|Description|Schema|
|---|---|---|
|**chain**  <br>*optional*|Chain|< [X509CertificateApiModel](definitions.md#x509certificateapimodel) > array|
|**status**  <br>*optional*|Chain validation status if validated|enum (NoError, NotTimeValid, Revoked, NotSignatureValid, NotValidForUsage, UntrustedRoot, RevocationStatusUnknown, Cyclic, InvalidExtension, InvalidPolicyConstraints, InvalidBasicConstraints, InvalidNameConstraints, HasNotSupportedNameConstraint, HasNotDefinedNameConstraint, HasNotPermittedNameConstraint, HasExcludedNameConstraint, PartialChain, CtlNotTimeValid, CtlNotSignatureValid, CtlNotValidForUsage, HasWeakSignature, OfflineRevocation, NoIssuanceChainPolicy, ExplicitDistrust, HasNotSupportedCriticalExtension)|


<a name="x509chainstatus"></a>
### X509ChainStatus
Status of x509 chain

*Type* : enum (NoError, NotTimeValid, Revoked, NotSignatureValid, NotValidForUsage, UntrustedRoot, RevocationStatusUnknown, Cyclic, InvalidExtension, InvalidPolicyConstraints, InvalidBasicConstraints, InvalidNameConstraints, HasNotSupportedNameConstraint, HasNotDefinedNameConstraint, HasNotPermittedNameConstraint, HasExcludedNameConstraint, PartialChain, CtlNotTimeValid, CtlNotSignatureValid, CtlNotValidForUsage, HasWeakSignature, OfflineRevocation, NoIssuanceChainPolicy, ExplicitDistrust, HasNotSupportedCriticalExtension)



