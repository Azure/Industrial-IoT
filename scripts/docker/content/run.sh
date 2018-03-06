#!/usr/bin/env bash

cd /app/

cd webservice && dotnet Microsoft.Azure.IoTSolutions.OpcTwin.WebService.dll && \
    fg
