@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

set module-name=opc-twin
set repo-name=%REPOSITORY%
set clean-build=

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--xtrace" goto :arg-trace
if "%1" equ  "-x" goto :arg-trace
if "%1" equ "--module-name" goto :arg-module-name
if "%1" equ  "-m" goto :arg-module-name
if "%1" equ "--clean" goto :arg-clean
if "%1" equ  "-c" goto :arg-clean
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo edgedbg.cmd [options]
echo options:
echo -m --module-name             The name of the module - default opc-twin.
echo -m --repo-name               The name of the repository - default to localhost.
echo -c --clean                   Force recreate the image
echo -x --xtrace                  print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue

:arg-clean
set clean-build=--no-cache
goto :args-continue

:arg-module-name
shift
if "%1" equ "" call :usage && exit /b 1
set module-name=%1
goto :args-continue

:arg-repo-name
shift
if "%1" equ "" call :usage && exit /b 1
set repo-name=%1/azure-iiot-
goto :args-continue

:args-done
if "%repo-name%" equ "" set repo-name=localhost:5000/azure-iiot-
if "%module-name%" equ "" set module-name=opc-twin
if not "%clean-build%" equ "" echo Clean build
goto :main

:main
rem build module
pushd %build_root%
docker build -f modules\Dockerfile.%module-name% %clean-build% -t %repo-name%%module-name%:debug .
docker push %repo-name%%module-name%:debug
popd