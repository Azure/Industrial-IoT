@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Example usage (see "travis help" for more information:
::  travis help
::  travis login --pro
::  travis whoami --pro
::  travis accounts --pro
::  travis history
::  travis monitor --pro
::  travis settings
::  travis show
::  travis status
::  travis token --pro
::  travis whatsup --pro

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

mkdir .travis 2>NUL

docker run -it ^
    -v %APP_HOME%\.travis:/root/.travis ^
    -v %APP_HOME%:/opt/code ^
    azureiotpcs/travis-cli:1.8.8 /root/bin/travis.sh %*

endlocal
