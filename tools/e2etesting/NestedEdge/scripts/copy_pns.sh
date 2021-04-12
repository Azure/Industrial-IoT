#!/usr/bin/env bash

function show_help() {
   # Display Help
   echo "Run this script to deploy Grafana and Prometheus to jumpbox."
   echo
   echo "Syntax: ./deploy_grafana.sh [-flag parameter]"
   echo ""
   echo "Required list of flags:"
   echo "-sshPublicKeyPath Path to the SSH public key that should be used to connect to the jump box, which is the entry point to the Purdue network."
   echo "-jbUserAndFQDN Username and FQDN for accessing jumpbox"
   echo ""
   echo "List of optional flags:"
   echo "-h                Print this help."
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
adminUsername="iiotadmin"
vmSize="Standard_B1ms" #"Standard_D3_v2"

# Get arguments
while :; do
    case $1 in
        -h|-\?|--help)
            show_help
            exit;;
        -jbUserAndFQDN=?*)
            jbUserAndFQDN=${1#*=}
            ;;
        -jbUserAndFQDN=)
            echo "Missing jbUserAndFQDN. Exiting."
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


#Verifying that mandatory parameters are there
if [ -z $jbUserAndFQDN ]; then
    echo "Missing jbUserAndFQDN. Exiting."
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


echo "==========================================================="
echo "==                Configure PNs                          =="
echo "==========================================================="
echo ""

# Load IoT Edge VMs to deploy from config file
source ${scriptFolder}/parseConfigFile.sh $configFilePath

vms="[$(passArrayToARM ${iotEdgeDevicesSubnets[@]})]"

scp -r $scriptFolder/publisher_assets/ $jbUserAndFQDN:/tmp/

for (( i=0; i<${#vms[@]}-1; i+=1))
do
    echo "Configuring ${vms[i]}"
    ssh $jbUserAndFQDN 'bash -s' < "ssh iotedgeadmin@${vms[i]} sudo mkdir /mount"
    ssh $jbUserAndFQDN 'bash -s' < "scp -r /tmp/published_node* iotedgeadmin@${vms[i]}:/mount/"
done