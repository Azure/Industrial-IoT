#!/usr/bin/env bash

function show_help() {
   # Display Help
   echo "Run this script to populate your Azure Container Registry (ACR) with required container images to use IoT Edge in nested configuration."
   echo
   echo "Syntax: ./import_acr.sh [-flag parameter]"
   echo ""
   echo "List of optional flags:"
   echo "-h      Print this help."
   echo "-c      Path to configuration file with path to file with ACR credentials information. Default: ../config.txt."
   echo "-s      Azure subscription ID to use to deploy resources. Default: use current subscription of Azure CLI."
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
        --)
            shift
            break;;
        *)
            break
    esac
    shift
done


# Prepare CLI
if [ ! -z $subscription ]; then
    az account set --subscription $subscription
fi
# subscriptionName=$(az account show --query 'name' -o tsv)
# echo "Executing script with Azure Subscription: ${subscriptionName}" 

# Parse the configuration file to get the ACR credentials info
source ${scriptFolder}/parseConfigFile.sh $configFilePath
# Verifying that the ACR environment variable file is here
if [ -z $acrEnvFilePath ]; then
    echo ".Env file with Azure Container Registry (ACR) credentials is missing from the configuration file. Please verify your configuration file. Exiting."
    exit 1
fi
acrEnvFilePath="${scriptFolder}/$acrEnvFilePath"

echo "==========================================================="
echo "==          Import container images to your ACR          =="
echo "==========================================================="
echo ""

# Get ACR name
source $acrEnvFilePath
if [ -z $ACR_ADDRESS ]; then
    echo "ACR_ADDRESS value is missing. Please verify your ACR.env file. Exiting."
    exit 1
fi
acrName="${ACR_ADDRESS/.azurecr.io/}"

echo "Importing container images to ACR ${acrName}"
echo ""
echo "edgeAgent..."
az acr import --name $acrName --force --source mcr.microsoft.com/azureiotedge-agent:1.2.0-rc4 --image azureiotedge-agent:1.2.0-rc4
echo "edgeHub..."
az acr import --name $acrName --force --source mcr.microsoft.com/azureiotedge-hub:1.2.0-rc4 --image azureiotedge-hub:1.2.0-rc4
echo "diagnostics..."
az acr import --name $acrName --force --source mcr.microsoft.com/azureiotedge-diagnostics:1.2.0-rc4 --image azureiotedge-diagnostics:1.2.0-rc4
echo "monitor..."
az acr import --name acrqq4mbi.azurecr.io --force --source mcr.microsoft.com/azuremonitor/containerinsights/ciprod:iot-0.1.3.3 --image ciprod:latest
echo "API proxy..."
az acr import --name $acrName --force --source mcr.microsoft.com/azureiotedge-api-proxy:latest --image azureiotedge-api-proxy:latest
echo "IIoT - OpcPlc server ..."
az acr import --name $acrName --force --source mcr.microsoft.com/iotedge/opc-plc:latest --image opc-plc:latest
echo "IoT - OPC Publisher..."
az acr import --name $acrName --force --source mcr.microsoft.com/iotedge/opc-publisher:2.7.206 --image opc-publisher:2.7.206
echo "IIoT - Twin..."
az acr import --name $acrName --force --source mcr.microsoft.com/iotedge/opc-twin:2.7.206 --image opc-twin:2.7.206
echo "IIoT - Discovery ..."
az acr import --name $acrName --force --source mcr.microsoft.com/iotedge/discovery:2.7.206 --image discovery:2.7.206

echo "...done"
echo ""
