#!/usr/bin/env bash

cd /app/

cd webservice && dotnet Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.dll && \
    fg
