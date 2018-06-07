@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%

set repository=
set image-version=
set build=linux windows
set deploy-only=
set location=
set subscription=
set resource-group=
set name-prefix=

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--xtrace" goto :arg-trace
if "%1" equ  "-x" goto :arg-trace
if "%1" equ "--version" goto :arg-version
if "%1" equ  "-v" goto :arg-version
if "%1" equ "--repository" goto :arg-repository
if "%1" equ  "-r" goto :arg-repository
if "%1" equ "--linux-only" goto :arg-linux
if "%1" equ  "-L" goto :arg-linux
if "%1" equ "--windows-only" goto :arg-windows
if "%1" equ  "-W" goto :arg-windows
if "%1" equ "--resource-group" goto :arg-resource-group
if "%1" equ  "-g" goto :arg-resource-group
if "%1" equ "--subscription" goto :arg-subscription
if "%1" equ  "-s" goto :arg-subscription
if "%1" equ "--location" goto :arg-location
if "%1" equ  "-l" goto :arg-location
if "%1" equ "--name-prefix" goto :arg-name-prefix
if "%1" equ  "-p" goto :arg-name-prefix
if "%1" equ "--deploy-only" goto :arg-deploy-only
if "%1" equ  "-d" goto :arg-deploy-only
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo make.cmd [options]
echo options:
echo -r --repository ^<value^>      Repository to optionally push images to.
echo -v --version ^<value^>         Image version (e.g. 1.0.4) (to tag).
echo    --linux-only              Build (and optionally push) only Linux images.
echo    --windows-only            Build (and optionally push) only Windows images.
echo -d --deploy-only             Deploy images only.
echo -g --resource-group ^<value^>  Azure resource group to use for deploy.
echo -p --name-prefix ^<value^>     Prefix of deployed services.
echo -s --subscription ^<value^>    Azure subscription to use.
echo -l --location ^<value^>        Location to use if resource group should be created.
echo -x --xtrace                  print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue

:arg-linux
set build=linux
goto :args-continue

:arg-windows
set build=windows
goto :args-continue

:arg-deploy-only
set deploy-only=1
goto :args-continue

:arg-version
shift
if "%1" equ "" goto :usage
set image-version=%1
goto :args-continue

:arg-repository
shift
if "%1" equ "" goto :usage
set repository=%1
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

:arg-name-prefix
shift
if "%1" equ "" goto :usage
set name-prefix=%1
goto :args-continue

@rem
@rem Validate command line
@rem
:args-done
if "%deploy-only%" == "1" set build= && goto :main
set current_docker_mode=linux
set docker_cli_exe=%ProgramFiles%\Docker\Docker\DockerCli.exe
if exist "%docker_cli_exe%" call :set_current_mode
set original_docker_mode=%current_docker_mode%
if not "%repository%" == "" if "%image-version%" == "" goto :usage
goto :main

@rem
@rem Build and push docker images
@rem
:build
call :build_%1
if exist _tmp.out del _tmp.out
goto :eof

@rem
@rem Build, and optionally tag and push docker images
@rem
:build_image
set _cmd_line=
set _cmd_line=%_cmd_line% -n %1
set _cmd_line=%_cmd_line% -f %2
if not "%3" == "" set _cmd_line=%_cmd_line% -p %3
if not "%image-version%" == "" set _cmd_line=%_cmd_line% -v %image-version%
if not "%repository%" == "" set _cmd_line=%_cmd_line% -r %repository%
cmd /c %current-path%\build.cmd %_cmd_line%
goto :eof

@rem
@rem Build and push linux docker images
@rem
:build_linux
call :switch_to_mode linux
echo Building Linux images...
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-opc-twin-service OpcUa\Microsoft.Azure.IIoT.OpcUa.Twin\src\Dockerfile
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-opc-registry-service OpcUa\Microsoft.Azure.IIoT.OpcUa.Registry\src\Dockerfile
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-edge-opc-twin OpcUa\Microsoft.Azure.IIoT.OpcUa.Edge\src\Dockerfile
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-edge-opc-twin OpcUa\Microsoft.Azure.IIoT.OpcUa.Edge\src\Dockerfile.Debug -debug
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-opc-onboarding-service OpcUa\Microsoft.Azure.IIoT.OpcUa.Onboarding\src\Dockerfile
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-opc-twin-service-ctrl OpcUa\Microsoft.Azure.IIoT.OpcUa.Api\cli\Dockerfile
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :revert_to_original
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof

@rem
@rem Build and push windows docker images
@rem
:build_windows
call :switch_to_mode windows
echo Building Windows images...
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-opc-twin-service OpcUa\Microsoft.Azure.IIoT.OpcUa.Twin\src\Dockerfile.Windows -nanoserver-1709
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-opc-registry-service OpcUa\Microsoft.Azure.IIoT.OpcUa.Registry\src\Dockerfile.Windows -nanoserver-1709
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-edge-opc-twin OpcUa\Microsoft.Azure.IIoT.OpcUa.Edge\src\Dockerfile.Windows -nanoserver-1709
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :build_image iot-opc-onboarding-service OpcUa\Microsoft.Azure.IIoT.OpcUa.Onboarding\src\Dockerfile.Windows -nanoserver-1709
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :revert_to_original
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof

