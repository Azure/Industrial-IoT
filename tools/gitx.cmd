@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set repo-root=%current-path%\..

set submodules=%submodules% modules\opc-twin
set submodules=%submodules% modules\opc-publisher
set submodules=%submodules% components\opc-ua
set submodules=%submodules% samples\opc-twin-webui
set submodules=%submodules% samples\opc-vault-webui
set submodules=%submodules% services
set submodules=%submodules% common
set submodules=%submodules% api

shift
echo running "git %*" in all modules and root
for %%i in (%submodules%) do call :__in_repo %%i "git %*"
rem call :__in_root "git %*"
goto :eof

:__in_root
echo.
echo In root
pushd %repo-root%
call :__do %1
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof

:__in_repo
if "%1"==".git" goto :eof
if "%1"==".vs" goto :eof
if "%1"=="packages" goto :eof
if "%1"=="tools" goto :eof
if not exist %repo-root%\%1\.git goto :eof
echo.
echo In %1
pushd %repo-root%\%1
call :__do %2
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof

:__do
echo %~1
cmd /c %~1
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof
