@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build-root=%current-path%\..

set repository=
set image-version=
set build=linux windows
set deploy-only=
set location=
set subscription=
set resource-group=
set name-prefix=
set projects=gateway registry twin onboarding

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

@rem
@rem Validate command line
@rem
:args-done
set current_docker_mode=linux
set docker_cli_exe=%ProgramFiles%\Docker\Docker\DockerCli.exe
if exist "%docker_cli_exe%" call :set_current_mode
set original_docker_mode=%current_docker_mode%
if not "%repository%" == "" if "%image-version%" == "" echo Must set an image version &&goto :usage
goto :main

@rem
@rem Build and push docker images
@rem
:build
pushd %build-root%
call :build_%1
popd
if exist _tmp.out del _tmp.out
goto :eof

@rem
@rem Build, and optionally tag and push docker images
@rem
:build_image
set dockerfile=%build-root%\%1
if not exist %dockerfile% goto :eof
set image-name=%2
set postfix=%3
echo Building %image-name%:latest%postfix% from %dockerfile%...
cmd /c docker build -t %image-name%:latest%postfix% -f %dockerfile% . > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
if "%image-version%" == "" goto :eof
cmd /c docker tag %image-name%:latest%postfix% %image-name%:%image-version%%postfix% > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
if "%repository%" == "" goto :eof
echo Pushing %image-name%:latest%postfix% ...
cmd /c docker tag %image-name%:latest%postfix% %repository%/%image-name%:latest%postfix% > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
cmd /c docker tag %image-name%:latest%postfix% %repository%/%image-name%:%image-version%%postfix%  > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
cmd /c docker push %repository%/%image-name%:%image-version%%postfix% > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
cmd /c docker push %repository%/%image-name%:latest%postfix% > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
goto :eof

@rem
@rem Build and push linux docker images
@rem
:build_linux
echo Building Linux images...
call :switch_to_mode linux
for %%i in (%projects%) do (
    call :build_image src\Microsoft.Azure.IIoT.OpcUa.Services.%%i\docker\Dockerfile azure-iiot-opc-ua-%%i-service
    if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
    call :build_image src\Microsoft.Azure.IIoT.OpcUa.Services.%%i\docker\Dockerfile.debug azure-iiot-opc-ua-%%i-service -debug
    if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
)
pushd %build-root%\reverse-proxy
call :build_image reverse-proxy\Dockerfile azure-iiot-reverse-proxy
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
call :revert_to_original
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof

@rem
@rem Build and push windows docker images
@rem
:build_windows
echo Building Windows images...
call :switch_to_mode windows
for %%i in (%projects%) do (
    call :build_image src\Microsoft.Azure.IIoT.OpcUa.Services.%%i\docker\Dockerfile.Windows azure-iiot-opc-ua-%%i-service -windows
    if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
)
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
@rem Main
@rem
:main
for %%i in (%build%) do (
    call :build %%i
    if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
)
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
endlocal