@rem
@rem Get current daemon mode
@rem
:set_current_mode
set current_docker_mode=
for /f "tokens=* delims= " %%i in ('docker version') do call :set_current_mode_1 %%i
goto :eof
:set_current_mode_1
@for /f "tokens=1*" %%i in ("%*") do if "%%i" == "OS/Arch:" call :set_current_mode_2 %%j
goto :eof
:set_current_mode_2
@for /f "delims=/" %%i in ("%*") do set current_docker_mode=%%i
goto :eof

@rem
@rem Switch to docker mode
@rem
:switch_to_mode
call :set_current_mode
if "%current_docker_mode%" == "%1" goto :eof
if not exist "%docker_cli_exe%" goto :switch_to_mode_error
echo Switching to %1
"%docker_cli_exe%" -SwitchDaemon
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :set_current_mode
if "%current_docker_mode%" == "%1" goto :eof
:switch_to_mode_error
echo ERROR: Not able to switch to %1 mode
exit /b 9
goto :eof

@rem
@rem Revert back to original
@rem
:revert_to_original
call :set_current_mode
if "%original_docker_mode%" == "%current_docker_mode%" goto :eof
echo Switching back to %original_docker_mode%...
"%docker_cli_exe%" -SwitchDaemon
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :set_current_mode
if "%current_docker_mode%" == "%original_docker_mode%" goto :eof
echo ERROR: Failed to revert back to %original_docker_mode%
exit /b 10
goto :eof

@rem
@rem Deploy images
@rem
:deploy
if "%build%" == "windows" if not "%deploy-only%" == "1" goto :deploy_skip
if "%repository%" == "" goto :deploy_skip
if "%resource-group%" == "" goto :deploy_skip
if "%image-version%" == "" set image-version=latest

rem set and check environment
echo Update environment...
set _cmd_line=
if not "%subscription%" == "" set _cmd_line=%_cmd_line% -s %subscription%
set _cmd_line=%_cmd_line% -g %resource-group%
cmd /c %current-path%\setenv.cmd %_cmd_line%
if "%_HUB_CS%" == "" echo Set _HUB_CS variable && goto :deploy_skip
if "%_STORE_CS%" == "" echo Set _STORE_CS variable && goto :deploy_skip
if "%_EH_CS%" == "" echo Set _EH_CS variable && goto :deploy_skip

echo Deploy onboarding service...
set _cmd_line=
set _cmd_line=%_cmd_line% -i %repository%/iot-opc-onboarding-service:%image-version%
set _cmd_line=%_cmd_line% -n %name-prefix%opconboarding
set _cmd_line=%_cmd_line% -g %resource-group%
set _cmd_line=%_cmd_line% -e "_HUB_CS=%_HUB_CS%"
set _cmd_line=%_cmd_line% -e "_STORE_CS=%_STORE_CS%"
set _cmd_line=%_cmd_line% -e "_EH_CS=%_EH_CS%"
if not "%subscription%" == "" set _cmd_line=%_cmd_line% -s %subscription%
if not "%location%" == "" set _cmd_line=%_cmd_line% -l %location%
cmd /c %current-path%\deploy.cmd -t instance %_cmd_line%
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!

echo Deploy twin web service...
set _cmd_line=
set _cmd_line=%_cmd_line% -i %repository%/iot-opc-twin-service:%image-version%
set _cmd_line=%_cmd_line% -n %name-prefix%twin
set _cmd_line=%_cmd_line% -g %resource-group%
set _cmd_line=%_cmd_line% -e "_HUB_CS=%_HUB_CS%"
if not "%subscription%" == "" set _cmd_line=%_cmd_line% -s %subscription%
if not "%location%" == "" set _cmd_line=%_cmd_line% -l %location%
cmd /c %current-path%\deploy.cmd -t webapp %_cmd_line%
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!

echo Deploy registry web service...
set _cmd_line=
set _cmd_line=%_cmd_line% -i %repository%/iot-opc-registry-service:%image-version%
set _cmd_line=%_cmd_line% -n %name-prefix%registry
set _cmd_line=%_cmd_line% -g %resource-group%
set _cmd_line=%_cmd_line% -e "_HUB_CS=%_HUB_CS%"
if not "%subscription%" == "" set _cmd_line=%_cmd_line% -s %subscription%
if not "%location%" == "" set _cmd_line=%_cmd_line% -l %location%
cmd /c %current-path%\deploy.cmd -t webapp %_cmd_line%
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!

goto :eof
:deploy_skip
echo skipping deploy...
if "%deploy-only%" == "1" exit /b 2
goto :eof

@rem
@rem Main
@rem
:main
for %%i in (%build%) do (
    call :build %%i
    if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
)
call :deploy
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
endlocal

