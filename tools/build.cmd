@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off
if exist "%1" goto :build-path

:build-root
shift
powershell ./scripts/build.ps1 %*
goto :eof

:build-path
set p=%1
shift
shift
powershell ./scripts/build.ps1 -BuildRoot %p% %*
set p=
goto :eof