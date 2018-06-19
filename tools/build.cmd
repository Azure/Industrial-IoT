@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%

set build_root=%current-path%\..
set repository=
set image-version=
set image-name=
set dockerfile=
set postfix=

cmd /c docker version > NUL 2>&1
if not !ERRORLEVEL! == 0 echo ERROR: Install docker! && exit /b !ERRORLEVEL!

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--xtrace" goto :arg-trace
if "%1" equ  "-x" goto :arg-trace
if "%1" equ "--version" goto :arg-version
if "%1" equ  "-v" goto :arg-version
if "%1" equ "--repository" goto :arg-repository
if "%1" equ  "-r" goto :arg-repository
if "%1" equ "--name" goto :arg-name
if "%1" equ  "-n" goto :arg-name
if "%1" equ "--file" goto :arg-dockerfile
if "%1" equ  "-f" goto :arg-dockerfile
if "%1" equ "--postfix" goto :arg-postfix
if "%1" equ  "-p" goto :arg-postfix
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo build.cmd [options]
echo options:
echo -n --name ^<value^>            Image name [mandatory].
echo -f --file ^<value^>            Docker file location [mandatory].
echo -r --repository ^<value^>      Repository to optionally push images to.
echo -v --version ^<value^>         Image version (e.g. 1.0.4) (to tag).
echo -p --postfix ^<value^>         Image tag postfix to append to tag.
echo -x --xtrace                  print a trace of each command.
exit /b 1

:arg-trace
echo on
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

:arg-name
shift
if "%1" equ "" goto :usage
set image-name=%1
goto :args-continue

:arg-dockerfile
shift
if "%1" equ "" goto :usage
set dockerfile=%1
goto :args-continue

:arg-postfix
shift
if "%1" equ "" goto :usage
set postfix=%1
goto :args-continue

@rem
@rem Validate command line
@rem
:args-done
if "%image-name%" == "" goto :usage
if "%dockerfile%" == "" goto :usage
if "%repository%" == "" goto :main
set repository=%repository%/
if "%image-version%" == "" goto :usage
goto :main

@rem
@rem Build, and optionally tag and push docker images
@rem
:build_tag_and_push
echo Building %image-name%:latest%postfix% from %dockerfile%...
cmd /c docker build -t %image-name%:latest%postfix% -f %dockerfile% . > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
if "%image-version%" == "" goto :eof
cmd /c docker tag %image-name%:latest%postfix% %image-name%:%image-version%%postfix% > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
if "%repository%" == "" goto :eof
echo Pushing %image-name%:latest%postfix% ...
cmd /c docker tag %image-name%:latest%postfix% %repository%%image-name%:latest%postfix% > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
cmd /c docker tag %image-name%:latest%postfix% %repository%%image-name%:%image-version%%postfix%  > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
cmd /c docker push %repository%%image-name%:%image-version%%postfix% > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
cmd /c docker push %repository%%image-name%:latest%postfix% > "%TEMP%\_build.out"
if not !ERRORLEVEL! == 0 type "%TEMP%\_build.out" && exit /b !ERRORLEVEL!
goto :eof

@rem
@rem Main
@rem
:main
pushd %build_root%
call :build_tag_and_push
if exist "%TEMP%\_build.out" del "%TEMP%\_build.out"
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
endlocal

