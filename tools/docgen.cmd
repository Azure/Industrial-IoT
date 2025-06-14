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
pushd %build_root%\docs

call :generate_doc_for_service opc-publisher

set service=
set convert=
popd
goto :eof

rem
rem generate doc
rem
:generate_doc_for_service
set service=%1
pushd %service%
if not exist openapi.json goto :eof
if exist security.md move security.md security_save.md
echo swagger2markup.markupLanguage=MARKDOWN                      > config.properties
echo swagger2markup.generatedExamplesEnabled=false              >> config.properties
echo swagger2markup.pathsGroupedBy=TAGS                         >> config.properties
echo swagger2markup.inlineSchemaEnabled=true                    >> config.properties
echo swagger2markup.lineSeparator=WINDOWS                       >> config.properties
echo swagger2markup.pathSecuritySectionEnabled=true             >> config.properties
echo swagger2markup.flatBodyEnabled=false                       >> config.properties
echo swagger2markup.interDocumentCrossReferencesEnabled=true    >> config.properties
rem echo swagger2markup.overviewDocument=overview               >> config.properties
echo swagger2markup.separatedDefinitionsEnabled=false           >> config.properties
echo swagger2markup.separatedOperationsEnabled=false            >> config.properties
docker run --rm --mount type=bind,source=%cd%,target=/opt swagger2markup/swagger2markup:1.3.1 convert -i /opt/openapi.json -d /opt -c /opt/config.properties
if exist security_save.md move security_save.md security.md
if exist paths.md type paths.md > api.md
if exist paths.md del /f paths.md
if exist overview.md del /f overview.md
if exist config.properties del /f config.properties
popd
goto :eof


@rem
@rem Main
@rem
:main
call :generate_docs
endlocal

