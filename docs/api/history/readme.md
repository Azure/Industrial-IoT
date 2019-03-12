# Opc-Historic-Access


<a name="overview"></a>
## Overview
Azure Industrial IoT OPC UA Historic Access Service


### Version information
*Version* : v1


### URI scheme
*Schemes* : HTTPS, HTTP


### Tags

* Delete : Services to delete history
* History : History raw access services
* Insert : History insert services
* Read : Historic access read services
* Replace : History replace services
* Status : Status checks




<a name="paths"></a>
## Resources

<a name="delete_resource"></a>
### Delete
Services to delete history


<a name="historydeleteevents"></a>
#### Delete historic events
```
POST /v1/delete/{endpointId}/events
```


##### Description
Delete historic events using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[DeleteEventsDetailsApiModel]](definitions.md#historyupdaterequestapimodel-deleteeventsdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/delete/string/events
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "eventIds" : [ "string" ]
  },
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historydeletevalues"></a>
#### Delete historic values
```
POST /v1/delete/{endpointId}/values
```


##### Description
Delete historic values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[DeleteValuesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-deletevaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/delete/string/values
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "startTime" : "string",
    "endTime" : "string"
  },
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historydeletemodifiedvalues"></a>
#### Delete historic values
```
POST /v1/delete/{endpointId}/values/modified
```


##### Description
Delete historic values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[DeleteModifiedValuesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-deletemodifiedvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/delete/string/values/modified
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "startTime" : "string",
    "endTime" : "string"
  },
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historydeletevaluesattimes"></a>
#### Delete value history at specified times
```
POST /v1/delete/{endpointId}/values/pick
```


##### Description
Delete value history using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[DeleteValuesAtTimesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-deletevaluesattimesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/delete/string/values/pick
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "reqTimes" : [ "string" ]
  },
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="history_resource"></a>
### History
History raw access services


<a name="historyreadraw"></a>
#### Read history using json details
```
POST /v1/history/read/{endpointId}
```


