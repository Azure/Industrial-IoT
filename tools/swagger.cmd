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
if "%1" equ "--hostname" goto :arg-hostname
if "%1" equ  "-h" goto :arg-hostname
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo swagger.cmd [options]
echo options:
echo -x --xtrace        print a trace of each command.
echo -h --hostname      specify host name to retrieve from.
exit /b 1

:arg-trace
echo on
goto :args-continue

:arg-hostname
shift
set _hostname=%1
goto :args-continue

:args-done
goto :main

rem
rem wait until listening
rem
:retrieve_spec
:retrieve_retry
if exist %1.json del /f %1.json
curl -o %1.json http://%_hostname%/%1/swagger/v2/openapi.json
if exist %1.json goto :eof
ping nowhere -w 5000 >nul 2>&1
goto :retrieve_retry

rem
rem retrieve swagger json
rem
:retrieve_specs
echo Retrieve swagger docs from %_hostname%.
if not exist %build_root%\api\swagger mkdir %build_root%\api\swagger
pushd %build_root%\api\swagger
call :retrieve_spec twin
call :retrieve_spec publisher
call :retrieve_spec registry
call :retrieve_spec history
call :retrieve_spec events
popd
goto :eof

@rem
@rem Main
@rem
:main

if not "%_hostname%" == "" goto :run
rem start all services
pushd %build_root%\services\src\Microsoft.Azure.IIoT.Services.All\src
rem force https scheme only
rem set PCS_AUTH_HTTPSREDIRECTPORT=443
start dotnet run --project Microsoft.Azure.IIoT.Services.All.csproj
set _hostname=localhost:9080
goto :run

:run
call :retrieve_specs

:done
if exist %TMP%\sdk_build.log del /f %TMP%\sdk_build.log
rem set PCS_AUTH_HTTPSREDIRECTPORT=
popd
endlocal

