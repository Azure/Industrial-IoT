@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off

if "%1" == "" goto :help
if not "%2" == "" set _sub_arg=-Subscription %2

powershell ./tools/scripts/get-env.ps1 -ResourceGroup %1 %_sub_arg% > .env
set _sub_arg=
goto :eof

:help
echo Please provide a resource group name as parameter.
exit /b 1