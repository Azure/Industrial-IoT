# Opc-Publisher-Service


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Publisher Service


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

* Jobs : Jobs controller
* Publish : Value and Event publishing services
* Workers : Agent controller




<a name="paths"></a>
## Resources

<a name="jobs_resource"></a>
### Jobs
Jobs controller


<a name="queryjobs"></a>
#### Query jobs
```
POST /publisher/v2/jobs
```


##### Description
List all jobs that are registered or continues a query.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|
|**Body**|**body**  <br>*optional*|Query specification to use as filter.|[JobInfoQueryApiModel](definitions.md#jobinfoqueryapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[JobInfoListApiModel](definitions.md#jobinfolistapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="listjobs"></a>
#### Get list of jobs
```
GET /publisher/v2/jobs
```


##### Description
List all jobs that are registered or continues a query.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[JobInfoListApiModel](definitions.md#jobinfolistapimodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getjob"></a>
#### Get job by id
```
GET /publisher/v2/jobs/{id}
```


##### Description
Returns a job with the provided identifier.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**id**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[JobInfoApiModel](definitions.md#jobinfoapimodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="deletejob"></a>
#### Delete job by id
```
DELETE /publisher/v2/jobs/{id}
```


##### Description
Deletes a job.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**id**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


<a name="canceljob"></a>
#### Cancel job by id
```
GET /publisher/v2/jobs/{id}/cancel
```


##### Description
Cancels a job execution.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**id**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


<a name="restartjob"></a>
#### Restart job by id
```
GET /publisher/v2/jobs/{id}/restart
```


##### Description
Restarts a cancelled job which sets it back to active.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**id**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|


<a name="publish_resource"></a>
### Publish
Value and Event publishing services


<a name="getfirstlistofpublishednodes"></a>
#### Get currently published nodes
```
POST /publisher/v2/publish/{endpointId}
```


##### Description
Returns currently published node ids for an endpoint. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The list request|[PublishedItemListRequestApiModel](definitions.md#publisheditemlistrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseApiModel](definitions.md#publisheditemlistresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getnextlistofpublishednodes"></a>
#### Get next set of published nodes
```
GET /publisher/v2/publish/{endpointId}
```


##### Description
Returns next set of currently published node ids for an endpoint. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Query**|**continuationToken**  <br>*required*|The continuation token to continue with|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishedItemListResponseApiModel](definitions.md#publisheditemlistresponseapimodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="bulkpublishvalues"></a>
#### Bulk publish node values
```
POST /publisher/v2/publish/{endpointId}/bulk
```


##### Description
Adds or removes in bulk values that should be published from a particular endpoint.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of an activated endpoint.|string|
|**Body**|**body**  <br>*required*|The bulk publish request|[PublishBulkRequestApiModel](definitions.md#publishbulkrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishBulkResponseApiModel](definitions.md#publishbulkresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="startpublishingvalues"></a>
#### Start publishing node values
```
POST /publisher/v2/publish/{endpointId}/start
```


##### Description
Start publishing variable node values to IoT Hub. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The publish request|[PublishStartRequestApiModel](definitions.md#publishstartrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStartResponseApiModel](definitions.md#publishstartresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="stoppublishingvalues"></a>
#### Stop publishing node values
```
POST /publisher/v2/publish/{endpointId}/stop
```


##### Description
Stop publishing variable node values to IoT Hub. The endpoint must be activated and connected and the module client and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**body**  <br>*required*|The unpublish request|[PublishStopRequestApiModel](definitions.md#publishstoprequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[PublishStopResponseApiModel](definitions.md#publishstopresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`
* `application/x-msgpack`


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="workers_resource"></a>
### Workers
Agent controller


<a name="listworkers"></a>
#### Get list of workers
```
GET /publisher/v2/workers
```


##### Description
List all workers that are registered or continues a query.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**continuationToken**  <br>*optional*|Optional Continuation token|string|
|**Query**|**pageSize**  <br>*optional*|Optional number of results to return|integer (int32)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[WorkerInfoListApiModel](definitions.md#workerinfolistapimodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="getworker"></a>
#### Get worker
```
GET /publisher/v2/workers/{id}
```


##### Description
Returns a worker with the provided identifier.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**id**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[WorkerInfoApiModel](definitions.md#workerinfoapimodel)|


##### Produces

* `text/plain`
* `application/json`
* `text/json`
* `application/x-msgpack`


<a name="deleteworker"></a>
#### Delete worker by id
```
DELETE /publisher/v2/workers/{id}
```


##### Description
Deletes an worker in the registry.


##### Parameters

|Type|Name|Schema|
|---|---|---|
|**Path**|**id**  <br>*required*|string|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|No Content|



