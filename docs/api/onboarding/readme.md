# Opc-Onboarding-Service


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Onboarding Service


### Version information
*Version* : v2


### License information
*License* : MIT LICENSE  
*License URL* : https://opensource.org/licenses/MIT  
*Terms of service* : null


### URI scheme
*Host* : localhost:9080  
*Schemes* : HTTP, HTTPS


### Tags

* Discovery : Handle discovery events and onboard applications




<a name="paths"></a>
## Resources

<a name="discovery_resource"></a>
### Discovery
Handle discovery events and onboard applications


<a name="processdiscoveryresults"></a>
#### Process discovery results
```
POST /onboarding/v2/discovery
```


##### Description
Bulk processes discovery events and onboards new entities to the application registry


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**discovererId**  <br>*required*||string|
|**Body**|**body**  <br>*required*|Discovery event list model|[DiscoveryResultListApiModel](definitions.md#discoveryresultlistapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`



