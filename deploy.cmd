@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
pushd %current-path%\deploy\scripts

set type=
if /i "%1" == "local" set type=local
if /i "%1" == "services" set type=services
if not "%type%" == "" shift && goto main
set type=all
:main
shift
powershell ./deploy.ps1 -type %type% %*
popd