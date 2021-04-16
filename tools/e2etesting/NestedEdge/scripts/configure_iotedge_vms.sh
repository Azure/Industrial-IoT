#!/usr/bin/env bash

function show_help() {
   # Display Help
   echo "Run this script to configure multiple Azure VMs running Azure IoT Edge."
   echo
   echo "Syntax: ./configure_iotedge_vms.sh [-flag parameter]"
   echo ""
   echo "List of mandatory flags:"
   echo "-edgerg           Azure Resource Group with the Azure IoT Edge VMs."
   echo "-hubrg            Azure Resource Group with the Azure IoT Hub controlling IoT Edge devices."
   echo "-hubname          Name of the Azure IoT Hub controlling the IoT Edge devices."
   echo ""
   echo "List of optional flags:"
   echo "-h                Print this help."
   echo "-c                Path to configuration file with IoT Edge VMs information. Default: ../config.txt."
   echo "-s                Azure subscription ID to use to deploy resources. Default: use current subscription of Azure CLI."
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
        -edgerg=?*)
            iotEdgeVMsResourceGroup=${1#*=}
            ;;
        -edgerg=)
            echo "Missing IoT Edge VMs resource group. Exiting."
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
if [ -z $iotEdgeVMsResourceGroup ]; then
    echo "Missing IoT Edge VMs resource group. Exiting."
    exit 1
fi
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

# Load IoT Edge VMs to deploy from config file
source ${scriptFolder}/parseConfigFile.sh $configFilePath

# Verifying that the ACR environment variable file is here
if [ -z $acrEnvFilePath ]; then
    echo ".Env file with Azure Container Registry (ACR) credentials is missing from the configuration file. Please verify your configuration file. Exiting."
    exit 1
fi
acrEnvFilePath="${scriptFolder}/$acrEnvFilePath"

# Get ACR name
source $acrEnvFilePath
if [[ -z $ACR_ADDRESS || -z $ACR_USERNAME || -z $ACR_PASSWORD ]]; then
    echo "One or more of ACR_ADDRESS, ACR_USERNAME or ACR_PASSWORD have empty values. Please verify your ACR.env file. Exiting."
    exit 1
fi

echo "==========================================================="
echo "==   Configuring certificates for IoT Edge devices       =="
echo "==========================================================="

# Get IoT Edge Ip Addresses
echo "Getting devices internal IP addresses from Azure..."
iotedgeVmIpAddressesQueryResults=($(az vm list-ip-addresses --ids $(az vm list -g $iotEdgeVMsResourceGroup --query "[].id" -o tsv) --query '[].[virtualMachine.name, virtualMachine.network.privateIpAddresses[0]]' -o tsv))

iotEdgeDevicesIpAddresses=()
iotEdgeParentDevicesIpAddresses=()

