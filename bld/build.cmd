@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%

set repo-root=%current-path%\..
rem // resolve to fully qualified path
for %%i in ("%repo-root%") do set repo-root=%%~fi

set build-sdk-root=%repo-root%\..\azure-iot-gateway-sdk
for %%i in ("%build-sdk-root%") do set build-sdk-root=%%~fi

rem ----------------------------------------------------------------------------
rem -- parse script arguments
rem ----------------------------------------------------------------------------

rem // default build options
set build-clean=
set build-configs=
set build-platform=Win32
if "%PROCESSOR_ARCHITECTURE%" == "AMD64" set build-platform=x64
set build-runtime=
set build-root=%repo-root%\build
set build-rel-root=%build-root%\release

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--config" goto :arg-build-config
if "%1" equ  "-C" goto :arg-build-config
if "%1" equ "--clean" goto :arg-build-clean
if "%1" equ  "-c" goto :arg-build-clean
if "%1" equ "--output" goto :arg-build-rel-root
if "%1" equ  "-o" goto :arg-build-rel-root
if "%1" equ "--sdk-root" goto :arg-sdk-root-folder
if "%1" equ  "-i" goto :arg-sdk-root-folder
if "%1" equ "--platform" goto :arg-build-platform
if "%1" equ  "-p" goto :arg-build-platform
if "%1" equ "--runtime" goto :arg-build-runtime
if "%1" equ  "-r" goto :arg-build-runtime
call :usage && exit /b 1

:arg-build-clean
set build-clean=1
goto :args-continue

:arg-build-config
shift
if "%1" equ "" call :usage && exit /b 1
set build-configs=%build-configs%%1 
goto :args-continue

:arg-sdk-root-folder
shift
if "%1" equ "" call :usage && exit /b 1
set build-sdk-root=%1
goto :args-continue

:arg-build-platform
shift
if "%1" equ "" call :usage && exit /b 1
set build-platform=%1
goto :args-continue

:arg-build-runtime
shift
if "%1" equ "" call :usage && exit /b 1
set build-runtime=-r %1
goto :args-continue

:arg-build-rel-root
shift
if "%1" equ "" call :usage && exit /b 1
set build-rel-root=%1

goto :args-continue
:args-continue
shift
goto :args-loop

:args-done
call dotnet --version
if not !ERRORLEVEL! == 0 echo No dotnet installed, install first... && exit /b 1
if not exist "%build-sdk-root%\tools\build.cmd" echo no sdk installed at %build-sdk-root%, only building module... && set build-sdk-root=
if "%build-configs%" == "" set build-configs=Release Debug 

rem // Start script
echo Building %build-configs%...
if not "%build-clean%" == "" (
    echo Cleaning previous build output...
    call :rmdir-force %build-root%
)
if not exist %build-root% mkdir %build-root%
call :sdk-build
if not !ERRORLEVEL! == 0 echo Failures during sdk build... && exit /b !ERRORLEVEL!
call :module-build
if not !ERRORLEVEL! == 0 echo Failures during dotnet build... && exit /b !ERRORLEVEL!
call :release-all
if not !ERRORLEVEL! == 0 echo Failures building release... && exit /b !ERRORLEVEL!
goto :build-done

rem -----------------------------------------------------------------------------
rem -- build the sdk
rem -----------------------------------------------------------------------------
:sdk-build
if "%build-sdk-root%" == "" goto :eof
rem // Build the sdk for all configurations
for %%c in (%build-configs%) do (
    pushd "%build-sdk-root%\tools
    call :sdk-build-and-test %%c
    popd
    if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
)
goto :eof
rem // Build the sdk for 1 configuration
:sdk-build-and-test
if /I not "%~1" == "Release" if /I not "%~1" == "Debug" if /I not "%~1" == "MinSizeRel" if /I not "%~1" == "RelWithDebInfo" goto :eof
rem // If incremental, check if we had a successful build before...
if exist %build-root%\sdk\%build-platform%-%~1.done goto :eof
rem // Force clean cmake output and install-deps to avoid errors.
call :rmdir-force %build-sdk-root%\build
call :rmdir-force %build-sdk-root%\install-deps
rem // Build sdk
echo Building SDK (%~1) ...
call build.cmd --config %~1 --platform %build-platform% --enable-dotnet-core-binding --disable-ble-module
echo Finished building SDK (%~1)
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
rem // Copy build output over and mark it as successfully built.
if not exist "%build-root%\sdk\%build-platform%\%~1" mkdir "%build-root%\sdk\%build-platform%\%~1"
xcopy /e /i /y /q "%build-sdk-root%\build" "%build-root%\sdk\%build-platform%\%~1"
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
echo %~1 >> %build-root%\sdk\%build-platform%-%~1.done
goto :eof

