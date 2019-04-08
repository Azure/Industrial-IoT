@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--xtrace" goto :arg-trace
if "%1" equ  "-x" goto :arg-trace
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo sdk.cmd [options]
echo options:
echo -x --xtrace                  print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue

:args-done
goto :main

rem
rem wait until listening
rem
:wait_up
:wait_up_0
set _registry_up=
set _twin_up=
set _history_up=
for /f %%i in ('netstat -na ^| findstr "9041" ^| findstr "LISTENING"') do set _twin_up=1
for /f %%i in ('netstat -na ^| findstr "9042" ^| findstr "LISTENING"') do set _registry_up=1
for /f %%i in ('netstat -na ^| findstr "9043" ^| findstr "LISTENING"') do set _history_up=1
if "%_twin_up%" == "" goto :wait_up_0
if "%_registry_up%" == "" goto :wait_up_0
if "%_history_up%" == "" goto :wait_up_0
ping nowhere -w 5000 >nul 2>&1
goto :eof

rem
rem generate docs
rem
:generate_docs
if not exist %build_root%\docs\api mkdir %build_root%\docs\api
pushd %build_root%\docs\api
echo swagger2markup.markupLanguage=MARKDOWN                      > config.properties
echo swagger2markup.generatedExamplesEnabled=false              >> config.properties
echo swagger2markup.pathsGroupedBy=TAGS                         >> config.properties
echo swagger2markup.inlineSchemaEnabled=true                    >> config.properties
echo swagger2markup.lineSeparator=WINDOWS                       >> config.properties
echo swagger2markup.pathSecuritySectionEnabled=true             >> config.properties
echo swagger2markup.flatBodyEnabled=false                       >> config.properties
echo swagger2markup.interDocumentCrossReferencesEnabled=true    >> config.properties
echo swagger2markup.overviewDocument=readme                     >> config.properties
rem echo swagger2markup.separatedDefinitionsEnabled=true            >> config.properties
rem echo swagger2markup.separatedOperationsEnabled=true             >> config.properties

docker pull swagger2markup/swagger2markup:latest
set convert=docker run --rm --mount type=bind,source=%cd%,target=/opt swagger2markup/swagger2markup:latest convert

if exist twin\security.md move twin\security.md twin\security_save.md
%convert% -i http://%_hostname%:9041/v2/swagger.json -d /opt/twin -c /opt/config.properties
if exist twin\security_save.md move twin\security_save.md twin\security.md
if exist twin\paths.md type twin\paths.md >> twin\readme.md
if exist twin\paths.md del /f twin\paths.md

if exist registry\security.md move registry\security.md registry\security_save.md
%convert% -i http://%_hostname%:9042/v2/swagger.json -d /opt/registry -c /opt/config.properties
if exist registry\security_save.md move registry\security_save.md registry\security.md
if exist registry\paths.md type registry\paths.md >> registry\readme.md
if exist registry\paths.md del /f registry\paths.md

if exist history\security.md move history\security.md history\security_save.md
%convert% -i http://%_hostname%:9043/v2/swagger.json -d /opt/history -c /opt/config.properties
if exist history\security_save.md move history\security_save.md history\security.md
if exist history\paths.md type history\paths.md >> history\readme.md
if exist history\paths.md del /f history\paths.md

set convert=
if exist config.properties del /f config.properties
popd
goto :eof

rem
rem generate sdk
rem
:generate_sdk
docker pull azuresdk/autorest:latest
if exist %build_root%\api\generated rmdir /s /q %build_root%\api\generated
mkdir %build_root%\api\generated
pushd %build_root%\api\generated

rem generate twin sdk
copy %build_root%\docs\api\twin\autorest.md readme.md
set args=--input-file=http://%_hostname%:9041/v2/swagger.json
docker run --rm --mount type=bind,source=%cd%,target=/opt -w /opt azuresdk/autorest:latest %args%
rem generate registry sdk
copy %build_root%\docs\api\registry\autorest.md readme.md
set args=--input-file=http://%_hostname%:9042/v2/swagger.json
docker run --rm --mount type=bind,source=%cd%,target=/opt -w /opt azuresdk/autorest:latest %args%
rem generate history sdk
copy %build_root%\docs\api\history\autorest.md readme.md
set args=--input-file=http://%_hostname%:9042/v2/swagger.json
docker run --rm --mount type=bind,source=%cd%,target=/opt -w /opt azuresdk/autorest:latest %args%

set args=
del /f readme.md
popd
goto :eof

:retrieve_spec
if not exist %build_root%\swagger mkdir %build_root%\swagger
pushd %build_root%\swagger
curl -o twin.json http://%_hostname%:9041/v2/swagger.json
curl -o registry.json http://%_hostname%:9042/v2/swagger.json
curl -o history.json http://%_hostname%:9043/v2/swagger.json
popd
goto :eof

@rem
@rem Main
@rem
:main
pushd %build_root%
rem start services
echo Rebuilding...
docker-compose build --no-cache > %TMP%\sdk_build.log 2>&1
if not !ERRORLEVEL! == 0 type %TMP%\sdk_build.log && goto :done

echo Starting...
rem force https scheme only
set PCS_AUTH_HTTPSREDIRECTPORT=443
docker-compose up -d
call :wait_up
echo ... Up.

for /f %%i in ('hostname') do set _hostname=%%i
if "%_hostname%" == "" set _hostname=localhost

echo Generate docs.
call :generate_docs
echo Retrieve spec.
call :retrieve_spec
echo Generate sdk.
call :generate_sdk

rem stop services
docker-compose down >nul 2>&1
echo ... Down.
if "%REPOSITORY%" == "" goto :done
docker-compose push
:done
if exist %TMP%\sdk_build.log del /f %TMP%\sdk_build.log
set PCS_AUTH_HTTPSREDIRECTPORT=
popd
endlocal

