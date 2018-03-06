@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE=microsoft/iot-opc-twin-service
:: "testing" is the latest dev build, usually matching the code in the "master" branch
SET DOCKER_TAG=%DOCKER_IMAGE%:testing

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\docker\") => 16
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-16%
cd %APP_HOME%

:: Check dependencies
    dotnet --version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOTNET
    docker version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER
    git version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_GIT

:: Restore packages and build the application
    call dotnet restore
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL
    call dotnet build --configuration %CONFIGURATION%
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: Build the container image
    git log --pretty=format:%%H -n 1 > tmpfile.tmp
    SET /P COMMIT=<tmpfile.tmp
    DEL tmpfile.tmp
    SET DOCKER_LABEL2=Commit=%COMMIT%

    rmdir /s /q out\docker
    rmdir /s /q WebService\bin\Docker
    
    mkdir out\docker\webservice
    
    dotnet publish WebService      --configuration %CONFIGURATION% --output bin\Docker
    
    xcopy /s WebService\bin\Docker\*       out\docker\webservice\
    
    copy scripts\docker\.dockerignore               out\docker\
    copy scripts\docker\Dockerfile                  out\docker\
    copy scripts\docker\content\run.sh              out\docker\
    
    cd out\docker\
    docker build --squash --compress --tag %DOCKER_TAG% --label "%DOCKER_LABEL2%" .
    
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: - - - - - - - - - - - - - -
goto :END

:MISSING_DOTNET
    echo ERROR: 'dotnet' command not found.
    echo Install .NET Core 1.1.2 and make sure the 'dotnet' command is in the PATH.
    echo Nuget installation: https://dotnet.github.io/
    exit /B 1

:MISSING_DOCKER
    echo ERROR: 'docker' command not found.
    echo Install Docker and make sure the 'docker' command is in the PATH.
    echo Docker installation: https://www.docker.com/community-edition#/download
    exit /B 1

:MISSING_GIT
    echo ERROR: 'git' command not found.
    echo Install Git and make sure the 'git' command is in the PATH.
    echo Git installation: https://git-scm.com
    exit /B 1

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:END
endlocal
