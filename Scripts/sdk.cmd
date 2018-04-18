@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%

set build_root=%current-path%\..
set root=
set config=

cmd /c autorest --help > NUL 2>&1
if not !ERRORLEVEL! == 0 echo ERROR: Install autorest && exit /b !ERRORLEVEL!

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--xtrace" goto :arg-trace
if "%1" equ  "-x" goto :arg-trace
if "%1" equ "--config" goto :arg-config
if "%1" equ  "-c" goto :arg-config
if "%1" equ "--root" goto :arg-root
if "%1" equ  "-r" goto :arg-root
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo sdk.cmd [options]
echo options:
echo -r --root ^<value^>            Project root to generate for [mandatory].
echo -c --config ^<value^>          Configuration file [mandatory].
echo -x --xtrace                  print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue

:arg-config
shift
if "%1" equ "" goto :usage
set config=%1
goto :args-continue

:arg-root
shift
if "%1" equ "" goto :usage
set root=%1
goto :args-continue

@rem
@rem Validate command line
@rem
:args-done
if "%root%" == "" goto :usage
if "%config%" == "" goto :usage
if not exist %build_root%\%root% goto :usage
if not exist %build_root%\%config% goto :usage
goto :main

@rem
@rem Build, and generate sdk
@rem
:build_and_generate
echo Building %image-name%:latest%postfix% from %dockerfile%...
cmd /c dotnet build -c Release
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
cmd /c dotnet swagger tofile --output %build_root%\%config%\swagger.json bin
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof

@rem
@rem Main
@rem
:main
pushd %build_root%\%root%
call :build_and_generate
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
endlocal