##### Description
Read node history if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[JToken]](definitions.md#historyreadrequestapimodel-jtoken)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[JToken]](definitions.md#historyreadresponseapimodel-jtoken)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/history/read/string
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : "object",
  "indexRange" : "string",
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : "object",
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyreadrawnext"></a>
#### Read next batch of history as json
```
POST /v1/history/read/{endpointId}/next
```


##### Description
Read next batch of node history values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read next request|[HistoryReadNextRequestApiModel](definitions.md#historyreadnextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadNextResponseApiModel[JToken]](definitions.md#historyreadnextresponseapimodel-jtoken)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/history/read/string/next
```


###### Request body
```json
{
  "continuationToken" : "string",
  "abort" : true,
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : "object",
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyupdateraw"></a>
#### Update node history using raw json
```
POST /v1/history/update/{endpointId}
```


##### Description
Update node history using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history update request|[HistoryUpdateRequestApiModel[JToken]](definitions.md#historyupdaterequestapimodel-jtoken)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/history/update/string
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : "object",
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="insert_resource"></a>
### Insert
History insert services


<a name="historyinsertevents"></a>
#### Insert historic events
```
POST /v1/insert/{endpointId}/events
```


##### Description
Insert historic events using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history insert request|[HistoryUpdateRequestApiModel[InsertEventsDetailsApiModel]](definitions.md#historyupdaterequestapimodel-inserteventsdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/insert/string/events
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "filter" : "object",
    "events" : [ {
      "eventFields" : [ "object" ]
    } ]
  },
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyinsertvalues"></a>
#### Insert historic values
```
POST /v1/insert/{endpointId}/values
```


##### Description
Insert historic values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history insert request|[HistoryUpdateRequestApiModel[InsertValuesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-insertvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/insert/string/values
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "values" : [ {
      "value" : "object",
      "statusCode" : 0,
      "sourceTimestamp" : "string",
      "sourcePicoseconds" : 0,
      "serverTimestamp" : "string",
      "serverPicoseconds" : 0,
      "modificationInfo" : {
        "modificationTime" : "string",
        "updateType" : "string",
        "userName" : "string"
      }
    } ]
  },
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="read_resource"></a>
### Read
Historic access read services


<a name="historyreadevents"></a>
#### Read historic events
```
POST /v1/read/{endpointId}/events
```


##### Description
Read historic events of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadEventsDetailsApiModel]](definitions.md#historyreadrequestapimodel-readeventsdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricEventApiModel[]]](definitions.md#historyreadresponseapimodel-historiceventapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/read/string/events
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "startTime" : "string",
    "endTime" : "string",
    "numEvents" : 0,
    "filter" : "object"
  },
  "indexRange" : "string",
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : [ {
    "eventFields" : [ "object" ]
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyreadeventsnext"></a>
#### Read next batch of historic events
```
POST /v1/read/{endpointId}/events/next
```


##### Description
Read next batch of historic events of a node using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read next request|[HistoryReadNextRequestApiModel](definitions.md#historyreadnextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadNextResponseApiModel[HistoricEventApiModel[]]](definitions.md#historyreadnextresponseapimodel-historiceventapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/read/string/events/next
```


###### Request body
```json
{
  "continuationToken" : "string",
  "abort" : true,
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : [ {
    "eventFields" : [ "object" ]
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyreadvalues"></a>
#### Read historic processed values at specified times
```
POST /v1/read/{endpointId}/values
```


##### Description
Read processed history values of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadValuesDetailsApiModel]](definitions.md#historyreadrequestapimodel-readvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadresponseapimodel-historicvalueapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/read/string/values
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "startTime" : "string",
    "endTime" : "string",
    "numValues" : 0,
    "returnBounds" : true
  },
  "indexRange" : "string",
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : [ {
    "value" : "object",
    "statusCode" : 0,
    "sourceTimestamp" : "string",
    "sourcePicoseconds" : 0,
    "serverTimestamp" : "string",
    "serverPicoseconds" : 0,
    "modificationInfo" : {
      "modificationTime" : "string",
      "updateType" : "string",
      "userName" : "string"
    }
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyreadmodifiedvalues"></a>
#### Read historic modified values at specified times
```
POST /v1/read/{endpointId}/values/modified
```


##### Description
Read processed history values of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadModifiedValuesDetailsApiModel]](definitions.md#historyreadrequestapimodel-readmodifiedvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadresponseapimodel-historicvalueapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/read/string/values/modified
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "startTime" : "string",
    "endTime" : "string",
    "numValues" : 0
  },
  "indexRange" : "string",
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : [ {
    "value" : "object",
    "statusCode" : 0,
    "sourceTimestamp" : "string",
    "sourcePicoseconds" : 0,
    "serverTimestamp" : "string",
    "serverPicoseconds" : 0,
    "modificationInfo" : {
      "modificationTime" : "string",
      "updateType" : "string",
      "userName" : "string"
    }
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyreadvaluenext"></a>
#### Read next batch of historic values
```
POST /v1/read/{endpointId}/values/next
```


##### Description
Read next batch of historic values of a node using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read next request|[HistoryReadNextRequestApiModel](definitions.md#historyreadnextrequestapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadNextResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadnextresponseapimodel-historicvalueapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/read/string/values/next
```


###### Request body
```json
{
  "continuationToken" : "string",
  "abort" : true,
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : [ {
    "value" : "object",
    "statusCode" : 0,
    "sourceTimestamp" : "string",
    "sourcePicoseconds" : 0,
    "serverTimestamp" : "string",
    "serverPicoseconds" : 0,
    "modificationInfo" : {
      "modificationTime" : "string",
      "updateType" : "string",
      "userName" : "string"
    }
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyreadvaluesattimes"></a>
#### Read historic values at specified times
```
POST /v1/read/{endpointId}/values/pick
```


##### Description
Read historic values of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadValuesAtTimesDetailsApiModel]](definitions.md#historyreadrequestapimodel-readvaluesattimesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadresponseapimodel-historicvalueapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/read/string/values/pick
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "reqTimes" : [ "string" ],
    "useSimpleBounds" : true
  },
  "indexRange" : "string",
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : [ {
    "value" : "object",
    "statusCode" : 0,
    "sourceTimestamp" : "string",
    "sourcePicoseconds" : 0,
    "serverTimestamp" : "string",
    "serverPicoseconds" : 0,
    "modificationInfo" : {
      "modificationTime" : "string",
      "updateType" : "string",
      "userName" : "string"
    }
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyreadprocessedvalues"></a>
#### Read historic processed values at specified times
```
POST /v1/read/{endpointId}/values/processed
```


##### Description
Read processed history values of a node if available using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history read request|[HistoryReadRequestApiModel[ReadProcessedValuesDetailsApiModel]](definitions.md#historyreadrequestapimodel-readprocessedvaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryReadResponseApiModel[HistoricValueApiModel[]]](definitions.md#historyreadresponseapimodel-historicvalueapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/read/string/values/processed
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "startTime" : "string",
    "endTime" : "string",
    "processingInterval" : 0.0,
    "aggregateTypeId" : "string",
    "aggregateConfiguration" : {
      "useServerCapabilitiesDefaults" : true,
      "treatUncertainAsBad" : true,
      "percentDataBad" : 0,
      "percentDataGood" : 0,
      "useSlopedExtrapolation" : true
    }
  },
  "indexRange" : "string",
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "history" : [ {
    "value" : "object",
    "statusCode" : 0,
    "sourceTimestamp" : "string",
    "sourcePicoseconds" : 0,
    "serverTimestamp" : "string",
    "serverPicoseconds" : 0,
    "modificationInfo" : {
      "modificationTime" : "string",
      "updateType" : "string",
      "userName" : "string"
    }
  } ],
  "continuationToken" : "string",
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="replace_resource"></a>
### Replace
History replace services


<a name="historyreplaceevents"></a>
#### Replace historic events
```
POST /v1/replace/{endpointId}/events
```


##### Description
Replace historic events using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history replace request|[HistoryUpdateRequestApiModel[ReplaceEventsDetailsApiModel]](definitions.md#historyupdaterequestapimodel-replaceeventsdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/replace/string/events
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "filter" : "object",
    "events" : [ {
      "eventFields" : [ "object" ]
    } ]
  },
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="historyreplacevalues"></a>
#### Replace historic values
```
POST /v1/replace/{endpointId}/values
```


##### Description
Replace historic values using historic access.
The endpoint must be activated and connected and the module client
and server must trust each other.


##### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Path**|**endpointId**  <br>*required*|The identifier of the activated endpoint.|string|
|**Body**|**request**  <br>*required*|The history replace request|[HistoryUpdateRequestApiModel[ReplaceValuesDetailsApiModel]](definitions.md#historyupdaterequestapimodel-replacevaluesdetailsapimodel)|


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[HistoryUpdateResponseApiModel](definitions.md#historyupdateresponseapimodel)|


##### Consumes

* `application/json-patch+json`
* `application/json`
* `text/json`
* `application/*+json`


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/replace/string/values
```


###### Request body
```json
{
  "nodeId" : "string",
  "browsePath" : [ "string" ],
  "details" : {
    "values" : [ {
      "value" : "object",
      "statusCode" : 0,
      "sourceTimestamp" : "string",
      "sourcePicoseconds" : 0,
      "serverTimestamp" : "string",
      "serverPicoseconds" : 0,
      "modificationInfo" : {
        "modificationTime" : "string",
        "updateType" : "string",
        "userName" : "string"
      }
    } ]
  },
  "header" : {
    "elevation" : {
      "type" : "string",
      "value" : "object"
    },
    "locales" : [ "string" ],
    "diagnostics" : {
      "level" : "string",
      "auditId" : "string",
      "timeStamp" : "string"
    }
  }
}
```


##### Example HTTP response

###### Response 200
```json
{
  "results" : [ {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  } ],
  "errorInfo" : {
    "statusCode" : 0,
    "errorMessage" : "string",
    "diagnostics" : "object"
  }
}
```


<a name="status_resource"></a>
### Status
Status checks


<a name="getstatus"></a>
#### Return the service status in the form of the service status api model.
```
GET /v1/status
```


##### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[StatusResponseApiModel](definitions.md#statusresponseapimodel)|


##### Produces

* `application/json`


##### Example HTTP request

###### Request path
```
/v1/status
```


##### Example HTTP response

###### Response 200
```json
{
  "name" : "string",
  "status" : "string",
  "currentTime" : "string",
  "startTime" : "string",
  "upTime" : 0,
  "uid" : "string",
  "properties" : {
    "string" : "string"
  },
  "dependencies" : {
    "string" : "string"
  },
  "$metadata" : {
    "string" : "string"
  }
}
```



