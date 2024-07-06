#!/bin/bash

docker build -t netcap:latest . > /dev/null 2>&1
docker run -it --cap-add=NET_ADMIN netcap:latest $@