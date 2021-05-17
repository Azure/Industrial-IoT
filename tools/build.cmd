@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
set script-name=%~nx0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

set _subscription=IOT-OPC-WALLS
set _resourceGroup=
set _location=westus
set _deploy=1
set _build=1
set _clean=
set _version=
set _sourceSubscription=IOT_GERMANY
set _sourceRegistry=industrialiot

:args-loop
if "%1" equ "" goto :args-done
if "%1" equ "--clean" goto :arg-clean
if "%1" equ  "-c" goto :arg-clean
if "%1" equ "--skip-deploy" goto :arg-no-deploy
if "%1" equ "--skip-build" goto :arg-no-build
if "%1" equ "--resourcegroup" goto :arg-resourcegroup
if "%1" equ  "-g" goto :arg-resourcegroup
if "%1" equ "--subscription" goto :arg-subscription
if "%1" equ  "-s" goto :arg-subscription
if "%1" equ "--location" goto :arg-location
if "%1" equ  "-l" goto :arg-location
if "%1" equ "--version" goto :arg-version
if "%1" equ  "-v" goto :arg-version
if "%1" equ "--help" goto :usage
if "%1" equ  "-h" goto :usage
goto :usage
:args-continue
shift
goto :args-loop

:usage
echo %script-name% [options]
echo options:
echo -g --resourcegroup Resource group name.
echo -s --subscription  Subscription name.
echo -l --location      Location to deploy to (%_location%).
echo -v --version       Version to deploy (instead of build).
echo -c --clean         Delete the resource group first.
echo    --skip-deploy   Do not deploy.
echo    --skip-build    Skip building
echo -h --help          This help.
exit /b 1

:arg-clean
set _clean=1
goto :args-continue

:arg-no-deploy
set _deploy=
goto :args-continue
:arg-no-build
set _build=
goto :args-continue
:arg-subscription
shift
set _subscription=%1
goto :args-continue
:arg-resourcegroup
shift
set _resourceGroup=%1
goto :args-continue
:arg-location
shift
set _location=%1
goto :args-continue
:arg-version
shift
set _version=%1
goto :args-continue
:args-done
goto :main

:main
if "%_resourceGroup%" == "" goto :usage
if "%_location%" == "" goto :usage
goto :clean

:clean
if not "%_clean%" == "1" goto :build
echo Clean...
cmd /c az group delete -y -g %_resourceGroup% > nul 2> nul
goto :build

:build
if not "%_build%" == "1" goto :copy
echo Build...
set __args=
set __args=%__args% -Subscription %_subscription%
set __args=%__args% -ResourceGroupLocation %_location%
set __args=%__args% -ResourceGroupName %_resourceGroup% 
pushd %build_root%\tools\scripts
powershell ./build.ps1 %__args%
popd
if !ERRORLEVEL! == 0 goto :copy
echo Build failed.
goto :done

:copy
if "%_version%" == "" goto :deploy
echo Copy...
set __args=
set __args=%__args% -BuildRegistry %_sourceRegistry%
set __args=%__args% -BuildSubscription %_sourceSubscription%
set __args=%__args% -ReleaseRegistry acr%_resourceGroup%
set __args=%__args% -ReleaseSubscription %_subscription%
set __args=%__args% -ResourceGroupLocation %_location%
set __args=%__args% -ResourceGroupName %_resourceGroup% 
set __args=%__args% -ReleaseVersion %_version%
set __args=%__args% -RemoveNamespaceOnRelease
set __args=%__args% -IsLatest
pushd %build_root%\tools\scripts
powershell ./acr-copy-release.ps1 %__args%
popd
if !ERRORLEVEL! == 0 goto :deploy
echo Copy failed.
goto :done

:deploy
if not "%_deploy%" == "1" goto :done
echo Deploy...
set __args=
set __args=%__args% -acrSubscriptionName %_subscription%
set __args=%__args% -acrRegistryName acr%_resourceGroup%
set __args=%__args% -subscriptionName %_subscription%
set __args=%__args% -ResourceGroupLocation %_location%
set __args=%__args% -ResourceGroupName %_resourceGroup% 
set __args=%__args% -ApplicationName %_resourceGroup%
pushd %build_root%\deploy\scripts
powershell ./deploy.ps1 -type all %__args% 
popd
if !ERRORLEVEL! == 0 goto :done
echo Deploy failed.
goto :done

:done
set __args=
set deploy=
goto :eof
