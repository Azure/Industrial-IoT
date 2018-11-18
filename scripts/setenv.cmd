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
set env-file=

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
if "%1" equ "--env-file" goto :arg-env-file
if "%1" equ  "-e" goto :arg-env-file
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo setenv.cmd [options]
echo    Saves required environment variables from deployment or .env environment 
echo    file in the local environment, so that it can be picked up when services
echo    are run or debuggerd locally.
echo options:
echo -e --env-file ^<value^>        PCS v2 .env file to parse (see pcs cli), or ...
echo -g --resource-group ^<value^>  Azure resource group that contains the deployment.
echo -s --subscription ^<value^>    (plus) Azure subscription to use if not default.
echo -p --persist                 Persist environment variables in registry.
echo -x --xtrace                  Print a trace of each command.
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

:arg-env-file
shift
if "%1" equ "" goto :usage
set env-file=%1
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
@rem Helper to write setting in set command
@rem
:save_setting
set _value=%~2
echo set %1=%_value%>> "%TEMP%\_set.cmd"
if "%persist%" == "1" if not "%_value%" == "" echo SETX %1 %_value%>> "%TEMP%\_set.cmd"
if "%persist%" == "1" if "%_value%" == "" echo SETX %1 "">> "%TEMP%\_set.cmd"
if "%persist%" == "1" if "%_value%" == "" echo reg delete "HKCU\Environment" /f /v %1>> "%TEMP%\_set.cmd"
goto :eof

@rem
@rem Helper to copy setting from file
@rem
:copy_setting
set _value=
for /f "tokens=1,* delims==" %%i in (%env-file%) do if /I "%%i" == "%1" set _value=%%j
call :save_setting %1 "%_value%"
goto :eof

@rem
@rem Main
@rem
:main
if not "%env-file%" == "" goto :set-from-env-file
cmd /c az --version > NUL 2>&1
if not !ERRORLEVEL! == 0 echo ERROR: Install Azure cli 2.0! && exit /b !ERRORLEVEL!
if "%resource-group%" == "" goto :usage
rem set subscription
if "%subscription%" == "" goto :check-group
cmd /c az account set --subscription "%subscription%"
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :check-group

@rem
@rem set vars from file
@rem
:set-from-env-file
call :copy_setting PCS_IOTHUB_CONNSTRING
call :copy_setting PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING
call :copy_setting PCS_TELEMETRY_DOCUMENTDB_CONNSTRING
call :copy_setting PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING
call :copy_setting PCS_IOTHUBREACT_HUB_NAME
call :copy_setting PCS_IOTHUBREACT_HUB_ENDPOINT
call :copy_setting PCS_IOTHUBREACT_HUB_CONSUMERGROUP
call :copy_setting PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT
call :copy_setting PCS_IOTHUBREACT_AZUREBLOB_KEY
call :copy_setting PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX
call :copy_setting PCS_ASA_DATA_AZUREBLOB_ACCOUNT
call :copy_setting PCS_ASA_DATA_AZUREBLOB_KEY
call :copy_setting PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX
call :copy_setting PCS_EVENTHUB_CONNSTRING
call :copy_setting PCS_EVENTHUB_NAME
call :copy_setting PCS_AUTH_REQUIRED
call :copy_setting PCS_AUTH_AUDIENCE
call :copy_setting PCS_WEBUI_AUTH_AAD_APPID
call :copy_setting PCS_WEBUI_AUTH_AAD_AUTHORITY
call :copy_setting PCS_WEBUI_AUTH_AAD_TENANT
goto :setvars

@rem
@rem check resource group exists
@rem
:check-group
set _resource-group_exists=
set _cmd_line=az group exists --name %resource-group%
for /f %%i in ('cmd /c %_cmd_line%') do set _resource-group_exists=%%i
if "%_resource-group_exists%" == "true" goto :check-iothub
echo Resource group %resource-group% does not exist.
goto :usage

@rem
@rem set iot hub configuration
@rem
:check-iothub
call :save_setting PCS_IOTHUBREACT_HUB_ENDPOINT
call :save_setting PCS_IOTHUB_CONNSTRING
call :save_setting PCS_IOTHUBREACT_HUB_NAME
set _hub_name=
set _cmd_line=az iot hub list -g %resource-group%
set _cmd_line=%_cmd_line% --query [0].name
for /f %%i in ('cmd /c %_cmd_line%') do call :set_value _hub_name %%i
if not "%_hub_name%" == "" goto :iothub
echo IoT Hub does not exist in resource group %resource-group%. Skip.
call :save_setting _HUB_CS %_HUB_CS%
call :save_setting _EH_CS %_EH_CS%
goto :check-storage

