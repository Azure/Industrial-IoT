#!/usr/bin/env bash

#TODO2: Deploy Proxy or not in Purdue model depending on config file

function show_help() {
   # Display Help
   echo "Run this script to simulate a Purdue Network in Azure."
   echo
   echo "Syntax: ./deploy_purdue.sh [-flag parameter]"
   echo ""
   echo "Required list of flags:"
   echo "-sshPublicKeyPath Path to the SSH public key that should be used to connect to the jump box, which is the entry point to the Purdue network."
   echo ""
   echo "List of optional flags:"
   echo "-h                       Print this help."
   echo "-a                       Prefix of the IP addresses used by all subnets in the Purdue Network. Default: 10.16."
   echo "-l                       Azure region to deploy resources to. Default: eastus."
   echo "-n                       Name of the Azure Virtual Network with the Purdue Network. Default: PurdueNetwork."
   echo "-rg                      Prefix used for all new Azure Resource Groups created by this script. Default: iotedge4iiot."
   echo "-s                       Azure subscription ID to use to deploy resources. Default: use current subscription of Azure CLI."
   echo "-vmSize                  Size of the Azure VMs to deploy. Default: Standard_B1ms."
   echo
}

# Default settings
location="eastus"
resourceGroupPrefix="iotedge4iiot"
networkName="PurdueNetwork"
addressPrefix="10.16"
vmSize="Standard_B1ms" #"Standard_D3_v2"

# Get arguments
while :; do
    case $1 in
        -h|-\?|--help)
            show_help
            exit;;
        -a=?*)
            addressPrefix=${1#*=}
            ;;
        -a=)
            echo "Missing subnet address prefix. Exiting."
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
        -vmSize=?*)
            vmSize=${1#*=}
            ;;
        -vmSize=)
            echo "Missing vmSize. Exiting."
            exit;;
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
supportResourceGroupName="${resourceGroupPrefix}-RG-support"
jumpboxResourceGroupName="${resourceGroupPrefix}-RG-jumpbox"
proxyResourceGroupName="${resourceGroupPrefix}-RG-proxy"

#Verifying that mandatory parameters are there
if [ -z $sshPublicKeyPath ]; then
    echo "Missing path to jump box SSH public key. Exiting."
    exit 1
fi

#global variable
scriptFolder=$(dirname "$(readlink -f "$0")")

# Prepare CLI
if [ ! -z $subscription ]; then
  az account set --subscription $subscription
fi
# subscriptionName=$(az account show --query 'name' -o tsv)
# echo "Executing script with Azure Subscription: ${subscriptionName}"

echo "==========================================================="
echo "==	                Purdue Network                   =="
echo "==========================================================="
echo ""

# Deploy Network
if ( $(az group exists -n "$networkResourceGroupName") )
then
  echo Existing resource group "$networkResourceGroupName" found
else
  az group create --name "$networkResourceGroupName" --location "$location" --tags "$resourceGroupPrefix" "CreationDate"=$(date --utc +%Y%m%d_%H%M%SZ) 1> /dev/null
  echo "Resource group: $networkResourceGroupName"
fi


networkDeployFilePath="${scriptFolder}/ARM-templates/networkdeploy.json"
networkOutput=$(az deployment group create --name PurdueEnvironmentDeployment --resource-group "$networkResourceGroupName" --template-file "$networkDeployFilePath" --parameters \
 networkName="$networkName" addressPrefix="$addressPrefix" \
 --query "properties.outputs.[l0SubnetName.value, l0SubnetCidr.value, l1SubnetName.value, l1SubnetCidr.value, l2SubnetName.value, l2SubnetCidr.value, l3SubnetName.value, l3SubnetCidr.value, otDmzSubnetName.value, otDmzSubnetCidr.value, l4SubnetName.value, l4SubnetCidr.value, l5SubnetName.value, l5SubnetCidr.value, itDmzSubnetName.value, itDmzSubnetCidr.value, supportSubnetName.value, supportSubnetCidr.value]" -o tsv)

echo "Purdue Network created. Key values:"
echo ""
networkOutputs=($networkOutput)
echo "Support layer: ${networkOutputs[16]} (${networkOutputs[17]})"
echo "IT DMZ layer : ${networkOutputs[14]} (${networkOutputs[15]})"
echo "L5 layer     : ${networkOutputs[12]} (${networkOutputs[13]})"
echo "L4 layer     : ${networkOutputs[10]} (${networkOutputs[11]})"
echo "OT DMZ layer : ${networkOutputs[8]} (${networkOutputs[9]})"
echo "L3 layer     : ${networkOutputs[6]} (${networkOutputs[7]})"
echo "L2 layer     : ${networkOutputs[4]} (${networkOutputs[5]})"
echo "L1 layer     : ${networkOutputs[2]} (${networkOutputs[3]})"
echo "L0 layer     : ${networkOutputs[0]} (${networkOutputs[1]})"
echo ""

echo "==========================================================="
echo "==	                     Jump box                  =="
echo "==========================================================="
echo ""

if ( $(az group exists -n "$jumpboxResourceGroupName") )
then
  echo "Existing jumpbox resource group found: $jumpboxResourceGroupName "
else
  az group create --name "$jumpboxResourceGroupName" --location "$location" --tags "$resourceGroupPrefix"  "CreationDate"=$(date --utc +%Y%m%d_%H%M%SZ)  1> /dev/null
  echo "Jumpbox resource group: $jumpboxResourceGroupName"
fi

