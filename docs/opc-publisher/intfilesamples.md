# Init file samples <!-- omit in toc -->

[Home](./readme.md#configuration-via-init-file)

You find here examples that leverage the [init file capabilities](./readme.md#configuration-via-init-file) of OPC Publisher (since 2.9.12).

## Table Of Contents <!-- omit in toc -->

- [Find and create writers for all machine tools in a server](#find-and-create-writers-for-all-machine-tools-in-a-server)
- [Add all variables in a server to a single data set writer](#add-all-variables-in-a-server-to-a-single-data-set-writer)
- [Add machine objects as data set writers](#add-machine-objects-as-data-set-writers)
- [Create a Web of Things Asset and add a data set writer](#create-a-web-of-things-asset-and-add-a-data-set-writer)

## Find and create writers for all machine tools in a server

This init file uses the [ExpandAndCreateOrUpdateDataSetWriterEntries API](./api.md#expandandcreateorupdatedatasetwriterentries) to generate writers for each machine tool found on the server. A machine tool is an object that compiles to the MachineTool ObjectType as defined in the [OPC 40501-1](https://reference.opcfoundation.org/MachineTool/v102/docs/8.1) (machine tool companion specification).

For this reason, this and the following 2 samples use the publicly hosted [Umati](https://umati.org/) reference server giving a good understanding on how to leverage the OPC UA companion specifications.

``` json
###

// 3 retries in case of failure, with a delay of 5 seconds between
// @delay 5
// @retries 3

// Creates writer entries for all objects that implement the
// machine tool object type or one of its subtypes on the server
ExpandAndCreateOrUpdateDataSetWriterEntries_V2

{
    "entry": {
        "EndpointUrl": "opc.tcp://opcua.umati.app:4840",
        "UseSecurity": false,
        "DataSetWriterGroup": "MachineTools",
        "OpcNodes": [
            { "Id": "nsu=http://opcfoundation.org/UA/MachineTool/;i=13" }
        ]
    }
}

###

// Shutdown the publisher in case the expansion failed
// and let docker restart it. The Fail fast argument
// provided as json payload.
# @on-error
Shutdown_V2

true

###
```

This results in a result (log) file that shows the result of the execution of the individual methods on the publisher API and that looks like this (the response payload is abbreviated and in any case not indented):

``` json
###

// 3 retries in case of failure, with a delay of 5 seconds between
// Creates writer entries for all objects that implement the
// machine tool object type or one of its subtypes on the server
ExpandAndCreateOrUpdateDataSetWriterEntries_V2
200

[{"result":{"DataSetWrite ....... tionMode":"Anonymous"}}]

###

// Shutdown the publisher in case the expansion failed
// and let docker restart it. The Fail fast argument
// provided as json payload.
Shutdown_V2
// @skipped reason = success
###
```

## Add all variables in a server to a single data set writer

The following example uses the same API but with the ObjectsFolder (`i=85`) node as root, drilling down 10 levels and capturing all variables into a single writer entry in the published nodes configuration.

``` json
###

// 3 retries in case of failure, with a delay of 5 seconds between
// @delay 5
// @retries 3
ExpandAndCreateOrUpdateDataSetWriterEntries_V2

{
    "entry": {
        "EndpointUrl": "opc.tcp://opcua.umati.app:4840",
        "UseSecurity": false,
        "DataSetWriterGroup": "All",
        "OpcNodes": [
            { "Id": "i=85" }
        ]
    },
    "request": {
        "createSingleWriter": true,
        "maxDepth": 10,
        "discardErrors": true
    }
}

###

// Shutdown the publisher in case the expansion failed
// and let docker restart it. The Fail fast argument
// provided as json payload.
# @on-error
Shutdown_V2

true

###
```

## Add machine objects as data set writers

The following example uses again the same API but with the Machines folder (`nsu=http://opcfoundation.org/UA/Machinery/;i=1001`) node as root capturing all variables into several writer entries in the published nodes configuration.

``` json
###
// @delay 5
ExpandAndCreateOrUpdateDataSetWriterEntries

{
    "entry": {
        "EndpointUrl": "opc.tcp://opcua.umati.app:4840",
        "UseSecurity": false,
        "DataSetWriterGroup": "Machinery Objects",
        "OpcNodes": [
            { "Id": "nsu=http://opcfoundation.org/UA/Machinery/;i=1001" }
        ]
    }
}

// @retries 3
###
Shutdown
// @on-error
###
```

## Create a Web of Things Asset and add a data set writer

The following shows how to create a Asset in a [WoT connectivity](https://reference.opcfoundation.org/WoT/v100/docs/) compatible server using a WoT Thing instance model using the [Asset](./api.md#createorupdateasset) API. An compatible sample server can be found [here](https://github.com/OPCFoundation/UA-EdgeTranslator).

> Please note that the asset name inside the configuration must match the `DataSetName` property.

``` json
###
CreateOrUpdateAsset

{
    "entry": {
        "EndpointUrl": "opc.tcp://localhost:4840",
        "UseSecurity": true,
        "DataSetWriterGroup": "Assets",
        "DataSetName": "MyAsset1"
    },
    "waitTime": "00:00:01",
    "configuration": {
        "@context": [
            "https://www.w3.org/2022/wot/td/v1.1"
        ],
        "id": "urn:Simple PLC",
        "securityDefinitions": {
            "nosec_sc": {
                "scheme": "nosec"
            }
        },
        "security": [
            "nosec_sc"
        ],
        "@type": [
            "tm:ThingModel"
        ],
        "name": "MyAsset1",
        "base": "ads://127.0.0.1:8534",
        "title": "Untitled1",
        "properties": {
        "Global_Version.stLibVersion_Tc2_Standard": {
            "type": "number",
            "opcua:nodeId": null,
            "opcua:type": null,
            "opcua:fieldPath": null,
            "readOnly": true,
            "observable": true,
            "forms": [
            {
                "href": "Global_Version.stLibVersion_Tc2_Standard?36",
                "op": [
                    "readproperty",
                    "observeproperty"
                ],
                "type": "xsd:float",
                "pollingTime": 1000
            }
            ]
        },
        "Global_Version.stLibVersion_Tc2_System": {
            "type": "number",
            "opcua:nodeId": null,
            "opcua:type": null,
            "opcua:fieldPath": null,
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "Global_Version.stLibVersion_Tc2_System?36",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "type": "xsd:float",
                    "pollingTime": 1000
                }
            ]
        },
        "Global_Version.stLibVersion_Tc3_Module": {
            "type": "number",
            "opcua:nodeId": null,
            "opcua:type": null,
            "opcua:fieldPath": null,
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "Global_Version.stLibVersion_Tc3_Module?36",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "type": "xsd:float",
                    "pollingTime": 1000
                }
            ]
        },
        "GVL_VAR.temp": {
            "type": "number",
            "opcua:nodeId": null,
            "opcua:type": null,
            "opcua:fieldPath": null,
            "readOnly": true,
            "observable": true,
            "forms": [
                {
                    "href": "GVL_VAR.temp?4",
                    "op": [
                        "readproperty",
                        "observeproperty"
                    ],
                    "type": "xsd:float",
                    "pollingTime": 1000
                }
            ]
        }
    }
}

###
# @on-error
Shutdown_V2
###
```
