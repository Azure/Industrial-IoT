
<a name="definitions"></a>
## Definitions

<a name="applicationinfoapimodel"></a>
### ApplicationInfoApiModel
Application model


|Name|Description|Schema|
|---|---|---|
|**applicationId**  <br>*optional*|Unique application id  <br>**Example** : `"string"`|string|
|**applicationName**  <br>*optional*|Name of server  <br>**Example** : `"string"`|string|
|**applicationType**  <br>*optional*|Type of application  <br>**Example** : `"Server"`|enum (Server, Client, ClientAndServer)|
|**applicationUri**  <br>*optional*|Unique application uri  <br>**Example** : `"string"`|string|
|**capabilities**  <br>*optional*|The capabilities advertised by the server.  <br>**Example** : `"LDS"`|< string > array|
|**certificate**  <br>*optional*|Application public cert  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`  <br>**Example** : `"string"`|string (byte)|
|**discoveryProfileUri**  <br>*optional*|Discovery profile uri  <br>**Example** : `"string"`|string|
|**discoveryUrls**  <br>*optional*|Discovery urls of the server  <br>**Example** : `[ "string" ]`|< string > array|
|**hostAddresses**  <br>*optional*|Host addresses of server application or null  <br>**Example** : `[ "string" ]`|< string > array|
|**locale**  <br>*optional*|Locale of name - defaults to "en"  <br>**Example** : `"en"`|string|
|**notSeenSince**  <br>*optional*|Last time application was seen  <br>**Example** : `"string"`|string (date-time)|
|**productUri**  <br>*optional*|Product uri  <br>**Example** : `"string"`|string|
|**siteId**  <br>*optional*|Site of the application  <br>**Example** : `"productionlineA"`|string|
|**supervisorId**  <br>*optional*|Supervisor having registered the application  <br>**Example** : `"string"`|string|


<a name="applicationinfolistapimodel"></a>
### ApplicationInfoListApiModel
List of registered applications


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final  <br>**Example** : `"string"`|string|
|**items**  <br>*optional*|Application infos  <br>**Example** : `[ "[applicationinfoapimodel](#applicationinfoapimodel)" ]`|< [ApplicationInfoApiModel](definitions.md#applicationinfoapimodel) > array|


<a name="applicationregistrationapimodel"></a>
### ApplicationRegistrationApiModel
Application with list of endpoints


|Name|Description|Schema|
|---|---|---|
|**application**  <br>*required*|Application information  <br>**Example** : `"[applicationinfoapimodel](#applicationinfoapimodel)"`|[ApplicationInfoApiModel](definitions.md#applicationinfoapimodel)|
|**endpoints**  <br>*optional*|List of endpoint twins  <br>**Example** : `[ "[endpointregistrationapimodel](#endpointregistrationapimodel)" ]`|< [EndpointRegistrationApiModel](definitions.md#endpointregistrationapimodel) > array|
|**securityAssessment**  <br>*optional*|Application security assessment  <br>**Example** : `"string"`|enum (Unknown, Low, Medium, High)|


<a name="applicationregistrationqueryapimodel"></a>
### ApplicationRegistrationQueryApiModel
Application information


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Name of application  <br>**Example** : `"string"`|string|
|**applicationType**  <br>*optional*|Type of application  <br>**Example** : `"string"`|enum (Server, Client, ClientAndServer)|
|**applicationUri**  <br>*optional*|Application uri  <br>**Example** : `"string"`|string|
|**capability**  <br>*optional*|Application capability to query with  <br>**Example** : `"string"`|string|
|**includeNotSeenSince**  <br>*optional*|Whether to include apps that were soft deleted  <br>**Example** : `true`|boolean|
|**locale**  <br>*optional*|Locale of application name - default is "en"  <br>**Example** : `"string"`|string|
|**productUri**  <br>*optional*|Product uri  <br>**Example** : `"string"`|string|
|**siteOrSupervisorId**  <br>*optional*|Supervisor or site the application belongs to.  <br>**Example** : `"string"`|string|


<a name="applicationregistrationrequestapimodel"></a>
### ApplicationRegistrationRequestApiModel
Application information


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Name of the server or client.  <br>**Example** : `"string"`|string|
|**applicationType**  <br>*optional*|Type of application  <br>**Example** : `"Server"`|enum (Server, Client, ClientAndServer)|
|**applicationUri**  <br>*required*|Unique application uri  <br>**Example** : `"string"`|string|
|**capabilities**  <br>*optional*|The OPC UA defined capabilities of the server.  <br>**Example** : `"LDS"`|< string > array|
|**discoveryProfileUri**  <br>*optional*|The discovery profile uri of the server.  <br>**Example** : `"string"`|string|
|**discoveryUrls**  <br>*optional*|Discovery urls of the server.  <br>**Example** : `[ "string" ]`|< string > array|
|**locale**  <br>*optional*|Locale of name  <br>**Example** : `"en"`|string|
|**productUri**  <br>*optional*|Product uri of the application.  <br>**Example** : `"http://contoso.com/fridge/1.0"`|string|


<a name="applicationregistrationresponseapimodel"></a>
### ApplicationRegistrationResponseApiModel
Result of an application registration


|Name|Description|Schema|
|---|---|---|
|**id**  <br>*optional*|New id application was registered under  <br>**Example** : `"string"`|string|


<a name="applicationregistrationupdateapimodel"></a>
### ApplicationRegistrationUpdateApiModel
Application registration update request


|Name|Description|Schema|
|---|---|---|
|**applicationName**  <br>*optional*|Application name  <br>**Example** : `"string"`|string|
|**capabilities**  <br>*optional*|Capabilities of the application  <br>**Example** : `[ "string" ]`|< string > array|
|**certificate**  <br>*optional*|Application public cert  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`  <br>**Example** : `"string"`|string (byte)|
|**discoveryProfileUri**  <br>*optional*|Discovery profile uri  <br>**Example** : `"string"`|string|
|**discoveryUrls**  <br>*optional*|Discovery urls of the application  <br>**Example** : `[ "string" ]`|< string > array|
|**locale**  <br>*optional*|Locale of name - defaults to "en"  <br>**Example** : `"string"`|string|
|**productUri**  <br>*optional*|Product uri  <br>**Example** : `"string"`|string|


