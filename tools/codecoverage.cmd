@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%
set build_root=%current-path%\..

cd %build_root%

dotnet test "Industrial-IoT.sln" -v n --configuration Release /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:./**/coverage.cobertura.xml -targetdir:./CodeCoverage -reporttypes:Badges;Html;HtmlSummary;Cobertura

