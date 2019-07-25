@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

pushd %build_root%
cmd /c npm install -g markdown-link-validator > _tmp.out 2>&1

if "%1" == "-q" goto :quiet
if "%1" == "--quiet" goto :quiet

call markdown-link-validator . -i #.* -f gi
if !ERRORLEVEL! == 0 goto :success
goto :error
:quiet
call markdown-link-validator . -i #.* -f gi > _tmp.out 2>&1
if !ERRORLEVEL! == 0 goto :success
goto :error
:error
if exist _tmp.out del /f _tmp.out
popd
exit /b 1
:success
if exist _tmp.out del /f _tmp.out
popd
goto :eof