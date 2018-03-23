@ECHO off
setlocal enableextensions enabledelayedexpansion

IF "%_HUB_CS%" == "" (
    echo Error: the _HUB_CS environment variable is not defined.
    exit /B 1
)

IF "%_EH_CS%" == "" (
    echo Error: the _EH_CS environment variable is not defined.
    exit /B 1
)

IF "%_STORE_CS%" == "" (
    echo Error: the _STORE_CS environment variable is not defined.
    exit /B 1
)

endlocal
