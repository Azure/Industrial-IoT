#!/usr/bin/env bash

function show_help() {
   # Display Help
   echo "Run this script to lockdown the Purdue Network. Each layers will then only have access to adjacent north and south layers."
   echo
   echo "Syntax: ./lockdown_purdue.sh [-flag parameter]"
   echo "-nrg              Azure Resource Group with the Purdue Network."
   echo ""
   echo "List of optional flags:"
   echo "-h                Print this help."
   echo "-s                Azure subscription ID to use to deploy resources. Default: use current subscription of Azure CLI."
   echo ""
}

# Get arguments
while :; do
    case $1 in
        -h|-\?|--help)
            show_help
            exit;;
        -n=?*)
            networkName=${1#*=}
            ;;
        -n=)
            echo "Missing network name. Exiting."
            exit;;
        -s=?*)
            subscription=${1#*=}
            ;;
        -s=)
            echo "Missing subscription. Exiting."
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

# Prepare CLI
if [ ! -z $subscription ]; then
  az account set --subscription $subscription
fi
# subscriptionName=$(az account show --query 'name' -o tsv)
# echo "Executing script with Azure Subscription: ${subscriptionName}" 

echo "==========================================================="
echo "==	        Locking down Purdue Network      	  =="
echo "==========================================================="

echo "Removing rules with Prefix 'ToRemove_'..."
echo ""
nsgListOutput=($(az network nsg list --resource-group $networkResourceGroupName --query '[].name' -o tsv))

rulesToDelete=""
for nsgName in "${nsgListOutput[@]}" 
do
    echo "...$nsgName"
	rulesToRemoveIds=($(az network nsg rule list --resource-group $networkResourceGroupName --nsg-name $nsgName --query "[?name.contains(@, 'ToRemove_')].id" -o tsv))
	for ruleId in "${rulesToRemoveIds[@]}" 
	do
		rulesToDelete="$rulesToDelete $ruleId"
	done
done
az network nsg rule delete --ids $rulesToDelete --output none

echo "done."
echo ""
echo "VMs in lower layers no longer have internet access."
