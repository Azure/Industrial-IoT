#!/usr/bin/env bash

configFilePath=$1

iotEdgeDevices=()
iotEdgeDevicesSubnets=()
iotEdgeParentDevices=()
iiotAssets=()
iiotAssetsSubnets=()
acrEnvFilePath=""
amlEnvFilePath=""
topLayerBaseDeploymentTemplateFilePath=""
middleLayerBaseDeploymentTemplateFilePath=""
bottomLayerBaseDeploymentTemplateFilePath=""
rootCA=""

while read line
do
    if [ "${line:0:1}" == "#" ]; then
        continue
    fi
    if [ "${line:0:14}" == "AcrEnvFilePath" ]; then
        acrEnvFilePath=$(echo ${line:2} | cut -d ":" -f2- | cut -d' ' -f 2 )
        continue
    fi
    if [ "${line:0:14}" == "AmlEnvFilePath" ]; then
        amlEnvFilePath=$(echo ${line:2} | cut -d ":" -f2- | cut -d' ' -f 2 )
        continue
    fi
    if [ "${line:0:38}" == "TopLayerBaseDeploymentTemplateFilePath" ]; then
        topLayerBaseDeploymentTemplateFilePath=$(echo ${line:2} | cut -d ":" -f2- | cut -d' ' -f 2 )
        continue
    fi
    if [ "${line:0:41}" == "MiddleLayerBaseDeploymentTemplateFilePath" ]; then
        middleLayerBaseDeploymentTemplateFilePath=$(echo ${line:2} | cut -d ":" -f2- | cut -d' ' -f 2 )
        continue
    fi
    if [ "${line:0:41}" == "BottomLayerBaseDeploymentTemplateFilePath" ]; then
        bottomLayerBaseDeploymentTemplateFilePath=$(echo ${line:2} | cut -d ":" -f2- | cut -d' ' -f 2 )
        continue
    fi
    if [ "${line:0:6}" == "RootCA" ]; then
        rootCA=$(echo $line | cut -d ":" -f2- | cut -d' ' -f 2 )
        continue
    fi
    i=0
    substrings=$(echo $line | tr ":" "\n")
    for substring in ${substrings[@]}; do
        if [ $i = 0 ]; then
            subnet=$substring
        else
            devices=$(echo $substring | tr " " "\n")
            for deviceWithParent in ${devices[@]}; do
                device=$(echo $deviceWithParent | cut -d "(" -f1)
                parent=$(echo $deviceWithParent | cut -d "(" -f2 | cut -d ")" -f1)
                if [[ ! "$parent" =~ ^(OPC-UA|OPCUA|OPC-UA-1|OPC-UA-2)$ ]]; then
                    iotEdgeDevicesSubnets+=($subnet)
                    iotEdgeDevices+=($device)
                    if [[ $device == $parent ]]; then
                        iotEdgeParentDevices+="IoTHub"
                    else
                        iotEdgeParentDevices+=($parent)
                    fi
                    else
                        iiotAssetsSubnets+=($subnet)
                        iiotAssets+=($device)
                fi
            done
        fi
        ((i++))
    done
done < $configFilePath

if [ ${#iotEdgeDevicesSubnets[@]} -ne ${#iotEdgeDevices[@]} ] && [ ${#iotEdgeDevicesSubnets[@]} -ne ${#iotEdgeParentDevices[@]} ] && [ ${#iiotAssetsSubnets[@]} -ne ${#iiotAssets[@]} ]
then
    echo "Error when parsing the configuration file. Please review the syntax of your configuration file."
    exit 1
fi