#!/bin/bash
chmod +x simulation
tar -xjvf simulation -C /home/docker
rm simulation
cp -r /home/docker/buildOutput/Config /home/docker
cp -r /home/docker/buildOutput/Logs /home/docker
cp -r /home/docker/buildOutput/Shared /home/docker
cp /home/docker/buildOutput/startsimulation /home/docker
chmod +x /home/docker/startsimulation
cp /home/docker/buildOutput/deletesimulation /home/docker
chmod +x /home/docker/deletesimulation
cp /home/docker/buildOutput/stopsimulation /home/docker
chmod +x /home/docker/stopsimulation



if [ "$2" != "" ] && [ -e ../../../$2.crt ]
then
    cp ../../../$2.crt "/home/docker/Shared/CertificateStores/UA Applications/certs/UAWebClient.der"
fi
cd /home/docker/buildOutput
docker build -t simulation:latest .


docker network create -d bridge -o 'com.docker.network.bridge.enable_icc'='true' munich0.corp.contoso
docker network create -d bridge -o 'com.docker.network.bridge.enable_icc'='true' capetown.corp.contoso
docker network create -d bridge -o 'com.docker.network.bridge.enable_icc'='true' mumbai.corp.contoso
docker network create -d bridge -o 'com.docker.network.bridge.enable_icc'='true' seattle.corp.contoso
docker network create -d bridge -o 'com.docker.network.bridge.enable_icc'='true' beijing.corp.contoso
docker network create -d bridge -o 'com.docker.network.bridge.enable_icc'='true' rio.corp.contoso

sudo chown -R docker:docker /home/docker
sudo chown -R root:root "/home/docker/Shared/CertificateStores/UA Applications/certs"
sudo chmod u+x "/home/docker/Shared/CertificateStores/UA Applications/certs/UAWebClient.der"
