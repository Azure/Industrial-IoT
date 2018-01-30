@ECHO off
setlocal enableextensions enabledelayedexpansion

IF "%PCS_IOTHUB_CONNSTRING%" == "" (
    echo Error: the PCS_IOTHUB_CONNSTRING environment variable is not defined.
    exit /B 1
)

IF "%PCS_IOTHUBMANAGER_WEBSERVICE_URL%" == "" (
    echo Error: the PCS_IOTHUBMANAGER_WEBSERVICE_URL environment variable is not defined.
    exit /B 1
)

IF "%PCS_AUTH_ISSUER%" == "" (
    echo Error: the PCS_AUTH_ISSUER environment variable is not defined.
    exit /B 1
)

IF "%PCS_AUTH_AUDIENCE%" == "" (
    echo Error: the PCS_AUTH_AUDIENCE environment variable is not defined.
    exit /B 1
)

endlocal
