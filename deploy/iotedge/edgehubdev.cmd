@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

set clean=
set _config=Release
set _verbose=

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--xtrace" goto :arg-trace
if "%1" equ  "-x" goto :arg-trace
if "%1" equ "--debug" goto :arg-config
if "%1" equ  "-d" goto :arg-config
if "%1" equ "--verbose" goto :arg-verbose
if "%1" equ  "-v" goto :arg-verbose
if "%1" equ "--hub-name" goto :arg-hub-name
if "%1" equ  "-n" goto :arg-hub-name
if "%1" equ "--tenant" goto :arg-tenant
if "%1" equ  "-t" goto :arg-tenant
if "%1" equ "--subscription" goto :arg-subscription
if "%1" equ  "-s" goto :arg-subscription
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo Run opc publisher in the edgehubdev context.
echo Usage: run.cmd [options]
echo options:
echo -n --hub-name [name]       specify hub name to install into (REQUIRED).
echo -t --tenant [id]           specify the tenant to log into.
echo -s --subscription [id]     set the subscription to use.
echo -v --verbose               Log output from edge hub dev.
echo -d --debug                 build debug container images.
echo -x --xtrace                print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue
:arg-config
set _config=Debug
goto :args-continue
:arg-verbose
set _verbose=-v
goto :args-continue
:arg-hub-name
shift
set _hub-name=%1
goto :args-continue
:arg-subscription
shift
set _subscription=%1
goto :args-continue
:arg-tenant
shift
set _tenant=-t %1
goto :args-continue
:args-done
goto :validate-args

:validate-args
if "%_hub-name%" == "" echo Missing required argument --hub-name. && goto :usage
goto :main

rem
rem Check edgehubdev and docker
rem
:main
call iotedgehubdev --version
if !ERRORLEVEL! == 0 goto :docker
echo First install iotedgehubdev per https://github.com/Azure/iotedgehubdev.
goto :error
:docker
call docker ps -a > nul 2>&1
if !ERRORLEVEL! == 0 goto :login
echo Docker is not running or installed.
goto :error

rem
rem Login
rem
:login
call az account show > nul 2>&1
if !ERRORLEVEL! == 0 goto :setsub
echo Login to Azure...
call az login %_tenant%
goto :setsub
:setsub
if "%_subscription%" == "" goto :build
call az account set -s %subscription% > nul 2>&1
if !ERRORLEVEL! == 0 goto :build
echo Failed to change subscription!
goto :error

rem
rem Build and publish
rem
:build
set c=--self-contained false /t:PublishContainer /p:ContainerImageTag=latest
set p=Azure.IIoT.OpcUa.Publisher.Module
echo Building %_config% image...
call dotnet publish ../../src/%p%/src/%p%.csproj -c %_config% %c% > nul 2>&1
if !ERRORLEVEL! == 0 goto :run
echo Failed to build %_config% image!
goto :error

rem
rem Setup and start
rem
:run
set c=
set c=%c% az iot hub connection-string show
set c=%c% --hub-name %_hub-name%
set c=%c% -o tsv --query connectionString
for /f "tokens=*" %%a in ('%c%') do set _HUB_CS=%%a
if !ERRORLEVEL! == 0 goto :check
echo Failed to get connection string for iot hub %_hub-name%!
goto :error
:check
for /f "tokens=*" %%a in ('hostname') do set hostname=%%a

call az iot hub device-identity show -n %_hub-name% --device-id %hostname% > nul 2>&1
if not !ERRORLEVEL! == 0 goto :create
set c=
set c=%c% az iot hub device-identity connection-string show
set c=%c% --hub-name %_hub-name% --device-id %hostname%
set c=%c% -o tsv --query connectionString
for /f "tokens=*" %%a in ('%c%') do set _EH_CS=%%a
if !ERRORLEVEL! == 0 goto :setup
:create
echo Creating device %hostname% in %_hub-name%.
call az iot edge devices create -n %_hub-name% --device id=%hostname%
if !ERRORLEVEL! == 0 goto :check
echo Failed to create edge device %hostname% in %_hub-name%.
goto :error
:setup
call iotedgehubdev setup -c "%_EH_CS%" -i "%_HUB_CS%" -g %hostname%
if !ERRORLEVEL! == 0 goto :start
echo Failed to setup iotedgehubdev!
goto :error
:start
if not exist %current-path%\edgehubdev.json goto :error
call iotedgehubdev start -d %current-path%\edgehubdev.json %_verbose%
if !ERRORLEVEL! == 0 goto :done
echo Failed to start iotedgehubdev!
goto :error

:done
goto :eof
:error
exit /b 1



