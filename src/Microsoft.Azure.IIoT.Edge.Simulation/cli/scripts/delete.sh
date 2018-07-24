#!/bin/bash
docker network rm munich0.corp.contoso
docker network rm capetown.corp.contoso
docker network rm mumbai.corp.contoso
docker network rm seattle.corp.contoso
docker network rm beijing.corp.contoso
docker network rm rio.corp.contoso
sudo rm -r /home/docker/Shared
sudo rm -r /home/docker/Logs
sudo rm -r /home/docker/Config
sudo rm -r /home/docker/buildOutput
