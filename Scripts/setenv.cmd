@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%

set subscription=
set resource-group=
set persist=

cmd /c az --version > NUL 2>&1
if not !ERRORLEVEL! == 0 echo ERROR: Install Azure cli 2.0! && exit /b !ERRORLEVEL!

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ  "-x" goto :arg-trace
if "%1" equ "--xtrace" goto :arg-trace
if "%1" equ  "-p" goto :arg-persist
if "%1" equ "--persist" goto :arg-persist
if "%1" equ "--resource-group" goto :arg-resource-group
if "%1" equ  "-g" goto :arg-resource-group
if "%1" equ "--subscription" goto :arg-subscription
if "%1" equ  "-s" goto :arg-subscription
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo setenv.cmd [options]
echo options:
echo -g --resource-group ^<value^>  Azure resource group to use for deploy [mandatory].
echo -s --subscription ^<value^>    Azure subscription to use.
echo -p --persist                 Persist settings.
echo -x --xtrace                  print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue

:arg-persist
set persist=1
goto :args-continue

:arg-type
shift
if "%1" equ "" goto :usage
set type=%1
goto :args-continue

:arg-subscription
shift
if "%1" equ "" goto :usage
set subscription=%1
goto :args-continue

:arg-resource-group
shift
if "%1" equ "" goto :usage
set resource-group=%1
goto :args-continue

:args-done
if exist "%TEMP%\_set.cmd" del "%TEMP%\_set.cmd"
goto :main

@rem
@rem Helper to remove quotes
@rem
:set_value
set %1=%~2%
goto :eof

@rem
@rem Main
@rem
:main
if "%resource-group%" == "" goto :usage
rem set subscription
if "%subscription%" == "" goto :check-group
cmd /c az account set --subscription "%subscription%"
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!

rem check group
:check-group
set _resource-group_exists=
set _cmd_line=az group exists --name %resource-group%
for /f %%i in ('cmd /c %_cmd_line%') do set _resource-group_exists=%%i
if "%_resource-group_exists%" == "true" goto :check-iothub
echo Resource group %resource-group% does not exist.
goto :usage

rem set iot hub configuration
:check-iothub
set _hub_name=
set _cmd_line=az iot hub list -g %resource-group%
set _cmd_line=%_cmd_line% --query [0].name
for /f %%i in ('cmd /c %_cmd_line%') do call :set_value _hub_name %%i
if not "%_hub_name%" == "" goto :iothub
echo IoT Hub does not exist in resource group %resource-group%. Skip.
goto :check-storage
:iothub
set __HUB_CS=%_HUB_CS%
set _HUB_CS=
set _cmd_line=az iot hub show-connection-string -g %resource-group% -n %_hub_name%
for /f %%i in ('cmd /c %_cmd_line% --query cs') do call :set_value _HUB_CS %%i
if not "%_HUB_CS%" == "" goto :iothub-done
for /f %%i in ('cmd /c %_cmd_line% --query connectionString') do call :set_value _HUB_CS %%i
if not "%_HUB_CS%" == "" goto :iothub-done
for /f %%i in ('cmd /c %_cmd_line% --query [0].connectionString') do call :set_value _HUB_CS %%i
if not "%_HUB_CS%" == "" goto :iothub-done
set _HUB_CS=%__HUB_CS%
echo IoT Hub %_hub_name% does not have connection string. Skip.
goto :check-storage
:iothub-done
echo set _HUB_CS=%_HUB_CS%>> "%TEMP%\_set.cmd"
if "%persist%" == "1" echo SETX _HUB_CS %_HUB_CS%>> "%TEMP%\_set.cmd"
set _hub_ep=
set _cmd_line=az iot hub show -g %resource-group% -n %_hub_name%
set _cmd_line=%_cmd_line% --query properties.eventHubEndpoints.events.endpoint
for /f %%i in ('cmd /c %_cmd_line%') do call :set_value _hub_ep %%i
if "%_hub_ep%" == "" echo Event hub endpoint not found in %_hub_name% hub. Skip.
set _EH_CS=Endpoint=%_hub_ep%;
for /f "tokens=1* delims=;" %%i in ("%_HUB_CS%") do set _EH_CS=%_EH_CS%%%j
echo set _EH_CS=%_EH_CS%>> "%TEMP%\_set.cmd"
if "%persist%" == "1" echo SETX _EH_CS %_EH_CS%>> "%TEMP%\_set.cmd"

rem set storage configuration
:check-storage
set __HUB_CS=
set _storage_name=
set _cmd_line=az storage account list -g %resource-group% --query [0].name
for /f %%i in ('cmd /c %_cmd_line%') do call :set_value _storage_name %%i
if not "%_storage_name%" == "" goto :storage
echo Storage not found in resource group %resource-group%. Skip.
goto :setvars

:storage
set _cmd_line=az storage account show-connection-string -g %resource-group%
set _cmd_line=%_cmd_line% -n %_storage_name% --query connectionString
for /f %%i in ('cmd /c %_cmd_line%') do call :set_value _STORE_CS %%i
if "%_STORE_CS%" == "" echo Storage connection string for %_storage_name% not found. Skip.
echo set _STORE_CS=%_STORE_CS%>> "%TEMP%\_set.cmd"
if "%persist%" == "1" echo SETX _STORE_CS %_STORE_CS%>> "%TEMP%\_set.cmd"

:setvars
endlocal
if not exist "%TEMP%\_set.cmd" goto :showvars
call "%TEMP%\_set.cmd" > NUL 2>&1
del "%TEMP%\_set.cmd"

:showvars
echo _HUB_CS=%_HUB_CS%
echo _STORE_CS=%_STORE_CS%
echo _EH_CS=%_EH_CS%
