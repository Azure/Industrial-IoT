#!/usr/bin/env bash

function show_help() {
   # Display Help
   echo "Run this script to simulate a loss of internet connectivity on Layer 5 of the Purdue Network, or restablish lost connectivity."
   echo
   echo "Syntax: ./goOffline.sh [-flag parameter]"
   echo "-nrg        Azure Resource Group with the Purdue Network."
   echo "-hubname    Name of the Azure IoT Hub controlling the IoT Edge devices."
   echo ""
   echo "List of optional flags:"
   echo "-h          Print this help."
   echo "-b          On/Off button. Set to true to go offline, set to false to go back online. Default: true."
   echo "-c          Path to configuration file with IoT Edge devices information. Default: ../config.txt."
   echo "-s          Azure subscription ID to use to deploy resources. Default: use current subscription of Azure CLI."
   echo ""
}

function toLowerWithoutSpecialChars () {
    echo $1 | tr -dc '[:alnum:]\n\r' | tr '[:upper:]' '[:lower:]'
}

function vmNameSuffix () {
    suffix=$( echo $1 | cut -d '-' -f 2 )
    echo "${suffix}"
}

function vmNameToNsgName () {
    suffix=$( vmNameSuffix $1 )
    echo "nsg-${suffix}"
}

#global variable
scriptFolder=$(dirname "$(readlink -f "$0")")

# Default settings
configFilePath="${scriptFolder}/../config.txt"

# Default settings
button=true

# Get arguments
while :; do
    case $1 in
        -h|-\?|--help)
            show_help
            exit;;
        -b=?*)
            button=${1#*=}
            ;;
        -b=)
            echo "Missing on/off button. Exiting."
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
            echo "Missing subscription. Exiting."
            exit;;
        -hubname=?*)
            hubName=${1#*=}
            ;;
        -hubname=)
            echo "Missing IoT Hub name. Exiting."
            exit;;
        -nrg=?*)
            networkResourceGroupName=${1#*=}
            ;;
        -nrg=)
            echo "Missing network resourge group. Exiting."
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
if [ -z $networkResourceGroupName ]; then
    echo "Missing network resource group. Exiting."
    exit 1
fi
if [ -z $hubName ]; then
    echo "Missing IoT Hub name. Exiting."
    exit 1
fi

# Prepare CLI
if [ ! -z $subscription ]; then
  az account set --subscription $subscription
fi
# subscriptionName=$(az account show --query 'name' -o tsv)
# echo "Executing script with Azure Subscription: ${subscriptionName}" 

# Load IoT Edge devices from config file
source ${scriptFolder}/parseConfigFile.sh $configFilePath


echo "==========================================================="
echo "==	    Simulating Internet Connectivity          	  =="
echo "==========================================================="

echo ""

nsgName="PurdueNetwork-L5-nsg"
if [ ${button} = true ]; then
    echo "L5 going offline..."
    az network nsg rule create \
    --resource-group $networkResourceGroupName \
    --nsg-name $nsgName \
    --name "Offline" \
    --direction Outbound \
    --protocol '*' \
    --priority 100 \
    --access deny \
    --source-address-prefixes '*' \
    --destination-address-prefixes '*' \
    --destination-port-range '*' \
    --output none
else
    echo "L5 going back online..."
    az network nsg rule delete \
    --resource-group $networkResourceGroupName \
    --nsg-name $nsgName \
    --name "Offline"
fi
echo "...done."

# Cutting existing connections by restarting edgeHub so that more restrictive changes become effective
if [ ${button} = true ]; then
    echo "Restarting the edgeHub of devices in the top layer (L5) so that connectivity changes become effective..."
    for (( i=0; i<${#iotEdgeDevices[@]}; i++))
    do
        if [[ ${iotEdgeParentDevices[i]} == "IoTHub" ]]; then
            echo "${iotEdgeDevices[i]}..."
            az iot hub invoke-module-method \
            --method-name 'RestartModule' \
            --hub-name $hubName \
            --device-id ${iotEdgeDevices[i]} \
            --module-id '$edgeAgent' \
            --output none \
            --method-payload \
            '
                {
                    "schemaVersion": "1.0",
                    "id": "edgeHub"
                }
            '
        fi
    done
    echo "...done"
    echo ""
fi

if [ ${button} = true ]; then
    echo "All IoT Edge devices in the top layer (L5) are now offline and cannot communicate with the internet."
else
    echo "All IoT Edge devices in the top layer (L5) are now online and can communicate with the internet."
fi