<a name="applicationsitelistapimodel"></a>
### ApplicationSiteListApiModel
List of application sites


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final  <br>**Example** : `"string"`|string|
|**sites**  <br>*optional*|Distinct list of sites applications were registered in.  <br>**Example** : `[ "string" ]`|< string > array|


<a name="authenticationmethodapimodel"></a>
### AuthenticationMethodApiModel
Authentication Method model


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*|Method specific configuration  <br>**Example** : `"object"`|object|
|**credentialType**  <br>*optional*|Type of credential  <br>**Default** : `"None"`  <br>**Example** : `"string"`|enum (None, UserName, X509Certificate, JwtToken)|
|**id**  <br>*required*|Method identifier  <br>**Example** : `"string"`|string|
|**securityPolicy**  <br>*optional*|Security policy to use when passing credential.  <br>**Example** : `"string"`|string|


<a name="callbackapimodel"></a>
### CallbackApiModel
A registered callback


|Name|Description|Schema|
|---|---|---|
|**authenticationHeader**  <br>*optional*|Authentication header to add or null if not needed  <br>**Example** : `"string"`|string|
|**method**  <br>*optional*|Method to use for callback  <br>**Example** : `"string"`|enum (Get, Post, Put, Delete)|
|**uri**  <br>*optional*|Uri to call - should use https scheme in which<br>case security is enforced.  <br>**Example** : `"string"`|string|


<a name="credentialapimodel"></a>
### CredentialApiModel
Credential model


|Name|Description|Schema|
|---|---|---|
|**type**  <br>*optional*|Type of credential  <br>**Default** : `"None"`  <br>**Example** : `"string"`|enum (None, UserName, X509Certificate, JwtToken)|
|**value**  <br>*optional*|Value to pass to server  <br>**Example** : `"object"`|object|


<a name="discoveryconfigapimodel"></a>
### DiscoveryConfigApiModel
Discovery configuration


