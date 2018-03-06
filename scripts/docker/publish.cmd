@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE="microsoft/iot-opc-twin-service"
:: "testing" is the latest dev build, usually matching the code in the "master" branch
SET DOCKER_TAG=%DOCKER_IMAGE%:testing

docker push %DOCKER_TAG%

endlocal
