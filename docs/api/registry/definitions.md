
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
|**certificate**  <br>*optional*|Application public cert  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
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


<a name="applicationrecordapimodel"></a>
### ApplicationRecordApiModel
Application with optional list of endpoints


|Name|Description|Schema|
|---|---|---|
|**application**  <br>*required*||[ApplicationInfoApiModel](definitions.md#applicationinfoapimodel)|
|**recordId**  <br>*required*|Record id|integer (int32)|


<a name="applicationrecordlistapimodel"></a>
### ApplicationRecordListApiModel
Create response


|Name|Description|Schema|
|---|---|---|
|**applications**  <br>*optional*|Applications found|< [ApplicationRecordApiModel](definitions.md#applicationrecordapimodel) > array|
|**lastCounterResetTime**  <br>*required*|Last counter reset|string (date-time)|
|**nextRecordId**  <br>*required*|Next record id|integer (int32)|


<a name="applicationrecordqueryapimodel"></a>
### ApplicationRecordQueryApiModel
Query by id


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Application name|string|
|**applicationType**  <br>*optional*||[ApplicationTypeNullable](definitions.md#applicationtypenullable)|
|**applicationUri**  <br>*optional*|Application uri|string|
|**maxRecordsToReturn**  <br>*optional*|Max records to return|integer (int32)|
|**productUri**  <br>*optional*|Product uri|string|
|**serverCapabilities**  <br>*optional*|Server capabilities|< string > array|
|**startingRecordId**  <br>*optional*|Starting record id|integer (int32)|


<a name="applicationregistrationapimodel"></a>
### ApplicationRegistrationApiModel
Application with list of endpoints


|Name|Description|Schema|
|---|---|---|
|**application**  <br>*required*||[ApplicationInfoApiModel](definitions.md#applicationinfoapimodel)|
|**endpoints**  <br>*optional*|List of endpoint twins|< [EndpointRegistrationApiModel](definitions.md#endpointregistrationapimodel) > array|
|**securityAssessment**  <br>*optional*||[SecurityAssessmentNullable](definitions.md#securityassessmentnullable)|


<a name="applicationregistrationqueryapimodel"></a>
### ApplicationRegistrationQueryApiModel
Application information


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Name of application|string|
|**applicationType**  <br>*optional*||[ApplicationTypeNullable](definitions.md#applicationtypenullable)|
|**applicationUri**  <br>*optional*|Application uri|string|
|**capability**  <br>*optional*|Application capability to query with|string|
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
|**applicationType**  <br>*optional*||[ApplicationTypeNullable](definitions.md#applicationtypenullable)|
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
|**certificate**  <br>*optional*|Application public cert  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
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
|**sites**  <br>*optional*|Distinct list of sites applications were registered in.|< string > array|


<a name="applicationtype"></a>
### ApplicationType
*Type* : enum (Server, Client, ClientAndServer, DiscoveryServer)


<a name="applicationtypenullable"></a>
### ApplicationTypeNullable
*Type* : enum (Server, Client, ClientAndServer, DiscoveryServer)


<a name="authenticationmethodapimodel"></a>
### AuthenticationMethodApiModel
Authentication Method model


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*|Method specific configuration|object|
|**credentialType**  <br>*optional*||[CredentialTypeNullable](definitions.md#credentialtypenullable)|
|**id**  <br>*required*|Authentication method id|string|
|**securityPolicy**  <br>*optional*|Security policy to use when passing credential.|string|


<a name="credentialtypenullable"></a>
### CredentialTypeNullable
*Type* : enum (None, UserName, X509Certificate, JwtToken)


<a name="discovererapimodel"></a>
### DiscovererApiModel
Discoverer registration model


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Whether discoverer is connected on this registration|boolean|
|**discovery**  <br>*optional*||[DiscoveryModeNullable](definitions.md#discoverymodenullable)|
|**discoveryConfig**  <br>*optional*||[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**id**  <br>*required*|Discoverer id|string|
|**logLevel**  <br>*optional*||[TraceLogLevelNullable](definitions.md#traceloglevelnullable)|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync between<br>client (module) and server (service) (default: false).|boolean|
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
|**discovery**  <br>*optional*||[DiscoveryModeNullable](definitions.md#discoverymodenullable)|
|**siteId**  <br>*optional*|Site of the discoverer|string|


<a name="discovererupdateapimodel"></a>
### DiscovererUpdateApiModel
Discoverer update request


|Name|Description|Schema|
|---|---|---|
|**discovery**  <br>*optional*||[DiscoveryModeNullable](definitions.md#discoverymodenullable)|
|**discoveryConfig**  <br>*optional*||[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**logLevel**  <br>*optional*||[TraceLogLevelNullable](definitions.md#traceloglevelnullable)|
|**siteId**  <br>*optional*|Site the discoverer is part of|string|


<a name="discoveryconfigapimodel"></a>
### DiscoveryConfigApiModel
Discovery configuration


|Name|Description|Schema|
|---|---|---|
|**activationFilter**  <br>*optional*||[EndpointActivationFilterApiModel](definitions.md#endpointactivationfilterapimodel)|
|**addressRangesToScan**  <br>*optional*|Address ranges to scan (null == all wired nics)|string|
|**discoveryUrls**  <br>*optional*|List of preset discovery urls to use|< string > array|
|**idleTimeBetweenScansSec**  <br>*optional*|Delay time between discovery sweeps in seconds|integer (int32)|
|**locales**  <br>*optional*|List of locales to filter with during discovery|< string > array|
|**maxNetworkProbes**  <br>*optional*|Max network probes that should ever run.|integer (int32)|
|**maxPortProbes**  <br>*optional*|Max port probes that should ever run.|integer (int32)|
|**minPortProbesPercent**  <br>*optional*|Probes that must always be there as percent of max.|integer (int32)|
|**networkProbeTimeoutMs**  <br>*optional*|Network probe timeout|integer (int32)|
|**portProbeTimeoutMs**  <br>*optional*|Port probe timeout|integer (int32)|
|**portRangesToScan**  <br>*optional*|Port ranges to scan (null == all unassigned)|string|


<a name="discoverymode"></a>
### DiscoveryMode
*Type* : enum (Off, Local, Network, Fast, Scan)


<a name="discoverymodenullable"></a>
### DiscoveryModeNullable
*Type* : enum (Off, Local, Network, Fast, Scan)


<a name="discoveryrequestapimodel"></a>
### DiscoveryRequestApiModel
Discovery request


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*||[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**discovery**  <br>*optional*||[DiscoveryModeNullable](definitions.md#discoverymodenullable)|
|**id**  <br>*optional*|Id of discovery request|string|


<a name="endpointactivationfilterapimodel"></a>
### EndpointActivationFilterApiModel
Endpoint Activation Filter model


|Name|Description|Schema|
|---|---|---|
|**securityMode**  <br>*optional*||[SecurityModeNullable](definitions.md#securitymodenullable)|
|**securityPolicies**  <br>*optional*|Endpoint security policies to filter against.<br>If set to null, all policies are in scope.|< string > array|
|**trustLists**  <br>*optional*|Certificate trust list identifiers to use for<br>activation, if null, all certificates are<br>trusted.  If empty list, no certificates are<br>trusted which is equal to no filter.|< string > array|


<a name="endpointactivationstatenullable"></a>
### EndpointActivationStateNullable
*Type* : enum (Deactivated, Activated, ActivatedAndConnected)


<a name="endpointactivationstatusapimodel"></a>
### EndpointActivationStatusApiModel
Endpoint Activation status model


|Name|Description|Schema|
|---|---|---|
|**activationState**  <br>*optional*||[EndpointActivationStateNullable](definitions.md#endpointactivationstatenullable)|
|**id**  <br>*required*|Identifier of the endoint|string|


<a name="endpointapimodel"></a>
### EndpointApiModel
Endpoint model


|Name|Description|Schema|
|---|---|---|
|**alternativeUrls**  <br>*optional*|Alternative endpoint urls that can be used for<br>accessing and validating the server|< string > array|
|**certificate**  <br>*optional*|Endpoint certificate that was registered.  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**securityMode**  <br>*optional*||[SecurityModeNullable](definitions.md#securitymodenullable)|
|**securityPolicy**  <br>*optional*|Security policy uri to use for communication<br>default to best.|string|
|**url**  <br>*required*|Endpoint url to use to connect with|string|


<a name="endpointconnectivitystatenullable"></a>
### EndpointConnectivityStateNullable
*Type* : enum (Connecting, NotReachable, Busy, NoTrust, CertificateInvalid, Ready, Error)


<a name="endpointinfoapimodel"></a>
### EndpointInfoApiModel
Endpoint registration model


|Name|Description|Schema|
|---|---|---|
|**activationState**  <br>*optional*||[EndpointActivationStateNullable](definitions.md#endpointactivationstatenullable)|
|**applicationId**  <br>*required*|Application id endpoint is registered under.|string|
|**endpointState**  <br>*optional*||[EndpointConnectivityStateNullable](definitions.md#endpointconnectivitystatenullable)|
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
|**certificate**  <br>*optional*|Certificate of the endpoint  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**connected**  <br>*optional*|Whether the endpoint is connected on supervisor.|boolean|
|**discovererId**  <br>*optional*|Discoverer id to filter with|string|
|**endpointState**  <br>*optional*||[EndpointConnectivityStateNullable](definitions.md#endpointconnectivitystatenullable)|
|**includeNotSeenSince**  <br>*optional*|Whether to include endpoints that were soft deleted|boolean|
|**securityMode**  <br>*optional*||[SecurityModeNullable](definitions.md#securitymodenullable)|
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
|**discoverer**  <br>*optional*|[DiscovererApiModel](definitions.md#discovererapimodel)|
|**gateway**  <br>*required*|[GatewayApiModel](definitions.md#gatewayapimodel)|
|**publisher**  <br>*optional*|[PublisherApiModel](definitions.md#publisherapimodel)|
|**supervisor**  <br>*optional*|[SupervisorApiModel](definitions.md#supervisorapimodel)|


<a name="gatewaylistapimodel"></a>
### GatewayListApiModel
Gateway registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final|string|
|**items**  <br>*optional*|Registrations|< [GatewayApiModel](definitions.md#gatewayapimodel) > array|


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
|**certificate**  <br>*optional*|Publisher public client cert  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**configuration**  <br>*optional*||[PublisherConfigApiModel](definitions.md#publisherconfigapimodel)|
|**connected**  <br>*optional*|Whether publisher is connected on this registration|boolean|
|**id**  <br>*required*|Publisher id|string|
|**logLevel**  <br>*optional*||[TraceLogLevelNullable](definitions.md#traceloglevelnullable)|
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
|**siteId**  <br>*optional*|Site of the publisher|string|


<a name="publisherupdateapimodel"></a>
### PublisherUpdateApiModel
Publisher registration update request


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*||[PublisherConfigApiModel](definitions.md#publisherconfigapimodel)|
|**logLevel**  <br>*optional*||[TraceLogLevelNullable](definitions.md#traceloglevelnullable)|
|**siteId**  <br>*optional*|Site of the publisher|string|


<a name="registryoperationapimodel"></a>
### RegistryOperationApiModel
Registry operation log model


|Name|Description|Schema|
|---|---|---|
|**authorityId**  <br>*required*|Operation User|string|
|**time**  <br>*required*|Operation time|string (date-time)|


<a name="securityassessmentnullable"></a>
### SecurityAssessmentNullable
*Type* : enum (Unknown, Low, Medium, High)


<a name="securitymodenullable"></a>
### SecurityModeNullable
*Type* : enum (Best, Sign, SignAndEncrypt, None)


<a name="serverregistrationrequestapimodel"></a>
### ServerRegistrationRequestApiModel
Application registration request


|Name|Description|Schema|
|---|---|---|
|**activationFilter**  <br>*optional*||[EndpointActivationFilterApiModel](definitions.md#endpointactivationfilterapimodel)|
|**discoveryUrl**  <br>*required*|Discovery url to use for registration|string|
|**id**  <br>*optional*|Registration id|string|


<a name="statusresponseapimodel"></a>
### StatusResponseApiModel
Status response model


|Name|Description|Schema|
|---|---|---|
|**$metadata**  <br>*optional*  <br>*read-only*||< string, string > map|
|**currentTime**  <br>*optional*  <br>*read-only*|Current time|string|
|**dependencies**  <br>*optional*  <br>*read-only*|A property bag with details about the internal dependencies|< string, string > map|
|**name**  <br>*optional*|Name of this service|string|
|**properties**  <br>*optional*  <br>*read-only*|A property bag with details about the service|< string, string > map|
|**startTime**  <br>*optional*  <br>*read-only*|Start time of service|string|
|**status**  <br>*optional*|Operational status|string|
|**uid**  <br>*optional*  <br>*read-only*|Value generated at bootstrap by each instance of the service and<br>used to correlate logs coming from the same instance. The value<br>changes every time the service starts.|string|
|**upTime**  <br>*optional*  <br>*read-only*|Up time of service|integer (int64)|


<a name="supervisorapimodel"></a>
### SupervisorApiModel
Supervisor registration model


|Name|Description|Schema|
|---|---|---|
|**certificate**  <br>*optional*|Supervisor public client cert  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`|string (byte)|
|**connected**  <br>*optional*|Whether supervisor is connected on this registration|boolean|
|**id**  <br>*required*|Supervisor id|string|
|**logLevel**  <br>*optional*||[TraceLogLevelNullable](definitions.md#traceloglevelnullable)|
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
|**siteId**  <br>*optional*|Site of the supervisor|string|


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
Supervisor registration update request


|Name|Description|Schema|
|---|---|---|
|**logLevel**  <br>*optional*||[TraceLogLevelNullable](definitions.md#traceloglevelnullable)|
|**siteId**  <br>*optional*|Site of the supervisor|string|


<a name="traceloglevelnullable"></a>
### TraceLogLevelNullable
*Type* : enum (Error, Information, Debug, Verbose)



