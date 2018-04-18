@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%

set type=webapp
set name=
set image=
set location=
set subscription=
set settings=
set resource-group=

cmd /c az --version > NUL 2>&1
if not !ERRORLEVEL! == 0 echo ERROR: Install Azure cli 2.0! && exit /b !ERRORLEVEL!

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ  "-x" goto :arg-trace
if "%1" equ "--xtrace" goto :arg-trace
if "%1" equ "--type" goto :arg-type
if "%1" equ  "-t" goto :arg-type
if "%1" equ "--resource-group" goto :arg-resource-group
if "%1" equ  "-g" goto :arg-resource-group
if "%1" equ "--subscription" goto :arg-subscription
if "%1" equ  "-s" goto :arg-subscription
if "%1" equ "--name" goto :arg-name
if "%1" equ  "-n" goto :arg-name
if "%1" equ "--image" goto :arg-image
if "%1" equ  "-i" goto :arg-image
if "%1" equ "--location" goto :arg-location
if "%1" equ  "-l" goto :arg-location
if "%1" equ "--env" goto :arg-env
if "%1" equ  "-e" goto :arg-env
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo deploy.cmd [options]
echo options:
echo -t --type [webapp, instance] Type of container deployment (default: webapp).
echo -g --resource-group ^<value^>  Azure resource group to use for deploy [mandatory].
echo -n --name ^<value^>            Name of the deployment [mandatory].
echo -i --image ^<value^>           Image name [mandatory].
echo -e --env "^<var^>=^<value^>" A setting to pass to deployed container.
echo -s --subscription ^<value^>    Azure subscription to use.
echo -l --location ^<value^>        Location to use if resource group should be created.
echo -x --xtrace                  print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue

:arg-type
shift
if "%1" equ "" goto :usage
set type=%1
goto :args-continue

:arg-name
shift
if "%1" equ "" goto :usage
set name=%1
goto :args-continue

:arg-image
shift
if "%1" equ "" goto :usage
set image=%1
goto :args-continue

:arg-location
shift
if "%1" equ "" goto :usage
set location=%1
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

:arg-env
shift
set settings=%settings% %~1
goto :args-continue

@rem
@rem Validate command line
@rem
:args-done
if not "%type%" equ "webapp" if not "%type%" equ "instance" goto :usage
if "%image%" == "" goto :usage
if "%name%" == "" goto :usage
if "%resource-group%" == "" goto :usage
goto :main

@rem
@rem Deploy container instance
@rem
:deploy_instance
echo Deploy container instance ...
set _cmd_line=
set _cmd_line=%_cmd_line% --resource-group %resource-group%
set _cmd_line=%_cmd_line% --name %name%
echo.
cmd /c az container delete %_cmd_line% -y > NUL 2>&1
set _cmd_line=%_cmd_line% --image %image%
set _cmd_line=%_cmd_line% --os-type Linux
if not "%settings%" == "" set _cmd_line=%_cmd_line% --environment-variables %settings%
set _cmd_line=%_cmd_line% --query name
echo.
echo Create container ...
cmd /c az container create %_cmd_line%
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
echo.
cmd /c az container list --resource-group %resource-group% --output table
set _cmd_line=
goto :eof

@rem
@rem Deploy webapp
@rem
:deploy_webapp
set _cmd_line=
set _cmd_line=%_cmd_line% --resource-group %resource-group%
set _cmd_line=%_cmd_line% --name %name%-plan
set _cmd_line=%_cmd_line% --sku S1
set _cmd_line=%_cmd_line% --is-linux
echo.
cmd /c az appservice plan create %_cmd_line%
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
set _cmd_line=
set _cmd_line=%_cmd_line% --resource-group %resource-group%
set _cmd_line=%_cmd_line% --name %name%
set _cmd_line=%_cmd_line% --plan %name%-plan
set _cmd_line=%_cmd_line% --deployment-container-image-name %image%
echo.
cmd /c az webapp create %_cmd_line%
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
set _cmd_line=
set _cmd_line=%_cmd_line% --resource-group %resource-group%
set _cmd_line=%_cmd_line% --name %name%
rem cmd /c az webapp identity assign %_cmd_line% > NUL 2>&1
rem if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
echo.
cmd /c az webapp config appsettings set %_cmd_line% --settings %settings% 
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
echo.
cmd /c az webapp update %_cmd_line% --https-only true --client-affinity-enabled false > NUL 2>&1
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
echo.
cmd /c az webapp restart %_cmd_line%
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
echo.
cmd /c az webapp list --resource-group %resource-group% --output table
set _cmd_line=
goto :eof

@rem
@rem Main
@rem
:main
if "%subscription%" == "" goto :create-group
cmd /c az account set --subscription "%subscription%"
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
:create-group
if "%location%" == "" goto :deploy
set _resource-group_exists=
for /f %%i in ('cmd /c az group exists --name %resource-group%') do set _resource-group_exists=%%i
if "%_resource-group_exists%" == "true" goto :deploy
cmd /c az group create --name %resource-group% --location "%location%"
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
:deploy
call :deploy_%type%
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
endlocal