rem -----------------------------------------------------------------------------
rem -- build module
rem -----------------------------------------------------------------------------
:module-build
rem // Clean 
:dotnet-clean
if "%build-clean%" == "" goto :dotnet-build
call :dotnet-project-clean %repo-root%\src\Opc.Ua.Client.Module
call :dotnet-project-clean %repo-root%\bld\publish

rem // Build
:dotnet-build
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
rem // Build and publish all specified configurations
for %%c in (%build-configs%) do (
	call :dotnet-build-and-publish %%c
    if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
)
goto :eof

rem // Build module and publish for 1 configuration
:dotnet-build-and-publish
if /I not "%~1" == "Release" if /I not "%~1" == "Debug" if /I not "%~1" == "Signed" goto :eof
pushd %repo-root%
rem // Restore packages
call dotnet restore
call dotnet build %build-runtime% -c %~1
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
rem // Publish all assemblies using publish dummy exe
pushd %repo-root%\bld\publish
rem // Restore packages
call dotnet restore
call dotnet publish %build-runtime% -c %~1 -o "%build-root%\module\%~1"
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof

rem // Clean a project
:dotnet-project-clean
pushd "%~1"
call dotnet clean
popd
goto :eof

rem -----------------------------------------------------------------------------
rem -- Copy everything into a final release folder
rem -----------------------------------------------------------------------------
:release-all
for %%c in (%build-configs%) do (
    call :release-binaries %%c
    if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
)
goto :eof
:release-binaries
rem // Clean release folder
if not exist "%build-rel-root%\%~1" mkdir "%build-rel-root%\%~1"
del /f /q "%build-rel-root%\%~1\*"

rem // Flatten runtimes for windows (TODO: Should be done by loader)
xcopy /y /i /q "%build-root%\module\%~1" "%build-rel-root%\%~1"
pushd "%build-root%\module\%~1\runtimes\win"
for /f %%i in ('dir /b /s *.dll') do copy /y "%%i" "%build-rel-root%\%~1"
popd
pushd "%build-root%\module\%~1\runtimes\win7"
for /f %%i in ('dir /b /s *.dll') do copy /y "%%i" "%build-rel-root%\%~1"
popd
rem // Copy configuration json
copy /y "%repo-root%\samples\*.json" "%build-rel-root%\%~1"
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
rem // Delete unnecessary publish.dll and .pdb
pushd "%build-rel-root%\%~1"
del /q /f publish.*
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!

if "%build-sdk-root%" == "" goto :eof
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
rem // Copy a sample gw host.exe, iothub.dll, iothub_client.dll
pushd %build-root%\sdk\%build-platform%\%~1
xcopy /e /y /i /q "samples\azure_functions_sample\%~1" "%build-rel-root%\%~1"
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
pushd %build-root%\sdk\%build-platform%\%~1
xcopy /e /y /i /q "samples\dotnet_core_module_sample\%~1" "%build-rel-root%\%~1"
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
pushd %build-root%\sdk\%build-platform%\%~1
xcopy /e /y /i /q "modules\iothub\%~1" "%build-rel-root%\%~1"
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
rem // rename sample host.exe and delete unneeded files
pushd "%build-rel-root%\%~1"
copy /y dotnet_core_module_sample.* sample_gateway.*
del /q /f dotnet_core_module_sample.*
del /q /f printermodule.*
del /q /f sensormodule.*
popd
if not !ERRORLEVEL! == 0 exit /b !ERRORLEVEL!
goto :eof

rem -----------------------------------------------------------------------------
rem -- subroutines
rem -----------------------------------------------------------------------------

:rmdir-force
set _attempt=0
:try-rmdir
if not exist %1 goto :done-rmdir
set /a _attempt+=1
if !_attempt! == 30 goto :done-rmdir
echo Removing %~1 (%_attempt%)...
rmdir /s /q %1
goto :try-rmdir
:done-rmdir
set _attempt=
goto :eof

:build-done
echo ... Success!
goto :eof

:usage
echo build.cmd [options]
echo options:
echo -c --clean                  Build clean (Removes previous build output).
echo -C --config ^<value^>         [Debug, Release] build configuration
echo -r --runtime ^<value^>        [win] The runtime to build module for.
echo -i --sdk-root ^<value^>       [../azure-iot-gateway-sdk] Gateway SDK repo root.
echo -p --platform ^<value^>       [Win32] build platform (e.g. Win32, x64, ...).
echo -o --output ^<value^>         [/build/release] Root in which to place release.
goto :eof

