#!/bin/bash

deviceId=$1

# Validating parameters
if [ -z $1 ]; then
        echo "Missing deviceId. Please pass a deviceId as a parameter. Exiting."
        exit 1
fi
echo "Setting up test certificates for IoT Edge device $deviceId"
echo ""

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
echo "Installation of IoT Edge is complete. Starting its configuration."
echo ""

# Installing certificates
#TODO2: erase certs folder first
echo "Installing test root certificate bundle. NOT TO BE USED FOR PRODUCTION."
sudo mkdir /certs
cd /certs
sudo wget -O test-certs.tar.bz2 "https://raw.githubusercontent.com/Azure/Industrial-IoT/feature/nested_edge/tools/e2etesting/NestedEdge/scripts/assets/test-certs.tar.bz2"
sudo tar -xjvf test-certs.tar.bz2
cd ./certs

echo "Generating edge device certificate"
sudo bash ./certGen.sh create_edge_device_certificate $deviceId
cd ./certs
sudo cp azure-iot-test-only.root.ca.cert.pem /usr/local/share/ca-certificates/azure-iot-test-only.root.ca.cert.pem.crt
sudo update-ca-certificates

echo "Updating IoT Edge configuration file to use the newly installed certificates"
device_ca_cert_path="file:///certs/certs/certs/iot-edge-device-$deviceId-full-chain.cert.pem"
device_ca_pk_path="file:///certs/certs/private/iot-edge-device-$deviceId.key.pem"
trusted_ca_certs_path="file:///certs/certs/certs/azure-iot-test-only.root.ca.cert.pem"
sudo sed -i "28s|.*|trust_bundle_cert = \""$trusted_ca_certs_path"\"|" /etc/aziot/config.toml
sudo sed -i "266s|.*|[edge_ca]|" /etc/aziot/config.toml
sudo sed -i "267s|.*|cert = \""$device_ca_cert_path"\"|" /etc/aziot/config.toml
sudo sed -i "269s|.*|pk = \""$device_ca_pk_path"\"|" /etc/aziot/config.toml

echo "Done."