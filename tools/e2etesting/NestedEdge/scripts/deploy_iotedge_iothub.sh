#!/usr/bin/env bash

function show_help() {
   # Display Help
   echo "Run this script to deploy baseline workloads on IoT Edge devices"
   echo
   echo "Syntax: ./deploy_iotedge_iothub.sh [-flag parameter]"
   echo ""
   echo "List of mandatory flags:"
   echo "-hubrg            Azure Resource Group with the Azure IoT Hub controlling IoT Edge devices."
   echo "-hubname          Name of the Azure IoT Hub controlling the IoT Edge devices."
   echo ""
   echo "List of optional flags:"
   echo "-h                Print this help."
   echo "-c                Path to configuration file with IoT Edge VMs information. Default: ../config.txt."
   echo "-s                Azure subscription ID to use to deploy resources. Default: use current subscription of Azure CLI."
   echo
}

function getLowestSubnet() {
    deviceSubnets=($@)
    lowestSubnetInt=10
    lowestSubnet=""
    for deviceSubnet in "${deviceSubnets[@]}"
    do
        deviceSubnetInt=$(echo $deviceSubnet | cut -d "-" -f1)
        if [[ $deviceSubnetInt -lt $lowestSubnetInt ]]; then
            lowestSubnetInt=$deviceSubnetInt
            lowestSubnet=$deviceSubnet
        fi
    done
    echo "${lowestSubnet}"
}


#global variable
scriptFolder=$(dirname "$(readlink -f "$0")")

# Default settings
configFilePath="${scriptFolder}/../config.txt"

# Get arguments
while :; do
    case $1 in
        -h|-\?|--help)
            show_help
            exit;;
        -c=?*)
            configFilePath=${1#*=}
            if [ ! -f "${configFilePath}" ]; then
              echo "Configuration file not found. Exiting."
              exit 1
            fi;;
        -s=?*)
            subscription=${1#*=}
            ;;
        -s=)
            echo "Missing subscription id. Exiting."
            exit;;
        -c=)
            echo "Missing configuration file path. Exiting."
            exit;;
        -hubrg=?*)
            iotHubResourceGroup=${1#*=}
            ;;
        -hubrg=)
            echo "Missing IoT Hub resource group. Exiting."
            exit;;
        -hubname=?*)
            iotHubName=${1#*=}
            ;;
        -hubname=)
            echo "Missing IoT Hub name. Exiting."
            exit;;
        --)
            shift
            break;;
        *)
            break
    esac
    shift
done

#Verifying that mandatory parameters are there
if [ -z $iotHubResourceGroup ]; then
    echo "Missing IoT Hub resource group. Exiting."
    exit 1
fi
if [ -z $iotHubName ]; then
    echo "Missing IoT Hub name. Exiting."
    exit 1
fi

# Prepare CLI
if [ ! -z $subscription ]; then
  az account set --subscription $subscription
fi
# subscriptionName=$(az account show --query 'name' -o tsv)
# echo "Executing script with Azure Subscription: ${subscriptionName}"

# Parse the configuration file
source ${scriptFolder}/parseConfigFile.sh $configFilePath

acrEnvFilePath="${scriptFolder}/$acrEnvFilePath"
amlEnvFilePath="${scriptFolder}/$amlEnvFilePath"
topLayerBaseDeploymentTemplateFilePath="${scriptFolder}/$topLayerBaseDeploymentTemplateFilePath"
topLayerBaseDeploymentFilePath="${topLayerBaseDeploymentTemplateFilePath/.template/}"
middleLayerBaseDeploymentTemplateFilePath="${scriptFolder}/$middleLayerBaseDeploymentTemplateFilePath"
middleLayerBaseDeploymentFilePath="${middleLayerBaseDeploymentTemplateFilePath/.template/}"
bottomLayerBaseDeploymentTemplateFilePath="${scriptFolder}/$bottomLayerBaseDeploymentTemplateFilePath"
bottomLayerBaseDeploymentFilePath="${bottomLayerBaseDeploymentTemplateFilePath/.template/}"

echo "==========================================================="
echo "==   Pushing base deployment to all IoT Edge devices     =="
echo "==========================================================="
echo ""

# Verifying that the deployment manifest files are here
if [ -z $acrEnvFilePath ]; then
    echo ".Env file with Azure Container Registry (ACR) credentials is missing from the configuration file. Please verify your configuration file. Exiting."
    exit 1
fi
if [ -z $amlEnvFilePath ]; then
    echo ".Env file with Azure Monitor Ids and Keys is missing from the configuration file. Please verify your configuration file. Exiting."
    exit 1
