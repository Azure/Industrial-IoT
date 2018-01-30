FROM microsoft/dotnet:1.1.2-runtime-jessie

MAINTAINER Devis Lucato (https://github.com/dluc)

LABEL Tags="Azure,IoT,Solutions,IoT Hub,.NET"

ENTRYPOINT ["/bin/bash", "/app/run.sh"]

COPY . /app/
WORKDIR /app
