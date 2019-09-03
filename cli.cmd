@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

set url=
set app=
set client=

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--cloud-url" goto :arg-url
if "%1" equ  "-u" goto :arg-url
if "%1" equ "--client-id" goto :arg-client
if "%1" equ  "-c" goto :arg-client
if "%1" equ "--application-name" goto :arg-app
if "%1" equ  "-a" goto :arg-app
goto :usage

:args-continue
shift
goto :args-loop

:usage
echo. 
echo Industrial IoT platform services command line interface (CLI)
echo.
echo Usage:                 cli.cmd [options] 
echo Run with a .env file in the root, or specify the following options:
echo. 
echo -u --cloud-url         The cloud endpoint shown after deployment completes.
echo -a --application-name  The name of the application as chosen during deployment.
echo -c --client-id         The AAD client id (Guid) from AAD.
echo. 
exit /b 1

:arg-url
shift
if "%1" equ "" call :usage && exit /b 1
set url=%1
goto :args-continue
:arg-app
shift
if "%1" equ "" call :usage && exit /b 1
set app=%1
goto :args-continue
:arg-client
shift
if "%1" equ "" call :usage && exit /b 1
set client=%1
goto :args-continue

:args-done
goto :setenv

:cleanup
set PCS_SERVICE_URL=
set PCS_AUTH_REQUIRED=
set PCS_WEBUI_AUTH_AAD_APPID=
set PCS_AUTH_AUDIENCE=
goto :eof

:setenv
call :cleanup
if "%url%" == "" if exist .env goto :main
if "%url%" == "" goto :usage
set PCS_SERVICE_URL=%url%
if not "%PCS_SERVICE_URL%" == "" echo Connecting to %PCS_SERVICE_URL% ...
if not "%app%" == "" set PCS_AUTH_AUDIENCE=https://microsoft.onmicrosoft.com/%app%-services
rem todo:  Should read from powershell
set PCS_WEBUI_AUTH_AAD_APPID=%client%
set PCS_AUTH_REQUIRED=true
if not "%PCS_AUTH_AUDIENCE%" == "" if not "%PCS_WEBUI_AUTH_AAD_APPID%" == "" goto :main
set PCS_AUTH_REQUIRED=false
echo WARNING: Accessing endpoint without authentication.
echo Specify an application name and client id to use authentication.
goto :main

:main
pushd api\src\Microsoft.Azure.IIoT.OpcUa.Api\cli
dotnet run console
popd
call :cleanup
goto :eof
