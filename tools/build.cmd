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