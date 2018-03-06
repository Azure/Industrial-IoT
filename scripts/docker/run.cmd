@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE="microsoft/iot-opc-twin-service"

:: strlen("\scripts\docker\") => 16
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-16%
cd %APP_HOME%

:: Check dependencies
docker version > NUL 2>&1
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

:: Check settings
call .\scripts\env-vars-check.cmd
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: Start the application
echo Starting OPC Twin ...
docker run -it -p 9002:9002 ^
    -e PCS_IOTHUB_CONNSTRING ^
    -e PCS_CONFIG_WEBSERVICE_URL ^
    -e PCS_AUTH_ISSUER ^
    -e PCS_AUTH_AUDIENCE ^
    %DOCKER_IMAGE%:testing

:: - - - - - - - - - - - - - -
goto :END

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:MISSING_DOCKER
    echo ERROR: 'docker' command not found.
    echo Install Docker and make sure the 'docker' command is in the PATH.
    echo Docker installation: https://www.docker.com/community-edition#/download
    exit /B 1

:END
endlocal
