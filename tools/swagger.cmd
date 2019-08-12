@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

set clean=

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--xtrace" goto :arg-trace
if "%1" equ  "-x" goto :arg-trace
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo swagger.cmd [options]
echo options:
echo -x --xtrace        print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue

:args-done
goto :main

rem
rem wait until listening
rem
:wait_up
:wait_up_0
set _registry_up=
set _twin_up=
set _history_up=
set _vault_up=
for /f %%i in ('netstat -na ^| findstr "9041" ^| findstr "LISTENING"') do set _twin_up=1
for /f %%i in ('netstat -na ^| findstr "9042" ^| findstr "LISTENING"') do set _registry_up=1
for /f %%i in ('netstat -na ^| findstr "9043" ^| findstr "LISTENING"') do set _history_up=1
for /f %%i in ('netstat -na ^| findstr "9044" ^| findstr "LISTENING"') do set _vault_up=1
if "%_twin_up%" == "" goto :wait_up_0
if "%_registry_up%" == "" goto :wait_up_0
if "%_history_up%" == "" goto :wait_up_0
if "%_vault_up%" == "" goto :wait_up_0
ping nowhere -w 5000 >nul 2>&1
goto :eof

rem
rem retrieve swagger json
rem
:retrieve_spec
if not exist %build_root%\api\swagger mkdir %build_root%\api\swagger
pushd %build_root%\api\swagger
curl -o twin.json http://%_hostname%:9041/v2/swagger.json
curl -o registry.json http://%_hostname%:9042/v2/swagger.json
curl -o history.json http://%_hostname%:9043/v2/swagger.json
curl -o vault.json http://%_hostname%:9044/v2/swagger.json
popd
goto :eof

@rem
@rem Main
@rem
:main
rem start all services
pushd %build_root%\services\src\Microsoft.Azure.IIoT.Services.All\src
rem force https scheme only
rem set PCS_AUTH_HTTPSREDIRECTPORT=443
start dotnet run --project Microsoft.Azure.IIoT.Services.All.csproj
call :wait_up
echo ... Up.

for /f %%i in ('hostname') do set _hostname=%%i
if "%_hostname%" == "" set _hostname=localhost
echo Retrieve swagger.
call :retrieve_spec

:done
if exist %TMP%\sdk_build.log del /f %TMP%\sdk_build.log
rem set PCS_AUTH_HTTPSREDIRECTPORT=
popd
endlocal

