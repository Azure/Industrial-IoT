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
echo docgen.cmd [options]
echo options:
echo -x --xtrace        print a trace of each command.
exit /b 1

:arg-trace
echo on
goto :args-continue

:args-done
goto :main

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

call :generate_doc_for_service twin
call :generate_doc_for_service registry
call :generate_doc_for_service history
call :generate_doc_for_service vault

set service=
set convert=
if exist config.properties del /f config.properties
popd
goto :eof

rem 
rem generate doc
rem
:generate_doc_for_service
set service=%1
if not exist %build_root%\api\swagger\%service%.json goto :eof
copy %build_root%\api\swagger\%service%.json %service%\swagger.json
if exist %service%\security.md move %service%\security.md %service%\security_save.md
%convert% -i /opt/%service%/swagger.json -d /opt/%service% -c /opt/config.properties
if exist %service%\security_save.md move %service%\security_save.md %service%\security.md
if exist %service%\paths.md type %service%\paths.md >> %service%\readme.md
if exist %service%\paths.md del /f %service%\paths.md
if exist %service%\swagger.json del /f %service%\swagger.json
goto :eof


@rem
@rem Main
@rem
:main
call :generate_docs
endlocal

