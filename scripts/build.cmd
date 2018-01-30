@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Usage:
:: Build the project in the local environment:  scripts\build
:: Build the project inside a Docker container: scripts\build -s
:: Build the project inside a Docker container: scripts\build --in-sandbox

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

IF "%1"=="-s" GOTO :RunInSandbox
IF "%1"=="--in-sandbox" GOTO :RunInSandbox


:RunLocally

    :: Check dependencies
    dotnet --version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOTNET

    :: Check settings
    call .\scripts\env-vars-check.cmd
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    :: Restore nuget packages and compile the application
    echo Downloading dependencies...
    call dotnet restore
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL
    echo Compiling code...
    call dotnet build --configuration %CONFIGURATION%
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    :: Find all the test assemblies and run the tests
    echo Running tests...
    for /d %%i in (*.Test) do (
        dotnet test %%i\%%i.csproj
        IF !ERRORLEVEL! NEQ 0 GOTO FAIL
    )

    goto :END


:RunInSandbox

    :: Folder where PCS sandboxes cache data. Reuse the same folder to speed up the
    :: sandbox and to save disk space.
    :: Use PCS_CACHE="%APP_HOME%\.cache" to cache inside the project folder
    SET PCS_CACHE="%TMP%\azure\iotpcs\.cache"

    :: Check dependencies
    docker version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

    :: Create cache folders to speed up future executions
    mkdir %PCS_CACHE%\sandbox\.config > NUL 2>&1
    mkdir %PCS_CACHE%\sandbox\.dotnet > NUL 2>&1
    mkdir %PCS_CACHE%\sandbox\.nuget > NUL 2>&1
    echo Note: caching build files in %PCS_CACHE%

    :: Start the sandbox and execute the build script
    docker run ^
        -e "PCS_IOTHUB_CONNSTRING=%PCS_IOTHUB_CONNSTRING%" ^
        -e "PCS_IOTHUBMANAGER_WEBSERVICE_URL=%PCS_IOTHUBMANAGER_WEBSERVICE_URL%" ^
        -e "PCS_AUTH_ISSUER=%PCS_AUTH_ISSUER%" ^
        -e "PCS_AUTH_AUDIENCE=%PCS_AUTH_AUDIENCE%" ^
        -v %PCS_CACHE%\sandbox\.config:/root/.config ^
        -v %PCS_CACHE%\sandbox\.dotnet:/root/.dotnet ^
        -v %PCS_CACHE%\sandbox\.nuget:/root/.nuget ^
        -v %APP_HOME%:/opt/code ^
        azureiotpcs/code-builder-dotnet:1.0-dotnetcore /opt/scripts/build

    :: Error 125 typically triggers on Windows if the drive is not shared
    IF %ERRORLEVEL% EQU 125 GOTO DOCKER_SHARE
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    goto :END


:: - - - - - - - - - - - - - -
goto :END

:MISSING_DOTNET
    echo ERROR: 'dotnet' command not found.
    echo Install .NET Core 1.1.2 and make sure the 'dotnet' command is in the PATH.
    echo Nuget installation: https://dotnet.github.io/
    exit /B 1

:DOCKER_SHARE
    echo ERROR: the drive containing the source code cannot be mounted.
    echo Open Docker settings from the tray icon, and fix the settings under 'Shared Drives'.
    exit /B 1

:MISSING_DOCKER
    echo ERROR: 'docker' command not found.
    echo Install Docker and make sure the 'docker' command is in the PATH.
    echo Docker installation: https://www.docker.com/community-edition#/download
    exit /B 1

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:END
endlocal
