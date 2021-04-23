#!/usr/bin/env bash

function show_help() {
   # Display Help
   echo "Run this script to deploy multiple Azure VMs with Azure IoT Edge pre-installed across a simulated Purdue Network in Azure."
   echo
   echo "Syntax: ./deploy_iotedge_vms.sh [-flag parameter]"
   echo ""
   echo "Required list of flags:"
   echo "-sshPublicKeyPath     File path to SSH public key that should be used to connect to the IoT Edge VMs from the jump box. Install script auto-generate this key from the jump box."
   echo ""
   echo "List of optional flags:"
   echo "-h                Print this help."
   echo "-adminUsername    Administrator username of the Azure VMs to deploy. Default: iotedgeadmin."
   echo "-c                Path to configuration file with IoT Edge devices information. Default: ../config.txt."
   echo "-l                Azure region to deploy resources to. Default: eastus."
   echo "-n                Name of the Azure Virtual Network with the Purdue Network. Default: PurdueNetwork."
   echo "-nrg              Azure Resource Group with the Purdue Network. Default: {rg}-RG-network."
   echo "-rg               Prefix used for all new Azure Resource Groups created by this script. Default: iotedge4iiot."
   echo "-s                Azure subscription ID to use to deploy resources. Default: use current subscription of Azure CLI."
   echo "-vmSize           Size of the Azure VMs to deploy. Default: Standard_B1ms."
   echo ""
}

function passArrayToARM() {
   array=("$@")
   output="["
   i=0
   for item in "${array[@]}"
   do
        if [[ $i -eq 0 ]]
        then
            output="${output}\"${item}\""
        else
            output="${output}, \"${item}\""
        fi
        ((i++))
   done
   output="${output}]"
   echo ${output}
}

#global variable
scriptFolder=$(dirname "$(readlink -f "$0")")

# Default settings
location="eastus"
resourceGroupPrefix="iotedge4iiot"
networkName="PurdueNetwork"
configFilePath="${scriptFolder}/../config.txt"
adminUsername="iotedgeadmin"
vmSize="Standard_B1ms" #"Standard_D3_v2"

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
        -n=?*)
            networkName=${1#*=}
            ;;
        -n=)
            echo "Missing network name. Exiting."
            exit;;
        -l=?*)
            location=${1#*=}
            ;;
        -l=)
            echo "Missing location. Exiting."
            exit;;
        -s=?*)
            subscription=${1#*=}
            ;;
        -s=)
            echo "Missing subscription. Exiting."
            exit;;
        -rg=?*)
            resourceGroupPrefix=${1#*=}
            ;;
        -rg=)
            echo "Missing resource group prefix. Exiting."
            exit;;
        -nrg=?*)
            networkResourceGroupName=${1#*=}
            ;;
        -nrg=)
            echo "Missing network resourge group. Exiting."
            exit;;
        -vmSize=?*)
            vmSize=${1#*=}
            ;;
        -vmSize=)
            echo "Missing vmSize. Exiting."
            exit;;
        -adminUsername=)
            echo "Missing VM adminitrator username. Exiting."
            exit;;
        -adminUsername=?*)
            adminUsername=${1#*=}
            ;;
        -sshPublicKeyPath=)
            echo "Missing path to jump box SSH public key. Exiting."
            exit;;
        -sshPublicKeyPath=?*)
            sshPublicKeyPath=${1#*=}
            ;;
        --)
            shift
            break;;
        *)
            break
    esac
    shift
done

# Derived default settings
networkResourceGroupName="${resourceGroupPrefix}-RG-network"
iotedgeResourceGroupName="${resourceGroupPrefix}-RG-iotedge"

#Verifying that mandatory parameters are there
if [ -z ${configFilePath} ]; then
    echo "Missing configuration file path. Exiting."
    exit 1
fi
if [ -z $sshPublicKeyPath ]; then
    echo "Missing file path to SSH public key. Exiting."
    exit 1
fi

# Prepare CLI
if [ ! -z $subscription ]; then
  az account set --subscription $subscription
fi
# subscriptionName=$(az account show --query 'name' -o tsv)
# echo "Executing script with Azure Subscription: ${subscriptionName}" 

echo "==========================================================="
echo "==	            IoT Edge VMs                         =="
echo "==========================================================="
echo ""

if ( $(az group exists -n "$iotedgeResourceGroupName") )
then
  echo "Existing IoT Edge VMs resource group found: $iotedgeResourceGroupName"
else
  az group create --name "$iotedgeResourceGroupName" --location "$location" --tags "$resourceGroupPrefix" "CreationDate"=$(date --utc +%Y%m%d_%H%M%SZ)  1> /dev/null
  echo "IoT Edge VMs Resource group: $iotedgeResourceGroupName"
fi
echo ""

# Load IoT Edge VMs to deploy from config file
source ${scriptFolder}/parseConfigFile.sh $configFilePath

#Deploy IoT Edge VMs
iotedgeDeployFilePath="${scriptFolder}/ARM-templates/iotedgedeploy.json"
sshPublicKey=$(cat $sshPublicKeyPath)
iotedgeVMsOutput=$(az deployment group create --name iotedgeDeployment --resource-group $iotedgeResourceGroupName --template-file "$iotedgeDeployFilePath" --parameters \
        networkName="$networkName" networkResourceGroupName="$networkResourceGroupName" subnetNames="$(passArrayToARM ${iotEdgeDevicesSubnets[@]})" \
        machineNames="$(passArrayToARM ${iotEdgeDevices[@]})" machineAdminName="$adminUsername" machineAdminSshPublicKey="${sshPublicKey}" vmSize="$vmSize" \
        --query "properties.outputs.vms.value[].[iotEdgeVmMachineName, iotEdgeVmMachinePrivateIPAddress, iotEdgeVmAdminUsername]" -o tsv)

echo "IoT Edge VMs created. Key values:"
echo ""
iotedgeVMsOutputs=($iotedgeVMsOutput)
for (( i=0; i<${#iotedgeVMsOutputs[@]}-1; i+=3))
do
    echo ""
    echo "IoT Edge VM machine name:       ${iotedgeVMsOutputs[i]}"
    echo "IoT Edge VM private IP address: ${iotedgeVMsOutputs[i+1]}"
    echo "IoT Edge VM username:           ${iotedgeVMsOutputs[i+2]}"
    echo "IoT Edge VM SSH:                ssh ${iotedgeVMsOutputs[i+2]}@${iotedgeVMsOutputs[i]}"
done
echo ""
echo ""
echo "IoT Edge VMs can still access internet at this point to enable their configuration."
echo "Please wait until completion of the install script or run the lockdown_purdue.sh script manually to lock down the Purdue network."
echo ""