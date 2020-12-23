@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%

set PCS_KEYVAULT_URL=
set PCS_KEYVAULT_APPID=
set PCS_KEYVAULT_SECRET=
set PCS_AUTH_TENANT=

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--keyvault-url" goto :arg-url
if "%1" equ  "-u" goto :arg-url
if "%1" equ "--sp-appid" goto :arg-appid
if "%1" equ  "-i" goto :arg-appid
if "%1" equ "--sp-secret" goto :arg-secret
if "%1" equ  "-p" goto :arg-secret
if "%1" equ "--sp-tenant" goto :arg-tenant
if "%1" equ  "-t" goto :arg-tenant
goto :usage

:args-continue
shift
goto :args-loop

:usage
echo. 
echo Industrial IoT platform services command line interface (CLI)
echo.
echo Usage:                 cli.cmd [options] 
echo Run with a .env file in the root, or specify the following options:
echo. 
echo -u --keyvault-url      The keyvault url to access.
echo -i --sp-appid          The service principal to use to access keyvault
echo -p --sp-secret         The secret key of the principal identity
echo -t --sp-tenant         The tenant of the service principal
echo. 
exit /b 1

:arg-url
shift
if "%1" equ "" call :usage && exit /b 1
set PCS_KEYVAULT_URL=%1
goto :args-continue
:arg-appid
shift
if "%1" equ "" call :usage && exit /b 1
set PCS_KEYVAULT_APPID=%1
goto :args-continue
:arg-secret
shift
if "%1" equ "" call :usage && exit /b 1
set PCS_KEYVAULT_SECRET=%1
goto :args-continue
:arg-tenant
shift
if "%1" equ "" call :usage && exit /b 1
set PCS_AUTH_TENANT=%1
goto :args-continue
:args-done

:main
pushd %build_root%\api\src\Microsoft.Azure.IIoT.Api\cli
dotnet run console
popd

set PCS_KEYVAULT_URL=
set PCS_KEYVAULT_APPID=
set PCS_KEYVAULT_SECRET=
set PCS_AUTH_TENANT=
goto :eof

