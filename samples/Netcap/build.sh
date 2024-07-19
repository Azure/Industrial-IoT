#!/bin/bash
set -e
docker build -t netcap:latest .
docker run -it --cap-add=NET_ADMIN netcap:latest $@