:iothub
set __HUB_CS=%_HUB_CS%
set _HUB_CS=
set _cmd_line=az iot hub show-connection-string -g %resource-group% -n %_hub_name%
for /f %%i in ('cmd /c %_cmd_line% --query cs') do call :set_value _HUB_CS %%i
if not "%_HUB_CS%" == "" goto :event-hub
for /f %%i in ('cmd /c %_cmd_line% --query connectionString') do call :set_value _HUB_CS %%i
if not "%_HUB_CS%" == "" goto :event-hub
for /f %%i in ('cmd /c %_cmd_line% --query [0].connectionString') do call :set_value _HUB_CS %%i
if not "%_HUB_CS%" == "" goto :event-hub
echo IoT Hub %_hub_name% does not have connection string. Skip.
goto :event-hub

@rem
@rem set event hub configuration
@rem
:event-hub
call :save_setting _HUB_CS %_HUB_CS%
set _hub_ep=
set _cmd_line=az iot hub show -g %resource-group% -n %_hub_name%
set _cmd_line=%_cmd_line% --query properties.eventHubEndpoints.events.endpoint
for /f %%i in ('cmd /c %_cmd_line%') do call :set_value _hub_ep %%i
if "%_hub_ep%" == "" echo Event hub endpoint not found in %_hub_name% hub. Skip.
set _EH_CS=Endpoint=%_hub_ep%;
for /f "tokens=1* delims=;" %%i in ("%_HUB_CS%") do set _EH_CS=%_EH_CS%%%j
call :save_setting _EH_CS %_EH_CS%
goto :check-storage

@rem
@rem set storage configuration
@rem
:check-storage
call :save_setting PCS_ASA_DATA_AZUREBLOB_ACCOUNT
call :save_setting PCS_ASA_DATA_AZUREBLOB_KEY
call :save_setting PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX
set __HUB_CS=
set _storage_name=
set _cmd_line=az storage account list -g %resource-group% --query [0].name
for /f %%i in ('cmd /c %_cmd_line%') do call :set_value _storage_name %%i
if not "%_storage_name%" == "" goto :storage
echo Storage not found in resource group %resource-group%. Skip.
call :save_setting _STORE_CS %_STORE_CS%
goto :setvars

:storage
set _cmd_line=az storage account show-connection-string -g %resource-group%
set _cmd_line=%_cmd_line% -n %_storage_name% --query connectionString
for /f %%i in ('cmd /c %_cmd_line%') do call :set_value _STORE_CS %%i
if "%_STORE_CS%" == "" echo Storage connection string for %_storage_name% not found. Skip.
call :save_setting _STORE_CS %_STORE_CS%
goto :setvars

@rem
@rem Commit and show variables
@rem
:setvars
endlocal
if not exist "%TEMP%\_set.cmd" goto :showvars
call "%TEMP%\_set.cmd" > NUL 2>&1
del "%TEMP%\_set.cmd"
:showvars
echo _HUB_CS=%_HUB_CS%
echo _STORE_CS=%_STORE_CS%
echo _EH_CS=%_EH_CS%
echo.
echo PCS_IOTHUB_CONNSTRING=%PCS_IOTHUB_CONNSTRING%
echo PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING=%PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING%
echo PCS_TELEMETRY_DOCUMENTDB_CONNSTRING=%PCS_TELEMETRY_DOCUMENTDB_CONNSTRING%
echo PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING=%PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING%
echo PCS_IOTHUBREACT_HUB_NAME=%PCS_IOTHUBREACT_HUB_NAME%
echo PCS_IOTHUBREACT_HUB_ENDPOINT=%PCS_IOTHUBREACT_HUB_ENDPOINT%
echo PCS_IOTHUBREACT_HUB_CONSUMERGROUP=%PCS_IOTHUBREACT_HUB_CONSUMERGROUP%
echo PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT=%PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT%
echo PCS_IOTHUBREACT_AZUREBLOB_KEY=%PCS_IOTHUBREACT_AZUREBLOB_KEY%
echo PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX=%PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX%
echo PCS_ASA_DATA_AZUREBLOB_ACCOUNT=%PCS_ASA_DATA_AZUREBLOB_ACCOUNT%
echo PCS_ASA_DATA_AZUREBLOB_KEY=%PCS_ASA_DATA_AZUREBLOB_KEY%
echo PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX=%PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX%
echo PCS_EVENTHUB_CONNSTRING=%PCS_EVENTHUB_CONNSTRING%
echo PCS_EVENTHUB_NAME=%PCS_EVENTHUB_NAME%
echo PCS_AUTH_REQUIRED=%PCS_AUTH_REQUIRED%
echo PCS_AUTH_AUDIENCE=%PCS_AUTH_AUDIENCE%
echo PCS_WEBUI_AUTH_AAD_APPID=%PCS_WEBUI_AUTH_AAD_APPID%
echo PCS_WEBUI_AUTH_AAD_AUTHORITY=%PCS_WEBUI_AUTH_AAD_AUTHORITY%
echo PCS_WEBUI_AUTH_AAD_TENANT=%PCS_WEBUI_AUTH_AAD_TENANT%
