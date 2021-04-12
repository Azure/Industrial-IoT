#!/usr/bin/env bash

function show_help() {
   # Display Help
   echo "Run this script to provision IoT Edge devices in IoT Hub."
   echo
   echo "Syntax: ./provision_iotedge_iothub.sh [-flag parameter]"
   echo ""
   echo "List of mandatory flags:"
   echo "-hubrg            Azure Resource Group with the Azure IoT Hub controlling IoT Edge devices."
   echo "-hubname          Name of the Azure IoT Hub controlling the IoT Edge devices."
   echo ""
   echo "List of optional flags:"
   echo "-h                Print this help."
   echo "-c                Path to configuration file with IoT Edge devices provisioning information. Default: ../config.txt."
   echo "-s                Azure subscription ID where resources have been deployed. Default: use current subscription of Azure CLI."
   echo
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
        -c=)
            echo "Missing configuration file path. Exiting."
            exit;;
        -s=?*)
            subscription=${1#*=}
            ;;
        -s=)
            echo "Missing subscription id. Exiting."
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

# Load IoT Edge devices to create in IoT Hub from config file
source ${scriptFolder}/parseConfigFile.sh $configFilePath

echo "==========================================================="
echo "==   Creating IoT Edge devices in IoT Hub       =="
echo "==========================================================="

for (( i=0; i<${#iotEdgeDevices[@]}; i++))
do
    echo "${iotEdgeDevices[i]}..."
    if [[ ${iotEdgeParentDevices[i]} == "IoTHub" ]]; then
        az iot hub device-identity create -n $iotHubName -d ${iotEdgeDevices[i]} --ee --output none
    else
        az iot hub device-identity create -n $iotHubName -d ${iotEdgeDevices[i]} --ee --pd ${iotEdgeParentDevices[i]} --output none
    fi
done
echo "...done"
echo ""