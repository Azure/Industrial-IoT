#!/bin/bash -e

while [ "$#" -gt 0 ]; do
    case "$1" in
        --dpsConnString)                  dpsConnString="$2" ;;
        --idScope)                        idScope="$2" ;;
    esac
    shift
done

curdir="$( cd "$(dirname "$0")" ; pwd -P )"

echo "In $curdir..."
echo "Prepare machine..."
DEBIAN_FRONTEND=noninteractive

# install powershell and iotedge
curl -O https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
apt-get update
apt-get install -y --no-install-recommends powershell
echo "Powershell installed."

curl -o microsoft-prod.list https://packages.microsoft.com/config/ubuntu/16.04/multiarch/prod.list
cp ./microsoft-prod.list /etc/apt/sources.list.d/
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
cp ./microsoft.gpg /etc/apt/trusted.gpg.d/
apt-get update
apt-get install -y --no-install-recommends moby-engine moby-cli
apt-get install -y --no-install-recommends iotedge
echo "Iotedge installed."

echo "Provisioning iotedge..."
sleep 3
pwsh -File $curdir/edge-setup.ps1 -dpsConnString $dpsConnString -idScope $idScope
echo "Iotedge provisioned."

echo "Restarting iotedge runtime..."
sleep 3
systemctl daemon-reload
systemctl restart iotedge
sleep 3
systemctl status iotedge
echo "Iotedge running."
