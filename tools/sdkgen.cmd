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
echo sdkgen.cmd [options]
echo options:
echo -x --xtrace        print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue

:args-done
goto :main


rem
rem generate sdk
rem
:generate_sdk

docker pull azuresdk/autorest:latest
if exist %build_root%\api\generated rmdir /s /q %build_root%\api\generated
mkdir %build_root%\api\generated
pushd %build_root%\api\generated

call :generate_sdk_for_service twin
call :generate_sdk_for_service registry
call :generate_sdk_for_service history
call :generate_sdk_for_service vault

popd
goto :eof

:generate_sdk_for_service
set service=%1
copy %build_root%\docs\api\%service%\autorest.md readme.md
copy %build_root%\api\swagger\%service%.json swagger.json
set args=--input-file=/opt/swagger.json
docker run --rm --mount type=bind,source=%cd%,target=/opt -w /opt azuresdk/autorest:latest %args%
if exist swagger.json del /f swagger.json
if exist readme.md del /f readme.md
set args=
set service=
goto :eof

@rem
@rem Main
@rem
:main
call :generate_sdk
endlocal

