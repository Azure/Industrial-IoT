#!/bin/bash
set -e
cd src
docker build -t netcap:latest .
docker run -it --cap-add=NET_ADMIN netcap:latest $@