fi
if [ -z $topLayerBaseDeploymentTemplateFilePath ]; then
    echo "TopLayerBaseDeploymentTemplateFilePath is missing from the configuration file. Please verify your configuration file. Exiting."
    exit 1
fi
if [ -z $middleLayerBaseDeploymentTemplateFilePath ]; then
    echo "MiddleLayerBaseDeploymentTemplateFilePath is missing from the configuration file. Please verify your configuration file. Exiting."
    exit 1
fi
if [ -z $bottomLayerBaseDeploymentTemplateFilePath ]; then
    echo "BottomLayerBaseDeploymentTemplateFilePath is missing from the configuration file. Please verify your configuration file. Exiting."
    exit 1
fi

# Generate top layer base deployment with ACR credentials from template
source $acrEnvFilePath
if [ -z $ACR_ADDRESS ]; then
    echo "ACR_ADDRESS value is missing. Please verify your ACR.env file. Exiting."
    exit 1
fi
if [ -z $ACR_USERNAME ]; then
    echo "ACR_USERNAME value is missing. Please verify your ACR.env file. Exiting."
    exit 1
fi
if [ -z $ACR_PASSWORD ]; then
    echo "ACR_PASSWORD value is missing. Please verify your ACR.env file. Exiting."
    exit 1
fi
export ACR_ADDRESS ACR_USERNAME ACR_PASSWORD

# Substitute AML env vars in deployment files
source $amlEnvFilePath
if [ -z $WORKSPACE_ID ]; then
    echo "WORKSPACE_ID value is missing. Please verify your ACR.env file. Exiting."
    exit 1
fi
if [ -z $WORKSPACE_KEY ]; then
    echo "WORKSPACE_KEY value is missing. Please verify your ACR.env file. Exiting."
    exit 1
fi
if [ -z $IOT_HUB_RESOURCE_ID ]; then
    echo "IOT_HUB_RESOURCE_ID value is missing. Please verify your ACR.env file. Exiting."
    exit 1
fi
export WORKSPACE_ID WORKSPACE_KEY IOT_HUB_RESOURCE_ID
${scriptFolder}/replaceEnv.sh $topLayerBaseDeploymentTemplateFilePath $topLayerBaseDeploymentFilePath 'ACR_ADDRESS' 'ACR_USERNAME' 'ACR_PASSWORD' 'WORKSPACE_ID' 'WORKSPACE_KEY' 'IOT_HUB_RESOURCE_ID'
if [ -z $topLayerBaseDeploymentFilePath ]; then
    echo "TopLayerBaseDeploymentFilePath is missing. It has not been generated properly from its template. Exiting."
    exit 1
fi
${scriptFolder}/replaceEnv.sh $middleLayerBaseDeploymentTemplateFilePath $middleLayerBaseDeploymentFilePath 'WORKSPACE_ID' 'WORKSPACE_KEY' 'IOT_HUB_RESOURCE_ID'
if [ -z $middleLayerBaseDeploymentFilePath ]; then
    echo "MiddleLayerBaseDeploymentFilePath is missing. It has not been generated properly from its template. Exiting."
    exit 1
fi
${scriptFolder}/replaceEnv.sh $bottomLayerBaseDeploymentTemplateFilePath $bottomLayerBaseDeploymentFilePath 'WORKSPACE_ID' 'WORKSPACE_KEY' 'IOT_HUB_RESOURCE_ID'
if [ -z $bottomLayerBaseDeploymentFilePath ]; then
    echo "BottomLayerBaseDeploymentFilePath is missing. It has not been generated properly from its template. Exiting."
    exit 1
fi

# Set modules
bottomLayer=$(getLowestSubnet "${iotEdgeDevicesSubnets[@]}")
i=0
for iotEdgeDevice in "${iotEdgeDevices[@]}"
do
    echo "${iotEdgeDevice}..."
    if [[ ${iotEdgeParentDevices[i]} == "IoTHub" ]]; then
        az iot edge set-modules --device-id $iotEdgeDevice --hub-name $iotHubName --content $topLayerBaseDeploymentFilePath --output none
    elif [[ ${iotEdgeDevicesSubnets[i]} == $bottomLayer ]]; then
        az iot edge set-modules --device-id $iotEdgeDevice --hub-name $iotHubName --content $bottomLayerBaseDeploymentFilePath --output none
    else
        az iot edge set-modules --device-id $iotEdgeDevice --hub-name $iotHubName --content $middleLayerBaseDeploymentFilePath --output none
    fi
    ((i++))
done

rm $topLayerBaseDeploymentFilePath

echo "done"
echo ""
