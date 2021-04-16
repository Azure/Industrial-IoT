#!/bin/bash

# Parsing arguments

dcs=$1
deviceId=$2
fqdn=$3
if [ $# -eq 7 ]; then
    #top layer with proxy and ACR
    proxySettings=$4
    acrAddress=$5
    acrUsername=$6
    acrPassword=$7
elif [ $# -eq 5 ]; then
    #middle or bottom layer with proxy
    parentFqdn=$4
    proxySettings=$5
else
    #middle or bottom layer
    parentFqdn=$4
fi

# Validating parameters
echo "Executing script with parameters:"
echo "- Device connection string: ${dcs}"
echo "- Device Id: ${deviceId}"
echo "- FQDN: ${fqdn}"
echo "- Parent FQDN: ${parentFqdn}"
echo "- ProxySettings: ${proxySettings}"
echo "- ACR address: ${acrAddress}"
echo "- ACR username: ${acrUsername}"
echo "- ACR password: ${acrPassword}"
echo ""
if [ -z ${dcs} ]; then
    echo "Missing device connection string. Please pass a device connection string as a primary parameter. Exiting."
    exit 1
fi
if [ -z ${deviceId} ]; then
    echo "Missing device Fully Domain Qualified Name (FQDN). Please pass a FQDN as a secondary parameter. Exiting."
    exit 1
fi
if [ -z ${fqdn} ]; then
    echo "Missing device Fully Domain Qualified Name (FQDN). Please pass a FQDN as a secondary parameter. Exiting."
    exit 1
fi

# Waiting for IoT Edge installation to be complete
i=0
iotedgeConfigFile="/etc/aziot/config.toml"
while [[ ! -f "$iotedgeConfigFile" ]]; do
    echo "Waiting 10s for IoT Edge to complete its installation"
    sleep 10
    ((i++))
    if [ $i -gt 100 ]; then
        echo "Something went wrong in the installation of IoT Edge. Please install IoT Edge first. Exiting."
        exit 1
   fi
done
echo "Installation of IoT Edge is complete."
echo ""

# Waiting for installation of certificates to be complete
i=0
deviceCaCertFile="/certs/certs/certs/iot-edge-device-$deviceId-full-chain.cert.pem"
while [[ ! -f "$deviceCaCertFile" ]]; do
    echo "Waiting 10s for installation of certificates to complete"
    sleep 10
    ((i++))
    if [ $i -gt 30 ]; then
        echo "Something went wrong in the installation of certificates. Please install certificates first. Exiting."
        exit 1
   fi
done
echo "Installation of certificates is complete. Starting configuration of the IoT Edge device."
echo ""

# Configuring IoT Edge
echo "Updating the device connection string"
sudo sed -i "63s|.*|[provisioning]|" /etc/aziot/config.toml
sudo sed -i "64s|.*|source = \"manual\"|" /etc/aziot/config.toml
sudo sed -i "65s|.*|connection_string = \"$dcs\"|" /etc/aziot/config.toml

echo "Updating the device hostname"
sudo sed -i "7s|.*|hostname = \"$fqdn\"|" /etc/aziot/config.toml

if [ ! -z $parentFqdn ]; then
    echo "Updating the parent hostname"
    sudo sed -i "17s|.*|parent_hostname = \"$parentFqdn\"|" /etc/aziot/config.toml
fi


echo "Updating the version of the bootstrapping edgeAgent to be the public preview one"
sudo sed -i "212s|.*|[agent]|" /etc/aziot/config.toml
sudo sed -i "213s|.*|\"name\" = \"edgeAgent\"|" /etc/aziot/config.toml
sudo sed -i "214s|.*|\"type\" = \"docker\"|" /etc/aziot/config.toml

if [ -z $parentFqdn ]; then
    edgeAgentImage="$acrAddress:443/azureiotedge-agent:1.2.0"
else
    edgeAgentImage="$parentFqdn:443/azureiotedge-agent:1.2.0"
fi
sudo sed -i "217s|.*|[agent.config]|" /etc/aziot/config.toml
sudo sed -i "218s|.*|image = \"${edgeAgentImage}\"|" /etc/aziot/config.toml

if [ -z $parentFqdn ]; then
    echo "Adding ACR credentials for IoT Edge daemon to download the bootstrapping edgeAgent"
    sudo sed -i "221s|.*|[agent.config.auth]|" /etc/aziot/config.toml
    sudo sed -i "222s|.*|serveraddress = \"${acrAddress}\"|" /etc/aziot/config.toml
    sudo sed -i "223s|.*|username = \"${acrUsername}\"|" /etc/aziot/config.toml
    sudo sed -i "224s|.*|password = \"${acrPassword}\"|" /etc/aziot/config.toml
fi

echo "Configuring the bootstrapping edgeAgent to use AMQP/WS"
sudo sed -i "226s|.*|[agent.env]|" /etc/aziot/config.toml
sudo sed -i "228s|.*|\"UpstreamProtocol\" = \"AmqpWs\"|" /etc/aziot/config.toml

if [ ! -z $proxySettings ]; then
    echo "Configuring the bootstrapping edgeAgent to use http proxy"
    httpProxyAddress=$(echo $proxySettings | cut -d "=" -f2-)
    sudo sed -i "230s|.*|\"https_proxy\" = \"${httpProxyAddress}\"|" /etc/aziot/config.toml

    echo "Adding proxy configuration to docker"
    sudo mkdir -p /etc/systemd/system/docker.service.d/
    { echo "[Service]";
    echo "Environment=${proxySettings}";
    } | sudo tee /etc/systemd/system/docker.service.d/http-proxy.conf
    sudo systemctl daemon-reload
    sudo systemctl restart docker

    echo "Adding proxy configuration to IoT Edge daemon"
    sudo mkdir -p /etc/systemd/system/aziot-identityd.service.d/
    { echo "[Service]";
    echo "Environment=\"${proxySettings}\"";
    } | sudo tee /etc/systemd/system/aziot-identityd.service.d/proxy.conf
    sudo mkdir -p /etc/systemd/system/aziot-edged.service.d/
    { echo "[Service]";
    echo "Environment=\"${proxySettings}\"";
    } | sudo tee /etc/systemd/system/aziot-edged.service.d/proxy.conf
    sudo systemctl daemon-reload
fi

echo "Restarting IoT Edge to apply new configuration"
sudo iotedge config apply

echo "Done."