|Name|Description|Schema|
|---|---|---|
|**activationFilter**  <br>*optional*|Activate all twins with this filter during onboarding.  <br>**Example** : `"[endpointactivationfilterapimodel](#endpointactivationfilterapimodel)"`|[EndpointActivationFilterApiModel](definitions.md#endpointactivationfilterapimodel)|
|**addressRangesToScan**  <br>*optional*|Address ranges to scan (null == all wired nics)  <br>**Example** : `"string"`|string|
|**callbacks**  <br>*optional*|Callbacks to invoke once onboarding finishes  <br>**Example** : `[ "[callbackapimodel](#callbackapimodel)" ]`|< [CallbackApiModel](definitions.md#callbackapimodel) > array|
|**discoveryUrls**  <br>*optional*|List of preset discovery urls to use  <br>**Example** : `[ "string" ]`|< string > array|
|**idleTimeBetweenScansSec**  <br>*optional*|Delay time between discovery sweeps in seconds  <br>**Example** : `0`|integer (int32)|
|**locales**  <br>*optional*|List of locales to filter with during discovery  <br>**Example** : `[ "string" ]`|< string > array|
|**maxNetworkProbes**  <br>*optional*|Max network probes that should ever run.  <br>**Example** : `0`|integer (int32)|
|**maxPortProbes**  <br>*optional*|Max port probes that should ever run.  <br>**Example** : `0`|integer (int32)|
|**minPortProbesPercent**  <br>*optional*|Probes that must always be there as percent of max.  <br>**Example** : `0`|integer (int32)|
|**networkProbeTimeoutMs**  <br>*optional*|Networking probe timeout  <br>**Example** : `0`|integer (int32)|
|**portProbeTimeoutMs**  <br>*optional*|Port probe timeout  <br>**Example** : `0`|integer (int32)|
|**portRangesToScan**  <br>*optional*|Port ranges to scan (null == all unassigned)  <br>**Example** : `"string"`|string|


<a name="discoveryrequestapimodel"></a>
### DiscoveryRequestApiModel
Discovery request


|Name|Description|Schema|
|---|---|---|
|**configuration**  <br>*optional*|Scan configuration to use  <br>**Example** : `"[discoveryconfigapimodel](#discoveryconfigapimodel)"`|[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**discovery**  <br>*optional*|Discovery mode to use  <br>**Example** : `"string"`|enum (Off, Local, Network, Fast, Scan)|
|**id**  <br>*optional*|Id of discovery request  <br>**Example** : `"string"`|string|


<a name="endpointactivationfilterapimodel"></a>
### EndpointActivationFilterApiModel
Endpoint Activation Filter model


|Name|Description|Schema|
|---|---|---|
|**securityMode**  <br>*optional*|Security mode level to activate. If null,<br>then Microsoft.Azure.IIoT.OpcUa.Registry.Models.SecurityMode.Best is assumed.  <br>**Example** : `"string"`|enum (Best, Sign, SignAndEncrypt, None)|
|**securityPolicies**  <br>*optional*|Endpoint security policies to filter against.<br>If set to null, all policies are in scope.  <br>**Example** : `[ "string" ]`|< string > array|
|**trustLists**  <br>*optional*|Certificate trust list identifiers to use for<br>activation, if null, all certificates are<br>trusted.  If empty list, no certificates are<br>trusted which is equal to no filter.  <br>**Example** : `[ "string" ]`|< string > array|


<a name="endpointapimodel"></a>
### EndpointApiModel
Endpoint model


|Name|Description|Schema|
|---|---|---|
|**securityMode**  <br>*optional*|Security Mode to use for communication<br>default to best.  <br>**Default** : `"Best"`  <br>**Example** : `"string"`|enum (Best, Sign, SignAndEncrypt, None)|
|**securityPolicy**  <br>*optional*|Security policy uri to use for communication<br>default to best.  <br>**Example** : `"string"`|string|
|**serverThumbprint**  <br>*optional*|Thumbprint to validate against or null to trust any.  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`  <br>**Example** : `"string"`|string (byte)|
|**url**  <br>*required*|Endpoint  <br>**Example** : `"string"`|string|
|**user**  <br>*optional*|User Authentication  <br>**Example** : `"[credentialapimodel](#credentialapimodel)"`|[CredentialApiModel](definitions.md#credentialapimodel)|


<a name="endpointinfoapimodel"></a>
### EndpointInfoApiModel
Endpoint registration model


|Name|Description|Schema|
|---|---|---|
|**activated**  <br>*optional*|Whether endpoint is activated on this registration  <br>**Example** : `true`|boolean|
|**applicationId**  <br>*required*|Application id endpoint is registered under.  <br>**Example** : `"string"`|string|
|**connected**  <br>*optional*|Whether endpoint is connected on this registration  <br>**Example** : `true`|boolean|
|**notSeenSince**  <br>*optional*|Last time endpoint was seen  <br>**Example** : `"string"`|string (date-time)|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync  <br>**Example** : `true`|boolean|
|**registration**  <br>*required*|Endpoint registration  <br>**Example** : `"[endpointregistrationapimodel](#endpointregistrationapimodel)"`|[EndpointRegistrationApiModel](definitions.md#endpointregistrationapimodel)|


<a name="endpointinfolistapimodel"></a>
### EndpointInfoListApiModel
Endpoint registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final  <br>**Example** : `"string"`|string|
|**items**  <br>*optional*|Endpoint registrations  <br>**Example** : `[ "[endpointinfoapimodel](#endpointinfoapimodel)" ]`|< [EndpointInfoApiModel](definitions.md#endpointinfoapimodel) > array|


<a name="endpointregistrationapimodel"></a>
### EndpointRegistrationApiModel
Endpoint registration model


|Name|Description|Schema|
|---|---|---|
|**authenticationMethods**  <br>*optional*|Supported authentication methods for the endpoint.  <br>**Example** : `[ "[authenticationmethodapimodel](#authenticationmethodapimodel)" ]`|< [AuthenticationMethodApiModel](definitions.md#authenticationmethodapimodel) > array|
|**certificate**  <br>*optional*|Endpoint cert that was registered.  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`  <br>**Example** : `"string"`|string (byte)|
|**endpoint**  <br>*required*|Endpoint information of the registration  <br>**Example** : `"[endpointapimodel](#endpointapimodel)"`|[EndpointApiModel](definitions.md#endpointapimodel)|
|**id**  <br>*required*|Registered identifier of the endpoint  <br>**Example** : `"string"`|string|
|**securityLevel**  <br>*optional*|Security level of the endpoint  <br>**Example** : `0`|integer (int32)|
|**siteId**  <br>*optional*|Registered site of the endpoint  <br>**Example** : `"string"`|string|


<a name="endpointregistrationqueryapimodel"></a>
### EndpointRegistrationQueryApiModel
Endpoint query


|Name|Description|Schema|
|---|---|---|
|**activated**  <br>*optional*|Whether the endpoint was activated  <br>**Example** : `true`|boolean|
|**certificate**  <br>*optional*|Certificate of the endpoint  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`  <br>**Example** : `"string"`|string (byte)|
|**connected**  <br>*optional*|Whether the endpoint is connected on supervisor.  <br>**Example** : `true`|boolean|
|**includeNotSeenSince**  <br>*optional*|Whether to include endpoints that were soft deleted  <br>**Example** : `true`|boolean|
|**securityMode**  <br>*optional*|Security Mode  <br>**Example** : `"string"`|enum (Best, Sign, SignAndEncrypt, None)|
|**securityPolicy**  <br>*optional*|Security policy uri  <br>**Example** : `"string"`|string|
|**url**  <br>*optional*|Endoint url for direct server access  <br>**Example** : `"string"`|string|
|**userAuthentication**  <br>*optional*|Type of credential selected for authentication  <br>**Example** : `"string"`|enum (None, UserName, X509Certificate, JwtToken)|


<a name="endpointregistrationupdateapimodel"></a>
### EndpointRegistrationUpdateApiModel
Endpoint registration update request


|Name|Description|Schema|
|---|---|---|
|**user**  <br>*optional*|User authentication to change on the endpoint.  <br>**Example** : `"[credentialapimodel](#credentialapimodel)"`|[CredentialApiModel](definitions.md#credentialapimodel)|


<a name="serverregistrationrequestapimodel"></a>
### ServerRegistrationRequestApiModel
Application registration request


|Name|Description|Schema|
|---|---|---|
|**activationFilter**  <br>*optional*|Upon discovery, activate all endpoints with this filter.  <br>**Example** : `"[endpointactivationfilterapimodel](#endpointactivationfilterapimodel)"`|[EndpointActivationFilterApiModel](definitions.md#endpointactivationfilterapimodel)|
|**callback**  <br>*optional*|An optional callback hook to register.  <br>**Example** : `"[callbackapimodel](#callbackapimodel)"`|[CallbackApiModel](definitions.md#callbackapimodel)|
|**discoveryUrl**  <br>*required*|Discovery url to use for registration  <br>**Example** : `"string"`|string|
|**id**  <br>*optional*|Registration id  <br>**Example** : `"string"`|string|


<a name="statusresponseapimodel"></a>
### StatusResponseApiModel
Status response model


|Name|Description|Schema|
|---|---|---|
|**$metadata**  <br>*optional*  <br>*read-only*|Optional meta data.  <br>**Example** : `{<br>  "string" : "string"<br>}`|< string, string > map|
|**currentTime**  <br>*optional*  <br>*read-only*|Current time  <br>**Example** : `"string"`|string|
|**dependencies**  <br>*optional*  <br>*read-only*|A property bag with details about the internal dependencies  <br>**Example** : `{<br>  "string" : "string"<br>}`|< string, string > map|
|**name**  <br>*optional*  <br>*read-only*|Name of this service  <br>**Example** : `"string"`|string|
|**properties**  <br>*optional*  <br>*read-only*|A property bag with details about the service  <br>**Example** : `{<br>  "string" : "string"<br>}`|< string, string > map|
|**startTime**  <br>*optional*  <br>*read-only*|Start time of service  <br>**Example** : `"string"`|string|
|**status**  <br>*optional*|Operational status  <br>**Example** : `"string"`|string|
|**uid**  <br>*optional*  <br>*read-only*|Value generated at bootstrap by each instance of the service and<br>used to correlate logs coming from the same instance. The value<br>changes every time the service starts.  <br>**Example** : `"string"`|string|
|**upTime**  <br>*optional*  <br>*read-only*|Up time of service  <br>**Example** : `0`|integer (int64)|


<a name="supervisorapimodel"></a>
### SupervisorApiModel
Supervisor registration model


|Name|Description|Schema|
|---|---|---|
|**certificate**  <br>*optional*|Supervisor public client cert  <br>**Pattern** : `"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==\|[A-Za-z0-9+/]{3}=)?$"`  <br>**Example** : `"string"`|string (byte)|
|**connected**  <br>*optional*|Whether supervisor is connected on this registration  <br>**Example** : `true`|boolean|
|**discovery**  <br>*optional*|Whether the supervisor is in discovery mode  <br>**Default** : `"Off"`  <br>**Example** : `"string"`|enum (Off, Local, Network, Fast, Scan)|
|**discoveryConfig**  <br>*optional*|Supervisor configuration  <br>**Example** : `"[discoveryconfigapimodel](#discoveryconfigapimodel)"`|[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**id**  <br>*required*|Supervisor id  <br>**Example** : `"string"`|string|
|**outOfSync**  <br>*optional*|Whether the registration is out of sync between<br>client (module) and server (service) (default: false).  <br>**Example** : `true`|boolean|
|**siteId**  <br>*optional*|Site of the supervisor  <br>**Example** : `"string"`|string|


<a name="supervisorlistapimodel"></a>
### SupervisorListApiModel
Supervisor registration list


|Name|Description|Schema|
|---|---|---|
|**continuationToken**  <br>*optional*|Continuation or null if final  <br>**Example** : `"string"`|string|
|**items**  <br>*optional*|Registrations  <br>**Example** : `[ "[supervisorapimodel](#supervisorapimodel)" ]`|< [SupervisorApiModel](definitions.md#supervisorapimodel) > array|


<a name="supervisorqueryapimodel"></a>
### SupervisorQueryApiModel
Supervisor registration query


|Name|Description|Schema|
|---|---|---|
|**connected**  <br>*optional*|Included connected or disconnected  <br>**Example** : `true`|boolean|
|**discovery**  <br>*optional*|Discovery mode of supervisor  <br>**Example** : `"string"`|enum (Off, Local, Network, Fast, Scan)|
|**siteId**  <br>*optional*|Site of the supervisor  <br>**Example** : `"string"`|string|


<a name="supervisorupdateapimodel"></a>
### SupervisorUpdateApiModel
Supervisor registration update request


|Name|Description|Schema|
|---|---|---|
|**discovery**  <br>*optional*|Whether the supervisor is in discovery mode.<br>If null, does not change.  <br>**Default** : `"Off"`  <br>**Example** : `"string"`|enum (Off, Local, Network, Fast, Scan)|
|**discoveryCallbacks**  <br>*optional*|Callbacks to add or remove (see below)  <br>**Example** : `[ "[callbackapimodel](#callbackapimodel)" ]`|< [CallbackApiModel](definitions.md#callbackapimodel) > array|
|**discoveryConfig**  <br>*optional*|Supervisor discovery configuration  <br>**Example** : `"[discoveryconfigapimodel](#discoveryconfigapimodel)"`|[DiscoveryConfigApiModel](definitions.md#discoveryconfigapimodel)|
|**removeDiscoveryCallbacks**  <br>*optional*|Whether to add or remove callbacks  <br>**Example** : `true`|boolean|
|**siteId**  <br>*optional*|Site of the supervisor  <br>**Example** : `"string"`|string|



