
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
|**id**  <br>*optional*|Method id|string|
|**securityPolicy**  <br>*optional*|Security policy to use when passing credential.|string|


<a name="credentialtype"></a>
### CredentialType
Type of credential to use for serverauth

*Type* : enum (None, UserName, X509Certificate, JwtToken)


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


<a name="discoveryeventapimodel"></a>
### DiscoveryEventApiModel
Discovery event


|Name|Description|Schema|
|---|---|---|
|**application**  <br>*optional*||[ApplicationInfoApiModel](definitions.md#applicationinfoapimodel)|
|**index**  <br>*optional*|Index in the batch with same timestamp.|integer (int32)|
|**registration**  <br>*optional*||[EndpointRegistrationApiModel](definitions.md#endpointregistrationapimodel)|
|**timeStamp**  <br>*optional*|Timestamp of the discovery sweep.|string (date-time)|


<a name="discoveryresultapimodel"></a>
### DiscoveryResultApiModel
Discovery result model


|Name|Description|Schema|
|---|---|---|
|**context**  <br>*optional*||[RegistryOperationApiModel](definitions.md#registryoperationapimodel)|
|**diagnostics**  <br>*optional*|If discovery failed, result information|string|
|**discoveryConfig**  <br>*optional*||[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**id**  <br>*optional*|Id of discovery request|string|
|**registerOnly**  <br>*optional*|If true, only register, do not unregister based<br>on these events.|boolean|


<a name="discoveryresultlistapimodel"></a>
### DiscoveryResultListApiModel
Discovery results


|Name|Description|Schema|
|---|---|---|
|**events**  <br>*optional*|Events|< [DiscoveryEventApiModel](definitions.md#discoveryeventapimodel) > array|
|**result**  <br>*optional*||[DiscoveryResultApiModel](definitions.md#discoveryresultapimodel)|


<a name="endpointactivationfilterapimodel"></a>
### EndpointActivationFilterApiModel
Endpoint Activation Filter model


|Name|Description|Schema|
|---|---|---|
|**securityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**securityPolicies**  <br>*optional*|Endpoint security policies to filter against.<br>If set to null, all policies are in scope.|< string > array|
|**trustLists**  <br>*optional*|Certificate trust list identifiers to use for<br>activation, if null, all certificates are<br>trusted.  If empty list, no certificates are<br>trusted which is equal to no filter.|< string > array|


<a name="endpointapimodel"></a>
### EndpointApiModel
Endpoint model


|Name|Description|Schema|
|---|---|---|
|**alternativeUrls**  <br>*optional*|Alternative endpoint urls that can be used for<br>accessing and validating the server|< string > array|
|**certificate**  <br>*optional*|Endpoint certificate thumbprint|string|
|**securityMode**  <br>*optional*||[SecurityMode](definitions.md#securitymode)|
|**securityPolicy**  <br>*optional*|Security policy uri to use for communication<br>default to best.|string|
|**url**  <br>*optional*|Endpoint url to use to connect with|string|


<a name="endpointregistrationapimodel"></a>
### EndpointRegistrationApiModel
Endpoint registration model


|Name|Description|Schema|
|---|---|---|
|**authenticationMethods**  <br>*optional*|Supported authentication methods that can be selected to<br>obtain a credential and used to interact with the endpoint.|< [AuthenticationMethodApiModel](definitions.md#authenticationmethodapimodel) > array|
|**discovererId**  <br>*optional*|Discoverer that registered the endpoint|string|
|**endpoint**  <br>*optional*||[EndpointApiModel](definitions.md#endpointapimodel)|
|**endpointUrl**  <br>*optional*|Original endpoint url of the endpoint|string|
|**id**  <br>*optional*|Registered identifier of the endpoint|string|
|**securityLevel**  <br>*optional*|Security level of the endpoint|integer (int32)|
|**siteId**  <br>*optional*|Registered site of the endpoint|string|
|**supervisorId**  <br>*optional*|Supervisor that can manage the endpoint.|string|


<a name="registryoperationapimodel"></a>
### RegistryOperationApiModel
Registry operation log model


|Name|Description|Schema|
|---|---|---|
|**authorityId**  <br>*optional*|Operation User|string|
|**time**  <br>*optional*|Operation time|string (date-time)|


<a name="securitymode"></a>
### SecurityMode
Security mode of endpoint

*Type* : enum (Best, Sign, SignAndEncrypt, None)