for iotEdgeDevice in "${iotEdgeDevices[@]}"
do
    for (( i=0; i<${#iotedgeVmIpAddressesQueryResults[@]}-1; i+=2))
    do
        if [[ ${iotedgeVmIpAddressesQueryResults[i]} == ${iotEdgeDevice} ]]; then
            iotEdgeDevicesIpAddresses+=(${iotedgeVmIpAddressesQueryResults[i+1]})
        fi
    done
done
for iotEdgeParentDevice in "${iotEdgeParentDevices[@]}"
do
    if [[ ${iotEdgeParentDevice} == "IoTHub" ]]; then
        iotEdgeParentDevicesIpAddresses+=("")
        continue
    fi
    for (( i=0; i<${#iotedgeVmIpAddressesQueryResults[@]}-1; i+=2))
    do
        if [[ ${iotedgeVmIpAddressesQueryResults[i]} == ${iotEdgeParentDevice} ]]; then
            iotEdgeParentDevicesIpAddresses+=(${iotedgeVmIpAddressesQueryResults[i+1]})
        fi
    done
done

if [ ${#iotEdgeDevicesIpAddresses[@]} -ne ${#iotEdgeDevices[@]} ] && [ ${#iotEdgeParentDevicesIpAddresses[@]} -ne ${#iotEdgeParentDevices[@]} ]
then
    echo "Error when parsing the getting the IP addresses of each IoT Edge devices."
    exit 1
fi

echo "...done"
echo ""

# TODO2: Update the cert script to use a root ca from a specific location
echo "Root certificate CA used for all devices: ${rootCA}"
echo ""
# Setup certificates
for (( i=0; i<${#iotEdgeDevices[@]}; i++))
do
    echo "...${iotEdgeDevices[$i]}"
    az vm extension set \
    --resource-group $iotEdgeVMsResourceGroup \
    --vm-name ${iotEdgeDevices[$i]} \
    --name customScript \
    --publisher Microsoft.Azure.Extensions \
    --settings '{"fileUris": ["https://raw.githubusercontent.com/Azure/Industrial-IoT/feature/nested_edge/tools/e2etesting/NestedEdge/scripts/CustomScripts/installTestCertificates.sh"],"commandToExecute": "./installTestCertificates.sh \"'${iotEdgeDevices[$i]}'\""}' \
    --output none
done
echo "done"
echo ""

echo "==========================================================="
echo "==   Provisioning devices and configuring their parents  =="
echo "==========================================================="

# Get device connection strings
echo "Getting devices connection strings from IoT Hub..."
deviceConnectionStrings=()
for iotEdgeDevice in "${iotEdgeDevices[@]}"
do
    echo "${iotEdgeDevice}..."
    dcs=$(az iot hub device-identity connection-string show --device-id ${iotEdgeDevice} --hub-name ${iotHubName} -g ${iotHubResourceGroup} --query 'connectionString' -o tsv)
    deviceConnectionStrings+=( $dcs )
done
echo "...done"
echo ""

# Update configuration file
echo "Configuring device connection strings, hostnames and parent hostnames for IoT Edge devices..."
for (( i=0; i<${#iotEdgeDevices[@]}; i++))
do
    if [ ${iotEdgeDevicesSubnets[i]} == "7-L5-IT-EnterpriseNetwork" ]; then
        echo "...${iotEdgeDevices[$i]}"
        az vm extension set \
        --resource-group $iotEdgeVMsResourceGroup \
        --vm-name ${iotEdgeDevices[$i]} \
        --name customScript \
        --publisher Microsoft.Azure.Extensions \
        --settings '{"fileUris": ["https://raw.githubusercontent.com/Azure/Industrial-IoT/feature/nested_edge/tools/e2etesting/NestedEdge/scripts/CustomScripts/updateOtherDaemonConfigs.sh"],"commandToExecute": "./updateOtherDaemonConfigs.sh \"'${deviceConnectionStrings[$i]}'\" \"'${iotEdgeDevices[$i]}'\" \"'${iotEdgeDevicesIpAddresses[$i]}'\" \"'https_proxy=http://10.16.8.4:3128'\" \"'${ACR_ADDRESS}'\" \"'${ACR_USERNAME}'\" \"'${ACR_PASSWORD}'\""}' \
        --output none \
        --no-wait
    elif [ ${iotEdgeDevicesSubnets[i]} == "4-L3-OT-SiteOperations" ]; then
        echo "...${iotEdgeDevices[$i]}"
        az vm extension set \
        --resource-group $iotEdgeVMsResourceGroup \
        --vm-name ${iotEdgeDevices[$i]} \
        --name customScript \
        --publisher Microsoft.Azure.Extensions \
        --settings '{"fileUris": ["https://raw.githubusercontent.com/Azure/Industrial-IoT/feature/nested_edge/tools/e2etesting/NestedEdge/scripts/CustomScripts/updateOtherDaemonConfigs.sh"],"commandToExecute": "./updateOtherDaemonConfigs.sh \"'${deviceConnectionStrings[$i]}'\"  \"'${iotEdgeDevices[$i]}'\" \"'${iotEdgeDevicesIpAddresses[$i]}'\" \"'${iotEdgeParentDevicesIpAddresses[$i]}'\" \"'https_proxy=http://10.16.5.4:3128'\""}' \
        --output none \
        --no-wait
    else
        echo "...${iotEdgeDevices[$i]}"
        az vm extension set \
        --resource-group $iotEdgeVMsResourceGroup \
        --vm-name ${iotEdgeDevices[$i]} \
        --name customScript \
        --publisher Microsoft.Azure.Extensions \
        --settings '{"fileUris": ["https://raw.githubusercontent.com/Azure/Industrial-IoT/feature/nested_edge/tools/e2etesting/NestedEdge/scripts/CustomScripts/updateOtherDaemonConfigs.sh"],"commandToExecute": "./updateOtherDaemonConfigs.sh \"'${deviceConnectionStrings[$i]}'\"  \"'${iotEdgeDevices[$i]}'\" \"'${iotEdgeDevicesIpAddresses[$i]}'\" \"'${iotEdgeParentDevicesIpAddresses[$i]}'\""}' \
        --output none \
        --no-wait
    fi
done
echo "done"
echo ""