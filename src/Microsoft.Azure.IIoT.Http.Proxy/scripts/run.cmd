@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

:: Check dependencies
dotnet --version > NUL 2>&1
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOTNET

:: Restore nuget packages and compile the application
call dotnet restore
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

dotnet run --configuration %CONFIGURATION% --project ProxyAgent.csproj
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

goto :END


:: - - - - - - - - - - - - - -

:MISSING_DOTNET
    echo ERROR: 'dotnet' command not found.
    echo Install .NET Core 2 and make sure the 'dotnet' command is in the PATH.
    echo Nuget installation: https://dotnet.github.io/
    exit /B 1

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:END
endlocal
