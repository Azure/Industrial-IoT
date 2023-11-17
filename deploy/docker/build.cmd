@echo off
setlocal

set CMDLINE=--self-contained false /t:PublishContainer
set PROJECT=../../src/Azure.IIoT.OpcUa.Publisher.Module/src/Azure.IIoT.OpcUa.Publisher.Module.csproj

dotnet publish %PROJECT% -c Release %CMDLINE% /p:ContainerImageTag=latest
dotnet publish %PROJECT% -c Debug %CMDLINE% /p:ContainerImageTag=debug