jumpboxDeployFilePath="${scriptFolder}/ARM-templates/jumpboxdeploy.json"
sshPublicKey=$(eval cat $sshPublicKeyPath)
jumpBoxOutput=$(az deployment group create --name PurdueJumpBoxDeployment --resource-group ${jumpboxResourceGroupName} --template-file "$jumpboxDeployFilePath" --parameters \
    networkName="${networkName}" subnetName='999-Demo-Support' networkResourceGroupName="${networkResourceGroupName}" machineName="jumpbox" machineAdminSshPublicKey="${sshPublicKey}" vmSize="$vmSize"\
    --query "properties.outputs.[adminUsername.value, fqdn.value]" -o tsv)

jumpBoxOutputs=($jumpBoxOutput)
jumpBoxUser=${jumpBoxOutputs[0]}
echo $jumpBoxUser > /tmp/jumpBoxUser
jumpBoxFullyQualifiedName=${jumpBoxOutputs[1]}
echo $jumpBoxFullyQualifiedName > /tmp/jumpBoxFQDN
jumpboxSSH="ssh $jumpBoxUser@$jumpBoxFullyQualifiedName"

# Creating SSH key pair to connect from the jump box to VMs within the Purdue network
runCommandOutput=$(az vm run-command invoke -g ${jumpboxResourceGroupName} -n jumpbox --command-id RunShellScript --scripts "sudo -u jbadmin ssh-keygen -m PEM -t rsa -b 4096 -f /home/jbadmin/.ssh/id_rsa -q -N "\"\"" && sudo -u jbadmin cat /home/jbadmin/.ssh/id_rsa.pub" --query "value[].message" -o tsv)
jbSshPublicKey=$(echo ${runCommandOutput} | grep -o -P '(?<=\[stdout\]\ ).*(?=\ \[stderr\])')
rm ${scriptFolder}/.jbSshPublicKey 2> /dev/null
echo "$jbSshPublicKey" >> "${scriptFolder}/.jbSshPublicKey"

echo "Jump box created. Key values:"
echo ""
echo "Jump box username: $jumpBoxUser"
echo "Jump box FQDN:     $jumpBoxFullyQualifiedName"
echo "Jump box SSH:      $jumpboxSSH"

echo ""
echo "==========================================================="
echo "==	               IT & OT Proxies                 =="
echo "==========================================================="
echo ""

if ( $(az group exists -n "$proxyResourceGroupName") )
then
  echo "Existing proxies resource group found: $proxyResourceGroupName"
else
  az group create --name "$proxyResourceGroupName" --location "$location" --tags "$resourceGroupPrefix" "CreationDate"=$(date --utc +%Y%m%d_%H%M%SZ)  1> /dev/null
  echo "Proxies resource group: $proxyResourceGroupName "
fi

proxyDeployFilePath="${scriptFolder}/ARM-templates/proxydeploy.json"
proxyOutputs=($(az deployment group create --name PurdueProxyDeployment --resource-group ${proxyResourceGroupName} --template-file "$proxyDeployFilePath" --parameters \
    networkResourceGroupName="${networkResourceGroupName}" networkName="${networkName}" itProxySubnetName='8-IT-Dmz' otProxySubnetName='5-OT-Dmz' \
    itProxyMachineName="itproxy" otProxyMachineName="otproxy" itProxyMachineAdminSshPublicKey="${jbSshPublicKey}" otProxyMachineAdminSshPublicKey="${jbSshPublicKey}"  vmSize="$vmSize"\
    --query "properties.outputs.[itProxyMachineName.value, itProxyPrivateIpAddress.value, itProxyAdminUsername.value, otProxyMachineName.value, otProxyPrivateIpAddress.value, otProxyAdminUsername.value]" -o tsv))

itProxyMachineName=${proxyOutputs[0]}
itProxyPrivateIpAddress=${proxyOutputs[1]}
itProxyUser=${proxyOutputs[2]}
otProxyMachineName=${proxyOutputs[3]}
otProxyPrivateIpAddress=${proxyOutputs[4]}
otProxyUser=${proxyOutputs[5]}
otProxySSH="ssh $otProxyUser@$otProxyMachineName"
itProxySSH="ssh $itProxyUser@$itProxyMachineName"

echo "IT & OT Proxies created. Key values:"
echo ""
echo "IT Proxy username:           $itProxyUser"
echo "IT Proxy machine name:       $itProxyMachineName"
echo "IT Proxy SSH:                $itProxySSH"
echo "IT Proxy private IP address: $itProxyPrivateIpAddress"
echo "IT Proxy HTTP_PROXY:         http_proxy=http://$itProxyPrivateIpAddress:3128"
echo "IT Proxy HTTPS_PROXY:        https_proxy=http://$itProxyPrivateIpAddress:3128"
echo ""
echo "OT Proxy username:           $otProxyUser"
echo "OT Proxy machine Name:       $otProxyMachineName"
echo "OT Proxy SSH:                $otProxySSH"
echo "OT Proxy private IP address: $otProxyPrivateIpAddress"
echo "OT Proxy HTTP_PROXY:         http_proxy=http://$otProxyPrivateIpAddress:3128"
echo "OT Proxy HTTPS_PROXY:        https_proxy=http://$otProxyPrivateIpAddress:3128"
echo ""
echo "Proxy VMs can still access internet at this point to enable their configuration."
echo "Please wait until completion of the install script or run the lockdown_purdue.sh script manually to lock down the Purdue network."
echo ""

