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
echo Commit and push all modules and root (%*)
call :push %*
goto :eof

:push
if exist %TEMP%\_commit.tmp del /f %TEMP%\_commit.tmp
if "%1" == "" notepad %TEMP%\_commit.tmp
if "%1" == "" if not exist %TEMP%\_commit.tmp goto :eof
for %%i in (%submodules%) do call :__in_repo %%i "%*"
call :__in_root "%*"
goto :eof

:__in_root
echo.
echo In root
pushd %repo-root%
call :__push %1
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
call :__push %2
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof

:__push
git add **
if exist %TEMP%\_commit.tmp git commit -a -F %TEMP%\_commit.tmp
if not exist %TEMP%\_commit.tmp git commit -am %1
git push
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof
